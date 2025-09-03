using System;
using System.Collections.Generic;

namespace LudusaviRestic
{
    public class GameSpecificSettings
    {
        // When true, any non-null setting here overrides global settings
        public bool OverrideGlobalSettings { get; set; }

        // Backup triggers (null => use global)
        public bool? BackupOnGameStopped { get; set; }
        public bool? BackupDuringGameplay { get; set; }
        public int? GameplayBackupIntervalMinutes { get; set; }
        public bool? BackupOnUninstall { get; set; }

        // Retention overrides
        public bool? UseCustomRetention { get; set; }
        public int? KeepLast { get; set; }
        public int? KeepDaily { get; set; }
        public int? KeepWeekly { get; set; }
        public int? KeepMonthly { get; set; }
        public int? KeepYearly { get; set; }

        // Additional custom tags to add to every backup of this game
        public List<string> CustomTags { get; set; } = new List<string>();
    }
}
