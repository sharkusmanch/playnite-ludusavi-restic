using System;
using System.Collections.Generic;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class PruneResultTests
    {
        [Fact]
        public void GetGameDeletionCounts_GroupsByGameName()
        {
            var result = new PruneResult
            {
                DeletedSnapshots = new List<DeletedSnapshot>
                {
                    new DeletedSnapshot { GameName = "GameA" },
                    new DeletedSnapshot { GameName = "GameA" },
                    new DeletedSnapshot { GameName = "GameB" },
                }
            };

            var counts = result.GetGameDeletionCounts();

            Assert.Equal(2, counts["GameA"]);
            Assert.Equal(1, counts["GameB"]);
            Assert.Equal(2, counts.Count);
        }

        [Fact]
        public void GetGameDeletionCounts_IgnoresEmptyNames()
        {
            var result = new PruneResult
            {
                DeletedSnapshots = new List<DeletedSnapshot>
                {
                    new DeletedSnapshot { GameName = "GameA" },
                    new DeletedSnapshot { GameName = "" },
                    new DeletedSnapshot { GameName = null },
                }
            };

            var counts = result.GetGameDeletionCounts();

            Assert.Single(counts);
            Assert.Equal(1, counts["GameA"]);
        }

        [Fact]
        public void GetTagDeletionCounts_CountsTagsAcrossSnapshots()
        {
            var result = new PruneResult
            {
                DeletedSnapshots = new List<DeletedSnapshot>
                {
                    new DeletedSnapshot { Tags = new List<string> { "tag1", "tag2" } },
                    new DeletedSnapshot { Tags = new List<string> { "tag1", "tag3" } },
                }
            };

            var counts = result.GetTagDeletionCounts();

            Assert.Equal(2, counts["tag1"]);
            Assert.Equal(1, counts["tag2"]);
            Assert.Equal(1, counts["tag3"]);
        }

        [Fact]
        public void GetTagDeletionCounts_IgnoresEmptyTags()
        {
            var result = new PruneResult
            {
                DeletedSnapshots = new List<DeletedSnapshot>
                {
                    new DeletedSnapshot { Tags = new List<string> { "tag1", "", null } },
                }
            };

            var counts = result.GetTagDeletionCounts();

            Assert.Single(counts);
            Assert.Equal(1, counts["tag1"]);
        }
    }

    public class DeletedSnapshotTests
    {
        [Fact]
        public void ToString_FormatsCorrectly()
        {
            var snapshot = new DeletedSnapshot
            {
                ShortId = "abcd1234",
                GameName = "TestGame",
                Time = new DateTime(2024, 6, 15, 14, 30, 0)
            };

            Assert.Equal("abcd1234 - TestGame (2024-06-15 14:30)", snapshot.ToString());
        }
    }

    public class PruneResultParserTests
    {
        [Fact]
        public void ParsePruneOutput_NonZeroExitCode_ReturnsFailure()
        {
            var cmd = new CommandResult(1, "some output", "some error");

            var result = PruneResultParser.ParsePruneOutput(cmd);

            Assert.False(result.Success);
            Assert.Equal(0, result.SnapshotsDeleted);
        }

        [Fact]
        public void ParsePruneOutput_JsonWithRemovedSnapshots()
        {
            string json = @"{
                ""removed_snapshots"": [
                    { ""id"": ""aabbccdd11223344"", ""short_id"": ""aabbccdd"" },
                    { ""id"": ""eeff0011aabbccdd"", ""short_id"": ""eeff0011"" }
                ]
            }";
            var cmd = new CommandResult(0, json, "");

            var result = PruneResultParser.ParsePruneOutput(cmd);

            Assert.True(result.Success);
            Assert.Equal(2, result.SnapshotsDeleted);
            Assert.Equal("aabbccdd", result.DeletedSnapshots[0].ShortId);
            Assert.Equal("eeff0011", result.DeletedSnapshots[1].ShortId);
        }

        [Fact]
        public void ParsePruneOutput_TextFallback_RemovingSnapshotLines()
        {
            string output = "loading indexes...\nremoving snapshot abcd1234\nremoving snapshot eeff5678\ndone\n";
            var cmd = new CommandResult(0, output, "");

            var result = PruneResultParser.ParsePruneOutput(cmd);

            Assert.True(result.Success);
            Assert.Equal(2, result.SnapshotsDeleted);
            Assert.Equal("abcd1234", result.DeletedSnapshots[0].ShortId);
            Assert.Equal("eeff5678", result.DeletedSnapshots[1].ShortId);
        }

        [Fact]
        public void ParsePruneOutput_ParsesDataDeleted()
        {
            string output = "removed 42.5 MB of data\n";
            var cmd = new CommandResult(0, output, "");

            var result = PruneResultParser.ParsePruneOutput(cmd);

            Assert.True(result.Success);
            Assert.Equal("42.5 MB", result.DataDeleted);
        }

        [Fact]
        public void ParsePruneOutput_IsDryRunFlag()
        {
            var cmd = new CommandResult(0, "", "");

            var result = PruneResultParser.ParsePruneOutput(cmd, isDryRun: true);

            Assert.True(result.IsDryRun);
        }

        [Fact]
        public void ParseForgetOutput_JsonRemoveActions()
        {
            string line1 = @"{""action"":""remove"",""snapshot"":{""id"":""aabb1122334455667788"",""short_id"":""aabb1122"",""time"":""2024-06-15T14:30:00Z"",""hostname"":""myhost"",""tags"":[""GameA"",""manual""],""paths"":[""/saves""]}}";
            string line2 = @"{""action"":""remove"",""snapshot"":{""id"":""ccdd5566778899001122"",""short_id"":""ccdd5566"",""time"":""2024-06-14T10:00:00Z"",""hostname"":""myhost"",""tags"":[""GameB""],""paths"":[""/saves""]}}";
            string output = line1 + "\n" + line2 + "\n";
            var cmd = new CommandResult(0, output, "");

            var result = PruneResultParser.ParseForgetOutput(cmd);

            Assert.True(result.Success);
            Assert.Equal(2, result.SnapshotsDeleted);
            Assert.Equal("aabb1122", result.DeletedSnapshots[0].ShortId);
            Assert.Equal("GameA", result.DeletedSnapshots[0].GameName);
            Assert.Equal(2, result.DeletedSnapshots[0].Tags.Count);
            Assert.Equal("GameB", result.DeletedSnapshots[1].GameName);
        }

        [Fact]
        public void ParseForgetOutput_CountsDistinctGamesAffected()
        {
            string line1 = @"{""action"":""remove"",""snapshot"":{""id"":""aabb1122334455667788"",""short_id"":""aabb1122"",""tags"":[""GameA""]}}";
            string line2 = @"{""action"":""remove"",""snapshot"":{""id"":""ccdd5566778899001122"",""short_id"":""ccdd5566"",""tags"":[""GameA""]}}";
            string line3 = @"{""action"":""remove"",""snapshot"":{""id"":""eeff9900112233445566"",""short_id"":""eeff9900"",""tags"":[""GameB""]}}";
            string output = line1 + "\n" + line2 + "\n" + line3 + "\n";
            var cmd = new CommandResult(0, output, "");

            var result = PruneResultParser.ParseForgetOutput(cmd);

            Assert.Equal(3, result.SnapshotsDeleted);
            Assert.Equal(2, result.GamesAffected);
        }

        [Fact]
        public void ParseForgetOutput_NonZeroExitCode_ReturnsFailure()
        {
            var cmd = new CommandResult(1, "", "error");

            var result = PruneResultParser.ParseForgetOutput(cmd);

            Assert.False(result.Success);
            Assert.Equal(0, result.SnapshotsDeleted);
        }
    }
}
