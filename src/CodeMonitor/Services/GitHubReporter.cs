using System;
using System.IO;
using System.Linq;
using System.Text;
using CodeMonitor.Models;

namespace CodeMonitor.Services
{
    public class GitHubReporter
    {
        public void EmitWorkflowAnnotations(AnalysisReport report, string workingDirectory, QualityConfig? config = null, string? errorReportRelPath = null)
        {
            config ??= new QualityConfig();
            Console.WriteLine("\n======================================================================");

            if (report.ErrorCount == 0 && report.WarningCount == 0)
            {
                Console.WriteLine("🛡️  CODE QUALITY GATE: PASSED ✅ (0 Errors, 0 Warnings)");
                Console.WriteLine("======================================================================");
                Console.WriteLine("All analyzed files satisfy repository quality, safety, and complexity standards.");
                Console.WriteLine("::notice title=Code Quality Monitor::✅ All analyzed C# files passed quality checks with zero errors and zero warnings.");
                return;
            }

            bool showcaseOnlyErrors = config.ShowcaseOnlyErrorsInConsole;

            if (report.ErrorCount == 0 && report.WarningCount > 0)
            {
                Console.WriteLine($"🛡️  CODE QUALITY GATE: PASSED WITH ADVISORIES ⚠️ (0 Errors, {report.WarningCount} Warning(s))");
                Console.WriteLine("======================================================================");
                Console.WriteLine("No blocking errors detected in changed files. Merge is allowed.");
            }
            else
            {
                Console.WriteLine($"🛡️  CODE QUALITY GATE: ACTION REQUIRED (❌ {report.ErrorCount} Error(s), ⚠️ {report.WarningCount} Warning(s))");
                Console.WriteLine("======================================================================");
                if (!string.IsNullOrWhiteSpace(errorReportRelPath))
                {
                    Console.WriteLine($"\n🚨 Critical Blocking Errors ({report.ErrorCount}) have been recorded for GitHub Issues:");
                    Console.WriteLine($"   📄 {errorReportRelPath}");
                    Console.WriteLine($"   📌 Stored under the '{config.ErrorsDirectory}' folder.");
                }
            }

            // Determine which violations to print to the console
            var violationsToDisplay = showcaseOnlyErrors
                ? report.Violations.Where(v => v.Severity == ViolationSeverity.Error).ToList()
                : report.Violations;

            if (violationsToDisplay.Count > 0)
            {
                var fileGroups = violationsToDisplay.GroupBy(v => v.TargetFile);

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
            }

            if (showcaseOnlyErrors && report.ErrorCount > 0 && report.WarningCount > 0)
            {
                Console.WriteLine("\n--------------------------------------------------");
                Console.WriteLine($"ℹ️  Notice: {report.WarningCount} advisory warning(s) were also identified.");
                Console.WriteLine("--------------------------------------------------");
            }

            Console.WriteLine("\n======================================================================");
        }

        public static DateTime GetIndianStandardTime(DateTime utcTime)
        {
            try
            {
                string tzId = OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata";
                var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
            }
            catch
            {
                return utcTime.AddHours(5).AddMinutes(30);
            }
        }

        public string? WriteErrorsMarkdownReport(AnalysisReport report, string workingDirectory, QualityConfig config)
        {
            if (!config.SaveErrorsToMarkdown)
            {
                return null;
            }

            try
            {
                string targetDirName = !string.IsNullOrWhiteSpace(config.ErrorsDirectory) ? config.ErrorsDirectory : "issues";
                string issuesDir = Path.Combine(workingDirectory, targetDirName);
                if (!Directory.Exists(issuesDir))
                {
                    Directory.CreateDirectory(issuesDir);
                }

                DateTime istTime = GetIndianStandardTime(report.AnalysisTime);
                string timestamp = istTime.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = $"errors_{timestamp}_IST.md";
                string fullFilePath = Path.Combine(issuesDir, fileName);
                string latestFilePath = Path.Combine(issuesDir, "latest_errors.md");

                var sb = new StringBuilder();
                if (report.ErrorCount == 0)
                {
                    sb.AppendLine("# 🛡️ Code Quality Critical Errors Report");
                    sb.AppendLine();
                    sb.AppendLine($"> **Status:** ✅ **0 Critical Blocking Errors Found**  ");
                    sb.AppendLine($"> **Generated:** `{istTime:yyyy-MM-dd hh:mm:ss tt} IST` (`{report.AnalysisTime:HH:mm:ss} UTC`)  ");
                    sb.AppendLine($"> **Repository:** `{report.Repository}`  ");
                    sb.AppendLine($"> **Branch:** `{report.Branch}` | **Commit:** `{report.CommitSha}`  ");
                    sb.AppendLine($"> **Author:** `{report.AuthorName}` `{(!string.IsNullOrWhiteSpace(report.AuthorEmail) ? $"<{report.AuthorEmail}>" : "")}`  ");
                    if (!string.IsNullOrWhiteSpace(report.PullRequestNumber))
                    {
                        sb.AppendLine($"> **Pull Request:** [#{report.PullRequestNumber}]({report.PullRequestUrl})  ");
                    }
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                    sb.AppendLine("### ✅ All Checks Passed With Zero Blocking Errors");
                    sb.AppendLine();
                    sb.AppendLine("No runtime safety traps, concurrency deadlocks, or critical vulnerabilities were found in the analyzed changes.");
                }
                else
                {
                    sb.AppendLine("# ❌ Code Quality Critical Blocking Errors Report");
                    sb.AppendLine();
                    sb.AppendLine($"> **Status:** 🚨 **Action Required &mdash; {report.ErrorCount} Blocking Error(s)**  ");
                    sb.AppendLine($"> **Generated:** `{istTime:yyyy-MM-dd hh:mm:ss tt} IST` (`{report.AnalysisTime:HH:mm:ss} UTC`)  ");
                    sb.AppendLine($"> **Repository:** `{report.Repository}`  ");
                    sb.AppendLine($"> **Branch:** `{report.Branch}` | **Commit:** `{report.CommitSha}`  ");
                    sb.AppendLine($"> **Author:** `{report.AuthorName}` `{(!string.IsNullOrWhiteSpace(report.AuthorEmail) ? $"<{report.AuthorEmail}>" : "")}`  ");
                    if (!string.IsNullOrWhiteSpace(report.PullRequestNumber))
                    {
                        sb.AppendLine($"> **Pull Request:** [#{report.PullRequestNumber}]({report.PullRequestUrl})  ");
                    }
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                    sb.AppendLine($"## 🚨 Summary of Critical Blocking Errors ({report.ErrorCount})");
                    sb.AppendLine();
                    sb.AppendLine("| Severity | Rule ID | Category | Location | Target | Actual vs Limit | Recommended Remediation |");
                    sb.AppendLine("| :---: | :--- | :--- | :--- | :--- | :---: | :--- |");

                    var errors = report.Violations.Where(v => v.Severity == ViolationSeverity.Error).ToList();
                    foreach (var v in errors)
                    {
                        string relPath = GetRelativePath(v.TargetFile, workingDirectory);
                        string location = $"`{relPath}:{v.LineNumber}`";
                        string metric = v.ThresholdValue > 0 ? $"**{v.ActualValue}** (Max: {v.ThresholdValue})" : "Critical Error";
                        string category = !string.IsNullOrWhiteSpace(v.Category) ? v.Category : GetCategoryFromRuleId(v.RuleId);

                        sb.AppendLine($"| ❌ **Error** | `{v.RuleId}` {v.RuleName} | {category} | {location} | `{v.MemberName}` | {metric} | {v.RecommendedFix} |");
                    }

                    // Remediation blueprints for errors
                    var detailedErrors = errors.Where(v => v.ActionSteps.Count > 0 || !string.IsNullOrWhiteSpace(v.CodeExample)).ToList();
                    if (detailedErrors.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();
                        sb.AppendLine("## 🛠️ Detailed Remediation & Refactoring Blueprints");
                        sb.AppendLine();

                        foreach (var v in detailedErrors)
                        {
                            string relPath = GetRelativePath(v.TargetFile, workingDirectory);
                            sb.AppendLine($"<details>");
                            sb.AppendLine($"<summary><b>❌ [{v.RuleId}] {v.RuleName} &mdash; <code>{relPath}:{v.LineNumber}</code></b></summary>");
                            sb.AppendLine();
                            sb.AppendLine($"- **Problem:** {v.Description}");
                            if (!string.IsNullOrWhiteSpace(v.Rationale))
                            {
                                sb.AppendLine($"- **Why it matters:** {v.Rationale}");
                            }
                            if (v.ActionSteps.Count > 0)
                            {
                                sb.AppendLine("- **Action Steps:**");
                                foreach (var step in v.ActionSteps)
                                {
                                    sb.AppendLine($"  1. {step}");
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(v.CodeExample))
                            {
                                sb.AppendLine();
                                sb.AppendLine("```csharp");
                                sb.AppendLine(v.CodeExample);
                                sb.AppendLine("```");
                            }
                            sb.AppendLine("</details>");
                            sb.AppendLine();
                        }
                    }
                }

                sb.AppendLine("---");
                sb.AppendLine("*🤖 Generated automatically by Code Quality Monitor (Microsoft Roslyn AST Engine).*");

                string markdownContent = sb.ToString();
                File.WriteAllText(fullFilePath, markdownContent);
                File.WriteAllText(latestFilePath, markdownContent);

                string relativePath = Path.Combine(targetDirName, fileName).Replace('\\', '/');
                Console.WriteLine($"[GitHubReporter] 📝 Timestamped error report saved to: {relativePath}");
                return relativePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHubReporter] ⚠️ Error writing error markdown report: {ex.Message}");
                return null;
            }
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
                sb.AppendLine($"- **Analysis Time:** `{GetIndianStandardTime(report.AnalysisTime):yyyy-MM-dd hh:mm:ss tt} IST`");
                return sb.ToString();
            }

            DateTime summaryIstTime = GetIndianStandardTime(report.AnalysisTime);
            if (report.ErrorCount == 0 && report.WarningCount > 0)
            {
                sb.AppendLine("## ⚠️ Code Quality Gate: **PASSED WITH ADVISORIES**");
                sb.AppendLine();
                sb.AppendLine($"> [!NOTE]");
                sb.AppendLine($"> Quality gate passed with **{report.WarningCount} advisory warning(s)**. Detailed warning blueprints have been archived under `issues/`.");
                sb.AppendLine($"> **Analysis Time:** `{summaryIstTime:yyyy-MM-dd hh:mm:ss tt} IST`");
            }
            else
            {
                sb.AppendLine("## 🚨 Code Quality Check: **ACTION REQUIRED**");
                sb.AppendLine();
                sb.AppendLine($"> [!WARNING]");
                sb.AppendLine($"> Found **{report.ErrorCount} error(s)** and **{report.WarningCount} warning(s)** in your changes. Please resolve the critical errors before merging.");
                sb.AppendLine($"> **Analysis Time:** `{summaryIstTime:yyyy-MM-dd hh:mm:ss tt} IST`");
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

            // Expandable Remediation Blueprints Section
            var detailedViolations = report.Violations.Where(v => v.ActionSteps.Count > 0 || !string.IsNullOrWhiteSpace(v.CodeExample)).ToList();
            if (detailedViolations.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### 🛠️ Remediation & Refactoring Blueprints");
                sb.AppendLine();

                foreach (var v in detailedViolations)
                {
                    string icon = v.Severity == ViolationSeverity.Error ? "❌" : "⚠️";
                    string relPath = GetRelativePath(v.TargetFile, workingDirectory);

                    sb.AppendLine($"<details>");
                    sb.AppendLine($"<summary><b>{icon} [{v.RuleId}] {v.RuleName} &mdash; <code>{relPath}:{v.LineNumber}</code></b></summary>");
                    sb.AppendLine();
                    sb.AppendLine($"- **Problem:** {v.Description}");
                    if (!string.IsNullOrWhiteSpace(v.Rationale))
                    {
                        sb.AppendLine($"- **Why it matters:** {v.Rationale}");
                    }
                    if (v.ActionSteps.Count > 0)
                    {
                        sb.AppendLine("- **Action Steps:**");
                        foreach (var step in v.ActionSteps)
                        {
                            sb.AppendLine($"  1. {step}");
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(v.CodeExample))
                    {
                        sb.AppendLine();
                        sb.AppendLine("```csharp");
                        sb.AppendLine(v.CodeExample);
                        sb.AppendLine("```");
                    }
                    sb.AppendLine("</details>");
                    sb.AppendLine();
                }
            }

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

        private static string GetCategoryFromRuleId(string ruleId) => ruleId switch
        {
            var id when id.StartsWith("CQ") => "Code Quality",
            var id when id.StartsWith("SAF") => "Runtime Safety",
            var id when id.StartsWith("CON") => "Concurrency",
            var id when id.StartsWith("SEC") => "Security",
            var id when id.StartsWith("PERF") => "Performance",
            var id when id.StartsWith("ARCH") => "Architecture",
            _ => "General"
        };
    }
}
