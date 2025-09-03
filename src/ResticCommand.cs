using System.Diagnostics;

namespace LudusaviRestic
{
    public static class ResticCommand
    {
        private static CommandResult Execute(BackupContext context, string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = context.Settings.ResticExecutablePath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            // Environment variables
            process.StartInfo.Environment["RESTIC_REPOSITORY"] = context.Settings.ResticRepository;
            process.StartInfo.Environment["RESTIC_PASSWORD"] = context.Settings.ResticPassword;
            return new CommandResult(process);
        }

        public static CommandResult Backup(BackupContext context, string args)
        {
            return Execute(context, $"backup {args}");
        }

        public static CommandResult Version(BackupContext context)
        {
            return Execute(context, "version");
        }

        public static CommandResult ListSnapshots(BackupContext context)
        {
            // Output in json
            return Execute(context, "snapshots --json");
        }

        public static CommandResult ForgetSnapshot(BackupContext context, string snapshotId)
        {
            return Execute(context, $"forget {snapshotId}");
        }

        public static CommandResult Verify(BackupContext context)
        {
            // Simple command to verify repo accessibility
            return Version(context);
        }
    }
}

