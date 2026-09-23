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
                string title = $"{violation.RuleId}: {violation.RuleName}";
                string message = $"{violation.Description} -> Recommendation: {violation.RecommendedFix}";

                // GitHub workflow command syntax:
                // ::error file={name},line={line},endLine={endLine},title={title}::{message}
                Console.WriteLine($"::{command} file={relativePath},line={violation.LineNumber},endLine={violation.EndLineNumber},title={title}::{EscapeProperty(message)}");
            }

            if (report.IsPassed)
            {
                Console.WriteLine("::notice title=Code Quality Monitor::All analyzed files passed quality gates with zero errors.");
            }
        }

        public void WriteJobSummary(AnalysisReport report, string workingDirectory)
        {
            string? summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (string.IsNullOrWhiteSpace(summaryPath))
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("# 🛡️ Automated Code Quality Report");
            sb.AppendLine();

            if (report.IsPassed)
            {
                sb.AppendLine("### ✅ **Status: Quality Gate Passed**");
                sb.AppendLine("All modified C# files comply with code quality thresholds.");
            }
            else
            {
                sb.AppendLine("### ❌ **Status: Quality Gate Failed**");
                sb.AppendLine($"Found **{report.ErrorCount}** error(s) and **{report.WarningCount}** warning(s).");
                sb.AppendLine("> [!IMPORTANT]");
                sb.AppendLine("> Code quality violations must be addressed before merging this pull request.");
            }

            sb.AppendLine();
            sb.AppendLine("#### 📊 Execution Metadata");
            sb.AppendLine($"- **Repository:** `{report.Repository}`");
            sb.AppendLine($"- **Branch / PR:** `{report.Branch}`");
            sb.AppendLine($"- **Commit SHA:** `{report.CommitSha}`");
            sb.AppendLine($"- **Author:** {report.AuthorName} ({report.AuthorEmail})");
            sb.AppendLine($"- **Analyzed Files:** `{report.AnalyzedFiles.Count}` file(s)");
            sb.AppendLine();

            if (report.Violations.Count > 0)
            {
                sb.AppendLine("#### 🔍 Detected Violations & Action Items");
                sb.AppendLine();
                sb.AppendLine("| Severity | Rule | Location | Target | Metric | Recommendation |");
                sb.AppendLine("| :---: | :--- | :--- | :--- | :---: | :--- |");

                foreach (var v in report.Violations)
                {
                    string badge = v.Severity == ViolationSeverity.Error ? "❌ **Error**" : "⚠️ **Warning**";
                    string relPath = GetRelativePath(v.TargetFile, workingDirectory);
                    string location = $"`{relPath}:{v.LineNumber}`";
                    string metric = $"{v.ActualValue} (Limit: {v.ThresholdValue})";

                    sb.AppendLine($"| {badge} | `{v.RuleId}` {v.RuleName} | {location} | `{v.MemberName}()` | {metric} | {v.RecommendedFix} |");
                }

                sb.AppendLine();
                sb.AppendLine("#### 💡 Detailed Reasoning & Guidance");
                foreach (var v in report.Violations)
                {
                    string relPath = GetRelativePath(v.TargetFile, workingDirectory);
                    sb.AppendLine($"<details><summary><b>[{v.RuleId}] {v.RuleName} in {relPath} (Line {v.LineNumber})</b></summary>");
                    sb.AppendLine();
                    sb.AppendLine($"**Description:** {v.Description}");
                    sb.AppendLine();
                    sb.AppendLine($"**Why this matters:** {v.Rationale}");
                    sb.AppendLine();
                    sb.AppendLine($"**How to fix:** {v.RecommendedFix}");
                    sb.AppendLine();
                    sb.AppendLine("</details>");
                }
            }
            else
            {
                sb.AppendLine("🎉 No code quality violations detected! Great job writing clean, maintainable code.");
            }

            try
            {
                File.AppendAllText(summaryPath, sb.ToString());
                Console.WriteLine($"[GitHubReporter] Step summary successfully appended to {summaryPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitHubReporter] Warning: Could not write step summary: {ex.Message}");
            }
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
