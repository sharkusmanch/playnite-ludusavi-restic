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
            System.IO.FileInfo listfileinfo = new System.IO.FileInfo(listfile);
            listfileinfo.Attributes = System.IO.FileAttributes.Temporary;
            System.IO.StreamWriter listfilewriter = System.IO.File.AppendText(listfile);

            foreach (string filename in files)
            {
                listfilewriter.WriteLine(NormalizePath(filename));
            }
            listfilewriter.Flush();
            listfilewriter.Close();

            return listfile;
        }

        internal static SnapshotResult CreateSnapshot(IList<string> files, BackupContext context, string game, IList<string> extraTags)
        {
            string listfile = WriteFilesToTempFile(files);
            string tags = ConstructTags(game, extraTags);
            string backupArgs = $"{tags} --files-from-verbatim \"{listfile}\"";

            CommandResult process;

            try
            {
                process = ResticCommand.Backup(context, backupArgs);
            }
            catch (Exception e)
            {
                logger.Debug(e, "Encountered error executing restic");
                return SnapshotResult.Error;
            }

            switch (process.ExitCode)
            {
                case 1:
                    logger.Error($"Failed to create restic game saves snapshot {game}");
                    return SnapshotResult.Failed;
                case 3:
                    logger.Error($"Restic failed to read some game save files for {game}");
                    return SnapshotResult.PartialFailure;
                default:
                    // Delete file list on success
                    System.IO.File.Delete(listfile);
                    return SnapshotResult.Success;
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
