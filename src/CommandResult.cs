using System.Diagnostics;
using Playnite.SDK;

namespace LudusaviRestic
{
    public class CommandResult
    {
        private int exitCode;
        private string stdout;
        private string stderr;

        public int ExitCode  { get { return this.exitCode; }}
        public string StdOut  { get { return this.stdout; }}
        public string StdErr  { get { return this.stderr; }}

        public CommandResult(Process process)
        {
            process.Start();
            this.stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit(4000);
            this.stderr = process.StandardError.ReadToEnd();
            this.exitCode = process.ExitCode;
        }
    }
}