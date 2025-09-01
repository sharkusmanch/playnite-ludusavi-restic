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
                ResticStatusText.Text = "✓ Restic executable found and verified.";
                ResticStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ResticStatusText.Text = "⚠ Could not automatically detect restic. Please specify the path manually.";
                ResticStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            }
        }

        private void AutoDetectLudusavi()
        {
            var detectedPath = ResticUtility.DetectLudusaviExecutable();
            if (!string.IsNullOrWhiteSpace(detectedPath))
            {
                LudusaviPathTextBox.Text = detectedPath;
                LudusaviStatusText.Text = "✓ Ludusavi executable found and verified.";
                LudusaviStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                LudusaviStatusText.Text = "⚠ Could not automatically detect ludusavi. Please specify the path manually.";
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
                Title = "Select Restic Executable",
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
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
                ResticStatusText.Text = "Please specify the path to restic executable.";
                ResticStatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (ResticUtility.IsValidResticExecutable(path))
            {
                ResticStatusText.Text = "✓ Restic executable is valid.";
                ResticStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                ResticStatusText.Text = "✗ Invalid restic executable or not accessible.";
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
                Title = "Select Ludusavi Executable",
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
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
                LudusaviStatusText.Text = "Please specify the path to ludusavi executable.";
                LudusaviStatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            if (ResticUtility.IsValidLudusaviExecutable(path))
            {
                LudusaviStatusText.Text = "✓ Ludusavi executable is valid.";
                LudusaviStatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                LudusaviStatusText.Text = "✗ Invalid ludusavi executable or not accessible.";
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
                Description = "Select Repository Location",
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
                        MessageBox.Show("Please specify the path to restic executable.", "Validation Error",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                    if (!ResticUtility.IsValidResticExecutable(resticPath))
                    {
                        MessageBox.Show("The specified restic executable is not valid or accessible.", "Validation Error",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                    return true;

                case 1: // Repository
                    var repoPath = RepositoryPathTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(repoPath))
                    {
                        MessageBox.Show("Please specify the repository path.", "Validation Error",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    var password = RepositoryPasswordBox.Password;
                    var confirmPassword = ConfirmPasswordBox.Password;

                    if (string.IsNullOrWhiteSpace(password))
                    {
                        MessageBox.Show("Please enter a repository password.", "Validation Error",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    if (password != confirmPassword)
                    {
                        MessageBox.Show("Passwords do not match.", "Validation Error",
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
            SummaryPasswordStatus.Text = string.IsNullOrWhiteSpace(RepositoryPasswordBox.Password) ? "No" : "Yes";
            SummaryInitStatus.Text = CreateNewRepositoryCheckBox.IsChecked == true ? "Yes" : "No";
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
                    var progressOptions = new GlobalProgressOptions("Initializing repository...", true)
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
                        MessageBox.Show("Failed to initialize the repository. Please check the logs for details.",
                                      "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"An error occurred during setup: {ex.Message}", "Setup Error",
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
