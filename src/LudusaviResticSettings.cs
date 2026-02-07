using Playnite.SDK;
using System.Collections.Generic;
using System;
using System.ComponentModel;

namespace LudusaviRestic
{
    public class LudusaviResticSettings : ISettings, INotifyPropertyChanged
    {
        private readonly LudusaviRestic plugin;
        private static readonly ILogger logger = LogManager.GetLogger();

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string ludusaviExecutablePath = "ludusavi";
        public string LudusaviExecutablePath { get { return ludusaviExecutablePath; } set { ludusaviExecutablePath = value; NotifyPropertyChanged("LudusaviExecutablePath"); } }

        private ExecutionMode backupExecutionMode = ExecutionMode.Exclude;

        public ExecutionMode BackupExecutionMode { get { return backupExecutionMode; } set { backupExecutionMode = value; NotifyPropertyChanged("BackupExecutionMode"); } }

        private Guid excludeTagID = Guid.Empty;
        public Guid ExcludeTagID
        {
            get
            {
                if (plugin != null && excludeTagID == Guid.Empty)
                {
                    excludeTagID = plugin.PlayniteApi.Database.Tags.Add("[LR] Exclude").Id;
                }
                else if (plugin != null && plugin.PlayniteApi.Database.Tags.Get(excludeTagID) == null)
                {
                    excludeTagID = plugin.PlayniteApi.Database.Tags.Add("[LR] Exclude").Id;
                }
                return excludeTagID;
            }
            set => excludeTagID = value;
        }
        private Guid includeTagID = Guid.Empty;
        public Guid IncludeTagID
        {
            get
            {
                if (plugin != null && includeTagID == Guid.Empty)
                {
                    includeTagID = plugin.PlayniteApi.Database.Tags.Add("[LR] Include").Id;
                }
                else if (plugin != null && plugin.PlayniteApi.Database.Tags.Get(includeTagID) == null)
                {
                    includeTagID = plugin.PlayniteApi.Database.Tags.Add("[LR] Include").Id;
                }
                return includeTagID;
            }
            set => includeTagID = value;
        }

        private string resticExecutablePath = "restic";
        public string ResticExecutablePath { get { return resticExecutablePath; } set { resticExecutablePath = value; NotifyPropertyChanged("ResticExecutablePath"); } }
        private string resticRepository;
        public string ResticRepository { get { return resticRepository; } set { resticRepository = value; NotifyPropertyChanged("ResticRepository"); } }
        private string resticPassword;
        public string ResticPassword { get { return resticPassword; } set { resticPassword = value; NotifyPropertyChanged("ResticPassword"); } }
        private string rcloneConfigPath;
        public string RcloneConfigPath { get { return rcloneConfigPath; } set { rcloneConfigPath = value; NotifyPropertyChanged("RcloneConfigPath"); } }
        private string rcloneConfigPassword;
        public string RcloneConfigPassword { get { return rcloneConfigPassword; } set { rcloneConfigPassword = value; NotifyPropertyChanged("RcloneConfigPassword"); } }
        private bool backupDuringGameplay = false;
        public bool BackupDuringGameplay { get { return backupDuringGameplay; } set { backupDuringGameplay = value; ; NotifyPropertyChanged("BackupDuringGameplay"); } }
        private bool additionalTagging = false;
        public bool AdditionalTagging { get { return additionalTagging; } set { additionalTagging = value; ; NotifyPropertyChanged("AdditionalTagging"); } }
        private bool backupWhenGameStopped = true;
        public bool BackupWhenGameStopped { get { return backupWhenGameStopped; } set { backupWhenGameStopped = value; ; NotifyPropertyChanged("BackupWhenGameStopped"); } }
        private bool promptForGameStoppedTag = false;
        public bool PromptForGameStoppedTag { get { return promptForGameStoppedTag; } set { promptForGameStoppedTag = value; ; NotifyPropertyChanged("PromptForGameStoppedTag"); } }
        private bool backupOnUninstall = false;
        public bool BackupOnUninstall { get { return backupOnUninstall; } set { backupOnUninstall = value; ; NotifyPropertyChanged("BackupOnUninstall"); } }
        private string manualSnapshotTag = "manual";
        public string ManualSnapshotTag { get { return manualSnapshotTag; } set { manualSnapshotTag = value; ; NotifyPropertyChanged("ManualSnapshotTag"); } }
        private string gameStoppedSnapshotTag = "game-stopped";
        public string GameStoppedSnapshotTag { get { return gameStoppedSnapshotTag; } set { gameStoppedSnapshotTag = value; ; NotifyPropertyChanged("GameStoppedSnapshotTag"); } }
        private string gameplaySnapshotTag = "gameplay";
        public string GameplaySnapshotTag { get { return gameplaySnapshotTag; } set { gameplaySnapshotTag = value; ; NotifyPropertyChanged("GameplaySnapshotTag"); } }

        private NotificationLevel notificationLevel = NotificationLevel.Summary;
        public NotificationLevel NotificationLevel { get { return notificationLevel; } set { notificationLevel = value; NotifyPropertyChanged("NotificationLevel"); } }

        private bool notifyOnManualBackup = true;
        public bool NotifyOnManualBackup { get { return notifyOnManualBackup; } set { notifyOnManualBackup = value; NotifyPropertyChanged("NotifyOnManualBackup"); } }

        private bool enableRetentionPolicy = false;
        public bool EnableRetentionPolicy { get { return enableRetentionPolicy; } set { enableRetentionPolicy = value; NotifyPropertyChanged("EnableRetentionPolicy"); } }

        private int keepLast = 10;
        public int KeepLast { get { return keepLast; } set { keepLast = value; NotifyPropertyChanged("KeepLast"); } }

        private int keepDaily = 7;
        public int KeepDaily { get { return keepDaily; } set { keepDaily = value; NotifyPropertyChanged("KeepDaily"); } }

        private int keepWeekly = 4;
        public int KeepWeekly { get { return keepWeekly; } set { keepWeekly = value; NotifyPropertyChanged("KeepWeekly"); } }

        private int keepMonthly = 12;
        public int KeepMonthly { get { return keepMonthly; } set { keepMonthly = value; NotifyPropertyChanged("KeepMonthly"); } }

        private int keepYearly = 5;
        public int KeepYearly { get { return keepYearly; } set { keepYearly = value; NotifyPropertyChanged("KeepYearly"); } }

        private Dictionary<string, GameOverride> gameIntervalOverrides = new Dictionary<string, GameOverride>();
        public Dictionary<string, GameOverride> GameIntervalOverrides
        {
            get { return gameIntervalOverrides; }
            set { gameIntervalOverrides = value ?? new Dictionary<string, GameOverride>(); }
        }

        private List<string> errors;

        private int gameplayBackupInterval = 5;
        public int GameplayBackupInterval
        {
            get { return gameplayBackupInterval; }
            set
            {
                string rawValue = value.ToString();
                int intValue;

                bool success = int.TryParse(rawValue, out intValue) && intValue > 0;

                if (success)
                {
                    gameplayBackupInterval = intValue;
                    NotifyPropertyChanged("GameplayBackupInterval");
                }
                else
                {
                    this.errors.Add("Backup interval must be a positive integer");
                }
            }
        }

        public LudusaviResticSettings()
        {
        }

        public LudusaviResticSettings(LudusaviRestic plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;
            Load();
        }
        private void Load()
        {
            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<LudusaviResticSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                LudusaviExecutablePath = savedSettings.LudusaviExecutablePath;
                ResticExecutablePath = savedSettings.ResticExecutablePath;
                ResticRepository = savedSettings.ResticRepository;
                ResticPassword = savedSettings.ResticPassword;
                RcloneConfigPath = savedSettings.RcloneConfigPath;
                RcloneConfigPassword = savedSettings.RcloneConfigPassword;
                BackupDuringGameplay = savedSettings.BackupDuringGameplay;
                GameplayBackupInterval = savedSettings.GameplayBackupInterval;
                AdditionalTagging = savedSettings.AdditionalTagging;
                ManualSnapshotTag = savedSettings.ManualSnapshotTag;
                GameStoppedSnapshotTag = savedSettings.GameStoppedSnapshotTag;
                GameplaySnapshotTag = savedSettings.GameplaySnapshotTag;
                PromptForGameStoppedTag = savedSettings.PromptForGameStoppedTag;
                BackupExecutionMode = savedSettings.BackupExecutionMode;
                BackupOnUninstall = savedSettings.BackupOnUninstall;
                BackupWhenGameStopped = savedSettings.BackupWhenGameStopped;

                // Load tag IDs if available
                if (savedSettings.ExcludeTagID != Guid.Empty)
                    excludeTagID = savedSettings.ExcludeTagID;
                if (savedSettings.IncludeTagID != Guid.Empty)
                    includeTagID = savedSettings.IncludeTagID;

                // Load retention policy settings
                KeepLast = savedSettings.KeepLast;
                KeepDaily = savedSettings.KeepDaily;
                KeepWeekly = savedSettings.KeepWeekly;
                KeepMonthly = savedSettings.KeepMonthly;
                KeepYearly = savedSettings.KeepYearly;
                EnableRetentionPolicy = savedSettings.EnableRetentionPolicy;
                GameIntervalOverrides = savedSettings.GameIntervalOverrides;

                // Load notification settings
                NotificationLevel = savedSettings.NotificationLevel;
                NotifyOnManualBackup = savedSettings.NotifyOnManualBackup;
            }

            // Auto-detect restic executable if not configured or invalid
            if (string.IsNullOrWhiteSpace(ResticExecutablePath) || ResticExecutablePath == "restic")
            {
                var detectedPath = ResticUtility.DetectResticExecutable();
                if (!string.IsNullOrWhiteSpace(detectedPath))
                {
                    ResticExecutablePath = detectedPath;
                    logger.Info($"Auto-detected restic executable: {detectedPath}");
                }
            }
            else if (!ResticUtility.IsValidResticExecutable(ResticExecutablePath))
            {
                logger.Warn($"Configured restic executable path is invalid: {ResticExecutablePath}");
                var detectedPath = ResticUtility.DetectResticExecutable();
                if (!string.IsNullOrWhiteSpace(detectedPath))
                {
                    ResticExecutablePath = detectedPath;
                    logger.Info($"Auto-detected alternative restic executable: {detectedPath}");
                }
            }

            // Auto-detect ludusavi executable if not configured or invalid
            if (string.IsNullOrWhiteSpace(LudusaviExecutablePath) || LudusaviExecutablePath == "ludusavi")
            {
                var detectedPath = ResticUtility.DetectLudusaviExecutable();
                if (!string.IsNullOrWhiteSpace(detectedPath))
                {
                    LudusaviExecutablePath = detectedPath;
                    logger.Info($"Auto-detected ludusavi executable: {detectedPath}");
                }
            }
            else if (!ResticUtility.IsValidLudusaviExecutable(LudusaviExecutablePath))
            {
                logger.Warn($"Configured ludusavi executable path is invalid: {LudusaviExecutablePath}");
                var detectedPath = ResticUtility.DetectLudusaviExecutable();
                if (!string.IsNullOrWhiteSpace(detectedPath))
                {
                    LudusaviExecutablePath = detectedPath;
                    logger.Info($"Auto-detected alternative ludusavi executable: {detectedPath}");
                }
            }
        }

        internal int GetEffectiveInterval(Guid gameId)
        {
            GameOverride over;
            if (gameIntervalOverrides.TryGetValue(gameId.ToString(), out over) && over.HasIntervalOverride)
            {
                return over.IntervalMinutes.Value;
            }
            return GameplayBackupInterval;
        }

        internal GameOverride FindOverrideByGameName(string gameName)
        {
            foreach (var kvp in gameIntervalOverrides)
            {
                if (string.Equals(kvp.Value.GameName, gameName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
            return null;
        }


        public void BeginEdit()
        {
            this.errors = new List<string>();
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Load();
        }

        public void Save()
        {
            // The plugin base class provides SavePluginSettings method
            plugin.SavePluginSettings(this);
        }

        public void EndEdit()
        {
            Save();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>(this.errors);
            this.errors.Clear();
            return errors.Count == 0;
        }
    }
}
