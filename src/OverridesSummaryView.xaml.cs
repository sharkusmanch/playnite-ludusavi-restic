using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Playnite.SDK;

namespace LudusaviRestic
{
    public partial class OverridesSummaryView : UserControl
    {
        private readonly LudusaviRestic plugin;
        private readonly IPlayniteAPI api;
        private readonly LudusaviResticSettings settings;
        private List<OverrideRow> rows = new List<OverrideRow>();
        public int OverridesCount => rows.Count;

        public OverridesSummaryView(LudusaviRestic plugin, IPlayniteAPI api, LudusaviResticSettings settings)
        {
            InitializeComponent();
            this.plugin = plugin;
            this.api = api;
            this.settings = settings;
            LoadData();
            OverridesGrid.ItemsSource = rows;
        }

        private void LoadData()
        {
            rows = settings.GameSettings
                .Where(kv => kv.Value.OverrideGlobalSettings)
                .Select(kv => new { kv.Key, kv.Value })
                .Select(x => new OverrideRow
                {
                    GameId = x.Key,
                    GameName = api.Database.Games.Get(x.Key)?.Name ?? x.Key.ToString(),
                    BackupOnGameStopped = x.Value.BackupOnGameStopped?.ToString() ?? "Global",
                    BackupDuringGameplay = x.Value.BackupDuringGameplay?.ToString() ?? "Global",
                    BackupOnUninstall = x.Value.BackupOnUninstall?.ToString() ?? "Global",
                    UseCustomRetention = (x.Value.UseCustomRetention ?? false) ? "Yes" : "No"
                })
                .OrderBy(r => r.GameName)
                .ToList();
        }

        private OverrideRow Selected => OverridesGrid.SelectedItem as OverrideRow;

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            ApplyFilter();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var term = SearchBox.Text?.Trim();
            if (string.IsNullOrEmpty(term))
            {
                OverridesGrid.ItemsSource = rows;
            }
            else
            {
                OverridesGrid.ItemsSource = rows.Where(r => r.GameName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var sel = Selected; if (sel == null) return;
            var game = api.Database.Games.Get(sel.GameId); if (game == null) return;
            if (plugin.ShowPerGameSettings(game))
            {
                plugin.SavePluginSettings(settings);
                Refresh_Click(null, null);
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            var sel = Selected; if (sel == null) return;
            if (settings.GameSettings.Remove(sel.GameId))
            {
                plugin.SavePluginSettings(settings);
                Refresh_Click(null, null);
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (rows.Count == 0) return;
            var result = api.Dialogs.ShowMessage(ResourceProvider.GetString("LOCLuduRestOverridesDialogClearAllPrompt"), ResourceProvider.GetString("LOCLuduRestOverridesDialogTitle"), MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                var keys = settings.GameSettings.Where(k => k.Value.OverrideGlobalSettings).Select(k => k.Key).ToList();
                foreach (var k in keys) settings.GameSettings.Remove(k);
                plugin.SavePluginSettings(settings);
                Refresh_Click(null, null);
                api.Dialogs.ShowMessage(ResourceProvider.GetString("LOCLuduRestOverridesClearedMessage"), ResourceProvider.GetString("LOCLuduRestOverridesCleared"));
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }

    public class OverrideRow
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public string BackupOnGameStopped { get; set; }
        public string BackupDuringGameplay { get; set; }
        public string BackupOnUninstall { get; set; }
        public string UseCustomRetention { get; set; }
    }
}
