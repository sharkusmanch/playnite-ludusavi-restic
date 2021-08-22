using System.Diagnostics;

namespace LudusaviRestic
{
    public class LudusaviCommand : BaseCommand
    {
        public static Process Version(BackupContext context)
        {
            return LudusaviExecute(context, "--version");
        }

        public static Process Backup(BackupContext context, string game)
        {
            return LudusaviExecute(context, $"backup --api --try-update --preview --merge \"{game}\"");
        }

        private static Process LudusaviExecute(BackupContext context, string args)
        {
            string command = context.Settings.LudusaviExecutablePath.Trim();
            return ExecuteCommand(command, args);
        }
    }
}