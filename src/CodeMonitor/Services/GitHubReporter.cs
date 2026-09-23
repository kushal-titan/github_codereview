using System;
using System.IO;
using System.Text;
using CodeMonitor.Models;

namespace CodeMonitor.Services
{
    public class GitHubReporter
    {
        public void EmitWorkflowAnnotations(AnalysisReport report, string workingDirectory)
        {
            foreach (var violation in report.Violations)
            {
                string relativePath = GetRelativePath(violation.TargetFile, workingDirectory);
                string command = violation.Severity == ViolationSeverity.Error ? "error" : "warning";
                string title = $"{violation.RuleId}: {violation.RuleName} ({violation.MemberName})";
                string message = $"{violation.Description} -> How to fix: {violation.RecommendedFix}";

                Console.WriteLine($"::{command} file={relativePath},line={violation.LineNumber},endLine={violation.EndLineNumber},title={title}::{EscapeProperty(message)}");
            }

            if (report.IsPassed)
            {
                Console.WriteLine("::notice title=Code Quality Monitor::✅ All modified C# files passed quality checks with zero errors.");
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

            if (report.IsPassed)
            {
                sb.AppendLine("## ✅ Code Quality Gate: **PASSED**");
                sb.AppendLine();
                sb.AppendLine($"Great work @{report.AuthorName}! All modified C# files meet the repository's code quality standards.");
                sb.AppendLine();
                sb.AppendLine("- **Status:** Ready for human review and merge");
                sb.AppendLine($"- **Files Analyzed:** `{report.AnalyzedFiles.Count}` file(s)");
                sb.AppendLine($"- **Commit:** `{report.CommitSha}`");
                return sb.ToString();
            }

            sb.AppendLine("## 🚨 Code Quality Check: **ACTION REQUIRED**");
            sb.AppendLine();
            sb.AppendLine($"> [!WARNING]");
            sb.AppendLine($"> Found **{report.ErrorCount} error(s)** and **{report.WarningCount} warning(s)** in your changes. Please review the locations and recommended fixes below before merging.");
            sb.AppendLine();

            sb.AppendLine("### 📋 Quick Summary Table");
            sb.AppendLine();
            sb.AppendLine("| Severity | Rule | Location | Target | Actual vs Limit | Quick Fix |");
            sb.AppendLine("| :---: | :--- | :--- | :--- | :---: | :--- |");

            foreach (var v in report.Violations)
            {
                string badge = v.Severity == ViolationSeverity.Error ? "❌ **Error**" : "⚠️ **Warning**";
                string relPath = GetRelativePath(v.TargetFile, workingDirectory);
                string location = $"`{relPath}:{v.LineNumber}`";
                string metric = $"**{v.ActualValue}** (Max: {v.ThresholdValue})";

                sb.AppendLine($"| {badge} | `{v.RuleId}` {v.RuleName} | {location} | `{v.MemberName}()` | {metric} | {v.RecommendedFix} |");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("### 🔍 Step-by-Step Breakdown & Refactoring Examples");
            sb.AppendLine();

            int index = 1;
            foreach (var v in report.Violations)
            {
                string relPath = GetRelativePath(v.TargetFile, workingDirectory);
                string badge = v.Severity == ViolationSeverity.Error ? "❌ ERROR" : "⚠️ WARNING";

                sb.AppendLine($"#### {index++}. [{badge}] `{v.RuleId}: {v.RuleName}` in `{v.MemberName}()`");
                sb.AppendLine();
                sb.AppendLine($"- **📍 WHERE:** File [`{relPath}`](file:///{v.TargetFile}) at **Line {v.LineNumber} to {v.EndLineNumber}**");
                sb.AppendLine($"- **⚠️ WHAT:** {v.Description}");
                sb.AppendLine($"- **💡 WHY:** {v.Rationale}");
                sb.AppendLine();
                sb.AppendLine("**🛠️ WHAT TO DO:**");
                foreach (var step in v.ActionSteps)
                {
                    sb.AppendLine($"  1. {step}");
                }

                if (!string.IsNullOrWhiteSpace(v.CodeExample))
                {
                    sb.AppendLine();
                    sb.AppendLine("<details><summary><b>👉 Click to view Refactoring Example (Before / After)</b></summary>");
                    sb.AppendLine();
                    sb.AppendLine("```csharp");
                    sb.AppendLine(v.CodeExample);
                    sb.AppendLine("```");
                    sb.AppendLine();
                    sb.AppendLine("</details>");
                }

                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine("*🤖 Generated automatically by Roslyn Code Quality Monitor in GitHub Actions.*");

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
    }
}
