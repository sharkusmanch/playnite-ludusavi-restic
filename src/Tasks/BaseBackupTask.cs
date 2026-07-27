using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LudusaviRestic
{
    internal enum SnapshotResult { Success, Failed, PartialFailure, Error }

    public abstract class BaseBackupTask
    {
        protected static readonly ILogger logger = LogManager.GetLogger();
        protected SemaphoreSlim semaphore;
        protected BackupContext context;
        protected IList<string> extraTags;

        public BaseBackupTask(SemaphoreSlim semaphore, BackupContext context)
        {
            this.semaphore = semaphore;
            this.context = context;
            this.extraTags = new List<string>();
        }

        public BaseBackupTask(SemaphoreSlim semaphore, BackupContext context, IList<string> extraTags)
        {
            this.semaphore = semaphore;
            this.context = context;
            this.extraTags = extraTags;
        }

        public void Run()
        {
            Task.Run(() => this.Backup());
        }

        protected abstract void Backup();

        internal static IList<String> GameFilesToList(JObject filesMap)
        {
            IList<string> files = new List<string>();

            foreach (JProperty property in filesMap.Properties())
            {
                files.Add($"{property.Name}");
            }

            return files;
        }

        private static string SanitizeTag(string tag)
        {
            return tag.Replace(",", "_");
        }

        internal static string ConstructTags(string game, IList<string> extraTags)
        {
            string tags = $"--tag \"{SanitizeTag(game)}\"";

            foreach (string tag in extraTags)
            {
                tags += $" --tag \"{tag}\"";
            }

            return tags;
        }

        protected static string ConstructTags(Game game, IList<string> extraTags)
        {
            return ConstructTags(game.Name, extraTags);
        }


        internal static string NormalizePath(string path)
        {
            // Normalize forward slashes to backslashes (ludusavi uses forward slashes)
            path = path.Replace('/', '\\');

            // Strip extended-length UNC prefix: \\?\UNC\server\share → \\server\share
            if (path.StartsWith("\\\\?\\UNC\\"))
            {
                path = "\\\\" + path.Substring(8);
            }
            // Strip extended-length local prefix: \\?\C:\path → C:\path
            else if (path.StartsWith("\\\\?\\"))
            {
                path = path.Substring(4);
            }

            return path;
        }

        private static string WriteFilesToTempFile(IList<string> files)
        {
            string listfile = System.IO.Path.GetTempFileName();
            new System.IO.FileInfo(listfile).Attributes = System.IO.FileAttributes.Temporary;
            using (var writer = System.IO.File.AppendText(listfile))
            {
                foreach (string filename in files)
                {
                    writer.WriteLine(NormalizePath(filename));
                }
            }
            return listfile;
        }

        /// <summary>
        /// Maps a restic exit code to a snapshot outcome. Only 0 means a snapshot was
        /// written: restic 0.17+ also returns 10 (no repository), 11 (lock failed) and
        /// 12 (wrong password), all of which previously reported success.
        /// </summary>
        internal static SnapshotResult MapExitCode(int exitCode)
        {
            switch (exitCode)
            {
                case 0:
                    return SnapshotResult.Success;
                case 3:
                    return SnapshotResult.PartialFailure;
                default:
                    return SnapshotResult.Failed;
            }
        }

        internal static SnapshotResult CreateSnapshot(IList<string> files, BackupContext context, string game, IList<string> extraTags)
        {
            string listfile = WriteFilesToTempFile(files);

            try
            {
                string tags = ConstructTags(game, extraTags);
                string backupArgs = $"{tags} --files-from-verbatim \"{listfile}\"";

                CommandResult process;

                try
                {
                    process = ResticCommand.Backup(context, backupArgs);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Encountered error executing restic");
                    return SnapshotResult.Error;
                }

                SnapshotResult result = MapExitCode(process.ExitCode);

                switch (result)
                {
                    case SnapshotResult.PartialFailure:
                        logger.Error($"Restic failed to read some game save files for {game}: {process.StdErr}");
                        break;
                    case SnapshotResult.Failed:
                        logger.Error($"Failed to create restic game saves snapshot {game} (exit code {process.ExitCode}): {process.StdErr}");
                        break;
                }

                return result;
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(listfile);
                }
                catch (Exception e)
                {
                    // Cleanup only. GetTempFileName creates the file, so leaking one per
                    // failed backup eventually exhausts the 65535-name limit, but failing
                    // the backup over an undeletable temp file would be worse.
                    logger.Warn(e, $"Failed to delete temporary file list {listfile}");
                }
            }
        }

        internal static SnapshotResult CreateSnapshot(IList<string> files, BackupContext context, Game game, IList<string> extraTags)
        {
            return CreateSnapshot(files, context, game.Name, extraTags);
        }

        protected static void SendNotification(string message, NotificationType type, BackupContext context, string notificationId = null)
        {
            string id = notificationId ?? context.NotificationID;
            context.API.Notifications.Add(new NotificationMessage(id, message, type));
        }

        protected static void SendErrorNotification(string message, BackupContext context, string notificationId = null)
        {
            // Errors always notify regardless of notification level
            SendNotification(message, NotificationType.Error, context, notificationId);
        }

        protected static void SendInfoNotification(string message, BackupContext context, string notificationId = null)
        {
            if (context.Settings.NotificationLevel == NotificationLevel.Verbose)
            {
                SendNotification(message, NotificationType.Info, context, notificationId);
            }
        }
    }
}
