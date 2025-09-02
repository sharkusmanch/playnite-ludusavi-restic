using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;

namespace LudusaviRestic
{
    public class BackupBrowserViewModel : INotifyPropertyChanged
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private BackupContext context;
        private ObservableCollection<BackupSnapshot> snapshots;
        private ObservableCollection<BackupSnapshot> allSnapshots;
        private BackupSnapshot selectedSnapshot;
        private bool isLoading;
        private string selectedGameFilter;
        private ObservableCollection<string> gameFilters;

        public ObservableCollection<BackupSnapshot> Snapshots
        {
            get => snapshots;
            set
            {
                snapshots = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> GameFilters
        {
            get => gameFilters;
            set
            {
                gameFilters = value;
                OnPropertyChanged();
            }
        }

        public string SelectedGameFilter
        {
            get => selectedGameFilter;
            set
            {
                selectedGameFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public BackupSnapshot SelectedSnapshot
        {
            get => selectedSnapshot;
            set
            {
                selectedSnapshot = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => isLoading;
            set
            {
                isLoading = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand DeleteSnapshotCommand { get; }
    public ICommand RestoreSnapshotCommand { get; }

        public BackupBrowserViewModel(BackupContext context)
        {
            this.context = context;
            this.Snapshots = new ObservableCollection<BackupSnapshot>();
            this.allSnapshots = new ObservableCollection<BackupSnapshot>();
            this.GameFilters = new ObservableCollection<string>();

            RefreshCommand = new RelayCommand(RefreshSnapshots);
            DeleteSnapshotCommand = new RelayCommand(DeleteSnapshot, CanDeleteSnapshot);
            RestoreSnapshotCommand = new RelayCommand(RestoreSnapshot, CanRestoreSnapshot);

            RefreshSnapshots();
        }

        public BackupBrowserViewModel(BackupContext context, string gameFilter)
        {
            this.context = context;
            this.Snapshots = new ObservableCollection<BackupSnapshot>();
            this.allSnapshots = new ObservableCollection<BackupSnapshot>();
            this.GameFilters = new ObservableCollection<string>();

            RefreshCommand = new RelayCommand(RefreshSnapshots);
            DeleteSnapshotCommand = new RelayCommand(DeleteSnapshot, CanDeleteSnapshot);
            RestoreSnapshotCommand = new RelayCommand(RestoreSnapshot, CanRestoreSnapshot);

            // Set the initial game filter
            this.selectedGameFilter = gameFilter;

            RefreshSnapshots();
        }

        private void ApplyFilter()
        {
            if (allSnapshots == null) return;

            if (string.IsNullOrEmpty(SelectedGameFilter) || SelectedGameFilter == "All Games")
            {
                Snapshots = new ObservableCollection<BackupSnapshot>(allSnapshots);
            }
            else
            {
                var filteredSnapshots = allSnapshots.Where(s => s.GameName == SelectedGameFilter).ToList();
                Snapshots = new ObservableCollection<BackupSnapshot>(filteredSnapshots);
            }
        }

        private void RefreshSnapshots()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                "Loading backup snapshots...",
                true
            );
            globalProgressOptions.IsIndeterminate = true;

            context.API.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                try
                {
                    var result = ResticCommand.ListSnapshots(context);
                    if (result.ExitCode == 0)
                    {
                        var snapshotArray = JArray.Parse(result.StdOut);
                        var snapshotList = new ObservableCollection<BackupSnapshot>();

                        foreach (var item in snapshotArray)
                        {
                            var snapshot = new BackupSnapshot
                            {
                                Id = item["short_id"]?.ToString() ?? item["id"]?.ToString(),
                                Date = DateTime.Parse(item["time"]?.ToString(), null, DateTimeStyles.RoundtripKind),
                                Tags = item["tags"]?.ToObject<List<string>>() ?? new List<string>()
                            };
                            snapshotList.Add(snapshot);
                        }

                        allSnapshots = snapshotList;

                        // Extract unique game names (first tag) for filtering
                        var gameNames = allSnapshots
                            .Select(s => s.GameName)
                            .Where(name => name != "Unknown")
                            .Distinct()
                            .OrderBy(name => name)
                            .ToList();

                        GameFilters = new ObservableCollection<string> { "All Games" };
                        foreach (var gameName in gameNames)
                        {
                            GameFilters.Add(gameName);
                        }

                        // Set default filter or preserve existing filter
                        if (string.IsNullOrEmpty(SelectedGameFilter))
                        {
                            SelectedGameFilter = "All Games";
                        }
                        else
                        {
                            // Ensure the pre-set filter exists in the list, if not default to "All Games"
                            if (!GameFilters.Contains(SelectedGameFilter))
                            {
                                SelectedGameFilter = "All Games";
                            }
                        }
                        ApplyFilter();
                    }
                    else
                    {
                        logger.Error($"Failed to list snapshots: {result.StdErr}");
                        context.API.Dialogs.ShowErrorMessage("Failed to load backup snapshots. Check your restic configuration.");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error loading snapshots");
                    context.API.Dialogs.ShowErrorMessage($"Error loading snapshots: {ex.Message}");
                }
            }, globalProgressOptions);
        }
        private bool CanDeleteSnapshot()
        {
            return SelectedSnapshot != null && !IsLoading;
        }

        private void DeleteSnapshot()
        {
            if (SelectedSnapshot == null) return;

            var result = context.API.Dialogs.ShowMessage(
                string.Format(ResourceProvider.GetString("LOCLuduRestDeleteSnapshotConfirmation"), SelectedSnapshot.ShortId, SelectedSnapshot.Date.ToString("yyyy-MM-dd HH:mm")),
                ResourceProvider.GetString("LOCLuduRestDeleteBackupSnapshot"),
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    var deleteResult = ResticCommand.ForgetSnapshot(context, SelectedSnapshot.Id);
                    if (deleteResult.ExitCode == 0)
                    {
                        // Remove from both collections
                        Snapshots.Remove(SelectedSnapshot);
                        allSnapshots.Remove(SelectedSnapshot);
                        SelectedSnapshot = null;
                        context.API.Dialogs.ShowMessage(ResourceProvider.GetString("LOCLuduRestSnapshotDeletedSuccessfully"), ResourceProvider.GetString("LOCLuduRestSuccess"));
                    }
                    else
                    {
                        logger.Error($"Failed to delete snapshot: {deleteResult.StdErr}");
                        context.API.Dialogs.ShowErrorMessage($"Failed to delete snapshot: {deleteResult.StdErr}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error deleting snapshot");
                    context.API.Dialogs.ShowErrorMessage($"Error deleting snapshot: {ex.Message}");
                }
            }
        }

        private bool CanRestoreSnapshot()
        {
            return SelectedSnapshot != null && !IsLoading;
        }

        private void RestoreSnapshot()
        {
            if (SelectedSnapshot == null) return;

            // Ask user to select destination directory
            var destination = context.API.Dialogs.SelectFolder();
            if (string.IsNullOrEmpty(destination))
            {
                return; // user cancelled
            }

            var confirm = context.API.Dialogs.ShowMessage(
                $"Restore snapshot {SelectedSnapshot.ShortId} to {destination}? This may overwrite existing files.",
                "Restore Snapshot",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }

            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"Restoring snapshot {SelectedSnapshot.ShortId}...",
                true
            );
            globalProgressOptions.IsIndeterminate = true;

            context.API.Dialogs.ActivateGlobalProgress(a =>
            {
                try
                {
                    var result = ResticCommand.RestoreSnapshot(context, SelectedSnapshot.Id, destination);
                    if (result.ExitCode == 0)
                    {
                        context.API.Dialogs.ShowMessage($"Snapshot {SelectedSnapshot.ShortId} restored successfully.", "Restore Complete");
                    }
                    else
                    {
                        logger.Error($"Failed to restore snapshot: {result.StdErr}");
                        context.API.Dialogs.ShowErrorMessage($"Failed to restore snapshot: {result.StdErr}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error restoring snapshot");
                    context.API.Dialogs.ShowErrorMessage($"Error restoring snapshot: {ex.Message}");
                }
            }, globalProgressOptions);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            execute();
        }
    }
}
