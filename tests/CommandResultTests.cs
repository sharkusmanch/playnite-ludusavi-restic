using System;
using System.IO;
using System.Text;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class CommandResultTests
    {
        [Fact]
        public void InternalConstructor_SetsProperties()
        {
            var result = new CommandResult(42, "out", "err");

            Assert.Equal(42, result.ExitCode);
            Assert.Equal("out", result.StdOut);
            Assert.Equal("err", result.StdErr);
        }

        [Fact]
        public void Execute_CapturesExitCode()
        {
            var result = BaseCommand.ExecuteCommand("cmd.exe", "/d /c exit 12", null);

            Assert.Equal(12, result.ExitCode);
        }

        [Fact]
        public void Execute_CapturesStdOutAndStdErrSeparately()
        {
            var result = BaseCommand.ExecuteCommand("cmd.exe", "/d /c echo to-stdout& echo to-stderr 1>&2", null);

            Assert.Contains("to-stdout", result.StdOut);
            Assert.DoesNotContain("to-stderr", result.StdOut);
            Assert.Contains("to-stderr", result.StdErr);
        }

        /// <summary>
        /// Writes a file large enough to overflow a pipe buffer, and returns the marker
        /// text present on its last line.
        /// </summary>
        private static string WriteBulkFile(string path, string marker)
        {
            var content = new StringBuilder();
            for (int i = 1; i <= 300; i++)
            {
                content.AppendLine($"{marker}-line-{i}-padding-padding-padding-padding");
            }
            File.WriteAllText(path, content.ToString());
            return $"{marker}-line-300-";
        }

        /// <summary>
        /// Regression test for the pipe deadlock: stdout was read to EOF before stderr was
        /// touched, so anything past the ~4 KB stderr buffer hung the call forever. This
        /// exact command reproduces the hang against the previous implementation.
        /// </summary>
        [Fact]
        public void Execute_LargeStdErr_DoesNotDeadlock()
        {
            string path = Path.GetTempFileName();
            try
            {
                string lastLine = WriteBulkFile(path, "bulk");

                var result = BaseCommand.ExecuteCommand("cmd.exe", $"/d /c type \"{path}\" 1>&2", null);

                Assert.Equal(0, result.ExitCode);
                Assert.True(result.StdErr.Length > 4096, $"expected >4 KB of stderr, got {result.StdErr.Length}");
                Assert.Contains(lastLine, result.StdErr);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Execute_LargeOutputOnBothStreams_DoesNotDeadlock()
        {
            string path = Path.GetTempFileName();
            try
            {
                string lastLine = WriteBulkFile(path, "bulk");

                var result = BaseCommand.ExecuteCommand(
                    "cmd.exe", $"/d /c type \"{path}\" & type \"{path}\" 1>&2", null);

                Assert.Equal(0, result.ExitCode);
                Assert.True(result.StdOut.Length > 4096, $"expected >4 KB of stdout, got {result.StdOut.Length}");
                Assert.True(result.StdErr.Length > 4096, $"expected >4 KB of stderr, got {result.StdErr.Length}");
                Assert.Contains(lastLine, result.StdOut);
                Assert.Contains(lastLine, result.StdErr);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// restic emits UTF-8; without an explicit StandardOutputEncoding the stream is
        /// decoded using the console code page and non-ASCII game names are mangled.
        /// </summary>
        [Fact]
        public void Execute_Utf8Output_DecodedCorrectly()
        {
            string path = Path.GetTempFileName();
            try
            {
                // No BOM: exactly the byte stream restic would produce.
                File.WriteAllText(path, "Ninja Gaiden Σ — 日本語", new UTF8Encoding(false));

                var result = BaseCommand.ExecuteCommand("cmd.exe", $"/d /c type \"{path}\"", null);

                Assert.Contains("Ninja Gaiden Σ", result.StdOut);
                Assert.Contains("日本語", result.StdOut);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Execute_PassesEnvironmentToChildProcess()
        {
            var environment = new System.Collections.Generic.Dictionary<string, string>
            {
                { "LUDUREST_TEST_VAR", "expected-value" }
            };

            var result = BaseCommand.ExecuteCommand("cmd.exe", "/d /c echo %LUDUREST_TEST_VAR%", environment);

            Assert.Contains("expected-value", result.StdOut);
        }

        /// <summary>
        /// The environment must reach the child only, never Playnite's own process block,
        /// which every launched game would inherit.
        /// </summary>
        [Fact]
        public void Execute_DoesNotLeakEnvironmentIntoCurrentProcess()
        {
            var environment = new System.Collections.Generic.Dictionary<string, string>
            {
                { "LUDUREST_LEAK_CHECK", "secret" }
            };

            BaseCommand.ExecuteCommand("cmd.exe", "/d /c exit 0", environment);

            Assert.Null(Environment.GetEnvironmentVariable("LUDUREST_LEAK_CHECK"));
        }

        /// <summary>
        /// A grandchild that inherited the pipes can outlive the process we launched. The
        /// EOF wait must be bounded, or this call would never return and would hold the
        /// backup semaphore for the rest of the session.
        /// </summary>
        [Fact]
        public void Execute_SurvivingGrandchildHoldingPipes_StillReturns()
        {
            var process = new System.Diagnostics.Process();
            // cmd exits immediately; the backgrounded ping inherits its stdout/stderr and
            // keeps the pipe write-ends open well past cmd's own exit.
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/d /c start /b ping -n 30 127.0.0.1";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            using (process)
            {
                var started = System.DateTime.UtcNow;

                // Short flush timeout so the test proves boundedness without waiting it out.
                var result = new CommandResult(process, CommandResult.DefaultTimeoutMilliseconds, 1000);

                var elapsed = System.DateTime.UtcNow - started;

                Assert.Equal(0, result.ExitCode);
                Assert.True(elapsed.TotalSeconds < 15,
                    $"call must be bounded by the flush timeout, took {elapsed.TotalSeconds:F1}s");
            }
        }

        [Fact]
        public void Execute_ProcessExceedingTimeout_IsTerminated()
        {
            var process = new System.Diagnostics.Process();
            // ping directly rather than via cmd.exe: killing a cmd wrapper leaves the
            // grandchild alive holding the pipes, which orphans a process and stalls
            // the run until it exits on its own.
            process.StartInfo.FileName = "ping.exe";
            process.StartInfo.Arguments = "-n 10 127.0.0.1";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            using (process)
            {
                Assert.Throws<TimeoutException>(() => new CommandResult(process, 1000));

                // Kill is asynchronous; give TerminateProcess a moment rather than racing it.
                Assert.True(process.WaitForExit(10000), "timed-out process should have been killed");
            }
        }
    }
}
