using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace LudusaviRestic
{
    public class LudusaviRestic : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private string NotificationID = "Lususavi Restic";

        public LudusaviResticSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("e9861c36-68a8-4654-8071-a9c50612bc24");

        private ResticBackupManager manager;

        public LudusaviRestic(IPlayniteAPI api) : base(api)
        { 
            this.settings = new LudusaviResticSettings(this);
            this.manager = new ResticBackupManager(this.settings, this.PlayniteApi);
        }

        public override List<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs menuArgs)
        {
            return new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    Description = "Create save snapshot",
                    MenuSection = "Ludusavi Restic Snapshot",
                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            this.manager.PerformBackup(game);
                        }
                    }
                }
            };
        }

        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            this.manager.PerformBackup(game);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new LudusaviResticSettingsView(this);
        }
    }
}