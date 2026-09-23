using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CodeMonitor.Analyzers;
using CodeMonitor.Models;
using CodeMonitor.Services;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeMonitor
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("🛡️  Automated Code Quality Monitor (.NET + Roslyn)");
            Console.WriteLine("==================================================");

            // Parse CLI options
            string targetDir = GetArgValue(args, "--path", "-p") ?? Directory.GetCurrentDirectory();
            string? baseRef = GetArgValue(args, "--base-ref", "-b") ?? Environment.GetEnvironmentVariable("BASE_REF");
            string prNumber = GetArgValue(args, "--pr-number") ?? Environment.GetEnvironmentVariable("PR_NUMBER") ?? "";
            string prUrl = GetArgValue(args, "--pr-url") ?? Environment.GetEnvironmentVariable("PR_URL") ?? "";
            string prAuthorEmail = GetArgValue(args, "--pr-author") ?? Environment.GetEnvironmentVariable("PR_AUTHOR_EMAIL") ?? "";
            bool dryRun = args.Contains("--dry-run") || bool.TryParse(Environment.GetEnvironmentVariable("DRY_RUN"), out var dr) && dr;

            var config = new QualityConfig();
            var gitDiffService = new GitDiffService();
            var githubReporter = new GitHubReporter();
            var emailService = new EmailService();

            var analyzers = new List<ICodeAnalyzer>
            {
                new MethodLengthAnalyzer(),
                new ComplexityAnalyzer(),
                new ParameterCountAnalyzer(),
                new NestingDepthAnalyzer()
            };

            // Retrieve Git and environment metadata
            var report = InitializeReport(targetDir, baseRef, prNumber, prUrl, prAuthorEmail);

            Console.WriteLine($"Repository: {report.Repository}");
            Console.WriteLine($"Branch/Ref: {report.Branch}");
            Console.WriteLine($"Commit SHA: {report.CommitSha}");
            Console.WriteLine($"Base Ref for Diff: {baseRef ?? "None (Full Scan Mode)"}");

            // 1. Get changed files and lines via git diff
            var changedFilesMap = gitDiffService.GetChangedFilesAndLines(targetDir, baseRef);
            List<string> targetFiles;

            if (changedFilesMap.Count > 0)
            {
                targetFiles = changedFilesMap.Keys.Where(File.Exists).ToList();
                Console.WriteLine($"\n[Git Diff] Detected {targetFiles.Count} modified C# file(s).");
            }
            else
            {
                Console.WriteLine("\n[Scan Mode] Analyzing all .cs files in directory tree...");
                targetFiles = Directory.GetFiles(targetDir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                                !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                                !f.Contains($"{Path.DirectorySeparatorChar}.vs{Path.DirectorySeparatorChar}"))
                    .ToList();
                Console.WriteLine($"Found {targetFiles.Count} C# file(s) for analysis.");
            }

            report.AnalyzedFiles = targetFiles;

            // 2. Perform Roslyn AST Analysis
            foreach (var filePath in targetFiles)
            {
                try
                {
                    string sourceCode = File.ReadAllText(filePath);
                    var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: filePath);

                    changedFilesMap.TryGetValue(filePath, out var changedLines);

                    foreach (var analyzer in analyzers)
                    {
                        var violations = analyzer.Analyze(syntaxTree, filePath, config, changedLines);
                        report.Violations.AddRange(violations);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Analyzer] Error reading/parsing {filePath}: {ex.Message}");
                }
            }

            // 3. Output GitHub workflow annotations & Step Summary
            Console.WriteLine("\n--------------------------------------------------");
            Console.WriteLine($"Analysis Finished: {report.ErrorCount} Error(s), {report.WarningCount} Warning(s)");
            Console.WriteLine("--------------------------------------------------");

            githubReporter.EmitWorkflowAnnotations(report, targetDir);
            githubReporter.WriteJobSummary(report, targetDir);

            // 4. Dispatch Email Report via Outlook SMTP directly to PR Author
            if (!dryRun)
            {
                string? senderEmail = Environment.GetEnvironmentVariable("OUTLOOK_SENDER_EMAIL");
                string? appPassword = Environment.GetEnvironmentVariable("OUTLOOK_APP_PASSWORD");
                // Priority: (1) PR Author Email -> (2) Explicit Recipient override -> (3) Sender Bot
                string? recipientEmail = !string.IsNullOrWhiteSpace(report.AuthorEmail)
                    ? report.AuthorEmail
                    : Environment.GetEnvironmentVariable("OUTLOOK_RECIPIENT_EMAIL");
                // Collect all configured additional recipients (Lead, Manager, Team)
                var additionalList = new List<string>();
                void CollectEmails(string? raw)
                {
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        var tokens = raw.Split(new[] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var token in tokens)
                        {
                            var clean = token.Trim();
                            if (clean.Contains("@") && !additionalList.Contains(clean, StringComparer.OrdinalIgnoreCase))
                            {
                                additionalList.Add(clean);
                            }
                        }
                    }
                }

                CollectEmails(GetArgValue(args, "--additional-recipients", "--cc"));
                CollectEmails(Environment.GetEnvironmentVariable("OUTLOOK_ADDITIONAL_RECIPIENTS"));
                CollectEmails(Environment.GetEnvironmentVariable("OUTLOOK_LEAD_EMAIL"));
                CollectEmails(Environment.GetEnvironmentVariable("OUTLOOK_MANAGER_EMAIL"));
                CollectEmails(Environment.GetEnvironmentVariable("OUTLOOK_CC_EMAILS"));

                string? additionalRecipients = additionalList.Count > 0 ? string.Join(", ", additionalList) : null;
                string smtpServer = Environment.GetEnvironmentVariable("OUTLOOK_SMTP_SERVER") ?? "smtp.office365.com";
                int.TryParse(Environment.GetEnvironmentVariable("OUTLOOK_SMTP_PORT") ?? "587", out int smtpPort);

                emailService.SendReport(report, senderEmail, appPassword, recipientEmail, additionalRecipients, smtpServer, smtpPort);
            }

            // 5. Exit code determines if CI workflow passes or fails (blocking PR)
            if (report.HasFailures && config.FailOnErrors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ [Quality Gate Failed] Critical code quality violations detected. Blocking merge.");
                Console.ResetColor();
                return 1;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✅ [Quality Gate Passed] Code meets all quality criteria.");
            Console.ResetColor();
            return 0;
        }

        private static AnalysisReport InitializeReport(string targetDir, string? baseRef, string prNumber, string prUrl, string prAuthorEmail)
        {
            string resolvedAuthorEmail = prAuthorEmail;
            if (string.IsNullOrWhiteSpace(resolvedAuthorEmail))
            {
                resolvedAuthorEmail = GetGitAuthorEmail(targetDir);
            }
            if (string.IsNullOrWhiteSpace(resolvedAuthorEmail))
            {
                resolvedAuthorEmail = GetGitCommitterEmail(targetDir);
            }

            var report = new AnalysisReport
            {
                Repository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY") ?? Path.GetFileName(targetDir),
                Branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ?? GetGitBranch(targetDir),
                CommitSha = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? GetGitCommitSha(targetDir),
                AuthorName = Environment.GetEnvironmentVariable("GITHUB_ACTOR") ?? GetGitAuthorName(targetDir),
                AuthorEmail = resolvedAuthorEmail,
                PullRequestNumber = prNumber,
                PullRequestUrl = prUrl
            };

            return report;
        }

        private static string? GetArgValue(string[] args, params string[] paramNames)
        {
            for (int i = 0; i < args.Length; i++)
            {
                foreach (var param in paramNames)
                {
                    if (args[i].Equals(param, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    {
                        string val = args[i + 1];
                        return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
                    }
                    if (args[i].StartsWith($"{param}=", StringComparison.OrdinalIgnoreCase))
                    {
                        string val = args[i].Substring(param.Length + 1);
                        return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
                    }
                }
            }
            return null;
        }

        private static string GetGitBranch(string dir) => RunGitCommand(dir, "rev-parse --abbrev-ref HEAD") ?? "unknown";
        private static string GetGitCommitSha(string dir) => RunGitCommand(dir, "rev-parse --short HEAD") ?? "local";
        private static string GetGitAuthorName(string dir) => RunGitCommand(dir, "log -1 --format=%an") ?? Environment.UserName;
        private static string GetGitAuthorEmail(string dir) => RunGitCommand(dir, "log -1 --format=%ae") ?? "";
        private static string GetGitCommitterEmail(string dir) => RunGitCommand(dir, "log -1 --format=%ce") ?? "";

        private static string? RunGitCommand(string dir, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = args,
                    WorkingDirectory = dir,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p == null) return null;
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return p.ExitCode == 0 ? output : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
