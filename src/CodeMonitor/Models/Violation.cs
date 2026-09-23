using System;

namespace CodeMonitor.Models
{
    public enum ViolationSeverity
    {
        Info,
        Warning,
        Error
    }

    public class Violation
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string TargetFile { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int EndLineNumber { get; set; }
        public int ActualValue { get; set; }
        public int ThresholdValue { get; set; }
        public ViolationSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public string RecommendedFix { get; set; } = string.Empty;
    }
}
