using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Playnite.SDK;

namespace LudusaviRestic
{
    public partial class LudusaviResticSettingsView : UserControl
    {
        private LudusaviRestic plugin;
        private static readonly ILogger logger = LogManager.GetLogger();

        public LudusaviResticSettingsView(LudusaviRestic plugin)
        {
            logger.Debug("LudusaviResticSettingsView init");
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

        private void OnBackupDuringGameplayChecked(object sender, RoutedEventArgs e)
        {
            GameplayBackupInterval.IsEnabled = true;
        }

        private void OnBackupDuringGameplayUnchecked(object sender, RoutedEventArgs e)
        {
            GameplayBackupInterval.IsEnabled = false;
        }
        private void OnBackupWhenGameStoppedChecked(object sender, RoutedEventArgs e)
        {
            PromptForGameStoppedTag.IsEnabled = true;
        }

        private void OnBackupWhenGameStoppedUnchecked(object sender, RoutedEventArgs e)
        {
            PromptForGameStoppedTag.IsEnabled = false;
            PromptForGameStoppedTag.IsChecked = false;
        }

        private void OnAdditionalTaggingChecked(object sender, RoutedEventArgs e)
        {
            GameStoppedSnapshotTag.IsEnabled = true;
            ManualSnapshotTag.IsEnabled = true;
            GameplaySnapshotTag.IsEnabled = true;
        }

        private void OnAdditionalTaggingUnchecked(object sender, RoutedEventArgs e)
        {
            GameStoppedSnapshotTag.IsEnabled = false;
            ManualSnapshotTag.IsEnabled = false;
            GameplaySnapshotTag.IsEnabled = false;
        }

        public void OnVerify(object sender, RoutedEventArgs e)
        {
            BackupContext context = new BackupContext(this.plugin.PlayniteApi, this.plugin.settings);

            Task.Run(() =>
            {
                string result = "Ludusavi Restic Settings Verification:";

                try
                {
                    CommandResult restic = ResticCommand.Verify(context);
                    CommandResult ludusavi = LudusaviCommand.Version(context);

                    if (restic.ExitCode == 0)
                    {
                        result += "\n\nrestic - repository connection succeeded";
                    }
                    else
                    {
                        result += $"\n\nrestic - {restic.StdErr}";
                    }

                    if (ludusavi.ExitCode == 0)
                    {
                        result += "\n\nludusavi - verification succeeded";
                    }
                    else
                    {
                        result += $"\n\nludusavi - {ludusavi.StdOut}";
                    }

                    this.plugin.PlayniteApi.Dialogs.ShowMessage(result);
                }
                catch (Exception ex)
                {
                    this.plugin.PlayniteApi.Dialogs.ShowMessage($"Ludusavi Restic Settings Verification failed: {ex.ToString()}");
                }
            });
        }
    }
}
