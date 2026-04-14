using Playnite.SDK;
using System.Collections.Generic;
using System;
using System.ComponentModel;

namespace LudusaviRestic
{
    public class LudusaviResticSettings : ISettings, INotifyPropertyChanged
    {
        private readonly LudusaviRestic _plugin;
        private static readonly ILogger logger = LogManager.GetLogger();

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _ludusaviExecutablePath = "ludusavi";
        public string LudusaviExecutablePath { get { return _ludusaviExecutablePath; } set { _ludusaviExecutablePath = value; NotifyPropertyChanged("LudusaviExecutablePath"); } }

        private ExecutionMode _backupExecutionMode = ExecutionMode.Exclude;

        public ExecutionMode BackupExecutionMode { get { return _backupExecutionMode; } set { _backupExecutionMode = value; NotifyPropertyChanged("BackupExecutionMode"); } }

        private Guid _excludeTagID = Guid.Empty;
        public Guid ExcludeTagID
        {
            get
            {
                if (_plugin != null && _excludeTagID == Guid.Empty)
                {
                    _excludeTagID = _plugin.PlayniteApi.Database.Tags.Add("[LR] Exclude").Id;
                }
                else if (_plugin != null && _plugin.PlayniteApi.Database.Tags.Get(_excludeTagID) == null)
                {
                    _excludeTagID = _plugin.PlayniteApi.Database.Tags.Add("[LR] Exclude").Id;
                }
                return _excludeTagID;
            }
            set => _excludeTagID = value;
        }
        private Guid _includeTagID = Guid.Empty;
        public Guid IncludeTagID
        {
            get
            {
                if (_plugin != null && _includeTagID == Guid.Empty)
                {
                    _includeTagID = _plugin.PlayniteApi.Database.Tags.Add("[LR] Include").Id;
                }
                else if (_plugin != null && _plugin.PlayniteApi.Database.Tags.Get(_includeTagID) == null)
                {
                    _includeTagID = _plugin.PlayniteApi.Database.Tags.Add("[LR] Include").Id;
                }
                return _includeTagID;
            }
            set => _includeTagID = value;
        }

        private string _resticExecutablePath = "restic";
        public string ResticExecutablePath { get { return _resticExecutablePath; } set { _resticExecutablePath = value; NotifyPropertyChanged("ResticExecutablePath"); } }
        private string _resticRepository;
        public string ResticRepository { get { return _resticRepository; } set { _resticRepository = value; NotifyPropertyChanged("ResticRepository"); } }
        private string _resticPassword;
        public string ResticPassword { get { return _resticPassword; } set { _resticPassword = value; NotifyPropertyChanged("ResticPassword"); } }
        private string _rcloneConfigPath;
        public string RcloneConfigPath { get { return _rcloneConfigPath; } set { _rcloneConfigPath = value; NotifyPropertyChanged("RcloneConfigPath"); } }
        private string _rcloneConfigPassword;
        public string RcloneConfigPassword { get { return _rcloneConfigPassword; } set { _rcloneConfigPassword = value; NotifyPropertyChanged("RcloneConfigPassword"); } }
        private bool _backupDuringGameplay = false;
        public bool BackupDuringGameplay { get { return _backupDuringGameplay; } set { _backupDuringGameplay = value; ; NotifyPropertyChanged("BackupDuringGameplay"); } }
        private bool _additionalTagging = false;
        public bool AdditionalTagging { get { return _additionalTagging; } set { _additionalTagging = value; ; NotifyPropertyChanged("AdditionalTagging"); } }
        private bool _backupWhenGameStopped = true;
        public bool BackupWhenGameStopped { get { return _backupWhenGameStopped; } set { _backupWhenGameStopped = value; ; NotifyPropertyChanged("BackupWhenGameStopped"); } }
        private bool _promptForGameStoppedTag = false;
        public bool PromptForGameStoppedTag { get { return _promptForGameStoppedTag; } set { _promptForGameStoppedTag = value; ; NotifyPropertyChanged("PromptForGameStoppedTag"); } }
        private bool _backupOnUninstall = false;
        public bool BackupOnUninstall { get { return _backupOnUninstall; } set { _backupOnUninstall = value; ; NotifyPropertyChanged("BackupOnUninstall"); } }
        private string _manualSnapshotTag = "manual";
        public string ManualSnapshotTag { get { return _manualSnapshotTag; } set { _manualSnapshotTag = value; ; NotifyPropertyChanged("ManualSnapshotTag"); } }
        private string _gameStoppedSnapshotTag = "game-stopped";
        public string GameStoppedSnapshotTag { get { return _gameStoppedSnapshotTag; } set { _gameStoppedSnapshotTag = value; ; NotifyPropertyChanged("GameStoppedSnapshotTag"); } }
        private string _gameplaySnapshotTag = "gameplay";
        public string GameplaySnapshotTag { get { return _gameplaySnapshotTag; } set { _gameplaySnapshotTag = value; ; NotifyPropertyChanged("GameplaySnapshotTag"); } }

        private NotificationLevel _notificationLevel = NotificationLevel.Summary;
        public NotificationLevel NotificationLevel { get { return _notificationLevel; } set { _notificationLevel = value; NotifyPropertyChanged("NotificationLevel"); } }

        private bool _notifyOnManualBackup = true;
        public bool NotifyOnManualBackup { get { return _notifyOnManualBackup; } set { _notifyOnManualBackup = value; NotifyPropertyChanged("NotifyOnManualBackup"); } }

        private bool _enableRetentionPolicy = false;
        public bool EnableRetentionPolicy { get { return _enableRetentionPolicy; } set { _enableRetentionPolicy = value; NotifyPropertyChanged("EnableRetentionPolicy"); } }

        private int _keepLast = 10;
        public int KeepLast { get { return _keepLast; } set { _keepLast = value; NotifyPropertyChanged("KeepLast"); } }

        private int _keepDaily = 7;
        public int KeepDaily { get { return _keepDaily; } set { _keepDaily = value; NotifyPropertyChanged("KeepDaily"); } }

        private int _keepWeekly = 4;
        public int KeepWeekly { get { return _keepWeekly; } set { _keepWeekly = value; NotifyPropertyChanged("KeepWeekly"); } }

        private int _keepMonthly = 12;
        public int KeepMonthly { get { return _keepMonthly; } set { _keepMonthly = value; NotifyPropertyChanged("KeepMonthly"); } }

        private int _keepYearly = 5;
        public int KeepYearly { get { return _keepYearly; } set { _keepYearly = value; NotifyPropertyChanged("KeepYearly"); } }

        private int _maxRepackSizeMB = 500;
        public int MaxRepackSizeMB { get { return _maxRepackSizeMB; } set { _maxRepackSizeMB = value; NotifyPropertyChanged("MaxRepackSizeMB"); } }

        private Dictionary<string, GameOverride> _gameIntervalOverrides = new Dictionary<string, GameOverride>();
        public Dictionary<string, GameOverride> GameIntervalOverrides
        {
            get { return _gameIntervalOverrides; }
            set { _gameIntervalOverrides = value ?? new Dictionary<string, GameOverride>(); }
        }

        private List<Guid> _excludedSourceIds = new List<Guid>();
        public List<Guid> ExcludedSourceIds
        {
            get { return _excludedSourceIds; }
            set { _excludedSourceIds = value ?? new List<Guid>(); }
        }

        private List<string> _errors;

        private int _gameplayBackupInterval = 5;
        public int GameplayBackupInterval
        {
            get { return _gameplayBackupInterval; }
            set
            {
                string rawValue = value.ToString();
                int intValue;

                bool success = int.TryParse(rawValue, out intValue) && intValue > 0;

                if (success)
                {
                    _gameplayBackupInterval = intValue;
                    NotifyPropertyChanged("GameplayBackupInterval");
                }
                else
                {
                    this._errors.Add("Backup interval must be a positive integer");
                }
            }
        }

        public LudusaviResticSettings()
        {
        }

        public LudusaviResticSettings(LudusaviRestic plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this._plugin = plugin;
            Load();
        }
        private void Load()
        {
            // Load saved settings.
            var savedSettings = _plugin.LoadPluginSettings<LudusaviResticSettings>();

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
                    _excludeTagID = savedSettings.ExcludeTagID;
                if (savedSettings.IncludeTagID != Guid.Empty)
                    _includeTagID = savedSettings.IncludeTagID;

                // Load retention policy settings
                KeepLast = savedSettings.KeepLast;
                KeepDaily = savedSettings.KeepDaily;
                KeepWeekly = savedSettings.KeepWeekly;
                KeepMonthly = savedSettings.KeepMonthly;
                KeepYearly = savedSettings.KeepYearly;
                EnableRetentionPolicy = savedSettings.EnableRetentionPolicy;
                MaxRepackSizeMB = savedSettings.MaxRepackSizeMB;
                GameIntervalOverrides = savedSettings.GameIntervalOverrides;
                ExcludedSourceIds = savedSettings.ExcludedSourceIds;

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
            if (_gameIntervalOverrides.TryGetValue(gameId.ToString(), out over) && over.HasIntervalOverride)
            {
                return over.IntervalMinutes.Value;
            }
            return GameplayBackupInterval;
        }

        internal GameOverride FindOverrideByGameName(string gameName)
        {
            foreach (var kvp in _gameIntervalOverrides)
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
            this._errors = new List<string>();
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
            _plugin.SavePluginSettings(this);
        }

        public void EndEdit()
        {
            Save();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>(this._errors);
            this._errors.Clear();
            return errors.Count == 0;
        }
    }
}
