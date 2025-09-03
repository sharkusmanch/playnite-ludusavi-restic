using System.Diagnostics;

namespace LudusaviRestic
{
    public abstract class BaseCommand
    {
        protected static CommandResult ExecuteCommand(string fileName, string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            return new CommandResult(process);
        }
    }
}

