using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("LudusaviRestic.Tests")]

namespace LudusaviRestic
{
    public class CommandResult
    {
        private int exitCode;
        private string stdout;
        private string stderr;

        public int ExitCode { get { return this.exitCode; } }
        public string StdOut { get { return this.stdout; } }
        public string StdErr { get { return this.stderr; } }

        internal CommandResult(int exitCode, string stdout, string stderr)
        {
            this.exitCode = exitCode;
            this.stdout = stdout;
            this.stderr = stderr;
        }

        public CommandResult(Process process)
        {
            process.Start();
            this.stdout = TransformProcessOutput(process.StandardOutput.ReadToEnd());
            process.WaitForExit(4000);
            this.stderr = TransformProcessOutput(process.StandardError.ReadToEnd());
            this.exitCode = process.ExitCode;
        }

        internal static string TransformProcessOutput(string output)
        {
            byte[] bytes = Encoding.Default.GetBytes(output);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
