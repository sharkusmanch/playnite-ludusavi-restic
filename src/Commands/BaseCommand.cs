using System.Diagnostics;
using Playnite.SDK;

namespace LudusaviRestic
{
    public class BaseCommand
    {
        protected static readonly ILogger logger = LogManager.GetLogger();

        protected static CommandResult ExecuteCommand(string command, string args)
        {
            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            logger.Debug(process.StartInfo.FileName);
            logger.Debug(process.StartInfo.Arguments);

            return new CommandResult(process);
        }
    }
}
