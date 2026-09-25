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
        public bool ShowcaseOnlyErrorsInConsole { get; set; } = true;
        public bool SaveWarningsToMarkdown { get; set; } = true;
        public string WarningsDirectory { get; set; } = "issues";
        public bool SendEmailNotification { get; set; } = true;
        public string EmailSubjectPrefix { get; set; } = "[Code Quality Monitor]";
        public List<string> AdditionalRecipients { get; set; } = new List<string>();

        public static QualityConfig Load(string targetDir)
        {
            try
            {
                var candidateDirs = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(targetDir)) candidateDirs.Add(targetDir);
                
                string currentDir = Directory.GetCurrentDirectory();
                if (!candidateDirs.Contains(currentDir, StringComparer.OrdinalIgnoreCase)) candidateDirs.Add(currentDir);
                
                string? ghWorkspace = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
                if (!string.IsNullOrWhiteSpace(ghWorkspace) && !candidateDirs.Contains(ghWorkspace, StringComparer.OrdinalIgnoreCase))
                {
                    candidateDirs.Add(ghWorkspace);
                }

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrWhiteSpace(baseDir) && !candidateDirs.Contains(baseDir, StringComparer.OrdinalIgnoreCase))
                {
                    candidateDirs.Add(baseDir);
                }

                // Add parent directories up to 4 levels
                int initialCount = candidateDirs.Count;
                for (int i = 0; i < initialCount; i++)
                {
                    var dir = new DirectoryInfo(candidateDirs[i]);
                    for (int depth = 0; depth < 4 && dir?.Parent != null; depth++)
                    {
                        dir = dir.Parent;
                        if (!candidateDirs.Contains(dir.FullName, StringComparer.OrdinalIgnoreCase))
                        {
                            candidateDirs.Add(dir.FullName);
                        }
                    }
                }

                string[] configNames = new[] { "code-quality.config.json", "codemonitor.json", ".codemonitor.json" };

                foreach (var dir in candidateDirs)
                {
                    foreach (var name in configNames)
                    {
                        string candidatePath = Path.Combine(dir, name);
                        if (File.Exists(candidatePath))
                        {
                            string json = File.ReadAllText(candidatePath);
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                AllowTrailingCommas = true,
                                ReadCommentHandling = JsonCommentHandling.Skip
                            };
                            var loaded = JsonSerializer.Deserialize<QualityConfig>(json, options);
                            if (loaded != null)
                            {
                                Console.WriteLine($"[Config] ✅ Successfully loaded custom quality settings from: {candidatePath}");
                                if (loaded.AdditionalRecipients != null && loaded.AdditionalRecipients.Count > 0)
                                {
                                    Console.WriteLine($"[Config] 👥 Configured Additional Recipients: {string.Join(", ", loaded.AdditionalRecipients)}");
                                }
                                return loaded;
                            }
                        }
                    }
                }

                Console.WriteLine("[Config] ℹ️ No custom code-quality.config.json found in candidate paths. Using default configuration.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] ⚠️ Error loading config: {ex.Message}. Using default settings.");
            }

            return new QualityConfig();
        }
    }
}
