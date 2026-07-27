using Playnite.SDK;
using System.Collections.Generic;

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
        }

        public string UniqueNotificationID(string suffix)
        {
            return $"LudusaviRestic_{suffix}";
        }

        /// <summary>
        /// Builds the environment restic and rclone need, for application to a single
        /// ProcessStartInfo. These must never be set on Playnite's own process block:
        /// every game, launcher and emulator Playnite spawns inherits it, which would
        /// disclose the repository password to arbitrary third-party executables.
        /// </summary>
        public IDictionary<string, string> BuildResticEnvironment()
        {
            var environment = new Dictionary<string, string>();

            AddIfSet(environment, "RCLONE_CONFIG_PASS", this._settings.RcloneConfigPassword);
            AddIfSet(environment, "RESTIC_REPOSITORY", this._settings.ResticRepository);
            AddIfSet(environment, "RESTIC_PASSWORD", this._settings.ResticPassword);
            AddIfSet(environment, "RCLONE_CONFIG", this._settings.RcloneConfigPath);

            return environment;
        }

        private static void AddIfSet(IDictionary<string, string> environment, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                environment[name] = value;
            }
        }
    }
}
