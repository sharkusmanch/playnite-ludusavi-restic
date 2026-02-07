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
    }
}
