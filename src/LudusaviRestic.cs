using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace LudusaviRestic
{
    public class LudusaviRestic : GenericPlugin
    {
        internal LudusaviResticSettings settings { get; private set; }
        private ResticBackupManager backupManager;
        private static readonly ILogger logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("e9861c36-68a8-4654-8071-a9c50612bc24");

        public LudusaviRestic(IPlayniteAPI api) : base(api)
        {
            settings = LoadPluginSettings<LudusaviResticSettings>() ?? new LudusaviResticSettings();
            backupManager = new ResticBackupManager(settings, api);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override ISettings GetSettings(bool firstRunSettings) => settings;
        public override UserControl GetSettingsView(bool firstRunSettings) => new LudusaviResticSettingsView(this);

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var items = new List<GameMenuItem>();
            // Manual backup create snapshot
            items.Add(new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOCLuduRestBackupGMCreate"),
                MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),
                Action = a =>
                {
                    foreach (var g in args.Games) backupManager.PerformManualBackup(g);
                }
            });
            // Include tag add/remove
            items.Add(new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOCLuduRestBackupGMIncludeAdd"),
                MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),
                Action = a => ToggleTag(args.Games, settings.IncludeTagID, true, true)
            });
            items.Add(new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOCLuduRestBackupGMIncludeRemove"),
                MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),
                Action = a => ToggleTag(args.Games, settings.IncludeTagID, false, true)
            });
            // Exclude tag add/remove
            items.Add(new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOCLuduRestBackupGMExcludeAdd"),
                MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),
                Action = a => ToggleTag(args.Games, settings.ExcludeTagID, true, false)
            });
            items.Add(new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOCLuduRestBackupGMExcludeRemove"),
                MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),
                Action = a => ToggleTag(args.Games, settings.ExcludeTagID, false, false)
            });
            // View snapshots
            items.Add(new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOCLuduRestViewBackupSnapshots"),
                MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),
                Action = a =>
                {
                    foreach (var g in args.Games)
                    {
                        var ctx = new BackupContext(PlayniteApi, settings);
                        var win = new BackupBrowserWindow(ctx, g.Name);
                        win.ShowDialog();
                    }
                }
            });
            // Configure overrides (new window)
            items.Add(new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOCLuduRestGameMenuConfigureOverrides"),
                MenuSection = ResourceProvider.GetString("LOCLuduRestBackupGM"),
                Action = a =>
                {
                    foreach (var g in args.Games)
                    {
                        try
                        {
                            var win = new GameOverrideSettingsWindow(g, settings, PlayniteApi)
                            {
                                Owner = PlayniteApi.Dialogs.GetCurrentAppWindow()
                            };
                            if (win.ShowDialog() == true)
                            {
                                SavePluginSettings(settings);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Error opening per-game settings for {g.Name}");
                        }
                    }
                }
            });
            return items;
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            var root = "@Ludusavi Restic";
            var items = new List<MainMenuItem>();
            items.Add(new MainMenuItem
            {
                Description = ResourceProvider.GetString("LOCLuduRestBackupAllGames"),
                MenuSection = root,
                Action = a => backupManager.BackupAllGames()
            });
            items.Add(new MainMenuItem
            {
                Description = ResourceProvider.GetString("LOCLuduRestBrowseBackupSnapshots"),
                MenuSection = root,
                Action = a =>
                {
                    var ctx = new BackupContext(PlayniteApi, settings);
                    var win = new BackupBrowserWindow(ctx);
                    win.ShowDialog();
                }
            });
            items.Add(new MainMenuItem
            {
                Description = ResourceProvider.GetString("LOCLuduRestMainMenuViewOverrides"),
                MenuSection = root,
                Action = a => ShowOverridesSummary()
            });
            return items;
        }

        private void ShowOverridesSummary()
        {
            try
            {
                if (settings.GameSettings == null || settings.GameSettings.Count == 0 || !settings.GameSettings.Any(kv => kv.Value.OverrideGlobalSettings))
                {
                    PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCLuduRestOverridesDialogNoGames"), ResourceProvider.GetString("LOCLuduRestOverridesDialogTitle"));
                    return;
                }

                var lines = new List<string>();
                foreach (var kv in settings.GameSettings.Where(k => k.Value.OverrideGlobalSettings))
                {
                    var game = PlayniteApi.Database.Games.Get(kv.Key);
                    if (game != null)
                    {
                        lines.Add(game.Name);
                    }
                }

                if (lines.Count == 0)
                {
                    PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCLuduRestOverridesDialogNoGames"), ResourceProvider.GetString("LOCLuduRestOverridesDialogTitle"));
                    return;
                }

                var listHeader = ResourceProvider.GetString("LOCLuduRestOverridesDialogListHeader");
                var message = listHeader + "\n\n" + string.Join("\n", lines.OrderBy(l => l));
                var clearPrompt = ResourceProvider.GetString("LOCLuduRestOverridesDialogClearAllPrompt");

                var result = PlayniteApi.Dialogs.ShowMessage(message + "\n\n" + clearPrompt, ResourceProvider.GetString("LOCLuduRestOverridesDialogTitle"), System.Windows.MessageBoxButton.YesNo);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var keys = settings.GameSettings.Where(k => k.Value.OverrideGlobalSettings).Select(k => k.Key).ToList();
                    foreach (var k in keys)
                    {
                        settings.GameSettings.Remove(k);
                    }
                    SavePluginSettings(settings);
                    PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCLuduRestOverridesClearedMessage"), ResourceProvider.GetString("LOCLuduRestOverridesCleared"));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error showing overrides summary");
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, ResourceProvider.GetString("LOCLuduRestOverridesDialogTitle"));
            }
        }

        private void ToggleTag(IEnumerable<Game> games, Guid tagId, bool add, bool includeTag)
        {
            // Ensure tag exists
            if (tagId == Guid.Empty)
            {
                var tagName = includeTag ? ResourceProvider.GetString("LOCLuduRestBackupIncludeTag") : ResourceProvider.GetString("LOCLuduRestBackupExcludeTag");
                var existing = PlayniteApi.Database.Tags.FirstOrDefault(t => t.Name == tagName);
                if (existing == null)
                {
                    existing = PlayniteApi.Database.Tags.Add(tagName);
                }
                if (includeTag) settings.IncludeTagID = existing.Id; else settings.ExcludeTagID = existing.Id;
                tagId = existing.Id;
                SavePluginSettings(settings);
            }
            foreach (var g in games)
            {
                var changed = false;
                var tags = g.TagIds == null ? new List<Guid>() : g.TagIds.ToList();
                if (add)
                {
                    if (!tags.Contains(tagId)) { tags.Add(tagId); changed = true; }
                }
                else
                {
                    if (tags.Contains(tagId)) { tags.Remove(tagId); changed = true; }
                }
                if (changed)
                {
                    g.TagIds = tags;
                    PlayniteApi.Database.Games.Update(g);
                }
            }
        }
    }
}
// Moved from root to Core folder
// ...existing code...
