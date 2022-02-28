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

        protected override void RunTask()
        {
            Backup(this.semaphore, this.context, this.extraTags);
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
                JObject gameData;

                try
                {
                    CommandResult ludusavi = LudusaviCommand.BackupAll(context);
                    gameData = JObject.Parse(ludusavi.StdOut);
                }
                catch (Exception e)
                {
                    logger.Debug(e, "Failed to get files from ludusavi");
                    SendErrorNotification("Failed to get files from ludusavi", context);
                    return;
                }

                logger.Debug($"Got all game data from ludusavi");

                JObject games = (JObject)gameData["games"];
                string backupText = $"{ResourceProvider.GetString("LOCLuduRestBackupGM")} - {ResourceProvider.GetString("LOCLuduRestBackupGPBackingUp")}";

                activateGlobalProgress.ProgressMaxValue = (double)gameData["overall"]["totalGames"];

                foreach (JProperty game in games.Properties())
                {
                    activateGlobalProgress.Text = $"{backupText} - {activateGlobalProgress.CurrentProgressValue + 1} of {gameData["overall"]["totalGames"]}";

                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    string gameName = game.Name;
                    IList<String> files = GameFilesToList((JObject)game.Value["files"]);
                    CreateSnapshot(files, context, gameName, extraTags);

                    activateGlobalProgress.CurrentProgressValue++;
                }
            }, globalProgressOptions);
        }
    }
}
