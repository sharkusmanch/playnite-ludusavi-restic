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

            return GameFilesToList((JObject)gameData["games"][$"{game}"]["files"]);
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
