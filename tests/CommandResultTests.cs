using Xunit;

namespace LudusaviRestic.Tests
{
    public class CommandResultTests
    {
        [Fact]
        public void TransformProcessOutput_PlainAscii_RoundTrips()
        {
            var result = CommandResult.TransformProcessOutput("hello world");

            Assert.Equal("hello world", result);
        }

        [Fact]
        public void TransformProcessOutput_EmptyString_ReturnsEmpty()
        {
            var result = CommandResult.TransformProcessOutput("");

            Assert.Equal("", result);
        }

        [Fact]
        public void InternalConstructor_SetsProperties()
        {
            var result = new CommandResult(42, "out", "err");

            Assert.Equal(42, result.ExitCode);
            Assert.Equal("out", result.StdOut);
            Assert.Equal("err", result.StdErr);
        }
    }
}
