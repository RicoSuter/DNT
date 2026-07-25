using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Dnt.Commands.Packages;
using Dnt.Commands.Packages.Switcher;
using NConsole;
using Xunit;

namespace Dnt.Tests
{
    public class SwitchCommandOverrideTests
    {
        /// <summary>Clears any ambient DNT_SWITCHER_VAR_* environment variables for the duration of a test (and
        /// restores them afterwards) so the environment override layer read by LoadConfiguration can't leak in from
        /// the developer's shell or CI and make the exact-value assertions non-deterministic.</summary>
        private sealed class SwitcherEnvironmentScope : IDisposable
        {
            private readonly Dictionary<string, string> _saved = new Dictionary<string, string>();

            public SwitcherEnvironmentScope()
            {
                foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                {
                    var key = entry.Key as string;
                    if (key != null && key.StartsWith(VariableOverrideParser.EnvironmentVariablePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        _saved[key] = entry.Value as string;
                        Environment.SetEnvironmentVariable(key, null);
                    }
                }
            }

            public void Dispose()
            {
                // Clear any DNT_SWITCHER_VAR_* set during the test, then restore the originals captured at construction.
                foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                {
                    var key = entry.Key as string;
                    if (key != null && key.StartsWith(VariableOverrideParser.EnvironmentVariablePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        Environment.SetEnvironmentVariable(key, null);
                    }
                }

                foreach (var entry in _saved)
                {
                    Environment.SetEnvironmentVariable(entry.Key, entry.Value);
                }
            }
        }

        private class TestableToProjects : SwitchPackagesToProjectsCommand
        {
            public ReferenceSwitcherConfiguration Invoke(IConsoleHost host) => LoadConfiguration(host);
            public static Dictionary<string, string> Snapshot(ReferenceSwitcherConfiguration c) => SnapshotEffectiveVariables(c);
        }

        private class TestableToPackages : SwitchProjectsToPackagesCommand
        {
            public ReferenceSwitcherConfiguration Invoke(IConsoleHost host) => LoadConfiguration(host);
        }

        private static string WriteConfig(string dir)
        {
            var file = Path.Combine(dir, "switcher.json");
            File.WriteAllText(file,
                "{ \"solution\": \"App.sln\", \"variables\": { \"Lib\": \"../Lib\" }, \"mappings\": { \"P\": \"$(Lib)/P.csproj\" } }");
            return file;
        }

        [Fact]
        public void BothCommands_ProduceIdenticalEffectiveVariables()
        {
            var dir = Path.Combine(Path.GetTempPath(), "dnt-switcher-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                using (new SwitcherEnvironmentScope())
                {
                    var file = WriteConfig(dir);
                    var host = new TestConsoleHost();

                    var toProjects = new TestableToProjects { Configuration = file, VariableOverrides = "Lib=../Override" };
                    var toPackages = new TestableToPackages { Configuration = file, VariableOverrides = "Lib=../Override" };

                    var a = toProjects.Invoke(host);
                    var b = toPackages.Invoke(host);

                    Assert.Equal("../Override", a.EffectiveVariables["Lib"]);
                    Assert.Equal(a.EffectiveVariables["Lib"], b.EffectiveVariables["Lib"]);
                }
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void LocalOverrideFile_IsDiscoveredNextToConfig_AndCliWins()
        {
            var dir = Path.Combine(Path.GetTempPath(), "dnt-switcher-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                using (new SwitcherEnvironmentScope())
                {
                    var file = WriteConfig(dir);
                    File.WriteAllText(Path.Combine(dir, "switcher.local.json"),
                        "{ \"variables\": { \"Lib\": \"../LocalLib\" } }");

                    var host = new TestConsoleHost();

                    // No CLI override: local file value takes effect.
                    var localOnly = new TestableToProjects { Configuration = file }.Invoke(host);
                    Assert.Equal("../LocalLib", localOnly.EffectiveVariables["Lib"]);

                    // CLI override outranks the local file.
                    var withCli = new TestableToProjects { Configuration = file, VariableOverrides = "Lib=../CliLib" }.Invoke(host);
                    Assert.Equal("../CliLib", withCli.EffectiveVariables["Lib"]);
                }
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void EnvironmentOverride_UpperCasedName_OverridesMixedCaseVariable()
        {
            var dir = Path.Combine(Path.GetTempPath(), "dnt-switcher-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                using (new SwitcherEnvironmentScope())
                {
                    // Committed variable is mixed-case "Lib"; the environment override uses the conventional all-caps name.
                    var file = WriteConfig(dir);
                    Environment.SetEnvironmentVariable(VariableOverrideParser.EnvironmentVariablePrefix + "LIB", "../EnvLib");

                    var host = new TestConsoleHost();
                    var config = new TestableToProjects { Configuration = file }.Invoke(host);

                    Assert.Equal("../EnvLib", config.EffectiveVariables["Lib"]);
                    Assert.Contains("EnvLib", config.GetActualPath("$(Lib)/P.csproj"));
                }
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void RestoreSnapshot_OnlyRecordedWhenAnOverrideChangesAValue()
        {
            var dir = Path.Combine(Path.GetTempPath(), "dnt-switcher-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                using (new SwitcherEnvironmentScope())
                {
                    var file = WriteConfig(dir);
                    var host = new TestConsoleHost();

                    // No override: effective == committed, so nothing is recorded (no restoreVariables written).
                    var plain = new TestableToProjects { Configuration = file }.Invoke(host);
                    Assert.Null(TestableToProjects.Snapshot(plain));

                    // With an override: the effective values are recorded so switch-to-packages can restore them.
                    var overridden = new TestableToProjects { Configuration = file, VariableOverrides = "Lib=../Override" }.Invoke(host);
                    var snapshot = TestableToProjects.Snapshot(overridden);
                    Assert.NotNull(snapshot);
                    Assert.Equal("../Override", snapshot["Lib"]);
                }
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void Profile_RelativePath_ResolvedNextToConfig()
        {
            var dir = Path.Combine(Path.GetTempPath(), "dnt-switcher-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                using (new SwitcherEnvironmentScope())
                {
                    var file = WriteConfig(dir);
                    File.WriteAllText(Path.Combine(dir, "switcher.wt.json"),
                        "{ \"variables\": { \"Lib\": \"../ProfileLib\" } }");

                    var host = new TestConsoleHost();

                    // A bare relative profile name resolves next to the config, not against the process CWD.
                    var config = new TestableToProjects { Configuration = file, Profile = "switcher.wt.json" }.Invoke(host);
                    Assert.Equal("../ProfileLib", config.EffectiveVariables["Lib"]);
                }
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
