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
        private PruneResult _pruneResult;
        private ObservableCollection<DeletedSnapshotViewModel> _allSnapshots;
        private ObservableCollection<DeletedSnapshotViewModel> _filteredSnapshots;

        public PruneResultsWindow(PruneResult result)
        {
            InitializeComponent();
            this._pruneResult = result;
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
            TitleText.Text = _pruneResult.IsDryRun ? "Pruning Preview (Dry Run)" : "Pruning Results";

            // Set summary text
            var operationType = _pruneResult.IsDryRun ? "would be" : "were";
            SummaryText.Text = $"The following snapshots {operationType} deleted from the repository.";

            // Update statistics
            SnapshotsDeletedText.Text = _pruneResult.SnapshotsDeleted.ToString();
            GamesAffectedText.Text = _pruneResult.GamesAffected.ToString();
            DataDeletedText.Text = string.IsNullOrEmpty(_pruneResult.DataDeleted) ? "N/A" : _pruneResult.DataDeleted;
            StatusText.Text = _pruneResult.Success ? "Success" : "Failed";
            StatusText.Foreground = _pruneResult.Success ?
                System.Windows.Media.Brushes.Green :
                System.Windows.Media.Brushes.Red;

            // Convert snapshots to view models
            _allSnapshots = new ObservableCollection<DeletedSnapshotViewModel>(
                _pruneResult.DeletedSnapshots.Select(s => new DeletedSnapshotViewModel(s))
            );
            _filteredSnapshots = new ObservableCollection<DeletedSnapshotViewModel>(_allSnapshots);

            // Set up data grids
            SnapshotsDataGrid.ItemsSource = _filteredSnapshots;

            // Games summary
            var gamesSummary = _pruneResult.GetGameDeletionCounts()
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key);
            GamesSummaryDataGrid.ItemsSource = gamesSummary;

            // Tags summary
            var tagsSummary = _pruneResult.GetTagDeletionCounts()
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key);
            TagsSummaryDataGrid.ItemsSource = tagsSummary;

            // Raw output
            RawOutputTextBox.Text = _pruneResult.RawOutput;

            // Set up game filter
            var gameNames = new List<string> { "All Games" };
            gameNames.AddRange(_pruneResult.DeletedSnapshots
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
                _filteredSnapshots.Clear();
                foreach (var snapshot in _allSnapshots)
                {
                    _filteredSnapshots.Add(snapshot);
                }
            }
            else
            {
                _filteredSnapshots.Clear();
                foreach (var snapshot in _allSnapshots.Where(s => s.GameName == selectedGame))
                {
                    _filteredSnapshots.Add(snapshot);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    internal class DeletedSnapshotViewModel
    {
        private readonly DeletedSnapshot _snapshot;

        public DeletedSnapshotViewModel(DeletedSnapshot snapshot)
        {
            this._snapshot = snapshot;
        }

        public string ShortId => _snapshot.ShortId;
        public string GameName => string.IsNullOrEmpty(_snapshot.GameName) ? "Unknown" : _snapshot.GameName;
        public DateTime Time => _snapshot.Time;
        public string TagsString => string.Join(", ", _snapshot.Tags);
        public List<string> Tags => _snapshot.Tags;
        public string Host => _snapshot.Host;
        public List<string> Paths => _snapshot.Paths;
    }
}
