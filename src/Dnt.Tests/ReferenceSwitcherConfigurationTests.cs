using System;
using System.Collections.Generic;
using System.IO;
using Dnt.Commands.Infrastructure;
using Dnt.Commands.Packages.Switcher;
using Xunit;

namespace Dnt.Tests
{
    public class ReferenceSwitcherConfigurationTests
    {
        private static ReferenceSwitcherConfiguration CreateConfig(string configDirectory)
        {
            return new ReferenceSwitcherConfiguration
            {
                Path = Path.Combine(configDirectory, "switcher.json")
            };
        }

        [Fact]
        public void GetActualPath_WithVariable_ExpandsBeforeResolving()
        {
            var dir = Path.Combine(Path.GetTempPath(), "cfgdir");
            var config = CreateConfig(dir);
            config.Variables = new Dictionary<string, string> { { "Lib", "../Lib" } };

            var result = config.GetActualPath("$(Lib)/src/X.csproj");

            Assert.Equal(PathUtilities.ToAbsolutePath("../Lib/src/X.csproj", dir), result);
        }

        [Fact]
        public void GetActualPath_WithoutVariables_LeavesPlainPathUnchanged()
        {
            var dir = Path.Combine(Path.GetTempPath(), "cfgdir");
            var config = CreateConfig(dir);

            var result = config.GetActualPath("../Lib/src/X.csproj");

            Assert.Equal(PathUtilities.ToAbsolutePath("../Lib/src/X.csproj", dir), result);
        }

        [Fact]
        public void ExpandVariables_UndefinedVariable_Throws()
        {
            var config = new ReferenceSwitcherConfiguration();
            var ex = Assert.Throws<ArgumentException>(() => config.ExpandVariables("$(Missing)/x.csproj"));
            Assert.Contains("Missing", ex.Message);
        }

        [Fact]
        public void ExpandVariables_NestedVariables_Expand()
        {
            var config = new ReferenceSwitcherConfiguration
            {
                Variables = new Dictionary<string, string>
                {
                    { "root", "../.." },
                    { "Lib", "$(root)/Lib" }
                }
            };

            Assert.Equal("../../Lib/X.csproj", config.ExpandVariables("$(Lib)/X.csproj"));
        }

        [Fact]
        public void ExpandVariables_CyclicVariables_Throw()
        {
            var config = new ReferenceSwitcherConfiguration
            {
                Variables = new Dictionary<string, string>
                {
                    { "a", "$(b)" },
                    { "b", "$(a)" }
                }
            };

            Assert.Throws<ArgumentException>(() => config.ExpandVariables("$(a)"));
        }

        [Fact]
        public void ExpandVariables_DeepAcyclicChain_Expands()
        {
            const int depth = 40; // well past the old depth cap; a valid chain must not be mistaken for a cycle
            var variables = new Dictionary<string, string>();
            for (var i = 0; i < depth; i++)
            {
                variables["v" + i] = "$(v" + (i + 1) + ")";
            }
            variables["v" + depth] = "../leaf";

            var config = new ReferenceSwitcherConfiguration { Variables = variables };

            Assert.Equal("../leaf/X.csproj", config.ExpandVariables("$(v0)/X.csproj"));
        }

        [Fact]
        public void ActualSolution_WithVariable_Expands()
        {
            var dir = Path.Combine(Path.GetTempPath(), "cfgdir");
            var config = CreateConfig(dir);
            config.Variables = new Dictionary<string, string> { { "root", ".." } };
            config.Solution = "$(root)/App.sln";

            Assert.Equal(PathUtilities.ToAbsolutePath("../App.sln", dir), config.ActualSolution);
        }

        [Fact]
        public void ApplyVariableOverrides_LayersInPrecedenceOrder()
        {
            var config = new ReferenceSwitcherConfiguration
            {
                Variables = new Dictionary<string, string> { { "Lib", "../a" }, { "Other", "../o" } }
            };

            config.ApplyVariableOverrides(
                new Dictionary<string, string> { { "Lib", "../b" } },                  // local file
                new Dictionary<string, string> { { "Lib", "../c" }, { "X", "../x" } }, // environment
                new Dictionary<string, string> { { "Lib", "../d" } });                 // command line

            Assert.Equal("../d", config.EffectiveVariables["Lib"]);   // command line wins
            Assert.Equal("../o", config.EffectiveVariables["Other"]); // committed key survives
            Assert.Equal("../x", config.EffectiveVariables["X"]);     // environment-only key
        }

        [Fact]
        public void Save_DoesNotPersistOverridesOrExpandedPaths()
        {
            var dir = Path.Combine(Path.GetTempPath(), "dnt-switcher-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "switcher.json");
            try
            {
                File.WriteAllText(file,
                    "{\n" +
                    "  \"solution\": \"App.sln\",\n" +
                    "  \"variables\": { \"Lib\": \"../Lib\" },\n" +
                    "  \"mappings\": { \"Some.Package\": \"$(Lib)/src/Some/Some.csproj\" }\n" +
                    "}");

                var host = new TestConsoleHost();
                var config = ReferenceSwitcherConfiguration.Load(file, host);
                config.ApplyVariableOverrides(new Dictionary<string, string> { { "Lib", "../OVERRIDE" } });

                // The override drives in-memory path resolution...
                Assert.Contains("OVERRIDE", config.GetActualPath("$(Lib)/src/Some/Some.csproj"));

                config.Save();

                // ...but is never written back to disk.
                var raw = File.ReadAllText(file);
                Assert.Contains("$(Lib)", raw);          // templated mapping preserved
                Assert.Contains("../Lib", raw);          // committed variable preserved
                Assert.DoesNotContain("OVERRIDE", raw);  // override never persisted
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
