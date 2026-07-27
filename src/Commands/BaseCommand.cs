using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Playnite.SDK;

namespace LudusaviRestic
{
    public class BaseCommand
    {
        protected static readonly ILogger logger = LogManager.GetLogger();

        protected static CommandResult ExecuteCommand(string command, string args)
        {
            return ExecuteCommand(command, args, null);
        }

        // internal rather than protected so ResticUtility can reuse it: a second
        // process-launch path is how the pipe-deadlock and encoding bugs got duplicated.
        internal static CommandResult ExecuteCommand(string command, string args, IDictionary<string, string> environment)
        {
            return ExecuteCommand(command, args, environment, CommandResult.DefaultTimeoutMilliseconds);
        }

        internal static CommandResult ExecuteCommand(string command, string args,
            IDictionary<string, string> environment, int timeoutMilliseconds)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;

                // restic and ludusavi emit UTF-8. Without this the streams are decoded
                // using the console code page, which mangles any non-ASCII game name.
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                if (environment != null)
                {
                    foreach (var variable in environment)
                    {
                        process.StartInfo.Environment[variable.Key] = variable.Value;
                    }
                }

                logger.Debug(process.StartInfo.FileName);
                logger.Debug(process.StartInfo.Arguments);

                return new CommandResult(process, timeoutMilliseconds);
            }
        }
    }
}
