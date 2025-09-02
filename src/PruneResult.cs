using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace LudusaviRestic
{
    public class PruneResult
    {
        public bool Success { get; set; }
        public int SnapshotsDeleted { get; set; }
        public int GamesAffected { get; set; }
        public List<DeletedSnapshot> DeletedSnapshots { get; set; } = new List<DeletedSnapshot>();
        public string DataDeleted { get; set; }
        public string RawOutput { get; set; }
        public bool IsDryRun { get; set; }

        public Dictionary<string, int> GetGameDeletionCounts()
        {
            return DeletedSnapshots
                .Where(s => !string.IsNullOrEmpty(s.GameName))
                .GroupBy(s => s.GameName)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public Dictionary<string, int> GetTagDeletionCounts()
        {
            var tagCounts = new Dictionary<string, int>();
            foreach (var snapshot in DeletedSnapshots)
            {
                foreach (var tag in snapshot.Tags.Where(t => !string.IsNullOrEmpty(t)))
                {
                    if (tagCounts.ContainsKey(tag))
                        tagCounts[tag]++;
                    else
                        tagCounts[tag] = 1;
                }
            }
            return tagCounts;
        }
    }

    public class DeletedSnapshot
    {
        public string Id { get; set; }
        public string ShortId { get; set; }
        public DateTime Time { get; set; }
        public string GameName { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string Host { get; set; }
        public List<string> Paths { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"{ShortId} - {GameName} ({Time:yyyy-MM-dd HH:mm})";
        }
    }

    public static class PruneResultParser
    {
        public static PruneResult ParsePruneOutput(CommandResult result, bool isDryRun = false)
        {
            var pruneResult = new PruneResult
            {
                Success = result.ExitCode == 0,
                RawOutput = result.StdOut + result.StdErr,
                IsDryRun = isDryRun
            };

            if (!pruneResult.Success)
            {
                return pruneResult;
            }

            try
            {
                ParseDeletedSnapshots(pruneResult, result.StdOut);
                ParseDataDeleted(pruneResult, result.StdOut);
            }
            catch (Exception ex)
            {
                // If parsing fails, we still have the raw output
                System.Diagnostics.Debug.WriteLine($"Error parsing prune output: {ex.Message}");
            }

            return pruneResult;
        }

        public static PruneResult ParseForgetOutput(CommandResult result, bool isDryRun = false)
        {
            var pruneResult = new PruneResult
            {
                Success = result.ExitCode == 0,
                RawOutput = result.StdOut + result.StdErr,
                IsDryRun = isDryRun
            };

            if (!pruneResult.Success)
            {
                return pruneResult;
            }

            try
            {
                ParseForgetSnapshots(pruneResult, result.StdOut);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing forget output: {ex.Message}");
            }

            return pruneResult;
        }

        private static void ParseDeletedSnapshots(PruneResult result, string output)
        {
            // Parse restic prune JSON output
            try
            {
                // Try to parse as JSON first
                if (output.Trim().StartsWith("{") || output.Trim().StartsWith("["))
                {
                    var jsonOutput = JObject.Parse(output);

                    // Handle JSON prune output format
                    if (jsonOutput["removed_snapshots"] != null)
                    {
                        var removedSnapshots = jsonOutput["removed_snapshots"] as JArray;
                        foreach (var snapshot in removedSnapshots)
                        {
                            result.DeletedSnapshots.Add(new DeletedSnapshot
                            {
                                Id = snapshot["id"]?.ToString(),
                                ShortId = snapshot["short_id"]?.ToString() ?? snapshot["id"]?.ToString()?.Substring(0, 8),
                                GameName = "Unknown" // Prune output typically doesn't include detailed snapshot info
                            });
                        }
                    }
                }
                else
                {
                    // Fallback to text parsing for non-JSON output
                    ParseDeletedSnapshotsText(result, output);
                }
            }
            catch (Exception)
            {
                // If JSON parsing fails, fall back to text parsing
                ParseDeletedSnapshotsText(result, output);
            }

            result.SnapshotsDeleted = result.DeletedSnapshots.Count;
        }

        private static void ParseDeletedSnapshotsText(PruneResult result, string output)
        {
            // Fallback text parsing for prune output
            var lines = output.Split('\n');

            foreach (var line in lines)
            {
                // Look for lines that indicate snapshot deletion
                if (line.Contains("removing") && line.Contains("snapshot"))
                {
                    var match = Regex.Match(line, @"removing.*snapshot\s+([a-f0-9]{8})");
                    if (match.Success)
                    {
                        var shortId = match.Groups[1].Value;
                        result.DeletedSnapshots.Add(new DeletedSnapshot
                        {
                            ShortId = shortId,
                            Id = shortId, // We only get short ID from prune output
                            GameName = "Unknown" // Prune output doesn't include game info
                        });
                    }
                }
            }
        }

        private static void ParseForgetSnapshots(PruneResult result, string output)
        {
            try
            {
                // Try to parse as JSON first
                if (output.Trim().StartsWith("{") || output.Trim().StartsWith("["))
                {
                    ParseForgetSnapshotsJson(result, output);
                }
                else
                {
                    // Fallback to text parsing for non-JSON output
                    ParseForgetSnapshotsText(result, output);
                }
            }
            catch (Exception)
            {
                // If JSON parsing fails, fall back to text parsing
                ParseForgetSnapshotsText(result, output);
            }

            result.SnapshotsDeleted = result.DeletedSnapshots.Count;
            result.GamesAffected = result.DeletedSnapshots
                .Where(s => !string.IsNullOrEmpty(s.GameName))
                .Select(s => s.GameName)
                .Distinct()
                .Count();
        }

        private static void ParseForgetSnapshotsJson(PruneResult result, string output)
        {
            // Parse JSON output from restic forget --json
            var lines = output.Split('\n').Where(l => l.Trim().StartsWith("{")).ToArray();

            foreach (var line in lines)
            {
                try
                {
                    var jsonOutput = JObject.Parse(line);

                    // Check for "remove" action in the JSON
                    if (jsonOutput["action"]?.ToString() == "remove")
                    {
                        var snapshot = jsonOutput["snapshot"];
                        if (snapshot != null)
                        {
                            var deletedSnapshot = new DeletedSnapshot
                            {
                                Id = snapshot["id"]?.ToString(),
                                ShortId = snapshot["short_id"]?.ToString() ?? snapshot["id"]?.ToString()?.Substring(0, 8),
                                Host = snapshot["hostname"]?.ToString()
                            };

                            // Parse time
                            if (DateTime.TryParse(snapshot["time"]?.ToString(), out DateTime time))
                            {
                                deletedSnapshot.Time = time;
                            }

                            // Parse tags
                            var tags = snapshot["tags"]?.ToObject<List<string>>();
                            if (tags != null && tags.Count > 0)
                            {
                                deletedSnapshot.Tags = tags;
                                deletedSnapshot.GameName = tags[0]; // First tag is the game name
                            }

                            // Parse paths
                            var paths = snapshot["paths"]?.ToObject<List<string>>();
                            if (paths != null)
                            {
                                deletedSnapshot.Paths = paths;
                            }

                            result.DeletedSnapshots.Add(deletedSnapshot);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing forget JSON line: {ex.Message}");
                }
            }
        }

        private static void ParseForgetSnapshotsText(PruneResult result, string output)
        {
            // Fallback text parsing for forget output
            var lines = output.Split('\n');
            DeletedSnapshot currentSnapshot = null;

            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                // Look for snapshot ID lines - updated regex to handle different formats
                var snapshotMatch = Regex.Match(line, @"^([a-f0-9]{8,})\s+(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})");
                if (snapshotMatch.Success)
                {
                    if (currentSnapshot != null)
                    {
                        result.DeletedSnapshots.Add(currentSnapshot);
                    }

                    currentSnapshot = new DeletedSnapshot
                    {
                        ShortId = snapshotMatch.Groups[1].Value.Substring(0, Math.Min(8, snapshotMatch.Groups[1].Value.Length)),
                        Id = snapshotMatch.Groups[1].Value
                    };

                    if (DateTime.TryParse(snapshotMatch.Groups[2].Value, out DateTime time))
                    {
                        currentSnapshot.Time = time;
                    }
                }

                // Look for tags in the line - improved tag detection
                if (currentSnapshot != null)
                {
                    // Check for [tag1, tag2] format
                    var tagMatch = Regex.Match(line, @"\[(.*?)\]");
                    if (tagMatch.Success)
                    {
                        var tagsString = tagMatch.Groups[1].Value;
                        var tags = tagsString.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                        currentSnapshot.Tags.AddRange(tags);

                        // Extract game name from first tag
                        if (tags.Count > 0 && string.IsNullOrEmpty(currentSnapshot.GameName))
                        {
                            currentSnapshot.GameName = tags[0];
                        }
                    }

                    // Alternative: look for standalone tags on the same line as the snapshot
                    if (string.IsNullOrEmpty(currentSnapshot.GameName))
                    {
                        // Try to extract from the rest of the line after the timestamp
                        var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 3)
                        {
                            // Look for parts that might be tags after the timestamp
                            for (int i = 3; i < parts.Length; i++)
                            {
                                if (!parts[i].Contains("/") && !parts[i].Contains("\\") && !parts[i].Contains(":"))
                                {
                                    currentSnapshot.Tags.Add(parts[i]);
                                    if (string.IsNullOrEmpty(currentSnapshot.GameName))
                                    {
                                        currentSnapshot.GameName = parts[i];
                                    }
                                }
                            }
                        }
                    }
                }

                // Look for paths
                if (currentSnapshot != null && (line.Trim().StartsWith("/") || line.Trim().Contains(":\\")))
                {
                    currentSnapshot.Paths.Add(line.Trim());
                }
            }

            // Add the last snapshot if any
            if (currentSnapshot != null)
            {
                result.DeletedSnapshots.Add(currentSnapshot);
            }
        }

        private static void ParseDataDeleted(PruneResult result, string output)
        {
            // Look for data deletion information
            var dataMatch = Regex.Match(output, @"removed (\d+(?:\.\d+)?)\s*([KMGT]?B) of data");
            if (dataMatch.Success)
            {
                result.DataDeleted = $"{dataMatch.Groups[1].Value} {dataMatch.Groups[2].Value}";
            }
            else
            {
                // Look for other patterns
                var altMatch = Regex.Match(output, @"(\d+(?:\.\d+)?)\s*([KMGT]?B).*(?:freed|deleted|removed)");
                if (altMatch.Success)
                {
                    result.DataDeleted = $"{altMatch.Groups[1].Value} {altMatch.Groups[2].Value}";
                }
            }
        }
    }
}
