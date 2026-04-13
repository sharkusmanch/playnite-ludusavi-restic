using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
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
                    SendErrorNotification(ResourceProvider.GetString("LOCLuduRestFailedToGetLudusaviFiles"), context);
                    return;
                }

                logger.Debug($"Got all game data from ludusavi");

                var allFiles = ParseAllGameFiles(ludusaviOutput);

                // Filter out games that are exclusively from excluded sources
                if (context.Settings.ExcludedSourceIds != null && context.Settings.ExcludedSourceIds.Count > 0)
                {
                    var toRemove = allFiles.Keys
                        .Where(gameName =>
                        {
                            var matchingGames = context.API.Database.Games
                                .Where(g => g.Name == gameName)
                                .ToList();
                            return matchingGames.Count > 0 && matchingGames.All(g =>
                                g.SourceId != Guid.Empty &&
                                context.Settings.ExcludedSourceIds.Contains(g.SourceId));
                        })
                        .ToList();
                    foreach (var name in toRemove)
                        allFiles.Remove(name);
                }

                string backupText = $"{ResourceProvider.GetString("LOCLuduRestBackupGM")} - {ResourceProvider.GetString("LOCLuduRestBackupGPBackingUp")}";

                activateGlobalProgress.ProgressMaxValue = allFiles.Count;

                int succeeded = 0;
                int failed = 0;
                int partial = 0;
                var failedGames = new List<string>();

                foreach (var entry in allFiles)
                {
                    activateGlobalProgress.Text = string.Format(ResourceProvider.GetString("LOCLuduRestBackupProgressCount"), backupText, activateGlobalProgress.CurrentProgressValue + 1, allFiles.Count);

                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var result = CreateSnapshot(entry.Value, context, entry.Key, extraTags);

                    switch (result)
                    {
                        case SnapshotResult.Success:
                            succeeded++;
                            break;
                        case SnapshotResult.Failed:
                        case SnapshotResult.Error:
                            failed++;
                            failedGames.Add(entry.Key);
                            break;
                        case SnapshotResult.PartialFailure:
                            partial++;
                            failedGames.Add(entry.Key);
                            break;
                    }

                    activateGlobalProgress.CurrentProgressValue++;
                }

                int total = succeeded + failed + partial;
                var level = context.Settings.NotificationLevel;
                string notifId = context.UniqueNotificationID("backup_all");

                if (failed > 0 || partial > 0)
                {
                    string failedList = failedGames.Count <= 5
                        ? string.Join(", ", failedGames)
                        : string.Join(", ", failedGames.Take(5)) + " " + string.Format(ResourceProvider.GetString("LOCLuduRestAndMore"), failedGames.Count - 5);

                    string message = string.Format(
                        ResourceProvider.GetString("LOCLuduRestBackupAllSummaryFailures"),
                        succeeded, total, failed + partial, failedList);

                    // Errors always notify regardless of level
                    SendNotification(message, NotificationType.Error, context, notifId);
                }
                else if (level == NotificationLevel.Summary || level == NotificationLevel.Verbose)
                {
                    string message = string.Format(
                        ResourceProvider.GetString("LOCLuduRestBackupAllSummarySuccess"),
                        succeeded, total);

                    SendNotification(message, NotificationType.Info, context, notifId);
                }
            }, globalProgressOptions);
        }
    }
}
