using System.Diagnostics;

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

        private static CommandResult ResticExecute(BackupContext context, string args)
        {
            string command = context.Settings.ResticExecutablePath.Trim();
            return ExecuteCommand(command, args);
        }
    }
}