using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Playnite.SDK;

[assembly: InternalsVisibleTo("LudusaviRestic.Tests")]

namespace LudusaviRestic
{
    public class CommandResult
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Upper bound on a single restic/ludusavi invocation. Generous, because a first
        /// backup or a "check --read-data" over a large repository legitimately takes a
        /// long time; the point is to eventually release the backup semaphore rather than
        /// to hold the session hostage to a wedged child process.
        /// </summary>
        internal const int DefaultTimeoutMilliseconds = 60 * 60 * 1000;

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

        public CommandResult(Process process) : this(process, DefaultTimeoutMilliseconds)
        {
        }

        /// <summary>
        /// How long to wait, after the child has exited, for its output pipes to reach EOF.
        /// A grandchild that inherited the pipes (restic spawning rclone) can hold them open
        /// after restic itself is gone, so this wait must be bounded: an unbounded one would
        /// hang the caller and hold the backup semaphore for the rest of the session.
        /// </summary>
        internal const int StreamFlushTimeoutMilliseconds = 10 * 1000;

        internal CommandResult(Process process, int timeoutMilliseconds)
            : this(process, timeoutMilliseconds, StreamFlushTimeoutMilliseconds)
        {
        }

        internal CommandResult(Process process, int timeoutMilliseconds, int flushTimeoutMilliseconds)
        {
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var outputLock = new object();

            var stdoutClosed = new ManualResetEventSlim(false);
            var stderrClosed = new ManualResetEventSlim(false);

            // Both pipes must be drained concurrently. Reading one to EOF before touching
            // the other deadlocks permanently as soon as the unread pipe's buffer fills
            // (~4 KB), which restic reaches easily on any error-heavy run.
            //
            // A null Data signals EOF on that stream. The lock guards against a torn read
            // if the flush wait below times out while a handler is still appending.
            DataReceivedEventHandler onStdout = (sender, e) =>
            {
                if (e.Data == null) { stdoutClosed.Set(); }
                else { lock (outputLock) { stdout.AppendLine(e.Data); } }
            };
            DataReceivedEventHandler onStderr = (sender, e) =>
            {
                if (e.Data == null) { stderrClosed.Set(); }
                else { lock (outputLock) { stderr.AppendLine(e.Data); } }
            };

            process.OutputDataReceived += onStdout;
            process.ErrorDataReceived += onStderr;

            try
            {
                process.Start();

                // Hand the child an immediately-closed stdin so anything that would prompt
                // interactively (restic asking for a password) fails fast instead of hanging.
                if (process.StartInfo.RedirectStandardInput)
                {
                    process.StandardInput.Close();
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    TryKill(process);
                    throw new TimeoutException(
                        $"'{process.StartInfo.FileName}' did not exit within {timeoutMilliseconds} ms and was terminated");
                }

                // The child has exited, but its output may not have been delivered yet.
                // Both waits share one deadline: waiting them independently would either
                // double the worst-case bound or, if short-circuited, skip stderr entirely
                // whenever stdout timed out first.
                int deadline = Environment.TickCount + flushTimeoutMilliseconds;
                bool stdoutFlushed = stdoutClosed.Wait(RemainingMilliseconds(deadline));
                bool stderrFlushed = stderrClosed.Wait(RemainingMilliseconds(deadline));

                if (!stdoutFlushed || !stderrFlushed)
                {
                    logger.Warn($"'{process.StartInfo.FileName}' exited but its output streams did not " +
                                "reach EOF; a surviving child process may still hold them. Output may be incomplete.");
                }

                this._exitCode = process.ExitCode;
            }
            finally
            {
                process.OutputDataReceived -= onStdout;
                process.ErrorDataReceived -= onStderr;
            }

            lock (outputLock)
            {
                this._stdout = stdout.ToString();
                this._stderr = stderr.ToString();
            }

            // Deliberately not disposed: an in-flight handler on a threadpool thread may
            // still touch them after the unsubscribe above, and ObjectDisposedException
            // there would be unobservable. They are finalizable and cost nothing to drop.
        }

        /// <summary>
        /// Milliseconds left until <paramref name="deadline"/>, never negative. Uses
        /// unchecked subtraction so a TickCount rollover still yields a correct interval.
        /// </summary>
        private static int RemainingMilliseconds(int deadline)
        {
            int remaining = unchecked(deadline - Environment.TickCount);
            return remaining > 0 ? remaining : 0;
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited) { process.Kill(); }
            }
            catch (Exception e)
            {
                // Best-effort cleanup: the process may have exited between the check and
                // the kill, and the timeout is reported either way.
                logger.Debug(e, "Failed to terminate timed-out process");
            }
        }
    }
}
