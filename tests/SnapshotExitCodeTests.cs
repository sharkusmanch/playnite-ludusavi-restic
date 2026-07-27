using Xunit;

namespace LudusaviRestic.Tests
{
    public class SnapshotExitCodeTests
    {
        [Fact]
        public void MapExitCode_Zero_IsSuccess()
        {
            Assert.Equal(SnapshotResult.Success, BaseBackupTask.MapExitCode(0));
        }

        [Fact]
        public void MapExitCode_Three_IsPartialFailure()
        {
            Assert.Equal(SnapshotResult.PartialFailure, BaseBackupTask.MapExitCode(3));
        }

        [Fact]
        public void MapExitCode_One_IsFailed()
        {
            Assert.Equal(SnapshotResult.Failed, BaseBackupTask.MapExitCode(1));
        }

        /// <summary>
        /// restic 0.17+ exit codes that previously fell through to "success": the backup
        /// reported a green notification while nothing had been written.
        /// </summary>
        [Theory]
        [InlineData(10)] // no repository
        [InlineData(11)] // failed to lock repository
        [InlineData(12)] // wrong password
        public void MapExitCode_ResticFailureCodes_AreFailed(int exitCode)
        {
            Assert.Equal(SnapshotResult.Failed, BaseBackupTask.MapExitCode(exitCode));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(130)]
        [InlineData(-1)]
        public void MapExitCode_UnknownNonZeroCodes_AreFailed(int exitCode)
        {
            Assert.Equal(SnapshotResult.Failed, BaseBackupTask.MapExitCode(exitCode));
        }
    }
}
