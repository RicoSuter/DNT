using System;
using Dnt.Commands.Packages.Switcher;
using Xunit;

namespace Dnt.Tests
{
    public class VariableOverrideParserTests
    {
        [Fact]
        public void Parse_SinglePair()
        {
            var result = VariableOverrideParser.Parse("Lib=../Lib");
            Assert.Single(result);
            Assert.Equal("../Lib", result["Lib"]);
        }

        [Fact]
        public void Parse_MultiplePairs()
        {
            var result = VariableOverrideParser.Parse("A=../a;B=../b");
            Assert.Equal(2, result.Count);
            Assert.Equal("../a", result["A"]);
            Assert.Equal("../b", result["B"]);
        }

        [Fact]
        public void Parse_ValueContainingEquals_KeepsRemainder()
        {
            var result = VariableOverrideParser.Parse("Q=a=b=c");
            Assert.Equal("a=b=c", result["Q"]);
        }

        [Fact]
        public void Parse_TrimsWhitespace()
        {
            var result = VariableOverrideParser.Parse("  Lib = ../Lib  ");
            Assert.Equal("../Lib", result["Lib"]);
        }

        [Fact]
        public void Parse_IgnoresEmptyEntries()
        {
            var result = VariableOverrideParser.Parse(";A=../a;;");
            Assert.Single(result);
            Assert.Equal("../a", result["A"]);
        }

        [Fact]
        public void Parse_DuplicateKey_LastWins()
        {
            var result = VariableOverrideParser.Parse("A=../a;A=../b");
            Assert.Equal("../b", result["A"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_NullOrEmpty_ReturnsEmpty(string spec)
        {
            Assert.Empty(VariableOverrideParser.Parse(spec));
        }

        [Fact]
        public void Parse_EntryWithoutEquals_Ignored()
        {
            Assert.Empty(VariableOverrideParser.Parse("justtext"));
        }

        [Fact]
        public void FromEnvironment_ReadsPrefixedVariables()
        {
            var prefix = "DNT_TEST_PREFIX_" + Guid.NewGuid().ToString("N") + "_";
            try
            {
                Environment.SetEnvironmentVariable(prefix + "FOO", "../foo");
                var result = VariableOverrideParser.FromEnvironment(prefix);
                Assert.Equal("../foo", result["FOO"]);
            }
            finally
            {
                Environment.SetEnvironmentVariable(prefix + "FOO", null);
            }
        }
    }
}
