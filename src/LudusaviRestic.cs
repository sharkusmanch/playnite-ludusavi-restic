using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading;
using System.IO;
using System.Reflection;

namespace LudusaviRestic
{
    public class LudusaviRestic : GenericPlugin
    {
        public LudusaviResticSettings settings { get; set; }
        private static readonly ILogger logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("e9861c36-68a8-4654-8071-a9c50612bc24");

        private ResticBackupManager manager;
        private Timer timer;

        public LudusaviRestic(IPlayniteAPI api) : base(api)
        {
            this.settings = new LudusaviResticSettings(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            this.manager = new ResticBackupManager(this.settings, this.PlayniteApi);

            // Initialize localization
            InitializeLocalization();
        }

        private void InitializeLocalization()
        {
            try
            {
                // Try to load the en_US localization file directly
                var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var localizationFile = Path.Combine(pluginDir, "Localization", "en_US.xaml");

                logger.Debug($"Looking for localization file at: {localizationFile}");

                if (File.Exists(localizationFile))
                {
                    logger.Debug("Localization file exists, attempting to load...");
                    // The file exists, Playnite should be able to load it automatically
                }
                else
                {
                    logger.Error($"Localization file not found at: {localizationFile}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error checking localization file");
            }
        }

        private string GetLocalizedString(string key, string fallback = null)
        {
            try
            {
                var result = PlayniteApi.Resources.GetString(key);
                if (string.IsNullOrEmpty(result) || result.StartsWith("<") && result.EndsWith(">"))
                {
                    // If the key isn't found or shows as <KEY>, return fallback
                    return fallback ?? key;
                }
                return result;
            }
            catch
            {
                return fallback ?? key;
            }
        }

        private void LocalizeTags()
        {
            if (PlayniteApi.Database.Tags.Get(this.settings.ExcludeTagID) is Tag excludeTag)
            {
                excludeTag.Name = GetLocalizedString("LOCLuduRestBackupExcludeTag", "LOCLuduRestBackupExcludeTag");
            }
            if (PlayniteApi.Database.Tags.Get(this.settings.IncludeTagID) is Tag includeTag)
            {
                includeTag.Name = GetLocalizedString("LOCLuduRestBackupIncludeTag", "LOCLuduRestBackupIncludeTag");
            }
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs menuArgs)
        {
            // Debug: Check if we can access localization
            logger.Debug($"Trying to get LOCLuduRestBackupGM: '{GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM")}'");
            logger.Debug($"Trying to get LOCLuduRestSetupWizard: '{GetLocalizedString("LOCLuduRestSetupWizard", "LOCLuduRestSetupWizard")}'");

            return new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestSetupWizard", "Setup wizard"),
                    MenuSection = "@" + GetLocalizedString("LOCLuduRestBackupGM", "LudusaviRestic") + "|" + GetLocalizedString("LOCLuduRestSetupSection", "Setup"),
                    Action = args => {
                        RunSetupWizard();
                    }
                },
                new MainMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestBackupAllGames", "Backup all games"),
                    MenuSection = "@" + GetLocalizedString("LOCLuduRestBackupGM", "LudusaviRestic"),
                    Action = args => {
                        if (CheckConfiguration())
                        {
                            this.manager.BackupAllGames();
                        }
                    }
                },
                new MainMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestBrowseBackupSnapshots", "LOCLuduRestBrowseBackupSnapshots"),
                    MenuSection = "@" + GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM"),
                    Action = args => {
                        if (CheckConfiguration())
                        {
                            var context = new BackupContext(this.PlayniteApi, this.settings);
                            var window = new BackupBrowserWindow(context);
                            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            window.ShowDialog();
                        }
                    }
                },
                new MainMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestMaintenanceAll", "LOCLuduRestMaintenanceAll"),
                    MenuSection = "@" + GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM") + "|" + GetLocalizedString("LOCLuduRestMaintenanceSection", "LOCLuduRestMaintenanceSection"),
                    Action = args => {
                        if (CheckConfiguration())
                        {
                            RunFullMaintenance();
                        }
                    }
                },
                new MainMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestMaintenanceVerify", "LOCLuduRestMaintenanceVerify"),
                    MenuSection = "@" + GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM") + "|" + GetLocalizedString("LOCLuduRestMaintenanceSection", "LOCLuduRestMaintenanceSection"),
                    Action = args => {
                        if (CheckConfiguration())
                        {
                            RunVerifyMaintenance();
                        }
                    }
                },
                new MainMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestMaintenanceDeleteOld", "LOCLuduRestMaintenanceDeleteOld"),
                    MenuSection = "@" + GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM") + "|" + GetLocalizedString("LOCLuduRestMaintenanceSection", "LOCLuduRestMaintenanceSection"),
                    Action = args => {
                        if (CheckConfiguration())
                        {
                            RunRetentionMaintenance();
                        }
                    }
                },
                new MainMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestMaintenancePrune", "LOCLuduRestMaintenancePrune"),
                    MenuSection = "@" + GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM") + "|" + GetLocalizedString("LOCLuduRestMaintenanceSection", "LOCLuduRestMaintenanceSection"),
                    Action = args => {
                        if (CheckConfiguration())
                        {
                            RunPruneMaintenance();
                        }
                    }
                }
            };
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs menuArgs)
        {
            return new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestBackupGMCreate", "LOCLuduRestBackupGMCreate"),
                    MenuSection = GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM"),

                    Action = args => {
                        if (CheckConfiguration() && args.Games.Count > 0)
                        {
                            this.manager.PerformManualBackup(args.Games);
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestViewBackupSnapshots", "LOCLuduRestViewBackupSnapshots"),
                    MenuSection = GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM"),

                    Action = args => {
                        if (CheckConfiguration() && args.Games.Count > 0)
                        {
                            var game = args.Games.First();
                            var context = new BackupContext(this.PlayniteApi, this.settings);
                            var window = new BackupBrowserWindow(context, game.Name);
                            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            window.ShowDialog();
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestBackupGMIncludeAdd", "LOCLuduRestBackupGMIncludeAdd"),
                    MenuSection = GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            AddTag(game, this.settings.IncludeTagID);
                            PlayniteApi.Database.Games.Update(game);
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestBackupGMIncludeRemove", "LOCLuduRestBackupGMIncludeRemove"),
                    MenuSection = GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            RemoveTag(game, this.settings.IncludeTagID);
                            PlayniteApi.Database.Games.Update(game);
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestBackupGMExcludeAdd", "LOCLuduRestBackupGMExcludeAdd"),
                    MenuSection = GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            AddTag(game, this.settings.ExcludeTagID);
                            PlayniteApi.Database.Games.Update(game);
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestBackupGMExcludeRemove", "LOCLuduRestBackupGMExcludeRemove"),
                    MenuSection = GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            RemoveTag(game, this.settings.ExcludeTagID);
                            PlayniteApi.Database.Games.Update(game);
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestSetBackupInterval", "Set backup interval"),
                    MenuSection = GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM"),

                    Action = args => {
                        if (args.Games.Count == 1)
                        {
                            var game = args.Games.First();
                            var key = game.Id.ToString();
                            var effective = settings.GetEffectiveInterval(game.Id);
                            var prompt = string.Format(
                                GetLocalizedString("LOCLuduRestSetBackupIntervalPrompt",
                                    "Enter backup interval in minutes for {0} (current: {1}, global default: {2}). Leave blank to remove override."),
                                game.Name, effective, settings.GameplayBackupInterval);

                            var existingInterval = settings.GameIntervalOverrides.ContainsKey(key)
                                && settings.GameIntervalOverrides[key].HasIntervalOverride
                                    ? settings.GameIntervalOverrides[key].IntervalMinutes.Value.ToString()
                                    : "";

                            var result = PlayniteApi.Dialogs.SelectString(
                                prompt,
                                GetLocalizedString("LOCLuduRestSetBackupIntervalTitle", "Backup Interval Override"),
                                existingInterval);

                            if (result.Result)
                            {
                                if (string.IsNullOrWhiteSpace(result.SelectedString))
                                {
                                    // Clear interval; remove entry entirely if no retention override either
                                    if (settings.GameIntervalOverrides.ContainsKey(key))
                                    {
                                        var entry = settings.GameIntervalOverrides[key];
                                        entry.IntervalMinutes = null;
                                        if (!entry.HasRetentionOverride)
                                        {
                                            settings.GameIntervalOverrides.Remove(key);
                                        }
                                    }
                                }
                                else
                                {
                                    int val;
                                    if (int.TryParse(result.SelectedString, out val) && val > 0)
                                    {
                                        if (!settings.GameIntervalOverrides.ContainsKey(key))
                                        {
                                            settings.GameIntervalOverrides[key] = new GameOverride(game.Name, val);
                                        }
                                        else
                                        {
                                            settings.GameIntervalOverrides[key].IntervalMinutes = val;
                                            settings.GameIntervalOverrides[key].GameName = game.Name.Replace(",", "_");
                                        }
                                    }
                                }
                                settings.Save();
                            }
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = GetLocalizedString("LOCLuduRestSetRetentionPolicy", "Set retention policy"),
                    MenuSection = GetLocalizedString("LOCLuduRestBackupGM", "LOCLuduRestBackupGM"),

                    Action = args => {
                        if (args.Games.Count == 1)
                        {
                            var game = args.Games.First();
                            var key = game.Id.ToString();
                            var sanitizedName = game.Name.Replace(",", "_");

                            GameOverride existing = null;
                            if (settings.GameIntervalOverrides.ContainsKey(key))
                            {
                                existing = settings.GameIntervalOverrides[key];
                            }

                            var window = new RetentionOverrideWindow(game.Name, settings, existing);
                            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();

                            if (window.ShowDialog() == true)
                            {
                                if (window.Removed)
                                {
                                    // Remove retention override; remove entry entirely if no interval override either
                                    if (settings.GameIntervalOverrides.ContainsKey(key))
                                    {
                                        var entry = settings.GameIntervalOverrides[key];
                                        entry.KeepLast = null;
                                        entry.KeepDaily = null;
                                        entry.KeepWeekly = null;
                                        entry.KeepMonthly = null;
                                        entry.KeepYearly = null;
                                        if (!entry.HasIntervalOverride)
                                        {
                                            settings.GameIntervalOverrides.Remove(key);
                                        }
                                    }
                                }
                                else
                                {
                                    if (!settings.GameIntervalOverrides.ContainsKey(key))
                                    {
                                        settings.GameIntervalOverrides[key] = new GameOverride(sanitizedName, 0);
                                    }

                                    var over = settings.GameIntervalOverrides[key];
                                    over.GameName = sanitizedName;
                                    over.KeepLast = window.KeepLast;
                                    over.KeepDaily = window.KeepDaily;
                                    over.KeepWeekly = window.KeepWeekly;
                                    over.KeepMonthly = window.KeepMonthly;
                                    over.KeepYearly = window.KeepYearly;
                                }
                                settings.Save();
                            }
                        }
                    }
                }
            };
        }

        private bool AddTag(Game game, Guid tagId)
        {
            if (game is Game && tagId != Guid.Empty)
            {
                if (game.TagIds is List<Guid> ids)
                {
                    return ids.AddMissing(tagId);
                }
                else
                {
                    game.TagIds = new List<Guid> { tagId };
                }
            }
            return false;
        }
        private bool RemoveTag(Game game, Guid tagId)
        {
            if (game is Game && tagId != Guid.Empty)
            {
                if (game.TagIds is List<Guid> ids)
                {
                    return ids.Remove(tagId);
                }
            }
            return false;
        }

        private void RunFullMaintenance()
        {
            var context = new BackupContext(this.PlayniteApi, this.settings);

            // Ask if user wants to do a dry run first
            var dryRunResult = PlayniteApi.Dialogs.ShowMessage(
                GetLocalizedString("LOCLuduRestMaintenanceDryRunPrompt", "LOCLuduRestMaintenanceDryRunPrompt"),
                GetLocalizedString("LOCLuduRestFullMaintenance", "LOCLuduRestFullMaintenance"),
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            if (dryRunResult == System.Windows.MessageBoxResult.Cancel)
                return;

            bool performDryRun = dryRunResult == System.Windows.MessageBoxResult.Yes;

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                GetLocalizedString("LOCLuduRestProgressRunningMaintenance", "LOCLuduRestProgressRunningMaintenance"),
                true
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    activateGlobalProgress.Text = GetLocalizedString("LOCLuduRestCheckingRepositoryIntegrity", "LOCLuduRestCheckingRepositoryIntegrity");
                    var checkResult = ResticCommand.Check(context);

                    if (checkResult.ExitCode != 0)
                    {
                        logger.Error($"Repository check failed: {checkResult.StdErr}");
                        PlayniteApi.Dialogs.ShowErrorMessage($"Repository check failed:\n{checkResult.StdErr}");
                        return;
                    }

                    // Perform dry run if requested
                    if (performDryRun)
                    {
                        activateGlobalProgress.Text = GetLocalizedString("LOCLuduRestRunningPruneDryRun", "LOCLuduRestRunningPruneDryRun");
                        var dryRunPruneResult = ResticCommand.PruneDryRun(context);
                        var parsedDryRun = PruneResultParser.ParsePruneOutput(dryRunPruneResult, true);

                        // Show dry run results
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var dryRunWindow = new PruneResultsWindow(parsedDryRun);
                            dryRunWindow.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            dryRunWindow.ShowDialog();

                            // Ask if user wants to proceed
                            var proceedResult = PlayniteApi.Dialogs.ShowMessage(
                                string.Format(GetLocalizedString("LOCLuduRestDryRunCompletedMessage", "LOCLuduRestDryRunCompletedMessage"), parsedDryRun.SnapshotsDeleted),
                                GetLocalizedString("LOCLuduRestProceedWithPruning", "LOCLuduRestProceedWithPruning"),
                                System.Windows.MessageBoxButton.YesNo,
                                System.Windows.MessageBoxImage.Question);

                            if (proceedResult != System.Windows.MessageBoxResult.Yes)
                                return;
                        });
                    }

                    // Perform actual prune
                    activateGlobalProgress.Text = GetLocalizedString("LOCLuduRestPruningUnusedData", "LOCLuduRestPruningUnusedData");
                    var pruneResult = ResticCommand.Prune(context);
                    var parsedResult = PruneResultParser.ParsePruneOutput(pruneResult, false);

                    if (pruneResult.ExitCode == 0)
                    {
                        // Show detailed results
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var resultsWindow = new PruneResultsWindow(parsedResult);
                            resultsWindow.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            resultsWindow.ShowDialog();
                        });

                        PlayniteApi.Dialogs.ShowMessage(GetLocalizedString("LOCLuduRestMaintenanceCompletedSuccessfully", "LOCLuduRestMaintenanceCompletedSuccessfully"), GetLocalizedString("LOCLuduRestMaintenanceComplete", "LOCLuduRestMaintenanceComplete"));
                    }
                    else
                    {
                        logger.Error($"Prune operation failed: {pruneResult.StdErr}");
                        PlayniteApi.Dialogs.ShowErrorMessage($"Prune operation failed:\n{pruneResult.StdErr}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error during full maintenance");
                    PlayniteApi.Dialogs.ShowErrorMessage($"Error during maintenance: {ex.Message}");
                }
            }, globalProgressOptions);
        }

        private void RunVerifyMaintenance()
        {
            var context = new BackupContext(this.PlayniteApi, this.settings);

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                GetLocalizedString("LOCLuduRestProgressVerifyingRepository", "LOCLuduRestProgressVerifyingRepository"),
                true
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    activateGlobalProgress.Text = GetLocalizedString("LOCLuduRestCheckingRepositoryIntegrityWithData", "LOCLuduRestCheckingRepositoryIntegrityWithData");
                    var checkResult = ResticCommand.CheckWithData(context);

                    if (checkResult.ExitCode == 0)
                    {
                        PlayniteApi.Dialogs.ShowMessage(GetLocalizedString("LOCLuduRestVerificationCompletedSuccessfully", "LOCLuduRestVerificationCompletedSuccessfully"), GetLocalizedString("LOCLuduRestVerificationComplete", "LOCLuduRestVerificationComplete"));
                    }
                    else
                    {
                        logger.Error($"Repository verification failed: {checkResult.StdErr}");
                        PlayniteApi.Dialogs.ShowErrorMessage($"Repository verification failed:\n{checkResult.StdErr}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error during repository verification");
                    PlayniteApi.Dialogs.ShowErrorMessage($"Error during verification: {ex.Message}");
                }
            }, globalProgressOptions);
        }

        private void RunRetentionMaintenance()
        {
            var context = new BackupContext(this.PlayniteApi, this.settings);

            if (!this.settings.EnableRetentionPolicy)
            {
                PlayniteApi.Dialogs.ShowMessage(GetLocalizedString("LOCLuduRestRetentionPolicyDisabledMessage", "LOCLuduRestRetentionPolicyDisabledMessage"), GetLocalizedString("LOCLuduRestRetentionPolicyDisabled", "LOCLuduRestRetentionPolicyDisabled"));
                return;
            }

            // Count per-game overrides with retention
            int overrideCount = 0;
            foreach (var kvp in this.settings.GameIntervalOverrides)
            {
                if (kvp.Value.HasRetentionOverride)
                    overrideCount++;
            }

            // Show confirmation dialog with retention policy details and ask about dry run
            var message = string.Format(GetLocalizedString("LOCLuduRestRetentionPolicyDetails", "LOCLuduRestRetentionPolicyDetails"),
                         this.settings.KeepLast, this.settings.KeepDaily, this.settings.KeepWeekly,
                         this.settings.KeepMonthly, this.settings.KeepYearly);

            if (overrideCount > 0)
            {
                message += "\n\n" + string.Format(
                    GetLocalizedString("LOCLuduRestRetentionOverrideCount", "{0} game(s) have per-game retention overrides."),
                    overrideCount);
            }

            var dryRunResult = PlayniteApi.Dialogs.ShowMessage(
                message,
                GetLocalizedString("LOCLuduRestRetentionPolicy", "LOCLuduRestRetentionPolicy"),
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            if (dryRunResult == System.Windows.MessageBoxResult.Cancel)
                return;

            bool performDryRun = dryRunResult == System.Windows.MessageBoxResult.Yes;

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                GetLocalizedString("LOCLuduRestApplyingRetentionPolicy", "LOCLuduRestApplyingRetentionPolicy"),
                true
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    // Step 1: List snapshots and extract game tags
                    activateGlobalProgress.Text = GetLocalizedString("LOCLuduRestListingSnapshots", "Listing snapshots...");
                    var snapshotsResult = ResticCommand.ListSnapshots(context);
                    if (snapshotsResult.ExitCode != 0)
                    {
                        logger.Error($"Failed to list snapshots: {snapshotsResult.StdErr}");
                        PlayniteApi.Dialogs.ShowErrorMessage($"Failed to list snapshots:\n{snapshotsResult.StdErr}");
                        return;
                    }

                    var gameTags = ResticCommand.ExtractGameTags(snapshotsResult.StdOut);
                    if (gameTags.Count == 0)
                    {
                        PlayniteApi.Dialogs.ShowMessage(
                            GetLocalizedString("LOCLuduRestNoSnapshotsFound", "No snapshots found in the repository."),
                            GetLocalizedString("LOCLuduRestRetentionPolicy", "LOCLuduRestRetentionPolicy"));
                        return;
                    }

                    // Step 2: Perform dry run if requested
                    if (performDryRun)
                    {
                        activateGlobalProgress.Text = GetLocalizedString("LOCLuduRestRunningRetentionPreview", "LOCLuduRestRunningRetentionPreview");
                        var dryRunResults = ResticCommand.ForgetWithPerGameRetention(context, gameTags, true);
                        var parsedDryRun = PruneResultParser.MergeForgetResults(dryRunResults, true);

                        // Show dry run results window
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var dryRunWindow = new PruneResultsWindow(parsedDryRun);
                            dryRunWindow.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            dryRunWindow.ShowDialog();
                        });

                        // Skip confirmation if nothing to delete
                        if (parsedDryRun.SnapshotsDeleted == 0)
                        {
                            PlayniteApi.Dialogs.ShowMessage(
                                GetLocalizedString("LOCLuduRestRetentionNothingToDelete", "No snapshots would be deleted. All snapshots match the current retention policy."),
                                GetLocalizedString("LOCLuduRestRetentionPolicy", "LOCLuduRestRetentionPolicy"));
                            return;
                        }

                        // Ask if user wants to proceed (outside Dispatcher.Invoke — Playnite handles dispatching)
                        var proceedResult = PlayniteApi.Dialogs.ShowMessage(
                            string.Format(GetLocalizedString("LOCLuduRestRetentionPreviewCompleted", "LOCLuduRestRetentionPreviewCompleted"), parsedDryRun.SnapshotsDeleted),
                            GetLocalizedString("LOCLuduRestProceedWithDeletion", "LOCLuduRestProceedWithDeletion"),
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Question);

                        if (proceedResult != System.Windows.MessageBoxResult.Yes)
                            return;
                    }

                    // Step 3: Apply actual retention policy per game
                    activateGlobalProgress.Text = GetLocalizedString("LOCLuduRestApplyingRetentionAndPruning", "LOCLuduRestApplyingRetentionAndPruning");
                    var retentionResults = ResticCommand.ForgetWithPerGameRetention(context, gameTags, false);
                    var parsedResult = PruneResultParser.MergeForgetResults(retentionResults, false);

                    if (parsedResult.Success)
                    {
                        // Show detailed results
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var resultsWindow = new PruneResultsWindow(parsedResult);
                            resultsWindow.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            resultsWindow.ShowDialog();
                        });

                        PlayniteApi.Dialogs.ShowMessage(GetLocalizedString("LOCLuduRestRetentionPolicyCompletedSuccessfully", "LOCLuduRestRetentionPolicyCompletedSuccessfully"), GetLocalizedString("LOCLuduRestRetentionPolicyComplete", "LOCLuduRestRetentionPolicyComplete"));
                    }
                    else
                    {
                        logger.Error("Retention policy application failed for one or more games");
                        PlayniteApi.Dialogs.ShowErrorMessage(GetLocalizedString("LOCLuduRestRetentionPolicyPartialFailure", "Retention policy application failed for one or more games. Check the raw output for details."));
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error during retention policy application");
                    PlayniteApi.Dialogs.ShowErrorMessage($"Error during retention policy: {ex.Message}");
                }
            }, globalProgressOptions);
        }

        private void RunPruneMaintenance()
        {
            var context = new BackupContext(this.PlayniteApi, this.settings);

            // Ask if user wants to do a dry run first
            var dryRunResult = PlayniteApi.Dialogs.ShowMessage(
                GetLocalizedString("LOCLuduRestPruneWarningMessage", "LOCLuduRestPruneWarningMessage"),
                GetLocalizedString("LOCLuduRestPruneRepository", "LOCLuduRestPruneRepository"),
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            if (dryRunResult == System.Windows.MessageBoxResult.Cancel)
                return;

            bool performDryRun = dryRunResult == System.Windows.MessageBoxResult.Yes;

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                GetLocalizedString("LOCLuduRestProgressPruningRepository", "Pruning repository..."),
                true
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    // Perform dry run if requested
                    if (performDryRun)
                    {
                        activateGlobalProgress.Text = GetLocalizedString("LOCLuduRestRunningPruneDryRun", "LOCLuduRestRunningPruneDryRun");
                        var dryRunPruneResult = ResticCommand.PruneDryRun(context);
                        var parsedDryRun = PruneResultParser.ParsePruneOutput(dryRunPruneResult, true);

                        // Show dry run results window
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var dryRunWindow = new PruneResultsWindow(parsedDryRun);
                            dryRunWindow.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            dryRunWindow.ShowDialog();
                        });

                        // Ask if user wants to proceed (outside Dispatcher.Invoke — Playnite handles dispatching)
                        var proceedResult = PlayniteApi.Dialogs.ShowMessage(
                            GetLocalizedString("LOCLuduRestPruneDryRunCompleted", "LOCLuduRestPruneDryRunCompleted"),
                            GetLocalizedString("LOCLuduRestProceedWithPruning", "LOCLuduRestProceedWithPruning"),
                            System.Windows.MessageBoxButton.YesNo,
                            System.Windows.MessageBoxImage.Question);

                        if (proceedResult != System.Windows.MessageBoxResult.Yes)
                            return;
                    }

                    // Perform actual prune
                    activateGlobalProgress.Text = GetLocalizedString("LOCLuduRestPruningUnusedData", "LOCLuduRestPruningUnusedData");
                    var pruneResult = ResticCommand.Prune(context);
                    var parsedResult = PruneResultParser.ParsePruneOutput(pruneResult, false);

                    if (pruneResult.ExitCode == 0)
                    {
                        // Show detailed results
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var resultsWindow = new PruneResultsWindow(parsedResult);
                            resultsWindow.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            resultsWindow.ShowDialog();
                        });

                        PlayniteApi.Dialogs.ShowMessage(GetLocalizedString("LOCLuduRestPruneCompletedSuccessfully", "LOCLuduRestPruneCompletedSuccessfully"), GetLocalizedString("LOCLuduRestPruneComplete", "LOCLuduRestPruneComplete"));
                    }
                    else
                    {
                        logger.Error($"Prune operation failed: {pruneResult.StdErr}");
                        PlayniteApi.Dialogs.ShowErrorMessage($"Prune operation failed:\n{pruneResult.StdErr}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error during prune operation");
                    PlayniteApi.Dialogs.ShowErrorMessage($"Error during prune: {ex.Message}");
                }
            }, globalProgressOptions);
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            LocalizeTags();

            // Check if this is the first run or if configuration is incomplete
            if (!CheckConfigurationSilent())
            {
                // Wait a bit for the UI to be ready, then prompt for setup
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(() =>
                    {
                        var result = PlayniteApi.Dialogs.ShowMessage(
                            GetLocalizedString("LOCLuduRestWelcomeMessage", "LOCLuduRestWelcomeMessage"),
                            GetLocalizedString("LOCLuduRestWelcomeSetupRequired", "LOCLuduRestWelcomeSetupRequired"),
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            RunSetupWizard();
                        }
                    })
                );
            }
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            if (settings.BackupOnUninstall)
            {
                this.manager.PerformBackup(args.Game);
            }
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (args?.Game is null) return;

            if (settings.BackupDuringGameplay)
            {
                var interval = settings.GetEffectiveInterval(args.Game.Id);
                this.timer = new Timer(GameplayBackupTimerElapsed, args.Game,
                    interval * 60000,
                    interval * 60000);
            }
        }

        private void GameplayBackupTimerElapsed(Object obj)
        {
            Game game = (Game)obj;
            this.manager.PerformGameplayBackup(game);
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            this.timer?.Dispose();

            if (this.settings.BackupWhenGameStopped)
            {
                string caption = GetLocalizedString("LOCLuduRestGameStoppedPromptCaption", "LOCLuduRestGameStoppedPromptCaption");
                string message = GetLocalizedString("LOCLuduRestGameStoppedPromptMessage", "LOCLuduRestGameStoppedPromptMessage");


                if (this.settings.PromptForGameStoppedTag)
                {
                    StringSelectionDialogResult result = PlayniteApi.Dialogs.SelectString(
                        $"{message}: {args.Game.Name}",
                        caption,
                        ""
                    );

                    if (result.Result)
                    {
                        logger.Debug($"Custom backup tag provided: {result.SelectedString}");
                    }

                    this.manager.PerformGameStoppedBackup(args.Game, result.SelectedString);
                }
                else
                {
                    this.manager.PerformGameStoppedBackup(args.Game);
                }
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new LudusaviResticSettingsView(this);
        }

        private bool CheckConfiguration()
        {
            // Check if basic configuration is complete
            if (string.IsNullOrWhiteSpace(settings.ResticExecutablePath) ||
                string.IsNullOrWhiteSpace(settings.ResticRepository) ||
                string.IsNullOrWhiteSpace(settings.ResticPassword))
            {
                var result = PlayniteApi.Dialogs.ShowMessage(
                    GetLocalizedString("LOCLuduRestNotConfiguredMessage", "LOCLuduRestNotConfiguredMessage"),
                    GetLocalizedString("LOCLuduRestConfigurationRequired", "LOCLuduRestConfigurationRequired"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    RunSetupWizard();
                }
                return false;
            }

            // Check if restic executable is valid
            if (!ResticUtility.IsValidResticExecutable(settings.ResticExecutablePath))
            {
                var result = PlayniteApi.Dialogs.ShowMessage(
                    GetLocalizedString("LOCLuduRestInvalidResticMessage", "LOCLuduRestInvalidResticMessage"),
                    GetLocalizedString("LOCLuduRestInvalidConfiguration", "LOCLuduRestInvalidConfiguration"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    RunSetupWizard();
                }
                return false;
            }

            return true;
        }

        private bool CheckConfigurationSilent()
        {
            // Silent check without showing dialogs - used for initial setup detection
            if (string.IsNullOrWhiteSpace(settings.ResticExecutablePath) ||
                string.IsNullOrWhiteSpace(settings.ResticRepository) ||
                string.IsNullOrWhiteSpace(settings.ResticPassword))
            {
                return false;
            }

            // Check if restic executable is valid
            if (!ResticUtility.IsValidResticExecutable(settings.ResticExecutablePath))
            {
                return false;
            }

            return true;
        }

        private void RunSetupWizard()
        {
            try
            {
                var context = new BackupContext(PlayniteApi, settings);
                var wizard = new SetupWizardWindow(context);
                wizard.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();

                if (wizard.ShowDialog() == true && wizard.SetupCompleted)
                {
                    // Update settings with the result from the wizard
                    var newSettings = wizard.ResultSettings;
                    settings.LudusaviExecutablePath = newSettings.LudusaviExecutablePath;
                    settings.ResticExecutablePath = newSettings.ResticExecutablePath;
                    settings.ResticRepository = newSettings.ResticRepository;
                    settings.ResticPassword = newSettings.ResticPassword;
                    settings.Save();

                    PlayniteApi.Dialogs.ShowMessage(
                        GetLocalizedString("LOCLuduRestSetupCompletedMessage", "LOCLuduRestSetupCompletedMessage"),
                        GetLocalizedString("LOCLuduRestSetupComplete", "LOCLuduRestSetupComplete"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error running setup wizard");
                PlayniteApi.Dialogs.ShowErrorMessage($"Error running setup wizard: {ex.Message}");
            }
        }
    }
}
