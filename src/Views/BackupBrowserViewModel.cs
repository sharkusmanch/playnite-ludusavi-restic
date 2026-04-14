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
using System.IO;

namespace LudusaviRestic
{
    public class BackupBrowserViewModel : INotifyPropertyChanged
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private BackupContext _context;
        private ObservableCollection<BackupSnapshot> _snapshots;
        private ObservableCollection<BackupSnapshot> _allSnapshots;
        private BackupSnapshot _selectedSnapshot;
        private bool _isLoading;
        private string _selectedGameFilter;
        private ObservableCollection<string> _gameFilters;

        public ObservableCollection<BackupSnapshot> Snapshots
        {
            get => _snapshots;
            set
            {
                if (_snapshots != null)
                {
                    _snapshots.CollectionChanged -= Snapshots_CollectionChanged;
                }
                _snapshots = value;
                if (_snapshots != null)
                {
                    _snapshots.CollectionChanged += Snapshots_CollectionChanged;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(SnapshotCount));
            }
        }

        public int SnapshotCount => Snapshots?.Count ?? 0;

        private void Snapshots_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(SnapshotCount));
        }

        public ObservableCollection<string> GameFilters
        {
            get => _gameFilters;
            set
            {
                _gameFilters = value;
                OnPropertyChanged();
            }
        }

        public string SelectedGameFilter
        {
            get => _selectedGameFilter;
            set
            {
                _selectedGameFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public BackupSnapshot SelectedSnapshot
        {
            get => _selectedSnapshot;
            set
            {
                _selectedSnapshot = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; private set; }
        public ICommand DeleteSnapshotCommand { get; private set; }

        public BackupBrowserViewModel(BackupContext context)
        {
            this._context = context;
            this.Snapshots = new ObservableCollection<BackupSnapshot>();
            this._allSnapshots = new ObservableCollection<BackupSnapshot>();
            this.GameFilters = new ObservableCollection<string>();

            RefreshCommand = new RelayCommand(RefreshSnapshots);
            DeleteSnapshotCommand = new RelayCommand(DeleteSnapshot, CanDeleteSnapshot);
            RefreshSnapshots();
        }

        public BackupBrowserViewModel(BackupContext context, string gameFilter)
        {
            this._context = context;
            this.Snapshots = new ObservableCollection<BackupSnapshot>();
            this._allSnapshots = new ObservableCollection<BackupSnapshot>();
            this.GameFilters = new ObservableCollection<string>();
            this._selectedGameFilter = gameFilter; // preset desired filter; will be applied after load
            RefreshCommand = new RelayCommand(RefreshSnapshots);
            DeleteSnapshotCommand = new RelayCommand(DeleteSnapshot, CanDeleteSnapshot);
            RefreshSnapshots();
        }

        internal static IList<BackupSnapshot> FilterSnapshots(IList<BackupSnapshot> all, string gameFilter)
        {
            if (all == null) return new List<BackupSnapshot>();
            if (string.IsNullOrEmpty(gameFilter) || gameFilter == "All Games")
            {
                return new List<BackupSnapshot>(all);
            }
            return all.Where(s => s.GameName == gameFilter).ToList();
        }

        private void ApplyFilter()
        {
            if (_allSnapshots == null) return;
            Snapshots = new ObservableCollection<BackupSnapshot>(FilterSnapshots(_allSnapshots, SelectedGameFilter));
        }

        internal static IList<BackupSnapshot> ParseSnapshots(string json)
        {
            var snapshotArray = JArray.Parse(json);
            var snapshotList = new List<BackupSnapshot>();
            foreach (var item in snapshotArray)
            {
                snapshotList.Add(new BackupSnapshot
                {
                    Id = item["short_id"]?.ToString() ?? item["id"]?.ToString(),
                    Date = DateTime.Parse(item["time"]?.ToString(), null, DateTimeStyles.RoundtripKind),
                    Tags = item["tags"]?.ToObject<List<string>>() ?? new List<string>()
                });
            }
            return snapshotList;
        }

        internal static IList<string> BuildGameFilters(IList<BackupSnapshot> snapshots)
        {
            var gameNames = snapshots.Select(s => s.GameName).Where(n => n != "Unknown").Distinct().OrderBy(n => n).ToList();
            var filters = new List<string> { "All Games" };
            filters.AddRange(gameNames);
            return filters;
        }

        private void RefreshSnapshots()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                ResourceProvider.GetString("LOCLuduRestLoadingBackupSnapshots"),
                true
            )
            { IsIndeterminate = true };

            _context.API.Dialogs.ActivateGlobalProgress(_ =>
            {
                try
                {
                    var result = ResticCommand.ListSnapshots(_context);
                    if (result.ExitCode == 0)
                    {
                        var parsed = ParseSnapshots(result.StdOut);
                        _allSnapshots = new ObservableCollection<BackupSnapshot>(parsed);
                        GameFilters = new ObservableCollection<string>(BuildGameFilters(parsed));
                        // If a specific game filter was preset (e.g., via game context menu), try to apply it
                        if (!string.IsNullOrEmpty(_selectedGameFilter) && GameFilters.Contains(_selectedGameFilter))
                        {
                            SelectedGameFilter = _selectedGameFilter; // triggers ApplyFilter()
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(SelectedGameFilter) || !GameFilters.Contains(SelectedGameFilter))
                            {
                                SelectedGameFilter = "All Games";
                            }
                            else
                            {
                                ApplyFilter();
                            }
                        }
                    }
                    else
                    {
                        logger.Error($"Failed to list snapshots: {result.StdErr}");
                        _context.API.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCLuduRestFailedToLoadSnapshots"));
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error loading snapshots");
                    _context.API.Dialogs.ShowErrorMessage(string.Format(ResourceProvider.GetString("LOCLuduRestErrorLoadingSnapshots"), ex.Message));
                }
            }, globalProgressOptions);
        }

        private bool CanDeleteSnapshot() => SelectedSnapshot != null && !IsLoading;

        internal static void RemoveSnapshotFromCache(
            ICollection<BackupSnapshot> allSnapshots,
            ICollection<BackupSnapshot> visibleSnapshots,
            BackupSnapshot snapshot)
        {
            allSnapshots.Remove(snapshot);
            visibleSnapshots.Remove(snapshot);
        }

        private void DeleteSnapshot()
        {
            if (SelectedSnapshot == null) return;
            var result = _context.API.Dialogs.ShowMessage(
                string.Format(ResourceProvider.GetString("LOCLuduRestDeleteSnapshotConfirmation"), SelectedSnapshot.ShortId, SelectedSnapshot.Date.ToString("yyyy-MM-dd HH:mm")),
                ResourceProvider.GetString("LOCLuduRestDeleteBackupSnapshot"),
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                var deleteResult = ResticCommand.ForgetSnapshot(_context, SelectedSnapshot.Id);
                if (deleteResult.ExitCode == 0)
                {
                    RemoveSnapshotFromCache(_allSnapshots, Snapshots, SelectedSnapshot);
                    SelectedSnapshot = null;
                    _context.API.Dialogs.ShowMessage(ResourceProvider.GetString("LOCLuduRestSnapshotDeletedSuccessfully"), ResourceProvider.GetString("LOCLuduRestSuccess"));
                }
                else
                {
                    logger.Error($"Failed to delete snapshot: {deleteResult.StdErr}");
                    _context.API.Dialogs.ShowErrorMessage(string.Format(ResourceProvider.GetString("LOCLuduRestFailedToDeleteSnapshot"), deleteResult.StdErr));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error deleting snapshot");
                _context.API.Dialogs.ShowErrorMessage(string.Format(ResourceProvider.GetString("LOCLuduRestErrorDeletingSnapshot"), ex.Message));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            this._execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this._canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
