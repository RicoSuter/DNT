using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Dnt.Commands.Infrastructure;
using NConsole;
using Newtonsoft.Json;

namespace Dnt.Commands.Packages.Switcher
{
    public class ReferenceSwitcherConfiguration
    {
        private static readonly Regex VariableTokenRegex = new Regex(@"\$\(([A-Za-z0-9_.\-]+)\)", RegexOptions.Compiled);

        [JsonIgnore]
        internal string Path { get; set; }

        [JsonProperty("solution")]
        public string Solution { get; set; }

        [JsonProperty("solutionFolder")]
        public string SolutionFolder { get; set; }

        [JsonProperty("variables", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Variables { get; set; }

        [JsonProperty("mappings", Required = Required.Always)]
        [JsonConverter(typeof(SingleOrArrayConverter))]
        public Dictionary<string, List<string>> Mappings { get; set; }

        [JsonProperty("restore", NullValueHandling = NullValueHandling.Ignore)]
        public List<RestoreProjectInformation> Restore { get; set; } = new List<RestoreProjectInformation>();

        [JsonProperty("restoreVariables", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> RestoreVariables { get; set; }

        [JsonIgnore]
        public IReadOnlyDictionary<string, string> EffectiveVariables { get; private set; }

        [JsonIgnore]
        public string ActualSolution => PathUtilities.ToAbsolutePath(ExpandVariables(Solution), System.IO.Path.GetDirectoryName(Path));

        [JsonProperty("removeProjects", NullValueHandling = NullValueHandling.Ignore)]
        public bool RemoveProjects { get; set; } = true;

        public string GetActualPath(string path)
        {
            return PathUtilities.ToAbsolutePath(ExpandVariables(path), System.IO.Path.GetDirectoryName(Path));
        }

        /// <summary>Gets the variable set used for expansion (layered overrides if applied, otherwise the committed variables).</summary>
        private IReadOnlyDictionary<string, string> Vars => EffectiveVariables ?? Variables;

        /// <summary>Layers variable overrides on top of the committed <see cref="Variables"/> (lowest to highest precedence).
        /// Overrides only affect in-memory path expansion and are never persisted by <see cref="Save"/>.</summary>
        public void ApplyVariableOverrides(params IReadOnlyDictionary<string, string>[] layersLowToHigh)
        {
            // Case-insensitive so an environment override such as DNT_SWITCHER_VAR_NJSONSCHEMA (env var names are
            // conventionally upper-cased) still overrides a committed variable named "NJsonSchema", and so $(NJsonSchema)
            // resolves regardless of the casing the override layer used.
            var effective = new Dictionary<string, string>(
                Variables ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            if (layersLowToHigh != null)
            {
                foreach (var layer in layersLowToHigh)
                {
                    if (layer == null)
                        continue;

                    foreach (var entry in layer)
                        effective[entry.Key] = entry.Value;
                }
            }

            EffectiveVariables = effective;
        }

        /// <summary>Expands MSBuild-style <c>$(name)</c> tokens using the effective variables. Tokens must appear only in
        /// directory portions of a path. Undefined variables and cyclic references throw <see cref="ArgumentException"/>.</summary>
        internal string ExpandVariables(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return VariableTokenRegex.Replace(value, match => ResolveVariable(match.Groups[1].Value, null));
        }

        private string ResolveVariable(string name, HashSet<string> resolving)
        {
            if (Vars == null || !Vars.TryGetValue(name, out var raw))
                throw new ArgumentException($"Undefined switcher variable '$({name})'.");

            // Track the chain of variables currently being resolved so a genuine cycle is caught by identity,
            // rather than rejecting legitimate deep (but acyclic) aliasing with a fixed depth cap.
            resolving = resolving ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!resolving.Add(name))
                throw new ArgumentException($"Cyclic switcher variable reference at '$({name})'.");

            var result = VariableTokenRegex.Replace(raw, match => ResolveVariable(match.Groups[1].Value, resolving));

            resolving.Remove(name);
            return result;
        }

        public static ReferenceSwitcherConfiguration Load(string fileName, IConsoleHost host)
        {
            if (!File.Exists(fileName))
            {
                host.WriteError($"File '{fileName}' not found.");
                return null;
            }

            try
            {
                var c = JsonConvert.DeserializeObject<ReferenceSwitcherConfiguration>(File.ReadAllText(fileName));
                c.Path = PathUtilities.ToAbsolutePath(fileName, Directory.GetCurrentDirectory());
                return c;
            }
            catch (JsonSerializationException e)
            {
                throw new ArgumentException(e.Message);
            }
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Path, json);
        }
    }
}
