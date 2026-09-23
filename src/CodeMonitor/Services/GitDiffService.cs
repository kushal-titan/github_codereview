using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeMonitor.Services
{
    public class GitDiffService
    {
        public Dictionary<string, HashSet<int>> GetChangedFilesAndLines(string workingDirectory, string? baseRef = null)
        {
            var result = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string diffArguments;
                if (!string.IsNullOrWhiteSpace(baseRef))
                {
                    diffArguments = $"diff --unified=0 {baseRef}...HEAD";
                }
                else
                {
                    // Fallback to diffing against HEAD~1 or staged/working tree
                    diffArguments = "diff --unified=0 HEAD~1 HEAD";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = diffArguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return result;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                {
                    // Fallback to unstaged/staged diff if previous commit diff failed
                    psi.Arguments = "diff --unified=0 HEAD";
                    using var fallbackProcess = Process.Start(psi);
                    if (fallbackProcess != null)
                    {
                        output = fallbackProcess.StandardOutput.ReadToEnd();
                        fallbackProcess.WaitForExit();
                    }
                }

                ParseUnifiedDiff(output, workingDirectory, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GitDiffService] Note: Git diff retrieval encountered: {ex.Message}. Falling back to full file scanning.");
            }

            return result;
        }

        private static void ParseUnifiedDiff(string diffOutput, string workingDirectory, Dictionary<string, HashSet<int>> result)
        {
            using var reader = new StringReader(diffOutput);
            string? line;
            string? currentFile = null;

            // Regex for hunk headers: @@ -from,len +to,len @@ or @@ -from +to @@
            var hunkRegex = new Regex(@"^@@\s+-(?:\d+)(?:,\d+)?\s+\+(\d+)(?:,(\d+))?\s+@@", RegexOptions.Compiled);

            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("+++ b/"))
                {
                    string relPath = line.Substring(6).Trim();
                    if (relPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        currentFile = Path.Combine(workingDirectory, relPath.Replace('/', Path.DirectorySeparatorChar));
                        if (!result.ContainsKey(currentFile))
                        {
                            result[currentFile] = new HashSet<int>();
                        }
                    }
                    else
                    {
                        currentFile = null;
                    }
                }
                else if (currentFile != null && line.StartsWith("@@"))
                {
                    var match = hunkRegex.Match(line);
                    if (match.Success)
                    {
                        int startLine = int.Parse(match.Groups[1].Value);
                        int count = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;

                        if (count == 0)
                        {
                            // Deletion only at this line
                            count = 1;
                        }

                        for (int i = 0; i < count; i++)
                        {
                            result[currentFile].Add(startLine + i);
                        }
                    }
                }
            }
        }
    }
}
