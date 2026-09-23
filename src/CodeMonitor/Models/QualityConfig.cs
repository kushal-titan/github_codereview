namespace CodeMonitor.Models
{
    public class QualityConfig
    {
        public int MaxMethodLines { get; set; } = 50;
        public int MaxCyclomaticComplexity { get; set; } = 10;
        public int MaxParameterCount { get; set; } = 4;
        public int MaxNestingDepth { get; set; } = 3;
        public bool FailOnErrors { get; set; } = true;
        public bool SendEmailNotification { get; set; } = true;
        public string EmailSubjectPrefix { get; set; } = "[Code Quality Monitor]";
    }
}
