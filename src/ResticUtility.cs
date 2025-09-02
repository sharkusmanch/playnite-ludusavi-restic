using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Playnite.SDK;

namespace LudusaviRestic
{
    public static class ResticUtility
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        /// <summary>
        /// Attempts to automatically detect the restic executable path
        /// </summary>
        /// <returns>The path to restic executable, or null if not found</returns>
        public static string DetectResticExecutable()
        {
            logger.Info("Attempting to detect restic executable...");

            // List of potential paths to check
            var candidatePaths = new List<string>();

            // 1. Check if "restic" is in PATH
            candidatePaths.Add("restic");

            // 2. Check common Scoop installation paths
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            candidatePaths.Add(Path.Combine(userProfile, "scoop", "apps", "restic", "current", "restic.exe"));
            candidatePaths.Add(Path.Combine(userProfile, "scoop", "shims", "restic.exe"));

            // 3. Check common Chocolatey installation paths
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            candidatePaths.Add(Path.Combine(programFiles, "restic", "restic.exe"));

            // 4. Check common manual installation paths
            candidatePaths.Add(Path.Combine(programFiles, "Restic", "restic.exe"));
            candidatePaths.Add(Path.Combine(@"C:\tools", "restic", "restic.exe"));
            candidatePaths.Add(Path.Combine(userProfile, "bin", "restic.exe"));
            candidatePaths.Add(Path.Combine(userProfile, "tools", "restic.exe"));

            // 5. Check if it's in the same directory as Playnite or the extension
            var currentDir = Directory.GetCurrentDirectory();
            candidatePaths.Add(Path.Combine(currentDir, "restic.exe"));

            foreach (var path in candidatePaths)
            {
                if (IsValidResticExecutable(path))
                {
                    logger.Info($"Found restic executable at: {path}");
                    return path;
                }
            }

            logger.Warn("Could not automatically detect restic executable");
            return null;
        }

        /// <summary>
        /// Attempts to automatically detect the ludusavi executable path
        /// </summary>
        /// <returns>The path to ludusavi executable, or null if not found</returns>
        public static string DetectLudusaviExecutable()
        {
            logger.Info("Attempting to detect ludusavi executable...");

            // List of potential paths to check
            var candidatePaths = new List<string>();

            // 1. Check if "ludusavi" is in PATH
            candidatePaths.Add("ludusavi");

            // 2. Check common Scoop installation paths
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            candidatePaths.Add(Path.Combine(userProfile, "scoop", "apps", "ludusavi", "current", "ludusavi.exe"));
            candidatePaths.Add(Path.Combine(userProfile, "scoop", "shims", "ludusavi.exe"));

            // 3. Check common Chocolatey installation paths
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            candidatePaths.Add(Path.Combine(programFiles, "ludusavi", "ludusavi.exe"));

            // 4. Check common manual installation paths
            candidatePaths.Add(Path.Combine(programFiles, "Ludusavi", "ludusavi.exe"));
            candidatePaths.Add(Path.Combine(@"C:\tools", "ludusavi", "ludusavi.exe"));
            candidatePaths.Add(Path.Combine(userProfile, "bin", "ludusavi.exe"));
            candidatePaths.Add(Path.Combine(userProfile, "tools", "ludusavi.exe"));

            // 5. Check AppData\Local for portable installations
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            candidatePaths.Add(Path.Combine(localAppData, "ludusavi", "ludusavi.exe"));

            // 6. Check common portable app locations
            candidatePaths.Add(Path.Combine(@"C:\PortableApps", "ludusavi", "ludusavi.exe"));
            candidatePaths.Add(Path.Combine(userProfile, "PortableApps", "ludusavi", "ludusavi.exe"));

            // 7. Check if it's in the same directory as Playnite or the extension
            var currentDir = Directory.GetCurrentDirectory();
            candidatePaths.Add(Path.Combine(currentDir, "ludusavi.exe"));

            // 8. Check Downloads folder (common for manual downloads)
            var downloadsFolder = Path.Combine(userProfile, "Downloads");
            candidatePaths.Add(Path.Combine(downloadsFolder, "ludusavi.exe"));

            foreach (var path in candidatePaths)
            {
                if (IsValidLudusaviExecutable(path))
                {
                    logger.Info($"Found ludusavi executable at: {path}");
                    return path;
                }
            }

            logger.Warn("Could not automatically detect ludusavi executable");
            return null;
        }

        /// <summary>
        /// Checks if the given path points to a valid restic executable
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if valid restic executable</returns>
        public static bool IsValidResticExecutable(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                // For "restic" (no path), check if it's available in PATH
                if (path == "restic")
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "restic",
                            Arguments = "version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        }
                    };

                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    return process.ExitCode == 0 && output.Contains("restic");
                }

                // For full paths, check if file exists and is executable
                if (!File.Exists(path))
                    return false;

                var fileProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                fileProcess.Start();
                var fileOutput = fileProcess.StandardOutput.ReadToEnd();
                fileProcess.WaitForExit();

                return fileProcess.ExitCode == 0 && fileOutput.Contains("restic");
            }
            catch (Exception ex)
            {
                logger.Debug($"Error checking restic executable at {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if the specified path is a valid ludusavi executable
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if valid ludusavi executable</returns>
        public static bool IsValidLudusaviExecutable(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                // For "ludusavi" (no path), check if it's available in PATH
                if (path == "ludusavi")
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "ludusavi",
                            Arguments = "--version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        }
                    };

                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    return process.ExitCode == 0 && output.ToLower().Contains("ludusavi");
                }

                // For full paths, check if file exists and is executable
                if (!File.Exists(path))
                    return false;

                var fileProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                fileProcess.Start();
                var fileOutput = fileProcess.StandardOutput.ReadToEnd();
                fileProcess.WaitForExit();

                return fileProcess.ExitCode == 0 && fileOutput.ToLower().Contains("ludusavi");
            }
            catch (Exception ex)
            {
                logger.Debug($"Error checking ludusavi executable at {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initialize a new restic repository
        /// </summary>
        /// <param name="context">Backup context</param>
        /// <param name="repositoryPath">Path where to initialize the repository</param>
        /// <param name="password">Password for the repository</param>
        /// <returns>Command result</returns>
        public static CommandResult InitializeRepository(BackupContext context, string repositoryPath, string password)
        {
            logger.Info($"Initializing restic repository at: {repositoryPath}");

            // Create a temporary context with the new repository settings
            var tempSettings = new LudusaviResticSettings
            {
                ResticExecutablePath = context.Settings.ResticExecutablePath,
                ResticRepository = repositoryPath,
                ResticPassword = password
            };

            var tempContext = new BackupContext(context.API, tempSettings);

            // Execute restic init command
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = tempSettings.ResticExecutablePath,
                    Arguments = "init",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            // Set environment variables for restic
            process.StartInfo.Environment["RESTIC_REPOSITORY"] = repositoryPath;
            process.StartInfo.Environment["RESTIC_PASSWORD"] = password;

            logger.Debug($"Executing: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            logger.Debug($"Repository: {repositoryPath}");

            return new CommandResult(process);
        }

        /// <summary>
        /// Checks if a repository exists and is valid
        /// </summary>
        /// <param name="context">Backup context</param>
        /// <returns>True if repository exists and is accessible</returns>
        public static bool IsRepositoryValid(BackupContext context)
        {
            if (string.IsNullOrWhiteSpace(context.Settings.ResticRepository) ||
                string.IsNullOrWhiteSpace(context.Settings.ResticPassword))
            {
                return false;
            }

            try
            {
                var result = ResticCommand.Version(context);
                return result.ExitCode == 0;
            }
            catch (Exception ex)
            {
                logger.Debug($"Error checking repository validity: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets suggestions for common repository locations
        /// </summary>
        /// <returns>List of suggested repository paths</returns>
        public static List<string> GetRepositorySuggestions()
        {
            var suggestions = new List<string>();
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            suggestions.Add(Path.Combine(documents, "PlayniteBackups"));
            suggestions.Add(Path.Combine(userProfile, "PlayniteBackups"));
            suggestions.Add(Path.Combine(@"C:\Backups", "Playnite"));
            suggestions.Add(@"D:\Backups\Playnite");
            suggestions.Add(@"E:\Backups\Playnite");

            return suggestions;
        }
    }
}
