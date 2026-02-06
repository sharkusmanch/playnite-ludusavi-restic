using Xunit;

namespace LudusaviRestic.Tests
{
    public class ResticCommandTests
    {
        [Fact]
        public void BuildRetentionArgs_DefaultSettings_WithPrune()
        {
            var settings = new LudusaviResticSettings();

            var result = ResticCommand.BuildRetentionArgs(settings, false);

            Assert.Contains("--keep-last 10", result);
            Assert.Contains("--keep-daily 7", result);
            Assert.Contains("--keep-weekly 4", result);
            Assert.Contains("--keep-monthly 12", result);
            Assert.Contains("--keep-yearly 5", result);
            Assert.Contains("--prune", result);
            Assert.DoesNotContain("--dry-run", result);
        }

        [Fact]
        public void BuildRetentionArgs_DryRun_UsesDryRunFlag()
        {
            var settings = new LudusaviResticSettings();

            var result = ResticCommand.BuildRetentionArgs(settings, true);

            Assert.Contains("--dry-run", result);
            Assert.DoesNotContain("--prune", result);
        }

        [Fact]
        public void BuildRetentionArgs_CustomSettings_ReflectedInOutput()
        {
            var settings = new LudusaviResticSettings();
            settings.KeepLast = 20;
            settings.KeepDaily = 14;
            settings.KeepWeekly = 8;
            settings.KeepMonthly = 6;
            settings.KeepYearly = 3;

            var result = ResticCommand.BuildRetentionArgs(settings, false);

            Assert.Contains("--keep-last 20", result);
            Assert.Contains("--keep-daily 14", result);
            Assert.Contains("--keep-weekly 8", result);
            Assert.Contains("--keep-monthly 6", result);
            Assert.Contains("--keep-yearly 3", result);
        }

        [Fact]
        public void BuildRetentionArgs_StartsWithForget()
        {
            var settings = new LudusaviResticSettings();

            var result = ResticCommand.BuildRetentionArgs(settings, false);

            Assert.StartsWith("forget", result);
        }
    }
}
