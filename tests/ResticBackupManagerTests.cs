using System;
using System.Collections.Generic;
using Playnite.SDK.Models;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class ResticBackupManagerTests
    {
        private static readonly Guid TestTagId = Guid.NewGuid();

        private static Game MakeGame(string name, List<Guid> tagIds = null)
        {
            return new Game(name) { TagIds = tagIds };
        }

        // --- GameHasTag ---

        [Fact]
        public void GameHasTag_TagPresent_ReturnsTrue()
        {
            var game = MakeGame("Test", new List<Guid> { TestTagId });

            Assert.True(ResticBackupManager.GameHasTag(game, TestTagId));
        }

        [Fact]
        public void GameHasTag_TagMissing_ReturnsFalse()
        {
            var game = MakeGame("Test", new List<Guid> { Guid.NewGuid() });

            Assert.False(ResticBackupManager.GameHasTag(game, TestTagId));
        }

        [Fact]
        public void GameHasTag_NullTagIds_ReturnsFalse()
        {
            var game = MakeGame("Test");

            Assert.False(ResticBackupManager.GameHasTag(game, TestTagId));
        }

        [Fact]
        public void GameHasTag_EmptyTagIds_ReturnsFalse()
        {
            var game = MakeGame("Test", new List<Guid>());

            Assert.False(ResticBackupManager.GameHasTag(game, TestTagId));
        }

        // --- BuildBackupTags ---

        [Fact]
        public void BuildBackupTags_TaggingEnabled_ReturnsListWithTag()
        {
            var result = ResticBackupManager.BuildBackupTags(true, "manual");

            Assert.Single(result);
            Assert.Equal("manual", result[0]);
        }

        [Fact]
        public void BuildBackupTags_TaggingDisabled_ReturnsEmptyList()
        {
            var result = ResticBackupManager.BuildBackupTags(false, "manual");

            Assert.Empty(result);
        }

        // --- ShouldSkipBackup ---

        [Fact]
        public void ShouldSkipBackup_ExcludeMode_HasExcludeTag_ReturnsTrue()
        {
            var excludeId = Guid.NewGuid();
            var includeId = Guid.NewGuid();
            var game = MakeGame("Test", new List<Guid> { excludeId });

            Assert.True(ResticBackupManager.ShouldSkipBackup(ExecutionMode.Exclude, game, excludeId, includeId));
        }

        [Fact]
        public void ShouldSkipBackup_ExcludeMode_LacksExcludeTag_ReturnsFalse()
        {
            var excludeId = Guid.NewGuid();
            var includeId = Guid.NewGuid();
            var game = MakeGame("Test", new List<Guid> { Guid.NewGuid() });

            Assert.False(ResticBackupManager.ShouldSkipBackup(ExecutionMode.Exclude, game, excludeId, includeId));
        }

        [Fact]
        public void ShouldSkipBackup_IncludeMode_HasIncludeTag_ReturnsFalse()
        {
            var excludeId = Guid.NewGuid();
            var includeId = Guid.NewGuid();
            var game = MakeGame("Test", new List<Guid> { includeId });

            Assert.False(ResticBackupManager.ShouldSkipBackup(ExecutionMode.Include, game, excludeId, includeId));
        }

        [Fact]
        public void ShouldSkipBackup_IncludeMode_LacksIncludeTag_ReturnsTrue()
        {
            var excludeId = Guid.NewGuid();
            var includeId = Guid.NewGuid();
            var game = MakeGame("Test", new List<Guid> { Guid.NewGuid() });

            Assert.True(ResticBackupManager.ShouldSkipBackup(ExecutionMode.Include, game, excludeId, includeId));
        }

        [Fact]
        public void ShouldSkipBackup_ExcludeMode_NullTags_ReturnsFalse()
        {
            var excludeId = Guid.NewGuid();
            var includeId = Guid.NewGuid();
            var game = MakeGame("Test");

            Assert.False(ResticBackupManager.ShouldSkipBackup(ExecutionMode.Exclude, game, excludeId, includeId));
        }

        [Fact]
        public void ShouldSkipBackup_IncludeMode_NullTags_ReturnsTrue()
        {
            var excludeId = Guid.NewGuid();
            var includeId = Guid.NewGuid();
            var game = MakeGame("Test");

            Assert.True(ResticBackupManager.ShouldSkipBackup(ExecutionMode.Include, game, excludeId, includeId));
        }
    }
}
