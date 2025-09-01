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
        }

        private void LocalizeTags()
        {
            if (PlayniteApi.Database.Tags.Get(this.settings.ExcludeTagID) is Tag excludeTag)
            {
                excludeTag.Name = ResourceProvider.GetString("LOCLuduRestBackupExcludeTag");
            }
            if (PlayniteApi.Database.Tags.Get(this.settings.IncludeTagID) is Tag includeTag)
            {
                includeTag.Name = ResourceProvider.GetString("LOCLuduRestBackupIncludeTag");
            }
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs menuArgs)
        {
            return new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Description = "Setup wizard",
                    MenuSection = "@" + ResourceProvider.GetString("LOCLuduRestBackupGM") + "|Setup",
                    Action = args => {
                        RunSetupWizard();
                    }
                },
                new MainMenuItem
                {
                    Description = "Backup all games",
                    MenuSection = "@" + ResourceProvider.GetString("LOCLuduRestBackupGM"),
                    Action = args => {
                        if (CheckConfiguration())
                        {
                            this.manager.BackupAllGames();
                        }
                    }
                },
                new MainMenuItem
                {
                    Description = "Browse backup snapshots",
                    MenuSection = "@" + ResourceProvider.GetString("LOCLuduRestBackupGM"),
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
                    Description = "All (check + prune)",
                    MenuSection = "@" + ResourceProvider.GetString("LOCLuduRestBackupGM") + "|Maintenance",
                    Action = args => {
                        if (CheckConfiguration())
                        {
                            RunFullMaintenance();
                        }
                    }
                },
                new MainMenuItem
                {
                    Description = "Verify",
                    MenuSection = "@" + ResourceProvider.GetString("LOCLuduRestBackupGM") + "|Maintenance",
                    Action = args => {
                        if (CheckConfiguration())
                        {
                            RunVerifyMaintenance();
                        }
                    }
                },
                new MainMenuItem
                {
                    Description = "Delete old snapshots",
                    MenuSection = "@" + ResourceProvider.GetString("LOCLuduRestBackupGM") + "|Maintenance",
                    Action = args => {
                        if (CheckConfiguration())
                        {
                            RunRetentionMaintenance();
                        }
                    }
                },
                new MainMenuItem
                {
                    Description = "Prune unused data",
                    MenuSection = "@" + ResourceProvider.GetString("LOCLuduRestBackupGM") + "|Maintenance",
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
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMCreate"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            this.manager.PerformManualBackup(game);
                        }
                    }
                },
                new GameMenuItem
                {
                    Description = "View backup snapshots",
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

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
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMIncludeAdd"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

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
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMIncludeRemove"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

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
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMExcludeAdd"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

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
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMExcludeRemove"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            RemoveTag(game, this.settings.ExcludeTagID);
                            PlayniteApi.Database.Games.Update(game);
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
                "Would you like to perform a dry run first to see what would be pruned before actually deleting data?",
                "Full Maintenance",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            if (dryRunResult == System.Windows.MessageBoxResult.Cancel)
                return;

            bool performDryRun = dryRunResult == System.Windows.MessageBoxResult.Yes;

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                "Running full maintenance...",
                true
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    activateGlobalProgress.Text = "Checking repository integrity...";
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
                        activateGlobalProgress.Text = "Running prune dry run...";
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
                                $"Dry run completed. {parsedDryRun.SnapshotsDeleted} snapshots would be pruned. Do you want to proceed with the actual pruning?",
                                "Proceed with Pruning?",
                                System.Windows.MessageBoxButton.YesNo,
                                System.Windows.MessageBoxImage.Question);

                            if (proceedResult != System.Windows.MessageBoxResult.Yes)
                                return;
                        });
                    }

                    // Perform actual prune
                    activateGlobalProgress.Text = "Pruning unused data...";
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

                        PlayniteApi.Dialogs.ShowMessage("Full maintenance completed successfully.", "Maintenance Complete");
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
                "Verifying repository...",
                true
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    activateGlobalProgress.Text = "Checking repository integrity with data verification...";
                    var checkResult = ResticCommand.CheckWithData(context);

                    if (checkResult.ExitCode == 0)
                    {
                        PlayniteApi.Dialogs.ShowMessage("Repository verification completed successfully.", "Verification Complete");
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
                PlayniteApi.Dialogs.ShowMessage("Retention policy is disabled. Please enable it in the extension settings to use this feature.", "Retention Policy Disabled");
                return;
            }

            // Show confirmation dialog with retention policy details and ask about dry run
            var message = $"This will delete old snapshots according to your retention policy:\n\n" +
                         $"• Keep last {this.settings.KeepLast} snapshots\n" +
                         $"• Keep daily snapshots for {this.settings.KeepDaily} days\n" +
                         $"• Keep weekly snapshots for {this.settings.KeepWeekly} weeks\n" +
                         $"• Keep monthly snapshots for {this.settings.KeepMonthly} months\n" +
                         $"• Keep yearly snapshots for {this.settings.KeepYearly} years\n\n" +
                         "Would you like to perform a dry run first to see what would be deleted?";

            var dryRunResult = PlayniteApi.Dialogs.ShowMessage(
                message,
                "Apply Retention Policy",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            if (dryRunResult == System.Windows.MessageBoxResult.Cancel)
                return;

            bool performDryRun = dryRunResult == System.Windows.MessageBoxResult.Yes;

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                "Applying retention policy...",
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
                        activateGlobalProgress.Text = "Running retention policy preview...";
                        var dryRunForgetResult = ResticCommand.ForgetWithRetentionDryRun(context);
                        var parsedDryRun = PruneResultParser.ParseForgetOutput(dryRunForgetResult, true);

                        // Show dry run results
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var dryRunWindow = new PruneResultsWindow(parsedDryRun);
                            dryRunWindow.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            dryRunWindow.ShowDialog();

                            // Ask if user wants to proceed
                            var proceedResult = PlayniteApi.Dialogs.ShowMessage(
                                $"Preview completed. {parsedDryRun.SnapshotsDeleted} snapshots would be deleted according to the retention policy. Do you want to proceed with the deletion?",
                                "Proceed with Deletion?",
                                System.Windows.MessageBoxButton.YesNo,
                                System.Windows.MessageBoxImage.Question);

                            if (proceedResult != System.Windows.MessageBoxResult.Yes)
                                return;
                        });
                    }

                    // Apply actual retention policy
                    activateGlobalProgress.Text = "Applying retention policy and pruning...";
                    var retentionResult = ResticCommand.ForgetWithRetention(context);
                    var parsedResult = PruneResultParser.ParseForgetOutput(retentionResult, false);

                    if (retentionResult.ExitCode == 0)
                    {
                        // Show detailed results
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            var resultsWindow = new PruneResultsWindow(parsedResult);
                            resultsWindow.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                            resultsWindow.ShowDialog();
                        });

                        PlayniteApi.Dialogs.ShowMessage("Retention policy applied successfully. Old snapshots have been deleted.", "Retention Policy Complete");
                    }
                    else
                    {
                        logger.Error($"Retention policy application failed: {retentionResult.StdErr}");
                        PlayniteApi.Dialogs.ShowErrorMessage($"Retention policy application failed:\n{retentionResult.StdErr}");
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
                "Pruning will remove unused data blobs from the repository. This operation is irreversible.\n\n" +
                "Would you like to perform a dry run first to see what would be removed before actually deleting data?",
                "Prune Repository",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            if (dryRunResult == System.Windows.MessageBoxResult.Cancel)
                return;

            bool performDryRun = dryRunResult == System.Windows.MessageBoxResult.Yes;

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                "Pruning repository...",
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
                        activateGlobalProgress.Text = "Running prune dry run...";
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
                                $"Dry run completed. This operation would remove unused data from the repository. Do you want to proceed with the actual pruning?",
                                "Proceed with Pruning?",
                                System.Windows.MessageBoxButton.YesNo,
                                System.Windows.MessageBoxImage.Question);

                            if (proceedResult != System.Windows.MessageBoxResult.Yes)
                                return;
                        });
                    }

                    // Perform actual prune
                    activateGlobalProgress.Text = "Pruning unused data...";
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

                        PlayniteApi.Dialogs.ShowMessage("Prune operation completed successfully.", "Prune Complete");
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
                            "Welcome to Ludusavi Restic!\n\nThis appears to be your first time using the extension or your configuration is incomplete. Would you like to run the setup wizard to configure your backup repository?",
                            "Welcome - Setup Required",
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
                this.timer = new Timer(GameplayBackupTimerElapsed, args.Game,
                    settings.GameplayBackupInterval * 60000,
                    settings.GameplayBackupInterval * 60000);
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
                string caption = ResourceProvider.GetString("LOCLuduRestGameStoppedPromptCaption");
                string message = ResourceProvider.GetString("LOCLuduRestGameStoppedPromptMessage");


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
                    "Ludusavi Restic is not configured yet. Would you like to run the setup wizard?",
                    "Configuration Required",
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
                    "The configured restic executable is not valid or accessible. Would you like to run the setup wizard to fix this?",
                    "Invalid Configuration",
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
                        "Setup completed successfully! You can now use all backup features.",
                        "Setup Complete",
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
