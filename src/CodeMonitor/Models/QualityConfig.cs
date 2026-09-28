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
        public bool SaveErrorsToMarkdown { get; set; } = true;
        public string ErrorsDirectory { get; set; } = "issues";
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
                string[] subfolders = new[] { "", "github_review", ".github" };

                foreach (var dir in candidateDirs)
                {
                    foreach (var sub in subfolders)
                    {
                        foreach (var name in configNames)
                        {
                            string candidatePath = string.IsNullOrEmpty(sub)
                                ? Path.Combine(dir, name)
                                : Path.Combine(dir, sub, name);

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
                }

                // Fallback: search directory tree recursively if not found in standard candidate locations
                foreach (var searchRoot in candidateDirs)
                {
                    if (Directory.Exists(searchRoot))
                    {
                        try
                        {
                            var foundFile = Directory.GetFiles(searchRoot, "code-quality.config.json", SearchOption.AllDirectories)
                                .FirstOrDefault(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                                                     !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                                                     !f.Contains($"{Path.DirectorySeparatorChar}.vs{Path.DirectorySeparatorChar}"));

                            if (foundFile != null && File.Exists(foundFile))
                            {
                                string json = File.ReadAllText(foundFile);
                                var options = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true,
                                    AllowTrailingCommas = true,
                                    ReadCommentHandling = JsonCommentHandling.Skip
                                };
                                var loaded = JsonSerializer.Deserialize<QualityConfig>(json, options);
                                if (loaded != null)
                                {
                                    Console.WriteLine($"[Config] ✅ Successfully loaded custom quality settings from (recursive search): {foundFile}");
                                    return loaded;
                                }
                            }
                        }
                        catch
                        {
                            // ignore directory traversal permission exceptions
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
