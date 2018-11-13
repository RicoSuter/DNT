using System;
using System.Collections.Generic;
using System.Linq;
using Dnt.Commands.Infrastructure;
using Newtonsoft.Json;

namespace Dnt.Commands.Packages.Switcher
{
    public class SwitchedProject
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("packages")]
        public List<SwitchedPackage> Packages { get; set; } = new List<SwitchedPackage>();

        public SwitchedPackage GetSwitchedPackage(string packageName)
        {
            return (
                from r in Packages
                where string.Equals(r.PackageName, packageName, StringComparison.OrdinalIgnoreCase)
                select r).FirstOrDefault();
        }

    }

    public class SwitchedPackage
    {
        [JsonProperty("packageName")]
        public string PackageName { get; set; }

        [JsonProperty("version")]
        public string PackageVersion { get; set; }

        [JsonProperty("include")]
        public string Include { get; set; }

        [JsonProperty("metadata")]
        public List<KeyValuePair<string, string>> Metadata { get; set; } = new List<KeyValuePair<string, string>>();
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