namespace LudusaviRestic
{
    public class LudusaviCommand : BaseCommand
    {
        public static CommandResult Version(BackupContext context)
        {
            return LudusaviExecute(context, "--version");
        }

        public static CommandResult BackupAll(BackupContext context)
        {
            return LudusaviExecute(context, BuildBackupAllArgs());
        }

        public static CommandResult Backup(BackupContext context, string game)
        {
            return LudusaviExecute(context, BuildBackupArgs(game));
        }

        internal static string BuildBackupAllArgs()
        {
            return "backup --api --preview";
        }

        internal static string BuildBackupArgs(string game)
        {
            return $"backup --api --preview \"{game}\"";
        }

        private static CommandResult LudusaviExecute(BackupContext context, string args)
        {
            string command = context.Settings.LudusaviExecutablePath.Trim();
            return ExecuteCommand(command, args);
        }
    }
}
