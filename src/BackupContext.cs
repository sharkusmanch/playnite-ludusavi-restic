using Playnite.SDK;

namespace LudusaviRestic
{
    public class BackupContext
    {
        public IPlayniteAPI API { get; }
        public LudusaviResticSettings Settings { get; }
        public string NotificationID { get; } = "LudusaviResticNotification";

        public BackupContext(IPlayniteAPI api, LudusaviResticSettings settings)
        {
            API = api;
            Settings = settings;
        }
    }
}

