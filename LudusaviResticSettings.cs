using Playnite.SDK;
using System.Collections.Generic;
using System.ComponentModel;

namespace LudusaviRestic
{
    public class LudusaviResticSettings : ISettings
    {
        private readonly LudusaviRestic plugin;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string ludisaviExecutablePath = "ludusavi";
        public string LudusaviExecutablePath { get { return ludisaviExecutablePath; } set { ludisaviExecutablePath = value; NotifyPropertyChanged("LudusaviExecutablePath"); } }

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

        // Parameterless constructor must exist if you want to use LoadPluginSettings method.
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
                LudusaviExecutablePath = savedSettings.ludisaviExecutablePath;
                ResticExecutablePath = savedSettings.resticExecutablePath;
                ResticRepository = savedSettings.resticRepository;
                ResticPassword = savedSettings.resticPassword;
                RcloneConfigPath = savedSettings.rcloneConfigPath;
                RcloneConfigPassword = savedSettings.rcloneConfigPassword;
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
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
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}