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
            settings.GameIntervalOverrides[gameId.ToString()] = new GameOverride("Test Game", 15);

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
            settings.GameIntervalOverrides[gameId.ToString()] = new GameOverride("Test Game", 0);

            Assert.Equal(settings.GameplayBackupInterval, settings.GetEffectiveInterval(gameId));
        }

        [Fact]
        public void GameIntervalOverrides_SerializationRoundTrip()
        {
            var settings = new LudusaviResticSettings();
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            settings.GameIntervalOverrides[id1] = new GameOverride("Game A", 10);
            settings.GameIntervalOverrides[id2] = new GameOverride("Game B", 30);

            var json = JsonConvert.SerializeObject(settings);
            var deserialized = JsonConvert.DeserializeObject<LudusaviResticSettings>(json);

            Assert.Equal(2, deserialized.GameIntervalOverrides.Count);
            Assert.Equal("Game A", deserialized.GameIntervalOverrides[id1].GameName);
            Assert.Equal(10, deserialized.GameIntervalOverrides[id1].IntervalMinutes);
            Assert.Equal("Game B", deserialized.GameIntervalOverrides[id2].GameName);
            Assert.Equal(30, deserialized.GameIntervalOverrides[id2].IntervalMinutes);
        }

        [Fact]
        public void HasRetentionOverride_DefaultIsFalse()
        {
            var over = new GameOverride("Test", 5);

            Assert.False(over.HasRetentionOverride);
        }

        [Fact]
        public void HasRetentionOverride_TrueWhenAnyFieldSet()
        {
            var over = new GameOverride("Test", 5);
            over.KeepLast = 20;

            Assert.True(over.HasRetentionOverride);
        }

        [Fact]
        public void HasRetentionOverride_TrueWhenMultipleFieldsSet()
        {
            var over = new GameOverride("Test", 5);
            over.KeepDaily = 14;
            over.KeepYearly = 3;

            Assert.True(over.HasRetentionOverride);
        }

        [Fact]
        public void GetEffectiveRetention_UsesOverrideWhenSet()
        {
            var settings = new LudusaviResticSettings();
            var over = new GameOverride("Test", 5);
            over.KeepLast = 20;
            over.KeepDaily = 14;

            var retention = over.GetEffectiveRetention(settings);

            Assert.Equal(20, retention.KeepLast);
            Assert.Equal(14, retention.KeepDaily);
            // Unset fields default to 0 (disabled), not global
            Assert.Equal(0, retention.KeepWeekly);
            Assert.Equal(0, retention.KeepMonthly);
            Assert.Equal(0, retention.KeepYearly);
        }

        [Fact]
        public void GetEffectiveRetention_UnsetFieldsDefaultToZero()
        {
            var settings = new LudusaviResticSettings();
            var over = new GameOverride("Test", 5);

            var retention = over.GetEffectiveRetention(settings);

            Assert.Equal(0, retention.KeepLast);
            Assert.Equal(0, retention.KeepDaily);
            Assert.Equal(0, retention.KeepWeekly);
            Assert.Equal(0, retention.KeepMonthly);
            Assert.Equal(0, retention.KeepYearly);
        }

        [Fact]
        public void RetentionOverride_SerializationRoundTrip()
        {
            var settings = new LudusaviResticSettings();
            var id = Guid.NewGuid().ToString();
            var over = new GameOverride("Test Game", 5);
            over.KeepLast = 20;
            over.KeepDaily = null;
            over.KeepWeekly = 8;
            settings.GameIntervalOverrides[id] = over;

            var json = JsonConvert.SerializeObject(settings);
            var deserialized = JsonConvert.DeserializeObject<LudusaviResticSettings>(json);

            var result = deserialized.GameIntervalOverrides[id];
            Assert.Equal(20, result.KeepLast);
            Assert.Null(result.KeepDaily);
            Assert.Equal(8, result.KeepWeekly);
            Assert.Null(result.KeepMonthly);
            Assert.Null(result.KeepYearly);
            Assert.True(result.HasRetentionOverride);
        }

        [Fact]
        public void BackwardCompat_OldJsonWithoutRetentionFields_DeserializesToNulls()
        {
            // Simulate old JSON that only had GameName and IntervalMinutes
            string json = @"{""GameIntervalOverrides"":{""test-id"":{""GameName"":""Old Game"",""IntervalMinutes"":10}}}";

            var deserialized = JsonConvert.DeserializeObject<LudusaviResticSettings>(json);

            var over = deserialized.GameIntervalOverrides["test-id"];
            Assert.Equal("Old Game", over.GameName);
            Assert.Equal(10, over.IntervalMinutes);
            Assert.Null(over.KeepLast);
            Assert.Null(over.KeepDaily);
            Assert.Null(over.KeepWeekly);
            Assert.Null(over.KeepMonthly);
            Assert.Null(over.KeepYearly);
            Assert.False(over.HasRetentionOverride);
        }

        [Fact]
        public void FindOverrideByGameName_Found()
        {
            var settings = new LudusaviResticSettings();
            var id = Guid.NewGuid().ToString();
            settings.GameIntervalOverrides[id] = new GameOverride("My Game", 10);

            var found = settings.FindOverrideByGameName("My Game");

            Assert.NotNull(found);
            Assert.Equal("My Game", found.GameName);
        }

        [Fact]
        public void FindOverrideByGameName_NotFound()
        {
            var settings = new LudusaviResticSettings();
            var id = Guid.NewGuid().ToString();
            settings.GameIntervalOverrides[id] = new GameOverride("My Game", 10);

            var found = settings.FindOverrideByGameName("Other Game");

            Assert.Null(found);
        }

        [Fact]
        public void FindOverrideByGameName_CaseInsensitive()
        {
            var settings = new LudusaviResticSettings();
            var id = Guid.NewGuid().ToString();
            settings.GameIntervalOverrides[id] = new GameOverride("My Game", 10);

            var found = settings.FindOverrideByGameName("my game");

            Assert.NotNull(found);
        }
    }
}
