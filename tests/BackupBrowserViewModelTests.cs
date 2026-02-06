using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class BackupBrowserViewModelTests
    {
        // --- ParseSnapshots ---

        [Fact]
        public void ParseSnapshots_ValidJson_ParsedCorrectly()
        {
            var json = @"[
                {
                    ""short_id"": ""abc123"",
                    ""id"": ""abc123full"",
                    ""time"": ""2024-01-15T10:30:00Z"",
                    ""tags"": [""MyGame"", ""manual""]
                }
            ]";

            var result = BackupBrowserViewModel.ParseSnapshots(json);

            Assert.Single(result);
            Assert.Equal("abc123", result[0].Id);
            Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), result[0].Date);
            Assert.Equal(2, result[0].Tags.Count);
            Assert.Equal("MyGame", result[0].Tags[0]);
            Assert.Equal("manual", result[0].Tags[1]);
        }

        [Fact]
        public void ParseSnapshots_NoShortId_FallsBackToId()
        {
            var json = @"[
                {
                    ""id"": ""fullid123456789"",
                    ""time"": ""2024-01-15T10:30:00Z"",
                    ""tags"": [""Game""]
                }
            ]";

            var result = BackupBrowserViewModel.ParseSnapshots(json);

            Assert.Equal("fullid123456789", result[0].Id);
        }

        [Fact]
        public void ParseSnapshots_NoTags_DefaultsToEmptyList()
        {
            var json = @"[
                {
                    ""short_id"": ""abc123"",
                    ""time"": ""2024-01-15T10:30:00Z""
                }
            ]";

            var result = BackupBrowserViewModel.ParseSnapshots(json);

            Assert.Empty(result[0].Tags);
            Assert.Equal("Unknown", result[0].GameName);
        }

        [Fact]
        public void ParseSnapshots_EmptyArray_ReturnsEmptyList()
        {
            var result = BackupBrowserViewModel.ParseSnapshots("[]");

            Assert.Empty(result);
        }

        [Fact]
        public void ParseSnapshots_MultipleSnapshots_AllParsed()
        {
            var json = @"[
                { ""short_id"": ""a1"", ""time"": ""2024-01-01T00:00:00Z"", ""tags"": [""Game1""] },
                { ""short_id"": ""b2"", ""time"": ""2024-02-01T00:00:00Z"", ""tags"": [""Game2""] },
                { ""short_id"": ""c3"", ""time"": ""2024-03-01T00:00:00Z"", ""tags"": [""Game3""] }
            ]";

            var result = BackupBrowserViewModel.ParseSnapshots(json);

            Assert.Equal(3, result.Count);
        }

        // --- FilterSnapshots ---

        [Fact]
        public void FilterSnapshots_AllGames_ReturnsAll()
        {
            var snapshots = new List<BackupSnapshot>
            {
                new BackupSnapshot { Tags = new List<string> { "Game1" } },
                new BackupSnapshot { Tags = new List<string> { "Game2" } }
            };

            var result = BackupBrowserViewModel.FilterSnapshots(snapshots, "All Games");

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void FilterSnapshots_NullFilter_ReturnsAll()
        {
            var snapshots = new List<BackupSnapshot>
            {
                new BackupSnapshot { Tags = new List<string> { "Game1" } },
                new BackupSnapshot { Tags = new List<string> { "Game2" } }
            };

            var result = BackupBrowserViewModel.FilterSnapshots(snapshots, null);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void FilterSnapshots_EmptyFilter_ReturnsAll()
        {
            var snapshots = new List<BackupSnapshot>
            {
                new BackupSnapshot { Tags = new List<string> { "Game1" } }
            };

            var result = BackupBrowserViewModel.FilterSnapshots(snapshots, "");

            Assert.Single(result);
        }

        [Fact]
        public void FilterSnapshots_SpecificGame_OnlyMatching()
        {
            var snapshots = new List<BackupSnapshot>
            {
                new BackupSnapshot { Tags = new List<string> { "Game1" } },
                new BackupSnapshot { Tags = new List<string> { "Game2" } },
                new BackupSnapshot { Tags = new List<string> { "Game1" } }
            };

            var result = BackupBrowserViewModel.FilterSnapshots(snapshots, "Game1");

            Assert.Equal(2, result.Count);
            Assert.All(result, s => Assert.Equal("Game1", s.GameName));
        }

        [Fact]
        public void FilterSnapshots_NoMatch_ReturnsEmpty()
        {
            var snapshots = new List<BackupSnapshot>
            {
                new BackupSnapshot { Tags = new List<string> { "Game1" } }
            };

            var result = BackupBrowserViewModel.FilterSnapshots(snapshots, "NonExistent");

            Assert.Empty(result);
        }

        [Fact]
        public void FilterSnapshots_NullInputList_ReturnsEmpty()
        {
            var result = BackupBrowserViewModel.FilterSnapshots(null, "Game1");

            Assert.Empty(result);
        }

        // --- BuildGameFilters ---

        [Fact]
        public void BuildGameFilters_ExtractsDistinctSortedNames()
        {
            var snapshots = new List<BackupSnapshot>
            {
                new BackupSnapshot { Tags = new List<string> { "Zelda" } },
                new BackupSnapshot { Tags = new List<string> { "Elden Ring" } },
                new BackupSnapshot { Tags = new List<string> { "Zelda" } },
                new BackupSnapshot { Tags = new List<string> { "Cyberpunk" } }
            };

            var result = BackupBrowserViewModel.BuildGameFilters(snapshots);

            Assert.Equal(4, result.Count);
            Assert.Equal("All Games", result[0]);
            Assert.Equal("Cyberpunk", result[1]);
            Assert.Equal("Elden Ring", result[2]);
            Assert.Equal("Zelda", result[3]);
        }

        [Fact]
        public void BuildGameFilters_ExcludesUnknown()
        {
            var snapshots = new List<BackupSnapshot>
            {
                new BackupSnapshot { Tags = new List<string> { "Game1" } },
                new BackupSnapshot { Tags = new List<string>() } // GameName = "Unknown"
            };

            var result = BackupBrowserViewModel.BuildGameFilters(snapshots);

            Assert.DoesNotContain("Unknown", result);
            Assert.Contains("Game1", result);
        }

        [Fact]
        public void BuildGameFilters_NoSnapshots_JustAllGames()
        {
            var result = BackupBrowserViewModel.BuildGameFilters(new List<BackupSnapshot>());

            Assert.Single(result);
            Assert.Equal("All Games", result[0]);
        }
    }
}
