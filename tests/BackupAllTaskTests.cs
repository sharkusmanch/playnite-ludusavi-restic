using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class BackupAllTaskTests
    {
        [Fact]
        public void ParseAllGameFiles_MultipleGames_ParsedCorrectly()
        {
            var json = @"{
                ""overall"": { ""totalGames"": 2 },
                ""games"": {
                    ""Game A"": {
                        ""files"": {
                            ""C:\\saves\\a1.dat"": { ""size"": 100 },
                            ""C:\\saves\\a2.dat"": { ""size"": 200 }
                        }
                    },
                    ""Game B"": {
                        ""files"": {
                            ""D:\\saves\\b1.dat"": { ""size"": 300 }
                        }
                    }
                }
            }";

            var result = BackupAllTask.ParseAllGameFiles(json);

            Assert.Equal(2, result.Count);
            Assert.Equal(2, result["Game A"].Count);
            Assert.Single(result["Game B"]);
            Assert.Contains("C:\\saves\\a1.dat", result["Game A"]);
            Assert.Contains("D:\\saves\\b1.dat", result["Game B"]);
        }

        [Fact]
        public void ParseAllGameFiles_EmptyGames_ReturnsEmptyDict()
        {
            var json = @"{
                ""overall"": { ""totalGames"": 0 },
                ""games"": {}
            }";

            var result = BackupAllTask.ParseAllGameFiles(json);

            Assert.Empty(result);
        }

        [Fact]
        public void ParseAllGameFiles_GameWithEmptyFiles_ReturnsEmptyFileList()
        {
            var json = @"{
                ""overall"": { ""totalGames"": 1 },
                ""games"": {
                    ""Empty Game"": {
                        ""files"": {}
                    }
                }
            }";

            var result = BackupAllTask.ParseAllGameFiles(json);

            Assert.Single(result);
            Assert.Empty(result["Empty Game"]);
        }

        [Fact]
        public void ParseAllGameFiles_SpecialCharactersInGameName_Preserved()
        {
            var json = @"{
                ""overall"": { ""totalGames"": 1 },
                ""games"": {
                    ""Game: The Sequel (2024) - Director's Cut"": {
                        ""files"": {
                            ""C:\\save.dat"": { ""size"": 100 }
                        }
                    }
                }
            }";

            var result = BackupAllTask.ParseAllGameFiles(json);

            Assert.True(result.ContainsKey("Game: The Sequel (2024) - Director's Cut"));
        }
    }
}
