using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class LudusaviResticSettingsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var settings = new LudusaviResticSettings();

            Assert.Equal("ludusavi", settings.LudusaviExecutablePath);
            Assert.Equal("restic", settings.ResticExecutablePath);
            Assert.Null(settings.ResticRepository);
            Assert.Null(settings.ResticPassword);
            Assert.False(settings.BackupDuringGameplay);
            Assert.Equal(5, settings.GameplayBackupInterval);
            Assert.False(settings.AdditionalTagging);
            Assert.Equal("manual", settings.ManualSnapshotTag);
            Assert.Equal("game-stopped", settings.GameStoppedSnapshotTag);
            Assert.Equal("gameplay", settings.GameplaySnapshotTag);
            Assert.True(settings.BackupWhenGameStopped);
            Assert.False(settings.BackupOnUninstall);
            Assert.False(settings.PromptForGameStoppedTag);
            Assert.Equal(ExecutionMode.Exclude, settings.BackupExecutionMode);
            Assert.False(settings.EnableRetentionPolicy);
            Assert.Equal(10, settings.KeepLast);
            Assert.Equal(7, settings.KeepDaily);
            Assert.Equal(4, settings.KeepWeekly);
            Assert.Equal(12, settings.KeepMonthly);
            Assert.Equal(5, settings.KeepYearly);
            Assert.NotNull(settings.GameIntervalOverrides);
            Assert.Empty(settings.GameIntervalOverrides);
        }

        [Fact]
        public void PropertyChanged_FiresForLudusaviExecutablePath()
        {
            var settings = new LudusaviResticSettings();
            var fired = new List<string>();
            settings.PropertyChanged += (s, e) => fired.Add(e.PropertyName);

            settings.LudusaviExecutablePath = "/usr/bin/ludusavi";

            Assert.Contains("LudusaviExecutablePath", fired);
        }

        [Fact]
        public void PropertyChanged_FiresForResticExecutablePath()
        {
            var settings = new LudusaviResticSettings();
            var fired = new List<string>();
            settings.PropertyChanged += (s, e) => fired.Add(e.PropertyName);

            settings.ResticExecutablePath = "/usr/bin/restic";

            Assert.Contains("ResticExecutablePath", fired);
        }

        [Fact]
        public void PropertyChanged_FiresForBooleanProperties()
        {
            var settings = new LudusaviResticSettings();
            var fired = new List<string>();
            settings.PropertyChanged += (s, e) => fired.Add(e.PropertyName);

            settings.BackupDuringGameplay = true;
            settings.AdditionalTagging = true;
            settings.BackupWhenGameStopped = false;
            settings.BackupOnUninstall = true;

            Assert.Contains("BackupDuringGameplay", fired);
            Assert.Contains("AdditionalTagging", fired);
            Assert.Contains("BackupWhenGameStopped", fired);
            Assert.Contains("BackupOnUninstall", fired);
        }

        [Fact]
        public void GameplayBackupInterval_RejectsZero()
        {
            var settings = new LudusaviResticSettings();
            settings.BeginEdit();

            settings.GameplayBackupInterval = 0;

            List<string> errors;
            bool valid = settings.VerifySettings(out errors);
            Assert.False(valid);
            Assert.Contains("Backup interval must be a positive integer", errors);
        }

        [Fact]
        public void GameplayBackupInterval_RejectsNegative()
        {
            var settings = new LudusaviResticSettings();
            settings.BeginEdit();

            settings.GameplayBackupInterval = -5;

            List<string> errors;
            bool valid = settings.VerifySettings(out errors);
            Assert.False(valid);
            Assert.Contains("Backup interval must be a positive integer", errors);
        }

        [Fact]
        public void GameplayBackupInterval_AcceptsPositive()
        {
            var settings = new LudusaviResticSettings();
            settings.BeginEdit();

            settings.GameplayBackupInterval = 10;

            List<string> errors;
            bool valid = settings.VerifySettings(out errors);
            Assert.True(valid);
            Assert.Equal(10, settings.GameplayBackupInterval);
        }

        [Fact]
        public void GetEffectiveInterval_WithOverride_ReturnsOverrideValue()
        {
            var settings = new LudusaviResticSettings();
            var gameId = Guid.NewGuid();
            settings.GameIntervalOverrides[gameId.ToString()] = new GameIntervalOverride("Test Game", 15);

            Assert.Equal(15, settings.GetEffectiveInterval(gameId));
        }

        [Fact]
        public void GetEffectiveInterval_WithoutOverride_ReturnsGlobalDefault()
        {
            var settings = new LudusaviResticSettings();
            var gameId = Guid.NewGuid();

            Assert.Equal(settings.GameplayBackupInterval, settings.GetEffectiveInterval(gameId));
        }

        [Fact]
        public void GetEffectiveInterval_WithZeroOverride_ReturnsGlobalDefault()
        {
            var settings = new LudusaviResticSettings();
            var gameId = Guid.NewGuid();
            settings.GameIntervalOverrides[gameId.ToString()] = new GameIntervalOverride("Test Game", 0);

            Assert.Equal(settings.GameplayBackupInterval, settings.GetEffectiveInterval(gameId));
        }

        [Fact]
        public void GameIntervalOverrides_SerializationRoundTrip()
        {
            var settings = new LudusaviResticSettings();
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            settings.GameIntervalOverrides[id1] = new GameIntervalOverride("Game A", 10);
            settings.GameIntervalOverrides[id2] = new GameIntervalOverride("Game B", 30);

            var json = JsonConvert.SerializeObject(settings);
            var deserialized = JsonConvert.DeserializeObject<LudusaviResticSettings>(json);

            Assert.Equal(2, deserialized.GameIntervalOverrides.Count);
            Assert.Equal("Game A", deserialized.GameIntervalOverrides[id1].GameName);
            Assert.Equal(10, deserialized.GameIntervalOverrides[id1].IntervalMinutes);
            Assert.Equal("Game B", deserialized.GameIntervalOverrides[id2].GameName);
            Assert.Equal(30, deserialized.GameIntervalOverrides[id2].IntervalMinutes);
        }
    }
}
