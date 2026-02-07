using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace LudusaviRestic
{
    public class ResticCommand : BaseCommand
    {
        public static CommandResult Version(BackupContext context)
        {
            return ResticExecute(context, "version");
        }

        public static CommandResult Unlock(BackupContext context)
        {
            return ResticExecute(context, "unlock");
        }

        public static CommandResult Stats(BackupContext context)
        {
            return ResticExecute(context, "stats");
        }

        public static CommandResult Backup(BackupContext context, string args)
        {
            return ResticExecute(context, $"backup {args}");
        }

        public static CommandResult List(BackupContext context, string args)
        {
            return ResticExecute(context, $"list {args}");
        }

        public static CommandResult Verify(BackupContext context)
        {
            return List(context, "keys");
        }

        public static CommandResult ListSnapshots(BackupContext context)
        {
            return ResticExecute(context, "snapshots --json");
        }

        public static CommandResult ForgetSnapshot(BackupContext context, string snapshotId)
        {
            return ResticExecute(context, $"forget {snapshotId}");
        }

        public static CommandResult Prune(BackupContext context)
        {
            return ResticExecute(context, "prune --json");
        }

        public static CommandResult PruneDryRun(BackupContext context)
        {
            return ResticExecute(context, "prune --json --dry-run");
        }

        internal static string BuildRetentionArgs(LudusaviResticSettings settings, bool dryRun)
        {
            var suffix = dryRun ? "--dry-run" : "--prune";
            return $"forget --keep-last {settings.KeepLast} " +
                   $"--keep-daily {settings.KeepDaily} " +
                   $"--keep-weekly {settings.KeepWeekly} " +
                   $"--keep-monthly {settings.KeepMonthly} " +
                   $"--keep-yearly {settings.KeepYearly} " +
                   $"--group-by tags --json {suffix}";
        }

        public static CommandResult ForgetWithRetention(BackupContext context)
        {
            if (!context.Settings.EnableRetentionPolicy)
            {
                return Prune(context);
            }

            return ResticExecute(context, BuildRetentionArgs(context.Settings, false));
        }

        public static CommandResult ForgetWithRetentionDryRun(BackupContext context)
        {
            if (!context.Settings.EnableRetentionPolicy)
            {
                // Return a check command as a placeholder when retention is disabled
                return Check(context);
            }

            return ResticExecute(context, BuildRetentionArgs(context.Settings, true));
        }

        internal static string BuildPerGameRetentionArgs(string gameTag, int keepLast, int keepDaily,
            int keepWeekly, int keepMonthly, int keepYearly, bool dryRun)
        {
            var suffix = dryRun ? "--dry-run" : "--prune";
            var parts = new List<string> { $"forget --tag \"{gameTag}\"" };
            if (keepLast > 0) parts.Add($"--keep-last {keepLast}");
            if (keepDaily > 0) parts.Add($"--keep-daily {keepDaily}");
            if (keepWeekly > 0) parts.Add($"--keep-weekly {keepWeekly}");
            if (keepMonthly > 0) parts.Add($"--keep-monthly {keepMonthly}");
            if (keepYearly > 0) parts.Add($"--keep-yearly {keepYearly}");
            parts.Add("--json");
            parts.Add(suffix);
            return string.Join(" ", parts);
        }

        internal static IList<string> ExtractGameTags(string snapshotsJson)
        {
            var tags = new HashSet<string>();
            try
            {
                var array = JArray.Parse(snapshotsJson);
                foreach (var snapshot in array)
                {
                    var snapshotTags = snapshot["tags"]?.ToObject<List<string>>();
                    if (snapshotTags != null && snapshotTags.Count > 0)
                    {
                        tags.Add(snapshotTags[0]);
                    }
                }
            }
            catch
            {
                // Return empty list on parse failure
            }
            return tags.ToList();
        }

        public static IList<CommandResult> ForgetWithPerGameRetention(BackupContext context,
            IList<string> gameTags, bool dryRun)
        {
            var results = new List<CommandResult>();
            logger.Debug($"ForgetWithPerGameRetention: {gameTags.Count} game tags, dryRun={dryRun}");
            foreach (var gameTag in gameTags)
            {
                var over = context.Settings.FindOverrideByGameName(gameTag);
                RetentionValues retention;
                if (over != null && over.HasRetentionOverride)
                {
                    retention = over.GetEffectiveRetention(context.Settings);
                    logger.Debug($"Game '{gameTag}': using override retention (last={retention.KeepLast}, daily={retention.KeepDaily}, weekly={retention.KeepWeekly}, monthly={retention.KeepMonthly}, yearly={retention.KeepYearly})");
                }
                else
                {
                    retention = new RetentionValues(
                        context.Settings.KeepLast, context.Settings.KeepDaily,
                        context.Settings.KeepWeekly, context.Settings.KeepMonthly,
                        context.Settings.KeepYearly);
                    logger.Debug($"Game '{gameTag}': using global retention (last={retention.KeepLast}, daily={retention.KeepDaily}, weekly={retention.KeepWeekly}, monthly={retention.KeepMonthly}, yearly={retention.KeepYearly})");
                }

                var args = BuildPerGameRetentionArgs(gameTag,
                    retention.KeepLast, retention.KeepDaily, retention.KeepWeekly,
                    retention.KeepMonthly, retention.KeepYearly, dryRun);
                logger.Debug($"Executing: restic {args}");
                var result = ResticExecute(context, args);
                logger.Debug($"Result for '{gameTag}': exitCode={result.ExitCode}, stdout length={result.StdOut?.Length ?? 0}, stderr length={result.StdErr?.Length ?? 0}");
                if (result.StdOut?.Length > 0)
                    logger.Debug($"Stdout for '{gameTag}': {result.StdOut.Substring(0, System.Math.Min(500, result.StdOut.Length))}");
                if (result.StdErr?.Length > 0)
                    logger.Debug($"Stderr for '{gameTag}': {result.StdErr.Substring(0, System.Math.Min(500, result.StdErr.Length))}");
                results.Add(result);
            }
            return results;
        }

        public static CommandResult Check(BackupContext context)
        {
            return ResticExecute(context, "check");
        }

        public static CommandResult CheckWithData(BackupContext context)
        {
            return ResticExecute(context, "check --read-data");
        }

        public static CommandResult Init(BackupContext context)
        {
            return ResticExecute(context, "init");
        }

        private static CommandResult ResticExecute(BackupContext context, string args)
        {
            string command = context.Settings.ResticExecutablePath.Trim();
            return ExecuteCommand(command, args);
        }
    }
}
