using Xunit;

namespace LudusaviRestic.Tests
{
    public class LudusaviCommandTests
    {
        // --- BuildBackupAllArgs (issue #60: deprecated flags removed) ---

        [Fact]
        public void BuildBackupAllArgs_ContainsApiFlag()
        {
            var args = LudusaviCommand.BuildBackupAllArgs();

            Assert.Contains("--api", args);
        }

        [Fact]
        public void BuildBackupAllArgs_ContainsPreviewFlag()
        {
            var args = LudusaviCommand.BuildBackupAllArgs();

            Assert.Contains("--preview", args);
        }

        [Fact]
        public void BuildBackupAllArgs_DoesNotContainDeprecatedTryUpdate()
        {
            var args = LudusaviCommand.BuildBackupAllArgs();

            Assert.DoesNotContain("--try-update", args);
        }

        [Fact]
        public void BuildBackupAllArgs_DoesNotContainDeprecatedMerge()
        {
            var args = LudusaviCommand.BuildBackupAllArgs();

            Assert.DoesNotContain("--merge", args);
        }

        [Fact]
        public void BuildBackupAllArgs_StartsWithBackup()
        {
            var args = LudusaviCommand.BuildBackupAllArgs();

            Assert.StartsWith("backup", args);
        }

        // --- BuildBackupArgs ---

        [Fact]
        public void BuildBackupArgs_ContainsQuotedGameName()
        {
            var args = LudusaviCommand.BuildBackupArgs("Elden Ring");

            Assert.Contains("\"Elden Ring\"", args);
        }

        [Fact]
        public void BuildBackupArgs_ContainsApiAndPreview()
        {
            var args = LudusaviCommand.BuildBackupArgs("TestGame");

            Assert.Contains("--api", args);
            Assert.Contains("--preview", args);
        }

        [Fact]
        public void BuildBackupArgs_DoesNotContainDeprecatedFlags()
        {
            var args = LudusaviCommand.BuildBackupArgs("TestGame");

            Assert.DoesNotContain("--try-update", args);
            Assert.DoesNotContain("--merge", args);
            Assert.DoesNotContain("--no-merge", args);
        }

        [Fact]
        public void BuildBackupArgs_SpecialCharsInGameName_Preserved()
        {
            var args = LudusaviCommand.BuildBackupArgs("Ninja Gaiden \u03A3");

            Assert.Contains("Ninja Gaiden \u03A3", args);
        }
    }
}
