using System;
using System.Collections.Generic;
using System.Linq;
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
            // Initialize password fields with current settings values
            Loaded += (s, e) =>
            {
                try
                {
                    if (ResticPasswordBox != null)
                    {
                        ResticPasswordBox.Password = this.plugin.settings.ResticPassword ?? string.Empty;
                    }
                    if (RclonePasswordBox != null)
                    {
                        RclonePasswordBox.Password = this.plugin.settings.RcloneConfigPassword ?? string.Empty;
                    }
                    if (OverridesDataGrid != null)
                    {
                        RefreshOverridesGrid();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error initializing password fields");
                }
            };
        }

        public void OnBrowseLudusaviExecutablePath(object sender, RoutedEventArgs e)
        {
            var choice = this.plugin.PlayniteApi.Dialogs.SelectFile(this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestExecutableFilterShort"));

            if (choice.Length > 0)
            {
                this.plugin.settings.LudusaviExecutablePath = choice;
            }
        }

        public void OnAutoDetectLudusavi(object sender, RoutedEventArgs e)
        {
            var detectedPath = ResticUtility.DetectLudusaviExecutable();
            if (!string.IsNullOrWhiteSpace(detectedPath))
            {
                this.plugin.settings.LudusaviExecutablePath = detectedPath;
                this.plugin.PlayniteApi.Dialogs.ShowMessage(string.Format(this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestSettingsLudusaviDetected"), detectedPath), this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestSettingsAutoDetectSuccess"));
            }
            else
            {
                this.plugin.PlayniteApi.Dialogs.ShowMessage(this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestSettingsLudusaviNotDetected"), this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestSettingsAutoDetectFailed"));
            }
        }

        public void OnBrowseResticExecutablePath(object sender, RoutedEventArgs e)
        {
            var choice = this.plugin.PlayniteApi.Dialogs.SelectFile(this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestExecutableFilterShort"));

            if (choice.Length > 0)
            {
                this.plugin.settings.ResticExecutablePath = choice;
            }
        }

        public void OnAutoDetectRestic(object sender, RoutedEventArgs e)
        {
            var detectedPath = ResticUtility.DetectResticExecutable();
            if (!string.IsNullOrWhiteSpace(detectedPath))
            {
                this.plugin.settings.ResticExecutablePath = detectedPath;
                this.plugin.PlayniteApi.Dialogs.ShowMessage(string.Format(this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestSettingsResticDetected"), detectedPath), this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestSettingsAutoDetectSuccess"));
            }
            else
            {
                this.plugin.PlayniteApi.Dialogs.ShowMessage(this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestSettingsResticNotDetected"), this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestSettingsAutoDetectFailed"));
            }
        }

        public void OnBrowseRcloneConfPath(object sender, RoutedEventArgs e)
        {
            var choice = this.plugin.PlayniteApi.Dialogs.SelectFile(this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestConfigFilter"));

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

        private void OnRetentionPolicyChecked(object sender, RoutedEventArgs e)
        {
            KeepLast.IsEnabled = true;
            KeepDaily.IsEnabled = true;
            KeepWeekly.IsEnabled = true;
            KeepMonthly.IsEnabled = true;
            KeepYearly.IsEnabled = true;
        }

        private void OnRetentionPolicyUnchecked(object sender, RoutedEventArgs e)
        {
            KeepLast.IsEnabled = false;
            KeepDaily.IsEnabled = false;
            KeepWeekly.IsEnabled = false;
            KeepMonthly.IsEnabled = false;
            KeepYearly.IsEnabled = false;
        }

        private void RefreshOverridesGrid()
        {
            var items = this.plugin.settings.GameIntervalOverrides.Select(kvp =>
            {
                var item = kvp.Value;
                item.GameId = kvp.Key;
                return item;
            }).ToList();
            OverridesDataGrid.ItemsSource = items;
        }

        public void OnRemoveOverride(object sender, RoutedEventArgs e)
        {
            if (OverridesDataGrid.SelectedItem is GameOverride selected && selected.GameId != null)
            {
                this.plugin.settings.GameIntervalOverrides.Remove(selected.GameId);
                RefreshOverridesGrid();
            }
        }

        private void OnOverrideCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit && e.Row.Item is GameOverride item && item.GameId != null)
            {
                var textBox = e.EditingElement as System.Windows.Controls.TextBox;
                if (textBox == null) return;

                int columnIndex = e.Column.DisplayIndex;
                var target = this.plugin.settings.GameIntervalOverrides[item.GameId];

                if (columnIndex == 1) // IntervalMinutes (nullable)
                {
                    if (string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        target.IntervalMinutes = null;
                    }
                    else
                    {
                        int val;
                        if (int.TryParse(textBox.Text, out val) && val > 0)
                        {
                            target.IntervalMinutes = val;
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }
                }
                else if (columnIndex >= 2 && columnIndex <= 6) // Retention columns
                {
                    int? parsedValue;
                    if (string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        parsedValue = null;
                    }
                    else
                    {
                        int val;
                        if (int.TryParse(textBox.Text, out val) && val >= 0)
                        {
                            parsedValue = val;
                        }
                        else
                        {
                            e.Cancel = true;
                            return;
                        }
                    }

                    switch (columnIndex)
                    {
                        case 2: target.KeepLast = parsedValue; break;
                        case 3: target.KeepDaily = parsedValue; break;
                        case 4: target.KeepWeekly = parsedValue; break;
                        case 5: target.KeepMonthly = parsedValue; break;
                        case 6: target.KeepYearly = parsedValue; break;
                    }
                }
            }
        }

        public void OnVerify(object sender, RoutedEventArgs e)
        {
            BackupContext context = new BackupContext(this.plugin.PlayniteApi, this.plugin.settings);
            var api = this.plugin.PlayniteApi;
            var resources = api.Resources;
            string title = resources.GetString("LOCLuduRestSettingsVerificationTitle");
            string verifyingRestic = resources.GetString("LOCLuduRestVerifyingRestic");
            string verifyingLudusavi = resources.GetString("LOCLuduRestVerifyingLudusavi");

            GlobalProgressOptions gpo = new GlobalProgressOptions(title, true)
            {
                IsIndeterminate = true
            };

            api.Dialogs.ActivateGlobalProgress(progress =>
            {
                try
                {
                    string result = title;

                    progress.Text = verifyingRestic;
                    var restic = ResticCommand.Verify(context);
                    if (restic.ExitCode == 0)
                    {
                        result += "\n\n" + resources.GetString("LOCLuduRestResticConnectionSucceeded");
                    }
                    else
                    {
                        result += $"\n\nrestic - {restic.StdErr}";
                    }

                    progress.Text = verifyingLudusavi;
                    var ludusavi = LudusaviCommand.Version(context);
                    if (ludusavi.ExitCode == 0)
                    {
                        result += "\n\n" + resources.GetString("LOCLuduRestLudusaviVerificationSucceeded");
                    }
                    else
                    {
                        result += $"\n\nludusavi - {ludusavi.StdOut}";
                    }

                    api.Dialogs.ShowMessage(result, title);
                }
                catch (Exception ex)
                {
                    api.Dialogs.ShowMessage(string.Format(resources.GetString("LOCLuduRestSettingsVerificationFailed"), ex.ToString()), title);
                }
            }, gpo);
        }

        private void SyncResticPasswordToSettings(string value)
        {
            this.plugin.settings.ResticPassword = value;
        }

        private void SyncRclonePasswordToSettings(string value)
        {
            this.plugin.settings.RcloneConfigPassword = value;
        }

        public void OnResticPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (ResticPasswordBox != null && ResticPasswordText != null && ResticPasswordText.Visibility == Visibility.Collapsed)
            {
                SyncResticPasswordToSettings(ResticPasswordBox.Password);
            }
        }

        public void OnResticPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            if (ResticPasswordText != null && ResticPasswordText.Visibility == Visibility.Visible)
            {
                SyncResticPasswordToSettings(ResticPasswordText.Text);
            }
        }

        public void OnRclonePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (RclonePasswordBox != null && RclonePasswordText != null && RclonePasswordText.Visibility == Visibility.Collapsed)
            {
                SyncRclonePasswordToSettings(RclonePasswordBox.Password);
            }
        }

        public void OnRclonePasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            if (RclonePasswordText != null && RclonePasswordText.Visibility == Visibility.Visible)
            {
                SyncRclonePasswordToSettings(RclonePasswordText.Text);
            }
        }

        public void OnToggleResticPasswordVisibility(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ResticPasswordText.Visibility == Visibility.Collapsed)
                {
                    // Show password
                    ResticPasswordText.Text = ResticPasswordBox.Password;
                    ResticPasswordBox.Visibility = Visibility.Collapsed;
                    ResticPasswordText.Visibility = Visibility.Visible;
                    ResticPasswordToggle.Content = this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestHide");
                }
                else
                {
                    // Hide password
                    ResticPasswordBox.Password = ResticPasswordText.Text;
                    ResticPasswordText.Visibility = Visibility.Collapsed;
                    ResticPasswordBox.Visibility = Visibility.Visible;
                    ResticPasswordToggle.Content = this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestShow");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error toggling restic password visibility");
            }
        }

        public void OnToggleRclonePasswordVisibility(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RclonePasswordText.Visibility == Visibility.Collapsed)
                {
                    RclonePasswordText.Text = RclonePasswordBox.Password;
                    RclonePasswordBox.Visibility = Visibility.Collapsed;
                    RclonePasswordText.Visibility = Visibility.Visible;
                    RclonePasswordToggle.Content = this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestHide");
                }
                else
                {
                    RclonePasswordBox.Password = RclonePasswordText.Text;
                    RclonePasswordText.Visibility = Visibility.Collapsed;
                    RclonePasswordBox.Visibility = Visibility.Visible;
                    RclonePasswordToggle.Content = this.plugin.PlayniteApi.Resources.GetString("LOCLuduRestShow");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error toggling rclone password visibility");
            }
        }
    }
}
