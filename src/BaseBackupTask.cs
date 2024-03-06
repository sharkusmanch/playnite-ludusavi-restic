using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LudusaviRestic
{
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

        protected static IList<String> GameFilesToList(JObject filesMap)
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

        protected static string ConstructTags(string game, IList<string> extraTags)
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


        private static string WriteFilesToTempFile(IList<string> files)
        {
            string listfile = System.IO.Path.GetTempFileName();
            System.IO.FileInfo listfileinfo = new System.IO.FileInfo(listfile);
            listfileinfo.Attributes = System.IO.FileAttributes.Temporary;
            System.IO.StreamWriter listfilewriter = System.IO.File.AppendText(listfile);

            foreach (string filename in files)
            {
                listfilewriter.WriteLine(filename);
            }
            listfilewriter.Flush();
            listfilewriter.Close();

            return listfile;
        }

        protected static void CreateSnapshot(IList<string> files, BackupContext context, string game, IList<string> extraTags)
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
                return;
            }

            switch (process.ExitCode)
            {
                case 1:
                    logger.Error($"Failed to create restic game saves snapshot {game}");
                    SendErrorNotification($"Failed to create restic game saves snapshot {game}", context);
                    break;
                case 3:
                    logger.Error($"Restic failed to read some game save files for {game}");
                    SendErrorNotification($"Restic failed to read some game save files for {game}", context);
                    break;
                default:
                    SendInfoNotification($"Successfully created game data snapshot for {game}", context);
                    // Delete file list on success
                    System.IO.File.Delete(listfile);
                    break;
            }
        }

        protected static void CreateSnapshot(IList<string> files, BackupContext context, Game game, IList<string> extraTags)
        {
            CreateSnapshot(files, context, game.Name, extraTags);
        }

        protected static void SendNotification(string message, NotificationType type, BackupContext context)
        {
            context.API.Notifications.Add(new NotificationMessage(context.NotificationID, message, type));
        }

        protected static void SendErrorNotification(string message, BackupContext context)
        {
            SendNotification(message, NotificationType.Error, context);
        }

        protected static void SendInfoNotification(string message, BackupContext context)
        {
            SendNotification(message, NotificationType.Info, context);
        }
    }
}
