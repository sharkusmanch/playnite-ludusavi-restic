using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Threading;

namespace LudusaviRestic
{
    public class LudusaviRestic : GenericPlugin
    {
        public LudusaviResticSettings settings { get; set; }
        private static readonly ILogger logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("e9861c36-68a8-4654-8071-a9c50612bc24");

        private ResticBackupManager manager;
        private Timer timer;

        public LudusaviRestic(IPlayniteAPI api) : base(api)
        {
            this.settings = new LudusaviResticSettings(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            this.manager = new ResticBackupManager(this.settings, this.PlayniteApi);
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs menuArgs)
        {
            return new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    Description = "Backup all games",
                    MenuSection = "@" + ResourceProvider.GetString("LOCLuduRestBackupGM"),
                    Action = args => {
                        this.manager.BackupAllGames();
                    }
                }
            };
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs menuArgs)
        {
            return new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    Description = ResourceProvider.GetString("LOCLuduRestBackupGMCreate"),
                    MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),

                    Action = args => {
                        foreach (var game in args.Games)
                        {
                            this.manager.PerformManualBackup(game);
                        }
                    }
                }
            };
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (args?.Game is null) return;

            if (settings.BackupDuringGameplay)
            {
                this.timer = new Timer(GameplayBackupTimerElapsed, args.Game,
                    settings.GameplayBackupInterval * 60000,
                    settings.GameplayBackupInterval * 60000);
            }
        }

        private void GameplayBackupTimerElapsed(Object obj)
        {
            Game game = (Game)obj;
            this.manager.PerformGameplayBackup(game);
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            this.timer?.Dispose();

            if (this.settings.BackupWhenGameStopped)
            {
                string caption = ResourceProvider.GetString("LOCLuduRestGameStoppedPromptCaption");
                string message = ResourceProvider.GetString("LOCLuduRestGameStoppedPromptMessage");


                if (this.settings.PromptForGameStoppedTag)
                {
                    StringSelectionDialogResult result = PlayniteApi.Dialogs.SelectString(
                        $"{message}: {args.Game.Name}",
                        caption,
                        ""
                    );

                    if (result.Result)
                    {
                        logger.Debug($"Custom backup tag provided: {result.SelectedString}");
                    }

                    this.manager.PerformGameStoppedBackup(args.Game, result.SelectedString);
                }
                else
                {
                    this.manager.PerformGameStoppedBackup(args.Game);
                }
            }
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
