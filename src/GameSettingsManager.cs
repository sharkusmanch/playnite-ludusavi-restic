using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LudusaviRestic
{
    public class GameSettingsManager
    {
        private readonly Game game;
        private readonly LudusaviResticSettings settings;
        private readonly IPlayniteAPI api;
        private static readonly ILogger logger = LogManager.GetLogger();

        public GameSettingsManager(Game game, LudusaviResticSettings settings, IPlayniteAPI api)
        {
            this.game = game;
            this.settings = settings;
            this.api = api;
        }

        public void ShowDialog()
        {
            try
            {
                var gameSettings = settings.GetGameSettings(game.Id);
                var hasOverrides = gameSettings?.OverrideGlobalSettings == true;

                var header = string.Format(ResourceProvider.GetString("LOCLuduRestPerGameHeader"), game.Name);
                var status = hasOverrides
                    ? ResourceProvider.GetString("LOCLuduRestPerGameStatusUsingCustom")
                    : ResourceProvider.GetString("LOCLuduRestPerGameStatusUsingGlobal");
                var message = header + "\n\n" + string.Format("{0}: {1}", ResourceProvider.GetString("LOCStatusLabel") ?? "Status", status) + "\n\n";

                if (hasOverrides)
                {
                    message += ResourceProvider.GetString("LOCLuduRestPerGameCustomSettingsHeader") + "\n";
                    message += string.Format(ResourceProvider.GetString("LOCLuduRestPerGameBackupOnStop"), (gameSettings.BackupOnGameStopped?.ToString() ?? "Global")) + "\n";
                    message += string.Format(ResourceProvider.GetString("LOCLuduRestPerGameBackupDuringGameplay"), (gameSettings.BackupDuringGameplay?.ToString() ?? "Global")) + "\n";
                    message += string.Format(ResourceProvider.GetString("LOCLuduRestPerGameBackupOnUninstall"), (gameSettings.BackupOnUninstall?.ToString() ?? "Global")) + "\n";
                    if (gameSettings.UseCustomRetention == true)
                    {
                        message += string.Format(ResourceProvider.GetString("LOCLuduRestPerGameCustomRetention"),
                            gameSettings.KeepLast ?? settings.KeepLast,
                            gameSettings.KeepDaily ?? settings.KeepDaily,
                            gameSettings.KeepWeekly ?? settings.KeepWeekly,
                            gameSettings.KeepMonthly ?? settings.KeepMonthly,
                            gameSettings.KeepYearly ?? settings.KeepYearly) + "\n";
                    }
                    if (gameSettings.CustomTags?.Any() == true)
                    {
                        message += string.Format(ResourceProvider.GetString("LOCLuduRestPerGameCustomTags"), string.Join(", ", gameSettings.CustomTags)) + "\n";
                    }
                }

                if (hasOverrides)
                {
                    var resetResult = api.Dialogs.ShowMessage(
                        message + "\n\n" + ResourceProvider.GetString("LOCLuduRestPerGamePromptReset"),
                        ResourceProvider.GetString("LOCLuduRestPerGameDialogTitle"),
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Question);
                    if (resetResult == System.Windows.MessageBoxResult.Yes)
                    {
                        settings.RemoveGameSettings(game.Id);
                        api.Dialogs.ShowMessage(ResourceProvider.GetString("LOCLuduRestPerGameSettingsResetMessage"), ResourceProvider.GetString("LOCLuduRestPerGameSettingsResetTitle"));
                        return;
                    }
                    else if (resetResult == System.Windows.MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                var configResult = api.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptConfigure"),
                    ResourceProvider.GetString("LOCLuduRestPerGameDialogTitle"),
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                if (configResult == System.Windows.MessageBoxResult.Yes)
                {
                    ConfigureSettings(gameSettings);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error showing game settings dialog for {game.Name}");
                api.Dialogs.ShowErrorMessage(string.Format(ResourceProvider.GetString("LOCLuduRestPerGameErrorOpening"), ex.Message));
            }
        }

        private void ConfigureSettings(GameSpecificSettings existingSettings)
        {
            try
            {
                var gs = existingSettings ?? new GameSpecificSettings();
                var overrideResult = api.Dialogs.ShowMessage(
                    string.Format(ResourceProvider.GetString("LOCLuduRestPerGamePromptOverride"), game.Name),
                    ResourceProvider.GetString("LOCLuduRestPerGameOverrideTitle"),
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);
                if (overrideResult == System.Windows.MessageBoxResult.Cancel)
                    return;
                if (overrideResult == System.Windows.MessageBoxResult.No)
                {
                    settings.RemoveGameSettings(game.Id);
                    api.Dialogs.ShowMessage(ResourceProvider.GetString("LOCLuduRestPerGameUseGlobalMessage"), ResourceProvider.GetString("LOCLuduRestPerGameSettingsSavedTitle"));
                    return;
                }
                gs.OverrideGlobalSettings = true;

                var stopResult = api.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptBackupOnStop"),
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptBackupOnStopTitle"),
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);
                if (stopResult == System.Windows.MessageBoxResult.Yes) gs.BackupOnGameStopped = true;
                else if (stopResult == System.Windows.MessageBoxResult.No) gs.BackupOnGameStopped = false;
                else gs.BackupOnGameStopped = null;

                var gameplayResult = api.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptBackupDuringGameplay"),
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptBackupDuringGameplayTitle"),
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);
                if (gameplayResult == System.Windows.MessageBoxResult.Yes)
                {
                    gs.BackupDuringGameplay = true;
                    var interval = api.Dialogs.SelectString(ResourceProvider.GetString("LOCLuduRestPerGameIntervalPrompt"), ResourceProvider.GetString("LOCLuduRestPerGameIntervalTitle"), (gs.GameplayBackupIntervalMinutes ?? settings.GameplayBackupIntervalMinutes).ToString());
                    if (int.TryParse(interval.SelectedString, out int mins) && mins > 0) gs.GameplayBackupIntervalMinutes = mins;
                }
                else if (gameplayResult == System.Windows.MessageBoxResult.No) gs.BackupDuringGameplay = false; else gs.BackupDuringGameplay = null;

                var uninstallResult = api.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptBackupOnUninstall"),
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptBackupOnUninstallTitle"),
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);
                if (uninstallResult == System.Windows.MessageBoxResult.Yes) gs.BackupOnUninstall = true;
                else if (uninstallResult == System.Windows.MessageBoxResult.No) gs.BackupOnUninstall = false; else gs.BackupOnUninstall = null;

                var retentionResult = api.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptCustomRetention"),
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptCustomRetentionTitle"),
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                if (retentionResult == System.Windows.MessageBoxResult.Yes)
                {
                    gs.UseCustomRetention = true;
                    ConfigureRetentionSettings(gs);
                }

                var tagsResult = api.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptCustomTags"),
                    ResourceProvider.GetString("LOCLuduRestPerGamePromptCustomTagsTitle"),
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                if (tagsResult == System.Windows.MessageBoxResult.Yes)
                {
                    var existing = gs.CustomTags?.Any() == true ? string.Join(", ", gs.CustomTags) : "";
                    var tagInput = api.Dialogs.SelectString(ResourceProvider.GetString("LOCLuduRestPerGamePromptTagsInput"), ResourceProvider.GetString("LOCLuduRestPerGamePromptCustomTagsTitle"), existing);
                    if (!string.IsNullOrWhiteSpace(tagInput.SelectedString))
                    {
                        var tags = tagInput.SelectedString.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
                        if (tags.Any()) gs.CustomTags = tags;
                    }
                }

                settings.SetGameSettings(game.Id, gs);
                api.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCLuduRestPerGameSettingsSavedMessage"), game.Name), ResourceProvider.GetString("LOCLuduRestPerGameSettingsSavedTitle"));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error configuring game settings");
                api.Dialogs.ShowErrorMessage(string.Format(ResourceProvider.GetString("LOCLuduRestPerGameErrorConfiguring"), ex.Message));
            }
        }

        private void ConfigureRetentionSettings(GameSpecificSettings gs)
        {
            try
            {
                var fields = new (string label, Action<int> set, int current)[]
                {
                    (ResourceProvider.GetString("LOCLuduRestPerGameRetentionKeepLastPrompt"), v => gs.KeepLast = v, gs.KeepLast ?? settings.KeepLast),
                    (ResourceProvider.GetString("LOCLuduRestPerGameRetentionKeepDailyPrompt"), v => gs.KeepDaily = v, gs.KeepDaily ?? settings.KeepDaily),
                    (ResourceProvider.GetString("LOCLuduRestPerGameRetentionKeepWeeklyPrompt"), v => gs.KeepWeekly = v, gs.KeepWeekly ?? settings.KeepWeekly),
                    (ResourceProvider.GetString("LOCLuduRestPerGameRetentionKeepMonthlyPrompt"), v => gs.KeepMonthly = v, gs.KeepMonthly ?? settings.KeepMonthly),
                    (ResourceProvider.GetString("LOCLuduRestPerGameRetentionKeepYearlyPrompt"), v => gs.KeepYearly = v, gs.KeepYearly ?? settings.KeepYearly)
                };
                foreach (var f in fields)
                {
                    var input = api.Dialogs.SelectString(f.label, ResourceProvider.GetString("LOCLuduRestPerGamePromptCustomRetentionTitle"), f.current.ToString());
                    if (int.TryParse(input.SelectedString, out int val) && val >= 0)
                    {
                        f.set(val);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error configuring retention settings");
                api.Dialogs.ShowErrorMessage(string.Format(ResourceProvider.GetString("LOCLuduRestPerGameErrorRetention"), ex.Message));
            }
        }
    }
}
