using Playnite.SDK;
using Playnite.SDK.Models;
using System.Threading;
using System.Collections.Generic;
using System;

namespace LudusaviRestic
{
    public class ResticBackupManager
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private BackupContext context;
        private SemaphoreSlim semaphore;

        public ResticBackupManager(LudusaviResticSettings settings, IPlayniteAPI api)
        {
            this.semaphore = new SemaphoreSlim(1);
            this.context = new BackupContext(api, settings);
        }

        public void BackupAllGames()
        {
            new BackupAllTask(this.semaphore, this.context, new List<string>()).Run();
        }

        public void BackupAllGames(IList<string> extraTags)
        {
            new BackupAllTask(this.semaphore, this.context, extraTags).Run();
        }

        public void PerformBackup(Game game)
        {
            PerformBackup(game, new List<string>());
        }

        private bool GameHasTag(Game game, Guid tagId)
        {
            return game.TagIds != null && game.TagIds.Contains(tagId);
        }

        public void PerformBackup(Game game, IList<string> extraTags)
        {
            logger.Debug($"Backup #{game.Name}");
            LudusaviResticSettings settings = this.context.Settings;

            if (settings.BackupExecutionMode == ExecutionMode.Exclude && GameHasTag(game, settings.ExcludeTagID))
            {
                logger.Info($"Skipping backup of {game.Name} due to exclude tag");
                return;
            }
            else if (settings.BackupExecutionMode == ExecutionMode.Include && !GameHasTag(game, settings.IncludeTagID))
            {
                logger.Info($"Skipping backup of {game.Name} due to lacking include tag");
                return;
            }

            BackupGameTask task = new BackupGameTask(game, this.semaphore, this.context, extraTags);
            task.Run();
        }

        public void PerformGameStoppedBackup(Game game, string backupTag)
        {
            var tags = GamestoppedBackupTags();
            tags.Add(backupTag);

            PerformBackup(game, tags);
        }

        public void PerformGameStoppedBackup(Game game)
        {
            PerformBackup(game, GamestoppedBackupTags());
        }

        public void PerformManualBackup(Game game)
        {
            var tags = ManualBackupTags();
            // Prompt user for an optional custom tag
            var result = this.context.API.Dialogs.SelectString(
                ResourceProvider.GetString("LOCLuduRestManualBackupTagPrompt"),
                ResourceProvider.GetString("LOCLuduRestManualBackupTagTitle"),
                ""
            );
            if (result?.Result == true && !string.IsNullOrWhiteSpace(result.SelectedString))
            {
                tags.Add(result.SelectedString.Trim());
            }
            PerformBackup(game, tags);
        }

        public void PerformGameplayBackup(Game game)
        {
            PerformBackup(game, GameplayBackupTags());
        }

        private IList<string> ManualBackupTags()
        {
            IList<string> tags = new List<string>();

            if (this.context.Settings.AdditionalTagging)
            {
                tags.Add(this.context.Settings.ManualSnapshotTag);
            }

            return tags;
        }
        private IList<string> GameplayBackupTags()
        {
            IList<string> tags = new List<string>();

            if (this.context.Settings.AdditionalTagging)
            {
                tags.Add(this.context.Settings.GameplaySnapshotTag);
            }

            return tags;
        }
        private IList<string> GamestoppedBackupTags()
        {
            IList<string> tags = new List<string>();

            if (this.context.Settings.AdditionalTagging)
            {
                tags.Add(this.context.Settings.GameStoppedSnapshotTag);
            }

            return tags;
        }
    }
}
