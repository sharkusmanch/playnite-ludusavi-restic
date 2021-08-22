using System.Diagnostics;

namespace LudusaviRestic
{
    public class ResticCommand : BaseCommand
    {
        public static Process Version(BackupContext context)
        {
            return ResticExecute(context, "version");
        }

        public static Process Unlock(BackupContext context)
        {
            return ResticExecute(context, "unlock");
        }

        public static Process Stats(BackupContext context)
        {
            return ResticExecute(context, "stats");
        }

        public static Process Backup(BackupContext context, string args)
        {
            return ResticExecute(context, $"backup {args}");
        }

        public static Process List(BackupContext context, string args)
        {
            return ResticExecute(context, $"list {args}");
        }

        public static Process Verify(BackupContext context)
        {
            return List(context, "keys");
        }

        private static Process ResticExecute(BackupContext context, string args)
        {
            string command = context.Settings.ResticExecutablePath.Trim();
            return ExecuteCommand(command, args);
        }
    }
}