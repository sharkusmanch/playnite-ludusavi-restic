using System.Linq;
using Xunit;

namespace LudusaviRestic.Tests
{
    public class ResticUtilityTests
    {
        [Fact]
        public void GetRepositorySuggestions_ReturnsNonEmptyList()
        {
            var suggestions = ResticUtility.GetRepositorySuggestions();

            Assert.NotEmpty(suggestions);
        }

        [Fact]
        public void GetRepositorySuggestions_ContainsPlayniteBackups()
        {
            var suggestions = ResticUtility.GetRepositorySuggestions();

            Assert.Contains(suggestions, s => s.Contains("PlayniteBackups"));
        }

        [Fact]
        public void GetRepositorySuggestions_AllEntriesNonEmpty()
        {
            var suggestions = ResticUtility.GetRepositorySuggestions();

            Assert.All(suggestions, s => Assert.False(string.IsNullOrEmpty(s)));
        }
    }
}
