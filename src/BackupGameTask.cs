using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace LudusaviRestic
{
    public class BackupGameTask : BaseBackupTask
    {
        private Game game;

        public BackupGameTask(Game game, SemaphoreSlim semaphore, BackupContext context) : base(semaphore, context)
        {
            this.game = game;
        }

        public BackupGameTask(Game game, SemaphoreSlim semaphore, BackupContext context, IList<string> extraTags) : base(semaphore, context, extraTags)
        {
            this.game = game;
        }

        protected override void Backup()
        {
            Backup(this.semaphore, this.context, this.game, this.extraTags);
        }

        protected static IList<String> GameFiles(Game game, BackupContext context)
        {
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
                return new List<string>(); ;
            }

            int totalGames = (int)gameData["overall"]["totalGames"];

            if (totalGames != 1)
            {
                logger.Error("Unable to get game info from ludusavi");
                SendErrorNotification($"No save files found for {game.Name}", context);
                return new List<string>(); ;
            }

            logger.Debug($"Got {game.Name} data from ludusavi");

            var filesToken = gameData["games"][$"{game}"]["files"];
            var filePaths = new List<string>();

            if (filesToken is JArray filesArray)
            {
                // Old format: array of file objects
                foreach (var file in filesArray)
                {
                    if (file["ignored"] == null || !file["ignored"].Value<bool>())
                    {
                        filePaths.Add(file["path"].ToString());
                    }
                }
            }
            else if (filesToken is JObject filesObj)
            {
                // New format: object/dictionary of file paths to file info
                foreach (var prop in filesObj.Properties())
                {
                    var fileInfo = prop.Value;
                    if (fileInfo["ignored"] == null || !fileInfo["ignored"].Value<bool>())
                    {
                        filePaths.Add(prop.Name);
                    }
                }
            }

            return filePaths;
        }

        protected static void Backup(SemaphoreSlim semaphore, BackupContext context, Game game, IList<string> extraTags)
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

        protected static void Backup(Game game, BackupContext context, IList<string> extraTags)
        {
            IList<String> files = GameFiles(game, context);

            if (files.Count == 0)
            {
                return;
            }

            CreateSnapshot(files, context, game, extraTags);
        }
    }
}
