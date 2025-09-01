using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
            // Parse restic prune output to find deleted snapshots
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
                            Id = shortId // We only get short ID from prune output
                        });
                    }
                }
            }

            result.SnapshotsDeleted = result.DeletedSnapshots.Count;
        }

        private static void ParseForgetSnapshots(PruneResult result, string output)
        {
            // Parse restic forget output which is more detailed
            var lines = output.Split('\n');
            DeletedSnapshot currentSnapshot = null;

            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                // Look for snapshot ID lines
                var snapshotMatch = Regex.Match(line, @"^([a-f0-9]{8})\s+(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})");
                if (snapshotMatch.Success)
                {
                    if (currentSnapshot != null)
                    {
                        result.DeletedSnapshots.Add(currentSnapshot);
                    }

                    currentSnapshot = new DeletedSnapshot
                    {
                        ShortId = snapshotMatch.Groups[1].Value,
                        Id = snapshotMatch.Groups[1].Value
                    };

                    if (DateTime.TryParse(snapshotMatch.Groups[2].Value, out DateTime time))
                    {
                        currentSnapshot.Time = time;
                    }
                }

                // Look for tags in the line
                if (currentSnapshot != null && line.Contains("[") && line.Contains("]"))
                {
                    var tagMatch = Regex.Match(line, @"\[(.*?)\]");
                    if (tagMatch.Success)
                    {
                        var tags = tagMatch.Groups[1].Value.Split(',').Select(t => t.Trim()).ToList();
                        currentSnapshot.Tags.AddRange(tags);

                        // Try to extract game name from first tag
                        if (tags.Count > 0 && string.IsNullOrEmpty(currentSnapshot.GameName))
                        {
                            currentSnapshot.GameName = tags[0];
                        }
                    }
                }

                // Look for paths
                if (currentSnapshot != null && line.Trim().StartsWith("/") || line.Trim().Contains(":\\"))
                {
                    currentSnapshot.Paths.Add(line.Trim());
                }
            }

            // Add the last snapshot if any
            if (currentSnapshot != null)
            {
                result.DeletedSnapshots.Add(currentSnapshot);
            }

            result.SnapshotsDeleted = result.DeletedSnapshots.Count;
            result.GamesAffected = result.DeletedSnapshots
                .Where(s => !string.IsNullOrEmpty(s.GameName))
                .Select(s => s.GameName)
                .Distinct()
                .Count();
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
