using System;
using System.ComponentModel;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class EnumExtensionsTests
    {
        private enum TestEnum
        {
            [Description("First item")]
            First = 0,
            [Description("Second item")]
            Second = 1,
            Third = 5,
            Fourth = 10
        }

        [Fact]
        public void GetMax_ReturnsHighestValue_TestEnum()
        {
            Assert.Equal(10, TestEnum.First.GetMax());
        }

        [Fact]
        public void GetMin_ReturnsLowestValue_TestEnum()
        {
            Assert.Equal(0, TestEnum.First.GetMin());
        }

        [Fact]
        public void GetMax_ReturnsHighestValue_ExecutionMode()
        {
            Assert.Equal(1, ExecutionMode.Exclude.GetMax());
        }

        [Fact]
        public void GetMin_ReturnsLowestValue_ExecutionMode()
        {
            Assert.Equal(0, ExecutionMode.Include.GetMin());
        }

        [Fact]
        public void GetMax_SameResultRegardlessOfMember()
        {
            Assert.Equal(TestEnum.First.GetMax(), TestEnum.Fourth.GetMax());
        }

        [Fact]
        public void GetDescription_WithDescriptionAttribute_ReturnsText()
        {
            Assert.Equal("First item", TestEnum.First.GetDescription());
        }

        [Fact]
        public void GetDescription_WithoutAttribute_ReturnsToString()
        {
            Assert.Equal("Third", TestEnum.Third.GetDescription());
        }

        [Fact]
        public void GetDescription_InvalidEnumValue_ReturnsEmpty()
        {
            var invalid = (TestEnum)999;
            Assert.Equal(string.Empty, invalid.GetDescription());
        }

        // --- NotificationLevel enum ---

        [Fact]
        public void GetMax_NotificationLevel_ReturnsTwo()
        {
            Assert.Equal(2, NotificationLevel.ErrorsOnly.GetMax());
        }

        [Fact]
        public void GetMin_NotificationLevel_ReturnsZero()
        {
            Assert.Equal(0, NotificationLevel.ErrorsOnly.GetMin());
        }

        [Fact]
        public void GetDescription_NotificationLevel_ErrorsOnly()
        {
            // ResourceProvider wraps LOC keys in <!...!> outside Playnite runtime
            var desc = NotificationLevel.ErrorsOnly.GetDescription();
            Assert.Contains("LOCLuduRestNotificationLevelErrorsOnly", desc);
        }

        [Fact]
        public void GetDescription_NotificationLevel_Summary()
        {
            var desc = NotificationLevel.Summary.GetDescription();
            Assert.Contains("LOCLuduRestNotificationLevelSummary", desc);
        }

        [Fact]
        public void GetDescription_NotificationLevel_Verbose()
        {
            var desc = NotificationLevel.Verbose.GetDescription();
            Assert.Contains("LOCLuduRestNotificationLevelVerbose", desc);
        }
    }
}
