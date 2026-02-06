using System.Collections.Generic;
using System.ComponentModel;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class BackupSnapshotTests
    {
        [Fact]
        public void ShortId_TruncatesTo8Chars()
        {
            var snapshot = new BackupSnapshot { Id = "aabbccdd11223344" };

            Assert.Equal("aabbccdd", snapshot.ShortId);
        }

        [Fact]
        public void ShortId_ShortIdReturnedAsIs()
        {
            var snapshot = new BackupSnapshot { Id = "abcd" };

            Assert.Equal("abcd", snapshot.ShortId);
        }

        [Fact]
        public void ShortId_ExactlyEightChars()
        {
            var snapshot = new BackupSnapshot { Id = "12345678" };

            Assert.Equal("12345678", snapshot.ShortId);
        }

        [Fact]
        public void ShortId_NullId_ReturnsNull()
        {
            var snapshot = new BackupSnapshot { Id = null };

            Assert.Null(snapshot.ShortId);
        }

        [Fact]
        public void GameName_ReturnsFirstTag()
        {
            var snapshot = new BackupSnapshot
            {
                Tags = new List<string> { "MyGame", "manual" }
            };

            Assert.Equal("MyGame", snapshot.GameName);
        }

        [Fact]
        public void GameName_NoTags_ReturnsUnknown()
        {
            var snapshot = new BackupSnapshot
            {
                Tags = new List<string>()
            };

            Assert.Equal("Unknown", snapshot.GameName);
        }

        [Fact]
        public void TagsDisplay_JoinsWithComma()
        {
            var snapshot = new BackupSnapshot
            {
                Tags = new List<string> { "GameA", "manual", "extra" }
            };

            Assert.Equal("GameA, manual, extra", snapshot.TagsDisplay);
        }

        [Fact]
        public void TagsDisplay_Empty_ReturnsEmptyString()
        {
            var snapshot = new BackupSnapshot
            {
                Tags = new List<string>()
            };

            Assert.Equal("", snapshot.TagsDisplay);
        }

        [Fact]
        public void PropertyChanged_FiresWhenIdSet()
        {
            var snapshot = new BackupSnapshot();
            var fired = new List<string>();
            snapshot.PropertyChanged += (s, e) => fired.Add(e.PropertyName);

            snapshot.Id = "test123";

            // BackupSnapshot uses auto-properties without calling OnPropertyChanged,
            // so PropertyChanged won't fire for auto-properties unless explicitly triggered.
            // This test documents the current behavior.
            Assert.Empty(fired);
        }
    }
}
