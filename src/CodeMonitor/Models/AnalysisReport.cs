using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeMonitor.Models
{
    public class AnalysisReport
    {
        public string Repository { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string CommitSha { get; set; } = string.Empty;
        public string CommitMessage { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorEmail { get; set; } = string.Empty;
        public string PullRequestNumber { get; set; } = string.Empty;
        public string PullRequestUrl { get; set; } = string.Empty;
        public List<string> Reviewers { get; set; } = new List<string>();
        public List<string> ReviewerEmails { get; set; } = new List<string>();
        public DateTime AnalysisTime { get; set; } = DateTime.UtcNow;

        public List<string> AnalyzedFiles { get; set; } = new List<string>();
        public List<Violation> Violations { get; set; } = new List<Violation>();

        public int ErrorCount => Violations.Count(v => v.Severity == ViolationSeverity.Error);
        public int WarningCount => Violations.Count(v => v.Severity == ViolationSeverity.Warning);
        public int InfoCount => Violations.Count(v => v.Severity == ViolationSeverity.Info);
        public bool HasFailures => ErrorCount > 0;
        public bool IsPassed => !HasFailures;
    }
}
