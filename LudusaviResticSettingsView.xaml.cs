using System.Windows;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;

namespace LudusaviRestic
{
    public partial class LudusaviResticSettingsView : UserControl
    {
        private LudusaviRestic plugin;

        public LudusaviResticSettingsView(LudusaviRestic plugin)
        {
            InitializeComponent();
            this.plugin = plugin;
        }

        public void OnBrowseLudusaviExecutablePath(object sender, RoutedEventArgs e)
        {
            var choice = this.plugin.PlayniteApi.Dialogs.SelectFile("Executable|*.exe");

            if (choice.Length > 0)
            {
                this.plugin.settings.LudusaviExecutablePath = choice;
            }
        }

        public void OnBrowseResticExecutablePath(object sender, RoutedEventArgs e)
        {
            var choice = this.plugin.PlayniteApi.Dialogs.SelectFile("Executable|*.exe");

            if (choice.Length > 0)
            {
                this.plugin.settings.ResticExecutablePath = choice;
            }
        }

        public void OnBrowseRcloneConfPath(object sender, RoutedEventArgs e)
        {
            var choice = this.plugin.PlayniteApi.Dialogs.SelectFile("Conf|*.conf");

            if (choice.Length > 0)
            {
                this.plugin.settings.RcloneConfigPath = choice;
            }
        }
    }
}