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

        public static CommandResult RestoreSnapshot(BackupContext context, string snapshotId, string targetDir)
        {
            return ResticExecute(context, $"restore {snapshotId} --target \"{targetDir}\"");
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

        public static CommandResult ForgetWithRetention(BackupContext context)
        {
            var settings = context.Settings;
            if (!settings.EnableRetentionPolicy)
            {
                return Prune(context);
            }

            var retentionArgs = $"forget --keep-last {settings.KeepLast} " +
                               $"--keep-daily {settings.KeepDaily} " +
                               $"--keep-weekly {settings.KeepWeekly} " +
                               $"--keep-monthly {settings.KeepMonthly} " +
                               $"--keep-yearly {settings.KeepYearly} " +
                               "--group-by tags --json --prune";

            return ResticExecute(context, retentionArgs);
        }

        public static CommandResult ForgetWithRetentionDryRun(BackupContext context)
        {
            var settings = context.Settings;
            if (!settings.EnableRetentionPolicy)
            {
                // Return a check command as a placeholder when retention is disabled
                return Check(context);
            }

            var retentionArgs = $"forget --keep-last {settings.KeepLast} " +
                               $"--keep-daily {settings.KeepDaily} " +
                               $"--keep-weekly {settings.KeepWeekly} " +
                               $"--keep-monthly {settings.KeepMonthly} " +
                               $"--keep-yearly {settings.KeepYearly} " +
                               "--group-by tags --json --dry-run";

            return ResticExecute(context, retentionArgs);
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
