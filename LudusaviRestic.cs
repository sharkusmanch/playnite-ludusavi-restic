using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace LudusaviRestic
{
    public class LudusaviRestic : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private string NotificationID = "Lususavi Restic";

        public LudusaviResticSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("e9861c36-68a8-4654-8071-a9c50612bc24");

        public LudusaviRestic(IPlayniteAPI api) : base(api)
        { 
            settings = new LudusaviResticSettings(this);
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
                            CreateGameDataSnapshot(game);
                        }
                    }
                }
            };
        }

        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            CreateGameDataSnapshot(game);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new LudusaviResticSettingsView(this);
        }

        private void ApplyEnvironment()
        {
            Environment.SetEnvironmentVariable("RCLONE_CONFIG_PASS", this.settings.RcloneConfigPassword);
            Environment.SetEnvironmentVariable("RESTIC_REPOSITORY", this.settings.ResticRepository);
            Environment.SetEnvironmentVariable("RESTIC_PASSWORD", this.settings.ResticPassword);
            Environment.SetEnvironmentVariable("RCLONE_CONFIG", this.settings.RcloneConfigPath);
        }

        private void CreateGameDataSnapshot(Game game){
            ApplyEnvironment();

            logger.Debug($"Starting backup for {game.Name}");
            IList<string> files = GameFiles(game);

            if (files.Count == 0) 
            {
                logger.Debug($"No game save files found for {game.Name}");
                return;
            }

            CreateSingleGameFilesSnapshot(game, files);

            SendInfoNotification($"Successfully created game data snapshot for {game.Name}");
        }

        private IList<String> GameFiles(Game game) 
        {
            IList<string> files = new List<string>();

            string command = settings.LudusaviExecutablePath.Trim();
            string args = $"backup --api --try-update --merge \"{game.Name}\"";

            string stdout;
            int exitCode;

            try
            {
                (exitCode, stdout) = ExecuteCommand(command, args);
            }
            catch (Exception e)
            {
                logger.Debug(e, "Failed to get files from ludusavi");
                return files;
            }

            JObject gameData = JObject.Parse(stdout);

            int totalGames = (int)gameData["overall"]["totalGames"];

            if (totalGames != 1)
            {
                logger.Error("Unable to get game info from ludusavi");
                SendErrorNotification($"No save files found for {game.Name}");
                return files;
            }

            logger.Debug($"Got {game.Name} data from ludusavi");

            JObject filesMap = (JObject)gameData["games"][$"{game}"]["files"];

            foreach (JProperty property in filesMap.Properties())
            {
                files.Add($"\"{property.Name}\"");
            }

            return files;
        }

        private void CreateSingleGameFilesSnapshot(Game game, IList<string> files)
        {
            string command = settings.ResticExecutablePath.Trim();
            string backupArgs = $"backup --tag  \"{game}\" {string.Join(" ", files)}";

            string stdout;
            int exitCode;

            try
            {
                (exitCode, stdout) = ExecuteCommand(command, backupArgs);
            }
            catch (Exception e)
            {
                logger.Debug(e, "Encountered error executing restic");
                return;
            }

            switch (exitCode)
            {
                case 1:
                    logger.Error($"Failed to create restic game saves snapshot {game.Name}");
                    SendErrorNotification($"Failed to create restic game saves snapshot {game.Name}");
                    break;
                case 3:
                    logger.Error($"Restic failed to read some game save files for {game.Name}");
                    SendErrorNotification($"Restic failed to read some game save files for {game.Name}");
                    break;
                default:
                    break;
            }
        }

        private (int, string) ExecuteCommand(string command, string args)
        {
            Process process = new Process();
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            string stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return (process.ExitCode, stdout);
        }

        private void SendNotification(string message, NotificationType type) 
        {
            PlayniteApi.Notifications.Add(new NotificationMessage(NotificationID, message, type));
        }

        private void SendErrorNotification(string message)
        {
            SendNotification(message, NotificationType.Error);
        }

        private void SendInfoNotification(string message)
        {
            SendNotification(message, NotificationType.Info);
        }
    }
}