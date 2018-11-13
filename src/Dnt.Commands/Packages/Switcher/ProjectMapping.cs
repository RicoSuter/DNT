using System;
using System.Collections.Generic;
using System.Linq;
using Dnt.Commands.Infrastructure;
using Newtonsoft.Json;

namespace Dnt.Commands.Packages.Switcher
{
    public class LegacyProject
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("references")]
        public List<LegacyReference> References { get; set; }

        public LegacyReference GetReference(string packageName)
        {
            return (
                from r in References
                where string.Equals(r.PackageName, packageName, StringComparison.OrdinalIgnoreCase)
                select r).FirstOrDefault();
        }

    }

    public class LegacyReference
    {
        [JsonProperty("packageName")]
        public string PackageName { get; set; }

        [JsonProperty("include")]
        public string Include { get; set; }

        [JsonProperty("metadata")]
        public List<KeyValuePair<string, string>> Metadata { get; set; } = new List<KeyValuePair<string, string>>();
    }

    public class SdkProject
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        public List<MappedPackage> MappedPackage { get; set; } = new List<MappedPackage>();
    }

    public class MappedPackage
    {
        [JsonProperty("packageName")]
        public string PackageName { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class ProjectMapping
    {
        internal ReferenceSwitcherConfiguration Parent { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonIgnore]
        public string ActualPath => PathUtilities.ToAbsolutePath(Path, System.IO.Path.GetDirectoryName(Parent.Path));
    }
}