using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("LudusaviRestic.Tests")]

namespace LudusaviRestic
{
    public class CommandResult
    {
        private int _exitCode;
        private string _stdout;
        private string _stderr;

        public int ExitCode { get { return this._exitCode; } }
        public string StdOut { get { return this._stdout; } }
        public string StdErr { get { return this._stderr; } }

        internal CommandResult(int exitCode, string stdout, string stderr)
        {
            this._exitCode = exitCode;
            this._stdout = stdout;
            this._stderr = stderr;
        }

        public CommandResult(Process process)
        {
            process.Start();
            this._stdout = TransformProcessOutput(process.StandardOutput.ReadToEnd());
            process.WaitForExit(4000);
            this._stderr = TransformProcessOutput(process.StandardError.ReadToEnd());
            this._exitCode = process.ExitCode;
        }

        internal static string TransformProcessOutput(string output)
        {
            byte[] bytes = Encoding.Default.GetBytes(output);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
