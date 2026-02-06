using Playnite.SDK;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace LudusaviRestic
{
    public class BackupAllTask : BaseBackupTask
    {

        public BackupAllTask(SemaphoreSlim semaphore, BackupContext context) : base(semaphore, context)
        {
        }

        public BackupAllTask(SemaphoreSlim semaphore, BackupContext context, IList<string> extraTags) : base(semaphore, context, extraTags)
        {
        }

        protected override void Backup()
        {
            Backup(this.semaphore, this.context, this.extraTags);
        }

        internal static IDictionary<string, IList<string>> ParseAllGameFiles(string ludusaviJson)
        {
            var gameData = JObject.Parse(ludusaviJson);
            var games = (JObject)gameData["games"];
            var result = new Dictionary<string, IList<string>>();

            foreach (JProperty game in games.Properties())
            {
                string gameName = game.Name;
                IList<string> files = GameFilesToList((JObject)game.Value["files"]);
                result[gameName] = files;
            }

            return result;
        }

        private static void Backup(SemaphoreSlim semaphore, BackupContext context, IList<string> extraTags)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{ResourceProvider.GetString("LOCLuduRestBackupGM")} - {ResourceProvider.GetString("LOCLuduRestBackupGPProcessing")}...",
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            context.API.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                string ludusaviOutput;

                try
                {
                    CommandResult ludusavi = LudusaviCommand.BackupAll(context);
                    ludusaviOutput = ludusavi.StdOut;
                }
                catch (Exception e)
                {
                    logger.Debug(e, "Failed to get files from ludusavi");
                    SendErrorNotification("Failed to get files from ludusavi", context);
                    return;
                }

                logger.Debug($"Got all game data from ludusavi");

                var allFiles = ParseAllGameFiles(ludusaviOutput);
                string backupText = $"{ResourceProvider.GetString("LOCLuduRestBackupGM")} - {ResourceProvider.GetString("LOCLuduRestBackupGPBackingUp")}";

                activateGlobalProgress.ProgressMaxValue = allFiles.Count;

                foreach (var entry in allFiles)
                {
                    activateGlobalProgress.Text = $"{backupText} - {activateGlobalProgress.CurrentProgressValue + 1} of {allFiles.Count}";

                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    CreateSnapshot(entry.Value, context, entry.Key, extraTags);

                    activateGlobalProgress.CurrentProgressValue++;
                }
            }, globalProgressOptions);
        }
    }
}
