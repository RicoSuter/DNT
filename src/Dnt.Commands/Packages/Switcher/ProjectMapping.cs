using System.Collections.Generic;
using Dnt.Commands.Infrastructure;
using Newtonsoft.Json;

namespace Dnt.Commands.Packages.Switcher
{
    public class LegacyProject
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("reference")]
        public LegacyReference Reference { get; set; }

    }

    public class LegacyReference
    {
        [JsonProperty("include")]
        public string Include { get; set; }

        [JsonProperty("metadata")]
        public List<KeyValuePair<string, string>> Metadata { get; set; } = new List<KeyValuePair<string, string>>();

    }

    public class ProjectMapping
    {
        internal ReferenceSwitcherConfiguration Parent { get; set; }

        [JsonProperty("legacyProjects")]
        public List<LegacyProject> LegacyProjects { get; set; } = new List<LegacyProject>();

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonIgnore]
        public string ActualPath => PathUtilities.ToAbsolutePath(Path, System.IO.Path.GetDirectoryName(Parent.Path));
    }
}