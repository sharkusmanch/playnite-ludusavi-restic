using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using Playnite.SDK;

namespace LudusaviRestic
{
    public partial class PruneResultsWindow : Window
    {
        private PruneResult pruneResult;
        private ObservableCollection<DeletedSnapshotViewModel> allSnapshots;
        private ObservableCollection<DeletedSnapshotViewModel> filteredSnapshots;

        public PruneResultsWindow(PruneResult result)
        {
            InitializeComponent();
            this.pruneResult = result;
            ApplyPlayniteTheme();
            InitializeData();
        }

        private void ApplyPlayniteTheme()
        {
            // Try to get the current Playnite API instance from the BackupContext
            // This is a basic approach - in a full implementation, we'd pass the API through
            try
            {
                var app = System.Windows.Application.Current;
                if (app?.Resources != null)
                {
                    // The DynamicResource bindings in XAML should automatically pick up theme changes
                    // This method serves as a placeholder for any additional theme setup if needed
                }
            }
            catch
            {
                // If theme resources aren't available, the DynamicResource bindings should still work
            }
        }

        private void InitializeData()
        {
            // Set title based on operation type
            TitleText.Text = pruneResult.IsDryRun ? "Pruning Preview (Dry Run)" : "Pruning Results";

            // Set summary text
            var operationType = pruneResult.IsDryRun ? "would be" : "were";
            SummaryText.Text = $"The following snapshots {operationType} deleted from the repository.";

            // Update statistics
            SnapshotsDeletedText.Text = pruneResult.SnapshotsDeleted.ToString();
            GamesAffectedText.Text = pruneResult.GamesAffected.ToString();
            DataDeletedText.Text = string.IsNullOrEmpty(pruneResult.DataDeleted) ? "N/A" : pruneResult.DataDeleted;
            StatusText.Text = pruneResult.Success ? "Success" : "Failed";
            StatusText.Foreground = pruneResult.Success ?
                System.Windows.Media.Brushes.Green :
                System.Windows.Media.Brushes.Red;

            // Convert snapshots to view models
            allSnapshots = new ObservableCollection<DeletedSnapshotViewModel>(
                pruneResult.DeletedSnapshots.Select(s => new DeletedSnapshotViewModel(s))
            );
            filteredSnapshots = new ObservableCollection<DeletedSnapshotViewModel>(allSnapshots);

            // Set up data grids
            SnapshotsDataGrid.ItemsSource = filteredSnapshots;

            // Games summary
            var gamesSummary = pruneResult.GetGameDeletionCounts()
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key);
            GamesSummaryDataGrid.ItemsSource = gamesSummary;

            // Tags summary
            var tagsSummary = pruneResult.GetTagDeletionCounts()
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key);
            TagsSummaryDataGrid.ItemsSource = tagsSummary;

            // Raw output
            RawOutputTextBox.Text = pruneResult.RawOutput;

            // Set up game filter
            var gameNames = new List<string> { "All Games" };
            gameNames.AddRange(pruneResult.DeletedSnapshots
                .Where(s => !string.IsNullOrEmpty(s.GameName))
                .Select(s => s.GameName)
                .Distinct()
                .OrderBy(g => g));
            GameFilterComboBox.ItemsSource = gameNames;
            GameFilterComboBox.SelectedIndex = 0;
        }

        private void GameFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyGameFilter();
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            GameFilterComboBox.SelectedIndex = 0;
        }

        private void ApplyGameFilter()
        {
            var selectedGame = GameFilterComboBox.SelectedItem as string;

            if (string.IsNullOrEmpty(selectedGame) || selectedGame == "All Games")
            {
                filteredSnapshots.Clear();
                foreach (var snapshot in allSnapshots)
                {
                    filteredSnapshots.Add(snapshot);
                }
            }
            else
            {
                filteredSnapshots.Clear();
                foreach (var snapshot in allSnapshots.Where(s => s.GameName == selectedGame))
                {
                    filteredSnapshots.Add(snapshot);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class DeletedSnapshotViewModel
    {
        private readonly DeletedSnapshot snapshot;

        public DeletedSnapshotViewModel(DeletedSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public string ShortId => snapshot.ShortId;
        public string GameName => string.IsNullOrEmpty(snapshot.GameName) ? "Unknown" : snapshot.GameName;
        public DateTime Time => snapshot.Time;
        public string TagsString => string.Join(", ", snapshot.Tags);
        public List<string> Tags => snapshot.Tags;
        public string Host => snapshot.Host;
        public List<string> Paths => snapshot.Paths;
    }
}
