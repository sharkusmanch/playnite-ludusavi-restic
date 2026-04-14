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
        private Game _game;
        private bool _isManual;

        public BackupGameTask(Game game, SemaphoreSlim semaphore, BackupContext context, bool isManual = false) : base(semaphore, context)
        {
            this._game = game;
            this._isManual = isManual;
        }

        public BackupGameTask(Game game, SemaphoreSlim semaphore, BackupContext context, IList<string> extraTags, bool isManual = false) : base(semaphore, context, extraTags)
        {
            this._game = game;
            this._isManual = isManual;
        }

        protected override void Backup()
        {
            Backup(this.semaphore, this.context, this._game, this.extraTags, this._isManual);
        }

        internal static IList<string> ParseGameFiles(string gameName, string ludusaviJson)
        {
            var gameData = JObject.Parse(ludusaviJson);
            int totalGames = (int)gameData["overall"]["totalGames"];

            if (totalGames != 1)
            {
                return new List<string>();
            }

            var filesToken = gameData["games"][gameName]["files"];
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

        protected static IList<String> GameFiles(Game game, BackupContext context)
        {
            try
            {
                CommandResult ludusavi = LudusaviCommand.Backup(context, game.Name);
                var files = ParseGameFiles(game.Name, ludusavi.StdOut);
                if (files.Count == 0)
                {
                    logger.Error("Unable to get game info from ludusavi");
                    SendErrorNotification(string.Format(ResourceProvider.GetString("LOCLuduRestNoSaveFilesFound"), game.Name), context);
                }
                else
                {
                    logger.Debug($"Got {game.Name} data from ludusavi");
                }
                return files;
            }
            catch (Exception e)
            {
                logger.Debug(e, "Failed to get files from ludusavi");
                SendErrorNotification(ResourceProvider.GetString("LOCLuduRestFailedToGetLudusaviFiles"), context);
                return new List<string>();
            }
        }

        protected static void Backup(SemaphoreSlim semaphore, BackupContext context, Game game, IList<string> extraTags, bool isManual)
        {
            try
            {
                semaphore.Wait();
                Backup(game, context, extraTags, isManual);
            }
            finally
            {
                semaphore.Release();
            }
        }

        protected static void Backup(Game game, BackupContext context, IList<string> extraTags, bool isManual)
        {
            IList<String> files = GameFiles(game, context);

            if (files.Count == 0)
            {
                return;
            }

            var result = CreateSnapshot(files, context, game, extraTags);
            string notifId = context.UniqueNotificationID($"game_{game.Name}");

            switch (result)
            {
                case SnapshotResult.Failed:
                    SendNotification(string.Format(ResourceProvider.GetString("LOCLuduRestFailedToCreateSnapshot"), game.Name), NotificationType.Error, context, notifId);
                    break;
                case SnapshotResult.PartialFailure:
                    SendNotification(string.Format(ResourceProvider.GetString("LOCLuduRestPartialSnapshotFailure"), game.Name), NotificationType.Error, context, notifId);
                    break;
                case SnapshotResult.Error:
                    SendNotification(string.Format(ResourceProvider.GetString("LOCLuduRestErrorCreatingSnapshot"), game.Name), NotificationType.Error, context, notifId);
                    break;
                case SnapshotResult.Success:
                    bool shouldNotify = (isManual && context.Settings.NotifyOnManualBackup)
                        || context.Settings.NotificationLevel == NotificationLevel.Verbose;
                    if (shouldNotify)
                    {
                        SendNotification(string.Format(ResourceProvider.GetString("LOCLuduRestSnapshotCreatedSuccessfully"), game.Name), NotificationType.Info, context, notifId);
                    }
                    break;
            }
        }
    }
}
