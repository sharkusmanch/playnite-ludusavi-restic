using Playnite.SDK;
using System;

namespace LudusaviRestic
{
    public class BackupContext
    {
        public readonly string NotificationID = "LudusaviRestic";
        private IPlayniteAPI _api;
        private LudusaviResticSettings _settings;

        public IPlayniteAPI API { get { return this._api; } }
        public LudusaviResticSettings Settings { get { return this._settings; } }

        public BackupContext(IPlayniteAPI api, LudusaviResticSettings settings)
        {
            this._api = api;
            this._settings = settings;
            ApplyEnvironment();
        }

        public string UniqueNotificationID(string suffix)
        {
            return $"LudusaviRestic_{suffix}";
        }

        public void ApplyEnvironment()
        {
            Environment.SetEnvironmentVariable("RCLONE_CONFIG_PASS", this._settings.RcloneConfigPassword);
            Environment.SetEnvironmentVariable("RESTIC_REPOSITORY", this._settings.ResticRepository);
            Environment.SetEnvironmentVariable("RESTIC_PASSWORD", this._settings.ResticPassword);
            Environment.SetEnvironmentVariable("RCLONE_CONFIG", this._settings.RcloneConfigPath);
        }
    }
}
