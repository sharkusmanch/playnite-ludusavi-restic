using System.Windows;
using Playnite.SDK;

namespace LudusaviRestic
{
    public partial class BackupBrowserWindow : Window
    {
        public BackupBrowserWindow(BackupContext context)
        {
            InitializeComponent();
            ApplyPlayniteTheme(context.API);
            DataContext = new BackupBrowserViewModel(context);
        }

        public BackupBrowserWindow(BackupContext context, string gameFilter)
        {
            InitializeComponent();
            ApplyPlayniteTheme(context.API);
            DataContext = new BackupBrowserViewModel(context, gameFilter);
            Title = $"Backup Browser - {gameFilter}";
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
                // because the window inherits from Playnite's application resources
            }
        }
    }
}
