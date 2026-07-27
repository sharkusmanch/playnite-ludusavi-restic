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
        /// Timeout for executable detection probes. Detection walks a list of candidate
        /// paths on the UI thread, so each probe must fail fast rather than block.
        /// </summary>
        private const int ProbeTimeoutMilliseconds = 10 * 1000;

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

            // 6. Check common Backrest installation paths
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            candidatePaths.Add(Path.Combine(localAppData, "Programs", "Backrest", "restic.exe"));
            candidatePaths.Add(Path.Combine(programFiles, "Backrest", "restic.exe"));
            // Since Playnite is currently a 32-bit application, programFiles points to "C:\Program Files (x86)";
            // using the %ProgramW6432% environment variable allows searching in "C:\Program Files"
            var programFiles64 = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
            candidatePaths.Add(Path.Combine(programFiles64, "Backrest", "restic.exe"));

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
            return ProbeExecutable(path, "restic", "version", "restic");
        }

        /// <summary>
        /// Check if the specified path is a valid ludusavi executable
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>True if valid ludusavi executable</returns>
        public static bool IsValidLudusaviExecutable(string path)
        {
            return ProbeExecutable(path, "ludusavi", "--version", "ludusavi");
        }

        /// <summary>
        /// Runs a version probe against a candidate executable. These run while settings
        /// load, on Playnite's main thread, so the probe is given a short timeout: a path
        /// on an unreachable network share would otherwise stall startup indefinitely.
        /// </summary>
        private static bool ProbeExecutable(string path, string bareName, string versionArgs, string expectedOutput)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // A bare name is resolved through PATH; anything else must exist on disk.
            if (path != bareName && !File.Exists(path))
                return false;

            try
            {
                var result = BaseCommand.ExecuteCommand(path, versionArgs, null, ProbeTimeoutMilliseconds);
                return result.ExitCode == 0
                    && result.StdOut.IndexOf(expectedOutput, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch (Exception ex)
            {
                logger.Debug($"Error checking {bareName} executable at {path}: {ex.Message}");
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

            // Start from the configured environment so an rclone-backed repository still
            // resolves, then point restic at the repository being created.
            var environment = context.BuildResticEnvironment();
            environment["RESTIC_REPOSITORY"] = repositoryPath;
            environment["RESTIC_PASSWORD"] = password;

            logger.Debug($"Repository: {repositoryPath}");

            return BaseCommand.ExecuteCommand(context.Settings.ResticExecutablePath.Trim(), "init", environment);
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
