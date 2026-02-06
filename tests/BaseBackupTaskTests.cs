using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class BaseBackupTaskTests
    {
        [Fact]
        public void ConstructTags_SingleGameNoExtras()
        {
            var result = BaseBackupTask.ConstructTags("My Game", new List<string>());

            Assert.Equal("--tag \"My Game\"", result);
        }

        [Fact]
        public void ConstructTags_WithExtraTags()
        {
            var result = BaseBackupTask.ConstructTags("My Game", new List<string> { "manual", "extra" });

            Assert.Equal("--tag \"My Game\" --tag \"manual\" --tag \"extra\"", result);
        }

        [Fact]
        public void ConstructTags_SanitizesCommasInGameName()
        {
            var result = BaseBackupTask.ConstructTags("Game, The", new List<string>());

            Assert.Equal("--tag \"Game_ The\"", result);
        }

        [Fact]
        public void ConstructTags_CommasInExtraTags_NotSanitized()
        {
            // Extra tags are not sanitized per current implementation — they are passed as-is
            var result = BaseBackupTask.ConstructTags("Game", new List<string> { "tag,with,commas" });

            Assert.Equal("--tag \"Game\" --tag \"tag,with,commas\"", result);
        }

        [Fact]
        public void GameFilesToList_ExtractsPropertyNames()
        {
            var json = JObject.Parse(@"{
                ""C:\\saves\\save1.dat"": { ""size"": 1024 },
                ""C:\\saves\\save2.dat"": { ""size"": 2048 }
            }");

            var files = BaseBackupTask.GameFilesToList(json);

            Assert.Equal(2, files.Count);
            Assert.Contains("C:\\saves\\save1.dat", files);
            Assert.Contains("C:\\saves\\save2.dat", files);
        }

        [Fact]
        public void GameFilesToList_EmptyObject_ReturnsEmpty()
        {
            var json = new JObject();

            var files = BaseBackupTask.GameFilesToList(json);

            Assert.Empty(files);
        }

        [Fact]
        public void NormalizePath_UncExtendedPrefix_StrippedToStandardUnc()
        {
            var result = BaseBackupTask.NormalizePath(@"\\?\UNC\SERVER\Share\path\file.txt");

            Assert.Equal(@"\\SERVER\Share\path\file.txt", result);
        }

        [Fact]
        public void NormalizePath_UncExtendedWithForwardSlashes_Normalized()
        {
            var result = BaseBackupTask.NormalizePath("\\\\?\\UNC\\SERVER\\Share/games/save/data.dat");

            Assert.Equal(@"\\SERVER\Share\games\save\data.dat", result);
        }

        [Fact]
        public void NormalizePath_LocalExtendedPrefix_Stripped()
        {
            var result = BaseBackupTask.NormalizePath(@"\\?\C:\Users\test\saves\game.dat");

            Assert.Equal(@"C:\Users\test\saves\game.dat", result);
        }

        [Fact]
        public void NormalizePath_ForwardSlashes_ConvertedToBackslashes()
        {
            var result = BaseBackupTask.NormalizePath("C:/Users/test/saves/game.dat");

            Assert.Equal(@"C:\Users\test\saves\game.dat", result);
        }

        [Fact]
        public void NormalizePath_StandardPath_Unchanged()
        {
            var result = BaseBackupTask.NormalizePath(@"C:\Users\test\saves\game.dat");

            Assert.Equal(@"C:\Users\test\saves\game.dat", result);
        }

        [Fact]
        public void NormalizePath_StandardUncPath_Unchanged()
        {
            var result = BaseBackupTask.NormalizePath(@"\\SERVER\Share\path\file.txt");

            Assert.Equal(@"\\SERVER\Share\path\file.txt", result);
        }
    }
}
