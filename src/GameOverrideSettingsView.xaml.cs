using System;
using System.Windows.Controls;
using Playnite.SDK;

namespace LudusaviRestic
{
    public partial class GameOverrideSettingsView : UserControl
    {
        private readonly LudusaviResticSettings pluginSettings;
        private readonly GameOverrideSettingsViewModel vm;
        private readonly Playnite.SDK.Models.Game game;
        private static readonly ILogger logger = LogManager.GetLogger();

        public event EventHandler<bool> CloseRequested; // bool = saved?

        public GameOverrideSettingsView(Playnite.SDK.Models.Game game, LudusaviResticSettings settings)
        {
            InitializeComponent();
            this.pluginSettings = settings;
            this.game = game;
            var existing = settings.GetGameSettings(game.Id);
            vm = new GameOverrideSettingsViewModel(game, settings, existing);
            DataContext = vm;
        }

        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
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
                CloseRequested?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error saving per-game settings");
                API.Instance?.Dialogs.ShowErrorMessage(ex.Message, "Per-Game Settings");
            }
        }

        private void Reset_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            vm.ResetToGlobal();
        }

        private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, false);
        }
    }
}
