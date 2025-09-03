using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Linq;
using System.Windows;

namespace LudusaviRestic
{
    public partial class GameOverrideSettingsWindow : Window
    {
        private readonly IPlayniteAPI api;
        private readonly LudusaviResticSettings pluginSettings;
        private readonly Game game;
        private readonly GameOverrideSettingsViewModel vm;
        private static readonly ILogger logger = LogManager.GetLogger();

        public GameOverrideSettingsWindow(Game game, LudusaviResticSettings settings, IPlayniteAPI api)
        {
            InitializeComponent();
            ApplyPlayniteTheme(api);
            this.api = api;
            this.pluginSettings = settings;
            this.game = game;
            var existing = settings.GetGameSettings(game.Id);
            vm = new GameOverrideSettingsViewModel(game, settings, existing);
            DataContext = vm;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!vm.OverrideGlobalSettings)
                {
                    pluginSettings.RemoveGameSettings(game.Id);
                }
                else
                {
                    pluginSettings.SetGameSettings(game.Id, vm.ToGameSpecificSettings());
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error saving per-game settings");
                api.Dialogs.ShowErrorMessage(ex.Message, "Per-Game Settings");
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            vm.ResetToGlobal();
        }

        private void ApplyPlayniteTheme(IPlayniteAPI api)
        {
            try
            {
                var themeResources = api.Resources.GetResource("ThemeResources") as ResourceDictionary;
                if (themeResources != null)
                {
                    Resources.MergedDictionaries.Add(themeResources);
                }
            }
            catch
            {
                // swallow; fallback to default styling
            }
        }
    }
}
