using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LudusaviRestic
{
    public class GameOverrideSettingsViewModel : INotifyPropertyChanged
    {
        private readonly LudusaviResticSettings global;
        private readonly Game game;
        public string GameName => game.Name;

        // Localization helper with robust fallback (ResourceProvider may return null OR empty string)
        private string Loc(string key, string fallback)
        {
            try
            {
                var val = ResourceProvider.GetString(key);
                // Playnite shows missing keys as <!KEY!>
                if (string.IsNullOrWhiteSpace(val) || (val.StartsWith("<!") && val.EndsWith("!>")))
                {
                    return fallback;
                }
                return val;
            }
            catch
            {
                return fallback;
            }
        }

        public string L_GameLabel => Loc("LOCLuduRestPerGameGameLabel", "Game:");
        public string L_WindowTitle => Loc("LOCLuduRestPerGameWindowTitle", "Per-Game Backup Settings");
        public string L_EnableCustom => Loc("LOCLuduRestPerGameEnableCustom", "Enable custom settings for this game");
        public string L_GroupBackupTriggers => Loc("LOCLuduRestPerGameGroupBackupTriggers", "Backup Triggers");
        public string L_BackupOnStop => Loc("LOCLuduRestPerGameBackupOnStopLabel", "Backup when game stops");
        public string L_BackupDuringGameplay => Loc("LOCLuduRestPerGameBackupDuringGameplayLabel", "Backup during gameplay");
        public string L_IntervalMinutes => Loc("LOCLuduRestPerGameIntervalMinutes", "Interval (minutes):");
        public string L_BackupOnUninstall => Loc("LOCLuduRestPerGameBackupOnUninstallLabel", "Backup on uninstall");
        public string L_GroupRetention => Loc("LOCLuduRestPerGameGroupRetention", "Retention Overrides");
        public string L_UseCustomRetention => Loc("LOCLuduRestPerGameUseCustomRetention", "Use custom retention for this game");
        public string L_KeepLast => Loc("LOCLuduRestPerGameKeepLast", "Keep Last:");
        public string L_KeepDaily => Loc("LOCLuduRestPerGameKeepDaily", "Daily (days):");
        public string L_KeepWeekly => Loc("LOCLuduRestPerGameKeepWeekly", "Weekly (weeks):");
        public string L_KeepMonthly => Loc("LOCLuduRestPerGameKeepMonthly", "Monthly (months):");
        public string L_KeepYearly => Loc("LOCLuduRestPerGameKeepYearly", "Yearly (years):");
        public string L_GroupCustomTags => Loc("LOCLuduRestPerGameGroupCustomTags", "Custom Tags");
        public string L_CustomTagsHelp => Loc("LOCLuduRestPerGameCustomTagsHelp", "Comma separated tags to always add to this game's backups:");
        public string L_Reset => Loc("LOCLuduRestPerGameReset", "Reset");
        public string L_Save => Loc("LOCLuduRestPerGameSave", "Save");
        public string L_Cancel => Loc("LOCLuduRestPerGameCancel", "Cancel");

        private bool overrideGlobalSettings;
        public bool OverrideGlobalSettings { get => overrideGlobalSettings; set { overrideGlobalSettings = value; OnPropertyChanged(); } }

        public bool BackupOnGameStopped { get => backupOnGameStopped; set { backupOnGameStopped = value; OnPropertyChanged(); } }
        private bool backupOnGameStopped;
        public bool BackupDuringGameplay { get => backupDuringGameplay; set { backupDuringGameplay = value; OnPropertyChanged(); } }
        private bool backupDuringGameplay;
        public int GameplayBackupIntervalMinutes { get => gameplayInterval; set { gameplayInterval = value; OnPropertyChanged(); } }
        private int gameplayInterval;
        public bool BackupOnUninstall { get => backupOnUninstall; set { backupOnUninstall = value; OnPropertyChanged(); } }
        private bool backupOnUninstall;

        public bool UseCustomRetention { get => useCustomRetention; set { useCustomRetention = value; OnPropertyChanged(); } }
        private bool useCustomRetention;
        public int KeepLast { get => keepLast; set { keepLast = value; OnPropertyChanged(); } }
        private int keepLast;
        public int KeepDaily { get => keepDaily; set { keepDaily = value; OnPropertyChanged(); } }
        private int keepDaily;
        public int KeepWeekly { get => keepWeekly; set { keepWeekly = value; OnPropertyChanged(); } }
        private int keepWeekly;
        public int KeepMonthly { get => keepMonthly; set { keepMonthly = value; OnPropertyChanged(); } }
        private int keepMonthly;
        public int KeepYearly { get => keepYearly; set { keepYearly = value; OnPropertyChanged(); } }
        private int keepYearly;

        public string CustomTagsText { get => customTagsText; set { customTagsText = value; OnPropertyChanged(); } }
        private string customTagsText;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public GameOverrideSettingsViewModel(Game game, LudusaviResticSettings global, GameSpecificSettings existing)
        {
            this.global = global;
            this.game = game;
            if (existing?.OverrideGlobalSettings == true)
            {
                OverrideGlobalSettings = true;
                BackupOnGameStopped = existing.BackupOnGameStopped ?? global.BackupWhenGameStopped;
                BackupDuringGameplay = existing.BackupDuringGameplay ?? global.BackupDuringGameplay;
                GameplayBackupIntervalMinutes = existing.GameplayBackupIntervalMinutes ?? global.GameplayBackupIntervalMinutes;
                BackupOnUninstall = existing.BackupOnUninstall ?? global.BackupOnUninstall;
                UseCustomRetention = existing.UseCustomRetention ?? false;
                KeepLast = existing.KeepLast ?? global.KeepLast;
                KeepDaily = existing.KeepDaily ?? global.KeepDaily;
                KeepWeekly = existing.KeepWeekly ?? global.KeepWeekly;
                KeepMonthly = existing.KeepMonthly ?? global.KeepMonthly;
                KeepYearly = existing.KeepYearly ?? global.KeepYearly;
                CustomTagsText = existing.CustomTags != null ? string.Join(", ", existing.CustomTags) : string.Empty;
            }
            else
            {
                ResetToGlobal();
                OverrideGlobalSettings = false;
            }
        }

        public void ResetToGlobal()
        {
            BackupOnGameStopped = global.BackupWhenGameStopped;
            BackupDuringGameplay = global.BackupDuringGameplay;
            GameplayBackupIntervalMinutes = global.GameplayBackupIntervalMinutes;
            BackupOnUninstall = global.BackupOnUninstall;
            UseCustomRetention = false;
            KeepLast = global.KeepLast;
            KeepDaily = global.KeepDaily;
            KeepWeekly = global.KeepWeekly;
            KeepMonthly = global.KeepMonthly;
            KeepYearly = global.KeepYearly;
            CustomTagsText = string.Empty;
        }

        public GameSpecificSettings ToGameSpecificSettings()
        {
            if (!OverrideGlobalSettings)
            {
                return new GameSpecificSettings { OverrideGlobalSettings = false };
            }
            var gs = new GameSpecificSettings
            {
                OverrideGlobalSettings = true,
                BackupOnGameStopped = BackupOnGameStopped,
                BackupDuringGameplay = BackupDuringGameplay,
                GameplayBackupIntervalMinutes = GameplayBackupIntervalMinutes,
                BackupOnUninstall = BackupOnUninstall,
                UseCustomRetention = UseCustomRetention,
                KeepLast = UseCustomRetention ? KeepLast : (int?)null,
                KeepDaily = UseCustomRetention ? KeepDaily : (int?)null,
                KeepWeekly = UseCustomRetention ? KeepWeekly : (int?)null,
                KeepMonthly = UseCustomRetention ? KeepMonthly : (int?)null,
                KeepYearly = UseCustomRetention ? KeepYearly : (int?)null,
                CustomTags = CustomTagsText?.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new System.Collections.Generic.List<string>()
            };
            return gs;
        }
    }
}
