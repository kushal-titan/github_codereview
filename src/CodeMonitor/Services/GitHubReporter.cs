using System;
using System.IO;
using System.Linq;
using System.Text;
using CodeMonitor.Models;

namespace CodeMonitor.Services
{
    public class GitHubReporter
    {
        public void EmitWorkflowAnnotations(AnalysisReport report, string workingDirectory)
        {
            Console.WriteLine("\n======================================================================");
            if (report.ErrorCount == 0 && report.WarningCount == 0)
            {
                Console.WriteLine("🛡️  CODE QUALITY GATE: PASSED ✅ (0 Errors, 0 Warnings)");
                Console.WriteLine("======================================================================");
                Console.WriteLine("All analyzed files satisfy repository quality, safety, and complexity standards.");
                Console.WriteLine("::notice title=Code Quality Monitor::✅ All analyzed C# files passed quality checks with zero errors.");
                return;
            }

            if (report.ErrorCount == 0 && report.WarningCount > 0)
            {
                Console.WriteLine($"🛡️  CODE QUALITY GATE: PASSED WITH ADVISORIES ⚠️ (0 Errors, {report.WarningCount} Warning(s))");
            }
            else
            {
                Console.WriteLine($"🛡️  CODE QUALITY GATE: ACTION REQUIRED (❌ {report.ErrorCount} Error(s), ⚠️ {report.WarningCount} Warning(s))");
            }
            Console.WriteLine("======================================================================");

            // Group violations by file for clean readability
            var fileGroups = report.Violations.GroupBy(v => v.TargetFile);

            foreach (var group in fileGroups)
            {
                string relFile = GetRelativePath(group.Key, workingDirectory);
                Console.WriteLine($"\n📁 File: {relFile}");

                foreach (var v in group)
                {
                    string icon = v.Severity == ViolationSeverity.Error ? "❌" : "⚠️";
                    string level = v.Severity == ViolationSeverity.Error ? "Error" : "Warning";
                    Console.WriteLine($"  {icon} [{level}] Line {v.LineNumber}: {v.RuleId} - {v.RuleName}");
                    Console.WriteLine($"     └─ {v.Description}");
                    Console.WriteLine($"     └─ 💡 Fix: {v.RecommendedFix}");

                    // Emit GitHub workflow command annotation
                    string command = v.Severity == ViolationSeverity.Error ? "error" : "warning";
                    string title = $"{v.RuleId}: {v.RuleName}";
                    string message = $"{v.Description} -> How to fix: {v.RecommendedFix}";

                    Console.WriteLine($"::{command} file={relFile},line={v.LineNumber},endLine={v.EndLineNumber},title={EscapeProperty(title)}::{EscapeData(message)}");
                }
            }
            Console.WriteLine("\n======================================================================");
        }

        public void WriteJobSummary(AnalysisReport report, string workingDirectory)
        {
            string markdown = BuildMarkdownReport(report, workingDirectory, isPrComment: false);

            string? summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (!string.IsNullOrWhiteSpace(summaryPath))
            {
                try
                {
                    File.AppendAllText(summaryPath, markdown);
                    Console.WriteLine($"[GitHubReporter] Step summary written to {summaryPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GitHubReporter] Warning: Could not write step summary: {ex.Message}");
                }
            }

            // Also write pr-comment.md so GitHub Actions can post it as a comment on the PR
            try
            {
                string prCommentPath = Path.Combine(workingDirectory, "pr-comment.md");
                string prCommentMarkdown = BuildMarkdownReport(report, workingDirectory, isPrComment: true);
                File.WriteAllText(prCommentPath, prCommentMarkdown);
                Console.WriteLine($"[GitHubReporter] PR comment written to {prCommentPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHubReporter] Note: Could not write pr-comment.md: {ex.Message}");
            }
        }

        private string BuildMarkdownReport(AnalysisReport report, string workingDirectory, bool isPrComment)
        {
            var sb = new StringBuilder();

            if (report.ErrorCount == 0 && report.WarningCount == 0)
            {
                sb.AppendLine("## ✅ Code Quality Gate: **PASSED**");
                sb.AppendLine();
                sb.AppendLine($"Great work @{report.AuthorName}! All modified C# files meet the repository's code quality and security standards.");
                sb.AppendLine();
                sb.AppendLine("- **Status:** Ready for peer review and merge ✅");
                sb.AppendLine($"- **Files Analyzed:** `{report.AnalyzedFiles.Count}` file(s)");
                sb.AppendLine($"- **Commit:** `{report.CommitSha}`");
                return sb.ToString();
            }

            if (report.ErrorCount == 0 && report.WarningCount > 0)
            {
                sb.AppendLine("## ⚠️ Code Quality Gate: **PASSED WITH ADVISORIES**");
                sb.AppendLine();
                sb.AppendLine($"> [!NOTE]");
                sb.AppendLine($"> Quality gate passed with **{report.WarningCount} advisory warning(s)**. Please review the recommendations below.");
            }
            else
            {
                sb.AppendLine("## 🚨 Code Quality Check: **ACTION REQUIRED**");
                sb.AppendLine();
                sb.AppendLine($"> [!WARNING]");
                sb.AppendLine($"> Found **{report.ErrorCount} error(s)** and **{report.WarningCount} warning(s)** in your changes. Please resolve the critical errors before merging.");
            }

            sb.AppendLine();
            sb.AppendLine("### 📋 Quality Summary Table");
            sb.AppendLine();
            sb.AppendLine("| Severity | Rule | Location | Target | Actual vs Limit | Recommended Remediation |");
            sb.AppendLine("| :---: | :--- | :--- | :--- | :---: | :--- |");

            foreach (var v in report.Violations)
            {
                string badge = v.Severity == ViolationSeverity.Error ? "❌ **Error**" : "⚠️ **Warning**";
                string relPath = GetRelativePath(v.TargetFile, workingDirectory);
                string location = $"`{relPath}:{v.LineNumber}`";
                string metric = v.ThresholdValue > 0 ? $"**{v.ActualValue}** (Max: {v.ThresholdValue})" : "Advisory";

                sb.AppendLine($"| {badge} | `{v.RuleId}` {v.RuleName} | {location} | `{v.MemberName}` | {metric} | {v.RecommendedFix} |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("*🤖 Automated analysis performed by Microsoft Roslyn in GitHub Actions.*");

            return sb.ToString();
        }

        private static string GetRelativePath(string fullPath, string basePath)
        {
            try
            {
                Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? basePath : basePath + Path.DirectorySeparatorChar);
                Uri fileUri = new Uri(fullPath);
                return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString().Replace('/', Path.DirectorySeparatorChar));
            }
            catch
            {
                return fullPath;
            }
        }

        private static string EscapeProperty(string value)
        {
            return value.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A").Replace(":", "%3A").Replace(",", "%2C");
        }

        private static string EscapeData(string value)
        {
            return value.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A");
        }
    }
}
