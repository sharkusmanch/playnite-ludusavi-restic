using System.Text;
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

        // Simulate process stdout: UTF-8 bytes misread via Encoding.Default
        private static string SimulateGarbledProcessOutput(string original)
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(original);
            return Encoding.Default.GetString(utf8Bytes);
        }

        [Fact]
        public void TransformProcessOutput_UnicodeGreekSigma_PreservesCharacter()
        {
            // Simulate what .NET's Process.StandardOutput.ReadToEnd() produces
            // when the process writes UTF-8 but .NET reads with Encoding.Default
            var garbled = SimulateGarbledProcessOutput("Ninja Gaiden \u03A3");

            var result = CommandResult.TransformProcessOutput(garbled);

            Assert.Equal("Ninja Gaiden \u03A3", result);
        }

        [Fact]
        public void TransformProcessOutput_UnicodeJsonWithSpecialChars_Preserved()
        {
            var original = "{\"games\":{\"Ninja Gaiden \u03A3\":{\"files\":{\"C:/Save/\u03A3.dat\":{}}}}}";
            var garbled = SimulateGarbledProcessOutput(original);

            var result = CommandResult.TransformProcessOutput(garbled);

            Assert.Contains("Ninja Gaiden \u03A3", result);
            Assert.Contains("\u03A3.dat", result);
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
