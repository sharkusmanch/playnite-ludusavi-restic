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
        // Parameterless constructor must exist if you want to use LoadPluginSettings method.

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
                LudusaviExecutablePath = savedSettings.ludusaviExecutablePath;
                ResticExecutablePath = savedSettings.resticExecutablePath;
                ResticRepository = savedSettings.resticRepository;
                ResticPassword = savedSettings.resticPassword;
                RcloneConfigPath = savedSettings.rcloneConfigPath;
                RcloneConfigPassword = savedSettings.rcloneConfigPassword;
                BackupDuringGameplay = savedSettings.backupDuringGameplay;
                GameplayBackupInterval = savedSettings.gameplayBackupInterval;
            }
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

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(this);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>(this.errors);
            this.errors.Clear();
            return errors.Count == 0;
        }
    }
}
