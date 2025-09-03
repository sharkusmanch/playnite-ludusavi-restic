using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LudusaviRestic
{
    public class LudusaviResticSettings : ISettings, INotifyPropertyChanged
    {
        private IPlayniteAPI api;

        // Core executables / paths
        public string ResticExecutablePath { get; set; } = "";
        public string LudusaviExecutablePath { get; set; } = "";
        public string ResticRepository { get; set; } = "";
        public string ResticPassword { get; set; } = "";
        public string RcloneConfigPath { get; set; } = "";
        public string RcloneConfigPassword { get; set; } = "";

        // Backup execution include/exclude tagging
        public ExecutionMode BackupExecutionMode { get; set; } = ExecutionMode.Exclude;
        public Guid IncludeTagID { get; set; } = Guid.Empty;
        public Guid ExcludeTagID { get; set; } = Guid.Empty;

        // Backup triggers
        public bool BackupDuringGameplay { get; set; } = false;
        public int GameplayBackupIntervalMinutes { get; set; } = 15;
        public bool BackupWhenGameStopped { get; set; } = true;
        public bool PromptForGameStoppedTag { get; set; } = false;
        public bool BackupOnUninstall { get; set; } = true;

        // Additional tagging
        public bool AdditionalTagging { get; set; } = true;
        public string ManualSnapshotTag { get; set; } = "manual";
        public string GameStoppedSnapshotTag { get; set; } = "stop";
        public string GameplaySnapshotTag { get; set; } = "gameplay";

        // Retention policy
        public bool EnableRetentionPolicy { get; set; } = false;
        public int KeepLast { get; set; } = 10;
        public int KeepDaily { get; set; } = 7;
        public int KeepWeekly { get; set; } = 4;
        public int KeepMonthly { get; set; } = 12;
        public int KeepYearly { get; set; } = 5;

        // Game specific overrides
        public Dictionary<Guid, GameSpecificSettings> GameSettings { get; set; } = new Dictionary<Guid, GameSpecificSettings>();

        // Copy for editing
        private LudusaviResticSettings editingClone;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public LudusaviResticSettings() { }

        public LudusaviResticSettings(IPlayniteAPI api)
        {
            this.api = api;
        }

        public void BeginEdit()
        {
            editingClone = (LudusaviResticSettings)MemberwiseClone();
            // Deep copy dictionary
            editingClone.GameSettings = new Dictionary<Guid, GameSpecificSettings>(GameSettings);
        }

        public void CancelEdit()
        {
            if (editingClone != null)
            {
                foreach (var prop in GetType().GetProperties())
                {
                    if (!prop.CanRead || !prop.CanWrite) continue;
                    prop.SetValue(this, prop.GetValue(editingClone));
                }
            }
        }

        public void EndEdit()
        {
            editingClone = null;
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return errors.Count == 0;
        }

        public GameSpecificSettings GetGameSettings(Guid gameId)
        {
            if (GameSettings == null)
            {
                GameSettings = new Dictionary<Guid, GameSpecificSettings>();
            }
            if (GameSettings.TryGetValue(gameId, out var settings))
            {
                return settings;
            }
            return null;
        }

        public void SetGameSettings(Guid gameId, GameSpecificSettings settings)
        {
            if (GameSettings == null)
            {
                GameSettings = new Dictionary<Guid, GameSpecificSettings>();
            }
            GameSettings[gameId] = settings;
        }

        public void RemoveGameSettings(Guid gameId)
        {
            GameSettings?.Remove(gameId);
        }
    }
}

