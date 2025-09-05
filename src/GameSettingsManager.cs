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

        private string Loc(string key, string fallback)
        {
            try
            {
                var val = ResourceProvider.GetString(key);
                if (string.IsNullOrWhiteSpace(val) || (val.StartsWith("<!") && val.EndsWith("!>"))) return fallback;
                return val;
            }
            catch { return fallback; }
        }

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

                var header = string.Format(Loc("LOCLuduRestPerGameHeader", "Backup Settings for: {0}"), game.Name);
                var status = hasOverrides
                    ? Loc("LOCLuduRestPerGameStatusUsingCustom", "Using custom settings")
                    : Loc("LOCLuduRestPerGameStatusUsingGlobal", "Using global settings");
                var message = header + "\n\n" + string.Format("{0}: {1}", ResourceProvider.GetString("LOCStatusLabel") ?? "Status", status) + "\n\n";

                if (hasOverrides)
                {
                    message += Loc("LOCLuduRestPerGameCustomSettingsHeader", "Custom Settings:") + "\n";
                    message += string.Format(Loc("LOCLuduRestPerGameBackupOnStop", "• Backup on game stop: {0}"), (gameSettings.BackupOnGameStopped?.ToString() ?? "Global")) + "\n";
                    message += string.Format(Loc("LOCLuduRestPerGameBackupDuringGameplay", "• Backup during gameplay: {0}"), (gameSettings.BackupDuringGameplay?.ToString() ?? "Global")) + "\n";
                    message += string.Format(Loc("LOCLuduRestPerGameBackupOnUninstall", "• Backup on uninstall: {0}"), (gameSettings.BackupOnUninstall?.ToString() ?? "Global")) + "\n";
                    if (gameSettings.UseCustomRetention == true)
                    {
                        message += string.Format(Loc("LOCLuduRestPerGameCustomRetention", "• Custom retention: Last {0}, Daily {1}, Weekly {2}, Monthly {3}, Yearly {4}"),
                            gameSettings.KeepLast ?? settings.KeepLast,
                            gameSettings.KeepDaily ?? settings.KeepDaily,
                            gameSettings.KeepWeekly ?? settings.KeepWeekly,
                            gameSettings.KeepMonthly ?? settings.KeepMonthly,
                            gameSettings.KeepYearly ?? settings.KeepYearly) + "\n";
                    }
                    if (gameSettings.CustomTags?.Any() == true)
                    {
                        message += string.Format(Loc("LOCLuduRestPerGameCustomTags", "• Custom tags: {0}"), string.Join(", ", gameSettings.CustomTags)) + "\n";
                    }
                }

                if (hasOverrides)
                {
                    var resetResult = api.Dialogs.ShowMessage(
                        message + "\n\n" + Loc("LOCLuduRestPerGamePromptReset", "Reset to global settings?"),
                        Loc("LOCLuduRestPerGameDialogTitle", "Game Backup Settings"),
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Question);
                    if (resetResult == System.Windows.MessageBoxResult.Yes)
                    {
                        settings.RemoveGameSettings(game.Id);
                        api.Dialogs.ShowMessage(Loc("LOCLuduRestPerGameSettingsResetMessage", "Game backup settings have been reset to use global defaults."), Loc("LOCLuduRestPerGameSettingsResetTitle", "Settings Reset"));
                        return;
                    }
                    else if (resetResult == System.Windows.MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                var configResult = api.Dialogs.ShowMessage(
                    Loc("LOCLuduRestPerGamePromptConfigure", "Configure custom settings for this game?"),
                    Loc("LOCLuduRestPerGameDialogTitle", "Game Backup Settings"),
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
                    string.Format(Loc("LOCLuduRestPerGamePromptOverride", "Create custom backup settings for '{0}'?"), game.Name),
                    Loc("LOCLuduRestPerGameOverrideTitle", "Override Global Settings?"),
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);
                if (overrideResult == System.Windows.MessageBoxResult.Cancel)
                    return;
                if (overrideResult == System.Windows.MessageBoxResult.No)
                {
                    settings.RemoveGameSettings(game.Id);
                    api.Dialogs.ShowMessage(Loc("LOCLuduRestPerGameUseGlobalMessage", "Game will use global backup settings."), Loc("LOCLuduRestPerGameSettingsSavedTitle", "Settings Saved"));
                    return;
                }
                gs.OverrideGlobalSettings = true;

                var stopResult = api.Dialogs.ShowMessage(
                    Loc("LOCLuduRestPerGamePromptBackupOnStop", "Backup when game stops? (Yes/No) Cancel = global"),
                    Loc("LOCLuduRestPerGamePromptBackupOnStopTitle", "Backup on Game Stop"),
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);
                if (stopResult == System.Windows.MessageBoxResult.Yes) gs.BackupOnGameStopped = true;
                else if (stopResult == System.Windows.MessageBoxResult.No) gs.BackupOnGameStopped = false;
                else gs.BackupOnGameStopped = null;

                var gameplayResult = api.Dialogs.ShowMessage(
                    Loc("LOCLuduRestPerGamePromptBackupDuringGameplay", "Backup during gameplay? (Yes/No) Cancel = global"),
                    Loc("LOCLuduRestPerGamePromptBackupDuringGameplayTitle", "Backup During Gameplay"),
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);
                if (gameplayResult == System.Windows.MessageBoxResult.Yes)
                {
                    gs.BackupDuringGameplay = true;
                    var interval = api.Dialogs.SelectString(Loc("LOCLuduRestPerGameIntervalPrompt", "Interval (minutes):"), Loc("LOCLuduRestPerGameIntervalTitle", "Backup Interval"), (gs.GameplayBackupIntervalMinutes ?? settings.GameplayBackupIntervalMinutes).ToString());
                    if (int.TryParse(interval.SelectedString, out int mins) && mins > 0) gs.GameplayBackupIntervalMinutes = mins;
                }
                else if (gameplayResult == System.Windows.MessageBoxResult.No) gs.BackupDuringGameplay = false; else gs.BackupDuringGameplay = null;

                var uninstallResult = api.Dialogs.ShowMessage(
                    Loc("LOCLuduRestPerGamePromptBackupOnUninstall", "Backup on uninstall? (Yes/No) Cancel = global"),
                    Loc("LOCLuduRestPerGamePromptBackupOnUninstallTitle", "Backup on Uninstall"),
                    System.Windows.MessageBoxButton.YesNoCancel,
                    System.Windows.MessageBoxImage.Question);
                if (uninstallResult == System.Windows.MessageBoxResult.Yes) gs.BackupOnUninstall = true;
                else if (uninstallResult == System.Windows.MessageBoxResult.No) gs.BackupOnUninstall = false; else gs.BackupOnUninstall = null;

                var retentionResult = api.Dialogs.ShowMessage(
                    Loc("LOCLuduRestPerGamePromptCustomRetention", "Custom retention policy?"),
                    Loc("LOCLuduRestPerGamePromptCustomRetentionTitle", "Custom Retention"),
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                if (retentionResult == System.Windows.MessageBoxResult.Yes)
                {
                    gs.UseCustomRetention = true;
                    ConfigureRetentionSettings(gs);
                }

                var tagsResult = api.Dialogs.ShowMessage(
                    Loc("LOCLuduRestPerGamePromptCustomTags", "Add custom tags?"),
                    Loc("LOCLuduRestPerGamePromptCustomTagsTitle", "Custom Tags"),
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                if (tagsResult == System.Windows.MessageBoxResult.Yes)
                {
                    var existing = gs.CustomTags?.Any() == true ? string.Join(", ", gs.CustomTags) : "";
                    var tagInput = api.Dialogs.SelectString(Loc("LOCLuduRestPerGamePromptTagsInput", "Tags (comma separated):"), Loc("LOCLuduRestPerGamePromptCustomTagsTitle", "Custom Tags"), existing);
                    if (!string.IsNullOrWhiteSpace(tagInput.SelectedString))
                    {
                        var tags = tagInput.SelectedString.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
                        if (tags.Any()) gs.CustomTags = tags;
                    }
                }

                settings.SetGameSettings(game.Id, gs);
                api.Dialogs.ShowMessage(string.Format(Loc("LOCLuduRestPerGameSettingsSavedMessage", "Custom backup settings saved for '{0}'."), game.Name), Loc("LOCLuduRestPerGameSettingsSavedTitle", "Settings Saved"));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error configuring game settings");
                api.Dialogs.ShowErrorMessage(string.Format(Loc("LOCLuduRestPerGameErrorConfiguring", "Error configuring settings: {0}"), ex.Message));
            }
        }

        private void ConfigureRetentionSettings(GameSpecificSettings gs)
        {
            try
            {
                var fields = new (string label, Action<int> set, int current)[]
                {
                    (Loc("LOCLuduRestPerGameRetentionKeepLastPrompt", "Keep Last (0 = disable):"), v => gs.KeepLast = v, gs.KeepLast ?? settings.KeepLast),
                    (Loc("LOCLuduRestPerGameRetentionKeepDailyPrompt", "Keep Daily (0 = disable):"), v => gs.KeepDaily = v, gs.KeepDaily ?? settings.KeepDaily),
                    (Loc("LOCLuduRestPerGameRetentionKeepWeeklyPrompt", "Keep Weekly (0 = disable):"), v => gs.KeepWeekly = v, gs.KeepWeekly ?? settings.KeepWeekly),
                    (Loc("LOCLuduRestPerGameRetentionKeepMonthlyPrompt", "Keep Monthly (0 = disable):"), v => gs.KeepMonthly = v, gs.KeepMonthly ?? settings.KeepMonthly),
                    (Loc("LOCLuduRestPerGameRetentionKeepYearlyPrompt", "Keep Yearly (0 = disable):"), v => gs.KeepYearly = v, gs.KeepYearly ?? settings.KeepYearly)
                };
                foreach (var f in fields)
                {
                    var input = api.Dialogs.SelectString(f.label, Loc("LOCLuduRestPerGamePromptCustomRetentionTitle", "Retention Settings"), f.current.ToString());
                    if (int.TryParse(input.SelectedString, out int val) && val >= 0)
                    {
                        f.set(val);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error configuring retention settings");
                api.Dialogs.ShowErrorMessage(string.Format(Loc("LOCLuduRestPerGameErrorRetention", "Error configuring retention: {0}"), ex.Message));
            }
        }
    }
}
