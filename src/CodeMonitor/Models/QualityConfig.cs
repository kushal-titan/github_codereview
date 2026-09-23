using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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
        public List<string> AdditionalRecipients { get; set; } = new List<string>();

        public static QualityConfig Load(string targetDir)
        {
            try
            {
                string[] possibleConfigPaths = new[]
                {
                    Path.Combine(targetDir, "code-quality.config.json"),
                    Path.Combine(targetDir, "codemonitor.json"),
                    Path.Combine(targetDir, ".codemonitor.json")
                };

                foreach (var path in possibleConfigPaths)
                {
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var loaded = JsonSerializer.Deserialize<QualityConfig>(json, options);
                        if (loaded != null)
                        {
                            Console.WriteLine($"[Config] Loaded custom quality settings and recipients from {Path.GetFileName(path)}");
                            return loaded;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Note: Using default config ({ex.Message})");
            }

            return new QualityConfig();
        }
    }
}
