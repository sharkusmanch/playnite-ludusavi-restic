using Xunit;

namespace LudusaviRestic.Tests
{
    public class BackupContextTests
    {
        private static BackupContext MakeContext()
        {
            return new BackupContext(null, new LudusaviResticSettings());
        }

        [Fact]
        public void NotificationID_IsCorrectlySpelled()
        {
            var context = MakeContext();

            Assert.Equal("LudusaviRestic", context.NotificationID);
        }

        [Fact]
        public void UniqueNotificationID_FormatsCorrectly()
        {
            var context = MakeContext();

            Assert.Equal("LudusaviRestic_backup_all", context.UniqueNotificationID("backup_all"));
        }

        [Fact]
        public void UniqueNotificationID_DifferentSuffixes_ProduceDifferentIDs()
        {
            var context = MakeContext();

            var id1 = context.UniqueNotificationID("game_A");
            var id2 = context.UniqueNotificationID("game_B");

            Assert.NotEqual(id1, id2);
        }

        [Fact]
        public void UniqueNotificationID_EmptySuffix_StillWorks()
        {
            var context = MakeContext();

            Assert.Equal("LudusaviRestic_", context.UniqueNotificationID(""));
        }

        [Fact]
        public void BuildResticEnvironment_PopulatesConfiguredVariables()
        {
            var context = new BackupContext(null, new LudusaviResticSettings
            {
                ResticRepository = "C:\\repo",
                ResticPassword = "hunter2",
                RcloneConfigPath = "C:\\rclone.conf",
                RcloneConfigPassword = "rclone-pass"
            });

            var environment = context.BuildResticEnvironment();

            Assert.Equal("C:\\repo", environment["RESTIC_REPOSITORY"]);
            Assert.Equal("hunter2", environment["RESTIC_PASSWORD"]);
            Assert.Equal("C:\\rclone.conf", environment["RCLONE_CONFIG"]);
            Assert.Equal("rclone-pass", environment["RCLONE_CONFIG_PASS"]);
        }

        /// <summary>
        /// Unset values must be omitted rather than written as empty strings, so an rclone
        /// config inherited from the user's own environment is not blanked out.
        /// </summary>
        [Fact]
        public void BuildResticEnvironment_OmitsUnsetVariables()
        {
            var context = new BackupContext(null, new LudusaviResticSettings
            {
                ResticRepository = "C:\\repo",
                ResticPassword = "hunter2"
            });

            var environment = context.BuildResticEnvironment();

            Assert.False(environment.ContainsKey("RCLONE_CONFIG"));
            Assert.False(environment.ContainsKey("RCLONE_CONFIG_PASS"));
        }

        [Fact]
        public void BuildResticEnvironment_OmitsEmptyStrings()
        {
            var context = new BackupContext(null, new LudusaviResticSettings
            {
                ResticRepository = "C:\\repo",
                RcloneConfigPath = "",
                RcloneConfigPassword = ""
            });

            var environment = context.BuildResticEnvironment();

            Assert.False(environment.ContainsKey("RCLONE_CONFIG"));
            Assert.False(environment.ContainsKey("RCLONE_CONFIG_PASS"));
        }

        /// <summary>
        /// Constructing a context previously wrote the repository password into Playnite's
        /// own process block, which every launched game inherited.
        /// </summary>
        [Fact]
        public void Constructor_DoesNotWriteToProcessEnvironment()
        {
            var original = System.Environment.GetEnvironmentVariable("RESTIC_PASSWORD");
            System.Environment.SetEnvironmentVariable("RESTIC_PASSWORD", null);

            try
            {
                new BackupContext(null, new LudusaviResticSettings
                {
                    ResticRepository = "C:\\repo",
                    ResticPassword = "hunter2"
                });

                Assert.Null(System.Environment.GetEnvironmentVariable("RESTIC_PASSWORD"));
            }
            finally
            {
                System.Environment.SetEnvironmentVariable("RESTIC_PASSWORD", original);
            }
        }
    }
}
