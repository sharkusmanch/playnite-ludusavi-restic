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

    public class MergeForgetResultsTests
    {
        [Fact]
        public void MergeForgetResults_CombinesSnapshots()
        {
            // Each per-game forget returns a ForgetGroup array with one group
            string json1 = @"[{""tags"":[""GameA""],""keep"":[],""remove"":[{""id"":""aabb1122334455667788"",""short_id"":""aabb1122"",""tags"":[""GameA""]}]}]";
            string json2 = @"[{""tags"":[""GameB""],""keep"":[],""remove"":[{""id"":""ccdd5566778899001122"",""short_id"":""ccdd5566"",""tags"":[""GameB""]}]}]";
            var results = new List<CommandResult>
            {
                new CommandResult(0, json1, ""),
                new CommandResult(0, json2, "")
            };

            var merged = PruneResultParser.MergeForgetResults(results, true);

            Assert.True(merged.Success);
            Assert.True(merged.IsDryRun);
            Assert.Equal(2, merged.SnapshotsDeleted);
            Assert.Equal(2, merged.GamesAffected);
            Assert.Equal("GameA", merged.DeletedSnapshots[0].GameName);
            Assert.Equal("GameB", merged.DeletedSnapshots[1].GameName);
        }

        [Fact]
        public void MergeForgetResults_AnyFailure_NotSuccess()
        {
            var results = new List<CommandResult>
            {
                new CommandResult(0, "[]", ""),
                new CommandResult(1, "", "error")
            };

            var merged = PruneResultParser.MergeForgetResults(results, false);

            Assert.False(merged.Success);
        }

        [Fact]
        public void MergeForgetResults_EmptyResults_Success()
        {
            var results = new List<CommandResult>();

            var merged = PruneResultParser.MergeForgetResults(results, false);

            Assert.True(merged.Success);
            Assert.Equal(0, merged.SnapshotsDeleted);
            Assert.Equal(0, merged.GamesAffected);
        }

        [Fact]
        public void MergeForgetResults_AggregatesCounts()
        {
            // GameA forget removes 2 snapshots, GameB removes 1
            string json1 = @"[{""tags"":[""GameA""],""keep"":[],""remove"":[
                {""id"":""aabb1122334455667788"",""short_id"":""aabb1122"",""tags"":[""GameA""]},
                {""id"":""ccdd5566778899001122"",""short_id"":""ccdd5566"",""tags"":[""GameA""]}
            ]}]";
            string json2 = @"[{""tags"":[""GameB""],""keep"":[],""remove"":[
                {""id"":""eeff9900112233445566"",""short_id"":""eeff9900"",""tags"":[""GameB""]}
            ]}]";
            var results = new List<CommandResult>
            {
                new CommandResult(0, json1, ""),
                new CommandResult(0, json2, "")
            };

            var merged = PruneResultParser.MergeForgetResults(results, false);

            Assert.True(merged.Success);
            Assert.Equal(3, merged.SnapshotsDeleted);
            Assert.Equal(2, merged.GamesAffected);
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
        public void ParseForgetOutput_ForgetGroupArray_ParsesRemoved()
        {
            // Real restic forget --json format: array of ForgetGroups
            string output = @"[{
                ""tags"":[""GameA""],
                ""keep"":[{""id"":""kept11112222333344445555"",""short_id"":""kept1111"",""tags"":[""GameA"",""manual""]}],
                ""remove"":[
                    {""id"":""aabb1122334455667788"",""short_id"":""aabb1122"",""time"":""2024-06-15T14:30:00Z"",""hostname"":""myhost"",""tags"":[""GameA"",""manual""],""paths"":[""/saves""]},
                    {""id"":""ccdd5566778899001122"",""short_id"":""ccdd5566"",""time"":""2024-06-14T10:00:00Z"",""hostname"":""myhost"",""tags"":[""GameA"",""gameplay""],""paths"":[""/saves""]}
                ]
            }]";
            var cmd = new CommandResult(0, output, "");

            var result = PruneResultParser.ParseForgetOutput(cmd);

            Assert.True(result.Success);
            Assert.Equal(2, result.SnapshotsDeleted);
            Assert.Equal("aabb1122", result.DeletedSnapshots[0].ShortId);
            Assert.Equal("GameA", result.DeletedSnapshots[0].GameName);
            Assert.Equal(2, result.DeletedSnapshots[0].Tags.Count);
            Assert.Equal("myhost", result.DeletedSnapshots[0].Host);
            Assert.Single(result.DeletedSnapshots[0].Paths);
        }

        [Fact]
        public void ParseForgetOutput_ForgetGroupArray_CountsDistinctGames()
        {
            // Two groups, one with 2 removals and one with 1
            string output = @"[
                {""tags"":[""GameA""],""keep"":[],""remove"":[
                    {""id"":""aabb1122334455667788"",""short_id"":""aabb1122"",""tags"":[""GameA""]},
                    {""id"":""ccdd5566778899001122"",""short_id"":""ccdd5566"",""tags"":[""GameA""]}
                ]},
                {""tags"":[""GameB""],""keep"":[],""remove"":[
                    {""id"":""eeff9900112233445566"",""short_id"":""eeff9900"",""tags"":[""GameB""]}
                ]}
            ]";
            var cmd = new CommandResult(0, output, "");

            var result = PruneResultParser.ParseForgetOutput(cmd);

            Assert.Equal(3, result.SnapshotsDeleted);
            Assert.Equal(2, result.GamesAffected);
        }

        [Fact]
        public void ParseForgetOutput_EmptyRemoveArray_ZeroDeletions()
        {
            // All snapshots kept, none removed
            string output = @"[{""tags"":[""GameA""],""keep"":[
                {""id"":""aabb1122334455667788"",""short_id"":""aabb1122"",""tags"":[""GameA""]}
            ],""remove"":[]}]";
            var cmd = new CommandResult(0, output, "");

            var result = PruneResultParser.ParseForgetOutput(cmd);

            Assert.True(result.Success);
            Assert.Equal(0, result.SnapshotsDeleted);
        }

        [Fact]
        public void ParseForgetOutput_KeepLast1_Removes2Of3()
        {
            // Simulates the Spider-Man 2 scenario: 3 snapshots, keep-last 1
            string output = @"[{
                ""tags"":[""Marvel's Spider-Man 2""],
                ""keep"":[
                    {""id"":""newest112233445566778899"",""short_id"":""newest11"",""time"":""2025-02-05T10:00:00Z"",""tags"":[""Marvel's Spider-Man 2""]}
                ],
                ""remove"":[
                    {""id"":""older1112233445566778899"",""short_id"":""older111"",""time"":""2025-02-04T10:00:00Z"",""tags"":[""Marvel's Spider-Man 2""]},
                    {""id"":""oldest112233445566778899"",""short_id"":""oldest11"",""time"":""2025-02-03T10:00:00Z"",""tags"":[""Marvel's Spider-Man 2""]}
                ]
            }]";
            var cmd = new CommandResult(0, output, "");

            var result = PruneResultParser.ParseForgetOutput(cmd);

            Assert.True(result.Success);
            Assert.Equal(2, result.SnapshotsDeleted);
            Assert.Equal(1, result.GamesAffected);
            Assert.Equal("Marvel's Spider-Man 2", result.DeletedSnapshots[0].GameName);
            Assert.Equal("Marvel's Spider-Man 2", result.DeletedSnapshots[1].GameName);
        }

        [Fact]
        public void ParseForgetOutput_MixedJsonAndText_ParsesJsonPortion()
        {
            // forget --prune --json produces JSON array followed by text prune output
            string output = @"[{""tags"":[""GameA""],""keep"":[],""remove"":[
                {""id"":""aabb1122334455667788"",""short_id"":""aabb1122"",""tags"":[""GameA""]}
            ]}]
loading indexes...
finding data that is still in use for 2 snapshots
[0:00] 100.00%  2 / 2 snapshots
searching used packs...
collecting packs for deletion and repacking
done";
            var cmd = new CommandResult(0, output, "");

            var result = PruneResultParser.ParseForgetOutput(cmd);

            Assert.True(result.Success);
            Assert.Equal(1, result.SnapshotsDeleted);
            Assert.Equal("GameA", result.DeletedSnapshots[0].GameName);
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
