using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LudusaviRestic
{
    public class BackupTask
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private Game game;
        private SemaphoreSlim semaphore;
        private BackupContext context;
        private IList<string> extraTags;

        public BackupTask(Game game, SemaphoreSlim semaphore, BackupContext context)
        {
            this.game = game;
            this.semaphore = semaphore;
            this.context = context;
            this.extraTags = new List<string>();
        }

        public BackupTask(Game game, SemaphoreSlim semaphore, BackupContext context, IList<string> extraTags)
        {
            this.game = game;
            this.semaphore = semaphore;
            this.context = context;
            this.extraTags = extraTags;
        }

        public void Run()
        {
            Task.Run(() => Backup(this.semaphore, this.context, this.game, this.extraTags));
        }


        private static void Backup(SemaphoreSlim semaphore, BackupContext context, Game game, IList<string> extraTags)
        {
            try
            {
                semaphore.Wait();
                Backup(game, context, extraTags);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static void Backup(Game game, BackupContext context, IList<string> extraTags)
        {
            IList<String> files = GameFiles(game, context);

            if (files.Count == 0)
            {
                return;
            }

            CreateSnapshot(files, context, game, extraTags);
        }

        private static IList<String> GameFiles(Game game, BackupContext context)
        {
            IList<string> files = new List<string>();

            JObject gameData;

            try
            {
                CommandResult ludusavi = LudusaviCommand.Backup(context, game.Name);
                gameData = JObject.Parse(ludusavi.StdOut);
            }
            catch (Exception e)
            {
                logger.Debug(e, "Failed to get files from ludusavi");
                SendErrorNotification("Failed to get files from ludusavi", context);
                return files;
            }

            int totalGames = (int)gameData["overall"]["totalGames"];

            if (totalGames != 1)
            {
                logger.Error("Unable to get game info from ludusavi");
                SendErrorNotification($"No save files found for {game.Name}", context);
                return files;
            }

            logger.Debug($"Got {game.Name} data from ludusavi");

            JObject filesMap = (JObject)gameData["games"][$"{game}"]["files"];

            foreach (JProperty property in filesMap.Properties())
            {
                files.Add($"{property.Name}");
            }

            return files;
        }

        private static string ConstructTags(Game game, IList<string> extraTags)
        {
            string tags = $"--tag \"{game}\"";

            foreach (string tag in extraTags)
            {
                tags += $" --tag \"{tag}\"";
            }

            return tags;
        }

        private static void CreateSnapshot(IList<string> files, BackupContext context, Game game, IList<string> extraTags)
        {
            // write files list to tempfile and pass to restic with --files-from
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
                    logger.Error($"Failed to create restic game saves snapshot {game.Name}");
                    SendErrorNotification($"Failed to create restic game saves snapshot {game.Name}", context);
                    break;
                case 3:
                    logger.Error($"Restic failed to read some game save files for {game.Name}");
                    SendErrorNotification($"Restic failed to read some game save files for {game.Name}", context);
                    break;
                default:
                    SendInfoNotification($"Successfully created game data snapshot for {game.Name}", context);
                    // Delete file list on success
                    System.IO.File.Delete(listfile);
                    break;
            }
        }

        private static void SendNotification(string message, NotificationType type, BackupContext context)
        {
            context.API.Notifications.Add(new NotificationMessage(context.NotificationID, message, type));
        }

        private static void SendErrorNotification(string message, BackupContext context)
        {
            SendNotification(message, NotificationType.Error, context);
        }

        private static void SendInfoNotification(string message, BackupContext context)
        {
            SendNotification(message, NotificationType.Info, context);
        }
    }
}
