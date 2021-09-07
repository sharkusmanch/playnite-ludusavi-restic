using Playnite.SDK;
using System.Collections.Generic;

namespace LudusaviRestic
{
    public class LudusaviResticSettings
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public string LudusaviExecutablePath { get; set; } = string.Empty;
        public string ResticExecutablePath { get; set; } = string.Empty;
        public string ResticRepository { get; set; } = string.Empty;
        public string ResticPassword { get; set; } = string.Empty;
        public string RcloneConfigPath { get; set; } = string.Empty;
        public string RcloneConfigPassword { get; set; } = string.Empty;
    }

    public class LudusaviResticSettingsViewModel : ObservableObject, ISettings
    {
        private readonly LudusaviRestic plugin;
        private LudusaviResticSettings editingClone { get; set; }
        private LudusaviResticSettings settings;
        public LudusaviResticSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public LudusaviResticSettingsViewModel(LudusaviRestic plugin)
        {
            this.plugin = plugin;
            var savedSettings = plugin.LoadPluginSettings<LudusaviResticSettings>();

            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new LudusaviResticSettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            Settings = editingClone;
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
