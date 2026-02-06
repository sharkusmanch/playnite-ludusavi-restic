using System.Collections.Generic;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class BackupGameTaskTests
    {
        private static string MakeJson(string gameName, string filesJson, int totalGames = 1)
        {
            return $@"{{
                ""overall"": {{ ""totalGames"": {totalGames} }},
                ""games"": {{
                    ""{gameName}"": {{
                        ""files"": {filesJson}
                    }}
                }}
            }}";
        }

        [Fact]
        public void ParseGameFiles_NewObjectFormat_ReturnsFilePaths()
        {
            var json = MakeJson("TestGame", @"{
                ""C:\\saves\\save1.dat"": { ""size"": 1024 },
                ""C:\\saves\\save2.dat"": { ""size"": 2048 }
            }");

            var files = BackupGameTask.ParseGameFiles("TestGame", json);

            Assert.Equal(2, files.Count);
            Assert.Contains("C:\\saves\\save1.dat", files);
            Assert.Contains("C:\\saves\\save2.dat", files);
        }

        [Fact]
        public void ParseGameFiles_NewObjectFormat_FiltersIgnored()
        {
            var json = MakeJson("TestGame", @"{
                ""C:\\saves\\save1.dat"": { ""ignored"": false },
                ""C:\\saves\\save2.dat"": { ""ignored"": true }
            }");

            var files = BackupGameTask.ParseGameFiles("TestGame", json);

            Assert.Single(files);
            Assert.Contains("C:\\saves\\save1.dat", files);
        }

        [Fact]
        public void ParseGameFiles_OldArrayFormat_ReturnsFilePaths()
        {
            var json = MakeJson("TestGame", @"[
                { ""path"": ""C:\\saves\\save1.dat"", ""size"": 1024 },
                { ""path"": ""C:\\saves\\save2.dat"", ""size"": 2048 }
            ]");

            var files = BackupGameTask.ParseGameFiles("TestGame", json);

            Assert.Equal(2, files.Count);
            Assert.Contains("C:\\saves\\save1.dat", files);
            Assert.Contains("C:\\saves\\save2.dat", files);
        }

        [Fact]
        public void ParseGameFiles_OldArrayFormat_FiltersIgnored()
        {
            var json = MakeJson("TestGame", @"[
                { ""path"": ""C:\\saves\\save1.dat"", ""ignored"": false },
                { ""path"": ""C:\\saves\\save2.dat"", ""ignored"": true }
            ]");

            var files = BackupGameTask.ParseGameFiles("TestGame", json);

            Assert.Single(files);
            Assert.Contains("C:\\saves\\save1.dat", files);
        }

        [Fact]
        public void ParseGameFiles_TotalGamesNotOne_ReturnsEmptyList()
        {
            var json = @"{
                ""overall"": { ""totalGames"": 0 },
                ""games"": {}
            }";

            var files = BackupGameTask.ParseGameFiles("TestGame", json);

            Assert.Empty(files);
        }

        [Fact]
        public void ParseGameFiles_EmptyFilesObject_ReturnsEmptyList()
        {
            var json = MakeJson("TestGame", "{}");

            var files = BackupGameTask.ParseGameFiles("TestGame", json);

            Assert.Empty(files);
        }

        [Fact]
        public void ParseGameFiles_AllFilesIgnored_ReturnsEmptyList()
        {
            var json = MakeJson("TestGame", @"{
                ""C:\\saves\\save1.dat"": { ""ignored"": true },
                ""C:\\saves\\save2.dat"": { ""ignored"": true }
            }");

            var files = BackupGameTask.ParseGameFiles("TestGame", json);

            Assert.Empty(files);
        }
    }
}
