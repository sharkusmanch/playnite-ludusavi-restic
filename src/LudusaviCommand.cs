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
            return LudusaviExecute(context, $"backup --api --try-update --preview --merge");
        }

        public static CommandResult Backup(BackupContext context, string game)
        {
            return LudusaviExecute(context, $"backup --api --try-update --preview --merge \"{game}\"");
        }

        private static CommandResult LudusaviExecute(BackupContext context, string args)
        {
            string command = context.Settings.LudusaviExecutablePath.Trim();
            return ExecuteCommand(command, args);
        }
    }
}
