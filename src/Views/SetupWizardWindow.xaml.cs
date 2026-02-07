using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Playnite.SDK;

namespace LudusaviRestic
{
    public partial class SetupWizardWindow : Window
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private BackupContext context;
        private int currentStep = 0;
        private const int TotalSteps = 3;

        public LudusaviResticSettings ResultSettings { get; private set; }
        public bool SetupCompleted { get; private set; } = false;

        public SetupWizardWindow(BackupContext context)
        {
            InitializeComponent();
            this.context = context;
            ApplyPlayniteTheme(context.API);
            InitializeWizard();
        }

        private void ApplyPlayniteTheme(IPlayniteAPI api)
        {
            // Apply Playnite's theme resources
            var resourceProvider = api.Resources;
            try
            {
                // Try to get theme resources and merge them
                var themeResources = resourceProvider.GetResource("ThemeResources") as ResourceDictionary;
                if (themeResources != null)
                {
                    Resources.MergedDictionaries.Add(themeResources);
                }
            }
            catch
            {
                // If specific theme resources aren't available,
                // the DynamicResource bindings in XAML should still work
            }
        }

        private void InitializeWizard()
        {
            // Populate suggested repository paths
            var suggestions = ResticUtility.GetRepositorySuggestions();
            SuggestedPathsComboBox.ItemsSource = suggestions;
            if (suggestions.Count > 0)
            {
                SuggestedPathsComboBox.SelectedIndex = 0;
            }

            // Auto-detect executables
            AutoDetectRestic();
            AutoDetectLudusavi();

            // Set initial tab
            WizardTabs.SelectedIndex = 0;
            UpdateNavigationButtons();
        }

        private void AutoDetectRestic()
        {
            var detectedPath = ResticUtility.DetectResticExecutable();
            if (!string.IsNullOrWhiteSpace(detectedPath))
            {
                ResticPathTextBox.Text = detectedPath;
                ResticStatusText.Text = context.API.Resources.GetString("LOCLuduRestSetupResticFoundVerified");
                ResticStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ResticStatusText.Text = context.API.Resources.GetString("LOCLuduRestSetupResticNotFound");
                ResticStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            }
        }

        private void AutoDetectLudusavi()
        {
            var detectedPath = ResticUtility.DetectLudusaviExecutable();
            if (!string.IsNullOrWhiteSpace(detectedPath))
            {
                LudusaviPathTextBox.Text = detectedPath;
                LudusaviStatusText.Text = context.API.Resources.GetString("LOCLuduRestSetupLudusaviFoundVerified");
                LudusaviStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                LudusaviStatusText.Text = context.API.Resources.GetString("LOCLuduRestSetupLudusaviNotFound");
                LudusaviStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            }
        }

        private void AutoDetectRestic_Click(object sender, RoutedEventArgs e)
        {
            AutoDetectRestic();
        }

        private void BrowseRestic_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = context.API.Resources.GetString("LOCLuduRestSelectResticExecutable"),
                Filter = context.API.Resources.GetString("LOCLuduRestExecutableFilter"),
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                ResticPathTextBox.Text = dialog.FileName;
                ValidateResticPath();
            }
        }

        private void ValidateResticPath()
        {
            var path = ResticPathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                ResticStatusText.Text = context.API.Resources.GetString("LOCLuduRestPleaseSpecifyResticPath");
                ResticStatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (ResticUtility.IsValidResticExecutable(path))
            {
                ResticStatusText.Text = context.API.Resources.GetString("LOCLuduRestValidResticExecutable");
                ResticStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ResticStatusText.Text = context.API.Resources.GetString("LOCLuduRestInvalidResticExecutable");
                ResticStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void ResticPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateResticPath();
        }

        private void AutoDetectLudusavi_Click(object sender, RoutedEventArgs e)
        {
            AutoDetectLudusavi();
        }

        private void BrowseLudusavi_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = context.API.Resources.GetString("LOCLuduRestSelectLudusaviExecutable"),
                Filter = context.API.Resources.GetString("LOCLuduRestExecutableFilter"),
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                LudusaviPathTextBox.Text = dialog.FileName;
                ValidateLudusaviPath();
            }
        }

        private void ValidateLudusaviPath()
        {
            var path = LudusaviPathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                LudusaviStatusText.Text = context.API.Resources.GetString("LOCLuduRestPleaseSpecifyLudusaviPath");
                LudusaviStatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (ResticUtility.IsValidLudusaviExecutable(path))
            {
                LudusaviStatusText.Text = context.API.Resources.GetString("LOCLuduRestValidLudusaviExecutable");
                LudusaviStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                LudusaviStatusText.Text = context.API.Resources.GetString("LOCLuduRestInvalidLudusaviExecutable");
                LudusaviStatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void LudusaviPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateLudusaviPath();
        }

        private void BrowseRepository_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = context.API.Resources.GetString("LOCLuduRestSelectRepositoryLocation"),
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RepositoryPathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void SuggestedPaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuggestedPathsComboBox.SelectedItem is string selectedPath)
            {
                RepositoryPathTextBox.Text = selectedPath;
            }
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep > 0)
            {
                currentStep--;
                WizardTabs.SelectedIndex = currentStep;
                UpdateNavigationButtons();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateCurrentStep())
            {
                if (currentStep < TotalSteps - 1)
                {
                    currentStep++;
                    WizardTabs.SelectedIndex = currentStep;
                    UpdateNavigationButtons();

                    if (currentStep == 2) // Summary step
                    {
                        UpdateSummary();
                    }
                }
            }
        }

        private bool ValidateCurrentStep()
        {
            switch (currentStep)
            {
                case 0: // Restic executable
                    var resticPath = ResticPathTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(resticPath))
                    {
                        MessageBox.Show(context.API.Resources.GetString("LOCLuduRestValidationResticPath"), context.API.Resources.GetString("LOCLuduRestValidationError"),
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                    if (!ResticUtility.IsValidResticExecutable(resticPath))
                    {
                        MessageBox.Show(context.API.Resources.GetString("LOCLuduRestValidationResticInvalid"), context.API.Resources.GetString("LOCLuduRestValidationError"),
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                    return true;

                case 1: // Repository
                    var repoPath = RepositoryPathTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(repoPath))
                    {
                        MessageBox.Show(context.API.Resources.GetString("LOCLuduRestValidationRepositoryPath"), context.API.Resources.GetString("LOCLuduRestValidationError"),
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    var password = RepositoryPasswordBox.Password;
                    var confirmPassword = ConfirmPasswordBox.Password;

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        MessageBox.Show(context.API.Resources.GetString("LOCLuduRestValidationRepositoryPassword"), context.API.Resources.GetString("LOCLuduRestValidationError"),
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    if (password != confirmPassword)
                    {
                        MessageBox.Show(context.API.Resources.GetString("LOCLuduRestValidationPasswordMismatch"), context.API.Resources.GetString("LOCLuduRestValidationError"),
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    return true;

                default:
                    return true;
            }
        }

        private void UpdateSummary()
        {
            SummaryResticPath.Text = ResticPathTextBox.Text;
            SummaryRepositoryPath.Text = RepositoryPathTextBox.Text;
            SummaryPasswordStatus.Text = string.IsNullOrWhiteSpace(RepositoryPasswordBox.Password) ?
                context.API.Resources.GetString("LOCLuduRestSummaryNo") : context.API.Resources.GetString("LOCLuduRestSummaryYes");
            SummaryInitStatus.Text = CreateNewRepositoryCheckBox.IsChecked == true ?
                context.API.Resources.GetString("LOCLuduRestSummaryYes") : context.API.Resources.GetString("LOCLuduRestSummaryNo");
        }

        private void UpdateNavigationButtons()
        {
            PreviousButton.IsEnabled = currentStep > 0;
            NextButton.Visibility = currentStep < TotalSteps - 1 ? Visibility.Visible : Visibility.Collapsed;
            FinishButton.Visibility = currentStep == TotalSteps - 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void WizardTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentStep = WizardTabs.SelectedIndex;
            UpdateNavigationButtons();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create new settings with the configured values
                ResultSettings = new LudusaviResticSettings
                {
                    LudusaviExecutablePath = LudusaviPathTextBox.Text.Trim(),
                    ResticExecutablePath = ResticPathTextBox.Text.Trim(),
                    ResticRepository = RepositoryPathTextBox.Text.Trim(),
                    ResticPassword = RepositoryPasswordBox.Password
                };

                // Initialize repository if requested
                if (CreateNewRepositoryCheckBox.IsChecked == true)
                {
                    var tempContext = new BackupContext(context.API, ResultSettings);

                    // Show progress
                    var progressOptions = new GlobalProgressOptions(context.API.Resources.GetString("LOCLuduRestInitializingRepository"), true)
                    {
                        IsIndeterminate = true
                    };

                    bool initSuccess = false;
                    context.API.Dialogs.ActivateGlobalProgress((progress) =>
                    {
                        try
                        {
                            var result = ResticUtility.InitializeRepository(tempContext,
                                ResultSettings.ResticRepository, ResultSettings.ResticPassword);

                            if (result.ExitCode == 0)
                            {
                                initSuccess = true;
                                logger.Info("Repository initialized successfully");
                            }
                            else
                            {
                                logger.Error($"Repository initialization failed: {result.StdErr}");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Error during repository initialization");
                        }
                    }, progressOptions);

                    if (!initSuccess)
                    {
                        MessageBox.Show(context.API.Resources.GetString("LOCLuduRestInitializationFailed"),
                                      context.API.Resources.GetString("LOCLuduRestInitializationError"), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                SetupCompleted = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during setup completion");
                MessageBox.Show(string.Format(context.API.Resources.GetString("LOCLuduRestSetupErrorMessage"), ex.Message), context.API.Resources.GetString("LOCLuduRestSetupError"),
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
