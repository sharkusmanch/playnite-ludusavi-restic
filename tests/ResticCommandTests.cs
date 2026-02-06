using System.Collections.Generic;
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

        [Fact]
        public void BuildPerGameRetentionArgs_IncludesTag()
        {
            var result = ResticCommand.BuildPerGameRetentionArgs("MyGame", 10, 7, 4, 12, 5, false);

            Assert.Contains("--tag \"MyGame\"", result);
            Assert.DoesNotContain("--group-by", result);
        }

        [Fact]
        public void BuildPerGameRetentionArgs_IncludesRetentionValues()
        {
            var result = ResticCommand.BuildPerGameRetentionArgs("MyGame", 20, 14, 8, 6, 3, false);

            Assert.Contains("--keep-last 20", result);
            Assert.Contains("--keep-daily 14", result);
            Assert.Contains("--keep-weekly 8", result);
            Assert.Contains("--keep-monthly 6", result);
            Assert.Contains("--keep-yearly 3", result);
            Assert.Contains("--prune", result);
        }

        [Fact]
        public void BuildPerGameRetentionArgs_OmitsZeroValues()
        {
            var result = ResticCommand.BuildPerGameRetentionArgs("MyGame", 1, 0, 0, 0, 0, false);

            Assert.Contains("--keep-last 1", result);
            Assert.DoesNotContain("--keep-daily", result);
            Assert.DoesNotContain("--keep-weekly", result);
            Assert.DoesNotContain("--keep-monthly", result);
            Assert.DoesNotContain("--keep-yearly", result);
            Assert.Contains("--prune", result);
        }

        [Fact]
        public void BuildPerGameRetentionArgs_OnlyKeepDaily_OmitsOthers()
        {
            var result = ResticCommand.BuildPerGameRetentionArgs("MyGame", 0, 14, 0, 0, 0, false);

            Assert.DoesNotContain("--keep-last", result);
            Assert.Contains("--keep-daily 14", result);
            Assert.DoesNotContain("--keep-weekly", result);
            Assert.DoesNotContain("--keep-monthly", result);
            Assert.DoesNotContain("--keep-yearly", result);
        }

        [Fact]
        public void BuildPerGameRetentionArgs_MixedZeroAndNonZero()
        {
            var result = ResticCommand.BuildPerGameRetentionArgs("MyGame", 5, 0, 4, 0, 2, false);

            Assert.Contains("--keep-last 5", result);
            Assert.DoesNotContain("--keep-daily", result);
            Assert.Contains("--keep-weekly 4", result);
            Assert.DoesNotContain("--keep-monthly", result);
            Assert.Contains("--keep-yearly 2", result);
        }

        [Fact]
        public void BuildPerGameRetentionArgs_AllZero_NoKeepFlags()
        {
            var result = ResticCommand.BuildPerGameRetentionArgs("MyGame", 0, 0, 0, 0, 0, false);

            Assert.DoesNotContain("--keep-last", result);
            Assert.DoesNotContain("--keep-daily", result);
            Assert.DoesNotContain("--keep-weekly", result);
            Assert.DoesNotContain("--keep-monthly", result);
            Assert.DoesNotContain("--keep-yearly", result);
            Assert.Contains("--tag \"MyGame\"", result);
            Assert.Contains("--json", result);
        }

        [Fact]
        public void BuildPerGameRetentionArgs_DryRun_UsesDryRunFlag()
        {
            var result = ResticCommand.BuildPerGameRetentionArgs("MyGame", 10, 7, 4, 12, 5, true);

            Assert.Contains("--dry-run", result);
            Assert.DoesNotContain("--prune", result);
        }

        [Fact]
        public void BuildPerGameRetentionArgs_StartsWithForget()
        {
            var result = ResticCommand.BuildPerGameRetentionArgs("MyGame", 10, 7, 4, 12, 5, false);

            Assert.StartsWith("forget", result);
        }

        [Fact]
        public void ExtractGameTags_ParsesUniqueFirstTags()
        {
            string json = @"[
                {""tags"":[""GameA"",""manual""],""id"":""aabb""},
                {""tags"":[""GameB""],""id"":""ccdd""},
                {""tags"":[""GameA"",""gameplay""],""id"":""eeff""}
            ]";

            var tags = ResticCommand.ExtractGameTags(json);

            Assert.Equal(2, tags.Count);
            Assert.Contains("GameA", tags);
            Assert.Contains("GameB", tags);
        }

        [Fact]
        public void ExtractGameTags_EmptyArray_ReturnsEmpty()
        {
            var tags = ResticCommand.ExtractGameTags("[]");

            Assert.Empty(tags);
        }

        [Fact]
        public void ExtractGameTags_InvalidJson_ReturnsEmpty()
        {
            var tags = ResticCommand.ExtractGameTags("not json");

            Assert.Empty(tags);
        }

        [Fact]
        public void ExtractGameTags_SnapshotsWithoutTags_SkipsThose()
        {
            string json = @"[
                {""tags"":[""GameA""],""id"":""aabb""},
                {""id"":""ccdd""},
                {""tags"":[],""id"":""eeff""}
            ]";

            var tags = ResticCommand.ExtractGameTags(json);

            Assert.Single(tags);
            Assert.Contains("GameA", tags);
        }
    }
}
