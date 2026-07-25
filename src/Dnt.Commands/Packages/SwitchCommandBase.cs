using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dnt.Commands.Packages.Switcher;
using NConsole;
using Newtonsoft.Json;

namespace Dnt.Commands.Packages
{
    /// <summary>Shared base for the reference-switcher commands. Hosts the common arguments and the layered
    /// variable-override loading so both directions resolve paths identically.</summary>
    public abstract class SwitchCommandBase : CommandBase
    {
        [Argument(Position = 1, IsRequired = false, Description = "Configuration .json file")]
        public string Configuration { get; set; } = "switcher.json";

        [Argument(Name = "variables", IsRequired = false,
            Description = "Override switcher variables (semicolon-delimited), e.g. /variables:\"NConsole=../../NConsole;Foo=../Foo\". Specify the config file explicitly when using this. Highest precedence.")]
        public string VariableOverrides { get; set; }

        [Argument(Name = "profile", IsRequired = false,
            Description = "Path to a local override file (defaults to <config>.local.json next to the config), e.g. /profile:switcher.wt.json.")]
        public string Profile { get; set; }

        /// <summary>Loads the configuration and applies the layered variable overrides
        /// (committed &lt; local file &lt; environment &lt; command line).</summary>
        protected ReferenceSwitcherConfiguration LoadConfiguration(IConsoleHost host)
        {
            var configuration = ReferenceSwitcherConfiguration.Load(Configuration, host);
            if (configuration == null)
            {
                return null;
            }

            var localFilePath = GetLocalOverridePath(configuration);
            var localVariables = LoadLocalFileVariables(localFilePath, host);
            var environmentVariables = VariableOverrideParser.FromEnvironment();
            var commandLineVariables = VariableOverrideParser.Parse(VariableOverrides);

            configuration.ApplyVariableOverrides(localVariables, environmentVariables, commandLineVariables);

            ReportEffectiveVariables(host, configuration, localFilePath, localVariables, environmentVariables, commandLineVariables);

            return configuration;
        }

        /// <summary>Returns a plain-dictionary copy of the effective variables to record for restore, or null when no
        /// override is active (the effective set equals the committed <c>variables</c>). Returning null keeps the shared
        /// <c>switcher.json</c> free of a redundant <c>restoreVariables</c> section in the common, no-override case, so
        /// override-only paths are written back only when an override is actually in play.</summary>
        protected static Dictionary<string, string> SnapshotEffectiveVariables(ReferenceSwitcherConfiguration configuration)
        {
            var effective = configuration.EffectiveVariables;
            if (effective == null || effective.Count == 0)
            {
                return null;
            }

            if (!DiffersFromCommitted(effective, configuration.Variables))
            {
                return null;
            }

            return effective.ToDictionary(entry => entry.Key, entry => entry.Value);
        }

        private static bool DiffersFromCommitted(
            IReadOnlyDictionary<string, string> effective, IReadOnlyDictionary<string, string> committed)
        {
            committed = committed ?? new Dictionary<string, string>();
            if (effective.Count != committed.Count)
            {
                return true;
            }

            foreach (var entry in effective)
            {
                if (!committed.TryGetValue(entry.Key, out var committedValue) ||
                    !string.Equals(committedValue, entry.Value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private string GetLocalOverridePath(ReferenceSwitcherConfiguration configuration)
        {
            var directory = Path.GetDirectoryName(configuration.Path);

            if (!string.IsNullOrWhiteSpace(Profile))
            {
                // Resolve a relative profile against the config directory (like the default below), not the process CWD,
                // so `/profile:switcher.wt.json` finds the file next to the config regardless of where dnt is run from.
                return Path.IsPathRooted(Profile) ? Profile : Path.Combine(directory ?? string.Empty, Profile);
            }

            var name = Path.GetFileNameWithoutExtension(configuration.Path);
            var extension = Path.GetExtension(configuration.Path);
            return Path.Combine(directory ?? string.Empty, name + ".local" + extension);
        }

        private static Dictionary<string, string> LoadLocalFileVariables(string path, IConsoleHost host)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                var file = JsonConvert.DeserializeObject<LocalOverrideFile>(File.ReadAllText(path));
                if (file?.Variables == null)
                {
                    return null;
                }

                // Match casing insensitively so a differently-cased local-file key is attributed and applied correctly.
                return new Dictionary<string, string>(file.Variables, StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException e)
            {
                host.WriteError($"Local override file '{path}' could not be loaded: {e.Message}");
                return null;
            }
        }

        private void ReportEffectiveVariables(IConsoleHost host, ReferenceSwitcherConfiguration configuration,
            string localFilePath, Dictionary<string, string> localVariables,
            Dictionary<string, string> environmentVariables, Dictionary<string, string> commandLineVariables)
        {
            var effective = configuration.EffectiveVariables;
            if (effective == null || effective.Count == 0)
            {
                return;
            }

            var overrideActive =
                (localVariables != null && localVariables.Count > 0) ||
                (environmentVariables != null && environmentVariables.Count > 0) ||
                (commandLineVariables != null && commandLineVariables.Count > 0);

            // Only chatter when an override layer is in play or the user asked for verbose output.
            if (!overrideActive && !Verbose)
            {
                return;
            }

            var localFileName = string.IsNullOrEmpty(localFilePath) ? "local file" : Path.GetFileName(localFilePath);

            host.WriteMessage("Effective switcher variables:\n");
            foreach (var entry in effective.OrderBy(e => e.Key))
            {
                string source;
                if (commandLineVariables != null && commandLineVariables.ContainsKey(entry.Key))
                    source = "/variables:";
                else if (environmentVariables != null && environmentVariables.ContainsKey(entry.Key))
                    source = VariableOverrideParser.EnvironmentVariablePrefix + entry.Key;
                else if (localVariables != null && localVariables.ContainsKey(entry.Key))
                    source = localFileName;
                else
                    source = Path.GetFileName(configuration.Path);

                host.WriteMessage($"    {entry.Key} = {entry.Value}   (source: {source})\n");
            }
        }

        private class LocalOverrideFile
        {
            [JsonProperty("variables")]
            public Dictionary<string, string> Variables { get; set; }
        }
    }
}
