using Playnite.SDK;
using Playnite.SDK.Models;
using System.Threading;

namespace LudusaviRestic
{
    public class ResticBackupManager
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private BackupContext context;
        private SemaphoreSlim semaphore;

        public ResticBackupManager(LudusaviResticSettings settings, IPlayniteAPI api)
        {
            this.semaphore = new SemaphoreSlim(1);
            this.context = new BackupContext(api, settings);
        }

        public void PerformBackup(Game game)
        {
            BackupTask task = new BackupTask(game, this.semaphore, this.context);
            task.Run();
        }
    }
}