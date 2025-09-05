using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LudusaviRestic
{
    public class LudusaviRestic : GenericPlugin
    {
        internal LudusaviResticSettings settings { get; private set; }
        private ResticBackupManager backupManager;
        private static readonly ILogger logger = LogManager.GetLogger();

        private static string SafeLoc(string key, string fallback)
        {
            try
            {
                var val = ResourceProvider.GetString(key);
                if (string.IsNullOrWhiteSpace(val) || (val.StartsWith("<!") && val.EndsWith("!>")))
                {
                    return fallback;
                }
                return val;
            }
            catch
            {
                return fallback;
            }
        }

        public override Guid Id { get; } = Guid.Parse("e9861c36-68a8-4654-8071-a9c50612bc24");

        public LudusaviRestic(IPlayniteAPI api) : base(api)
        {
            settings = LoadPluginSettings<LudusaviResticSettings>() ?? new LudusaviResticSettings();
            backupManager = new ResticBackupManager(settings, api);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };

            // Defer localization diagnostics to OnApplicationStarted (Playnite loads dictionaries automatically from plugin folder root).
        }
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            try
            {
                var asmPath = Path.GetDirectoryName(GetType().Assembly.Location);
                logger.Info($"[LudusaviRestic] Assembly path: {asmPath}");
                // Playnite copies plugin contents into Extensions/<Id> directory; ensure Localization dir exists there.
                if (!string.IsNullOrEmpty(asmPath))
                {
                    // Typical structure: .../Extensions/LudusaviRestic_<guid>/LudusaviRestic.dll
                    var locDir = Path.Combine(asmPath, "Localization");
                    logger.Info($"[LudusaviRestic] Expected localization dir: {locDir} Exists={Directory.Exists(locDir)}");
                    if (Directory.Exists(locDir))
                    {
                        var files = Directory.GetFiles(locDir, "*.xaml");
                        logger.Info($"[LudusaviRestic] Localization files present: {string.Join(", ", files.Select(Path.GetFileName))}");
                        // Spot check one key via ResourceProvider to see what we get
                        var test = ResourceProvider.GetString("LOCLuduRestBackupAllGames");
                        logger.Info($"[LudusaviRestic] Test lookup LOCLuduRestBackupAllGames => '{test}'");
                    }
                }

                EnsureLocalizationLoaded();
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "[LudusaviRestic] OnApplicationStarted localization diagnostics failed");
            }
        }

        private bool localizationAttempted;
        private void EnsureLocalizationLoaded()
        {
            if (localizationAttempted) return; // only once
            localizationAttempted = true;
            try
            {
                // If a known key already resolves properly, skip
                var probe = ResourceProvider.GetString("LOCLuduRestBackupAllGames");
                if (!string.IsNullOrEmpty(probe) && !(probe.StartsWith("<!") && probe.EndsWith("!>")))
                {
                    logger.Info("[LudusaviRestic] Localization already resolved by Playnite.");
                    return;
                }

                var asmPath = Path.GetDirectoryName(GetType().Assembly.Location);
                if (string.IsNullOrEmpty(asmPath)) return;
                var locDir = Path.Combine(asmPath, "Localization");
                if (!Directory.Exists(locDir))
                {
                    logger.Warn("[LudusaviRestic] Localization directory not found; keys will show placeholders.");
                    return;
                }

                // Determine current UI language; fallback to en_US
                var lang = PlayniteApi?.ApplicationSettings?.Language ?? "en_US";
                var primaryFile = Path.Combine(locDir, lang + ".xaml");
                var fallbackFile = Path.Combine(locDir, "en_US.xaml");
                var filesToLoad = new List<string>();
                if (File.Exists(primaryFile)) filesToLoad.Add(primaryFile);
                if (File.Exists(fallbackFile) && !filesToLoad.Contains(fallbackFile)) filesToLoad.Add(fallbackFile);
                if (filesToLoad.Count == 0)
                {
                    logger.Warn($"[LudusaviRestic] No localization files found for '{lang}' nor fallback en_US.");
                    return;
                }
                foreach (var f in filesToLoad)
                {
                    try
                    {
                        using (var fs = File.OpenRead(f))
                        {
                            var dict = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(fs);
                            Application.Current?.Resources?.MergedDictionaries.Add(dict);
                            logger.Info($"[LudusaviRestic] Loaded localization dictionary {Path.GetFileName(f)} ({dict.Count} entries)");
                        }
                    }
                    catch (Exception exFile)
                    {
                        logger.Warn(exFile, $"[LudusaviRestic] Failed to load localization dictionary {f}");
                    }
                }
                var probeAfter = ResourceProvider.GetString("LOCLuduRestBackupAllGames");
                logger.Info($"[LudusaviRestic] Post-load probe LOCLuduRestBackupAllGames => '{probeAfter}'");
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "[LudusaviRestic] EnsureLocalizationLoaded failed");
            }
        }

        public override ISettings GetSettings(bool firstRunSettings) => settings;
        public override UserControl GetSettingsView(bool firstRunSettings) => new LudusaviResticSettingsView(this);

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var items = new List<GameMenuItem>();
            // Manual backup create snapshot
            items.Add(new GameMenuItem
            {
                Description = SafeLoc("LOCLuduRestBackupGMCreate", "Create save snapshot"),
                MenuSection = SafeLoc("LOCLuduRestBackupGM", "LudusaviRestic"),
                Action = a =>
                {
                    foreach (var g in args.Games) backupManager.PerformManualBackup(g);
                }
            });
            // Include tag add/remove
            items.Add(new GameMenuItem
            {
                Description = SafeLoc("LOCLuduRestBackupGMIncludeAdd", "Add backup include tag"),
                MenuSection = SafeLoc("LOCLuduRestBackupGM", "LudusaviRestic"),
                Action = a => ToggleTag(args.Games, settings.IncludeTagID, true, true)
            });
            items.Add(new GameMenuItem
            {
                Description = SafeLoc("LOCLuduRestBackupGMIncludeRemove", "Remove backup include tag"),
                MenuSection = SafeLoc("LOCLuduRestBackupGM", "LudusaviRestic"),
                Action = a => ToggleTag(args.Games, settings.IncludeTagID, false, true)
            });
            // Exclude tag add/remove
            items.Add(new GameMenuItem
            {
                Description = SafeLoc("LOCLuduRestBackupGMExcludeAdd", "Add backup exclude tag"),
                MenuSection = SafeLoc("LOCLuduRestBackupGM", "LudusaviRestic"),
                Action = a => ToggleTag(args.Games, settings.ExcludeTagID, true, false)
            });
            items.Add(new GameMenuItem
            {
                Description = SafeLoc("LOCLuduRestBackupGMExcludeRemove", "Remove backup exclude tag"),
                MenuSection = SafeLoc("LOCLuduRestBackupGM", "LudusaviRestic"),
                Action = a => ToggleTag(args.Games, settings.ExcludeTagID, false, false)
            });
            // View snapshots
            items.Add(new GameMenuItem
            {
                Description = SafeLoc("LOCLuduRestViewBackupSnapshots", "View backup snapshots"),
                MenuSection = SafeLoc("LOCLuduRestBackupGM", "LudusaviRestic"),
                Action = a =>
                {
                    foreach (var g in args.Games)
                    {
                        ShowBackupBrowser(g.Name);
                    }
                }
            });
            // Configure overrides (new window)
            items.Add(new GameMenuItem
            {
                Description = SafeLoc("LOCLuduRestGameMenuConfigureOverrides", "Configure backup settings..."),
                MenuSection = SafeLoc("LOCLuduRestBackupGM", "LudusaviRestic"),
                Action = a =>
                {
                    foreach (var g in args.Games)
                    {
                        try
                        {
                            if (ShowPerGameSettings(g))
                            {
                                SavePluginSettings(settings); // persist after save
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
                Description = SafeLoc("LOCLuduRestBackupAllGames", "Backup all games"),
                MenuSection = root,
                Action = a => backupManager.BackupAllGames()
            });
            items.Add(new MainMenuItem
            {
                Description = SafeLoc("LOCLuduRestBrowseBackupSnapshots", "Browse backup snapshots"),
                MenuSection = root,
                Action = a =>
                {
                    ShowBackupBrowser();
                }
            });
            items.Add(new MainMenuItem
            {
                Description = SafeLoc("LOCLuduRestMainMenuViewOverrides", "View games with custom backup settings"),
                MenuSection = root,
                Action = a => ShowOverridesSummary()
            });
            return items;
        }

        private void ShowOverridesSummary()
        {
            try
            {
                EnsureLocalizationLoaded();
                var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowMaximizeButton = false,
                    ShowMinimizeButton = false
                });
                window.Title = SafeLoc("LOCLuduRestOverridesWindowTitle", "Custom Backup Overrides");
                window.Width = 700;
                window.Height = 480;
                var view = new OverridesSummaryView(this, PlayniteApi, settings);
                window.Content = view;
                window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error showing overrides manager");
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message, SafeLoc("LOCLuduRestOverridesDialogTitle", "Custom Backup Overrides"));
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

        private void ShowBackupBrowser(string gameFilter = null)
        {
            try
            {
                var ctx = new BackupContext(PlayniteApi, settings);
                var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowMaximizeButton = true,
                    ShowMinimizeButton = false
                });
                window.Title = gameFilter == null ? SafeLoc("LOCLuduRestBrowseBackupSnapshots", "Browse backup snapshots") : $"Backup Browser - {gameFilter}";
                window.Height = 600;
                window.Width = 800;
                window.Content = new BackupBrowserView { DataContext = new BackupBrowserViewModel(ctx, gameFilter) };
                window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error opening backup browser");
            }
        }

        public bool ShowPerGameSettings(Game game)
        {
            var view = new GameOverrideSettingsView(game, settings);
            var saved = false;
            view.CloseRequested += (s, ok) => { saved = ok; ((System.Windows.Window)System.Windows.Window.GetWindow(view)).Close(); };
            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMaximizeButton = false,
                ShowMinimizeButton = false
            });
            window.Title = SafeLoc("LOCLuduRestPerGameWindowTitle", "Per-Game Backup Settings");
            window.Height = 520;
            window.Width = 660;
            window.Content = view;
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.ShowDialog();
            return saved;
        }
    }
}
// Moved from root to Core folder
// ...existing code...
