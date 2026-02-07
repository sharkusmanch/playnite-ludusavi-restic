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
            PerformBackup(game, new List<string>(), false);
        }

        internal static bool GameHasTag(Game game, Guid tagId)
        {
            return game.TagIds != null && game.TagIds.Contains(tagId);
        }

        internal static bool ShouldSkipBackup(ExecutionMode mode, Game game, Guid excludeTagId, Guid includeTagId)
        {
            if (mode == ExecutionMode.Exclude && GameHasTag(game, excludeTagId))
            {
                return true;
            }
            if (mode == ExecutionMode.Include && !GameHasTag(game, includeTagId))
            {
                return true;
            }
            return false;
        }

        internal static IList<string> BuildBackupTags(bool additionalTagging, string tagValue)
        {
            IList<string> tags = new List<string>();
            if (additionalTagging)
            {
                tags.Add(tagValue);
            }
            return tags;
        }

        public void PerformBackup(Game game, IList<string> extraTags, bool isManual = false)
        {
            logger.Debug($"Backup #{game.Name}");
            LudusaviResticSettings settings = this.context.Settings;

            if (ShouldSkipBackup(settings.BackupExecutionMode, game, settings.ExcludeTagID, settings.IncludeTagID))
            {
                logger.Info($"Skipping backup of {game.Name}");
                return;
            }

            BackupGameTask task = new BackupGameTask(game, this.semaphore, this.context, extraTags, isManual);
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
            IList<Game> games = new List<Game>();
            games.Add(game);

            PerformManualBackup(games);
        }

        public void PerformManualBackup(IList<Game> games)
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
            foreach (var game in games)
            {
                PerformBackup(game, tags, true);
            }
        }

        public void PerformGameplayBackup(Game game)
        {
            PerformBackup(game, GameplayBackupTags());
        }

        private IList<string> ManualBackupTags()
        {
            return BuildBackupTags(this.context.Settings.AdditionalTagging, this.context.Settings.ManualSnapshotTag);
        }

        private IList<string> GameplayBackupTags()
        {
            return BuildBackupTags(this.context.Settings.AdditionalTagging, this.context.Settings.GameplaySnapshotTag);
        }

        private IList<string> GamestoppedBackupTags()
        {
            return BuildBackupTags(this.context.Settings.AdditionalTagging, this.context.Settings.GameStoppedSnapshotTag);
        }
    }
}
