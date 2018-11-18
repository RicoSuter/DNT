using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dnt.Commands.Packages.Switcher
{
    public class SwitchedPackage
    {
        [JsonProperty("packageName")]
        public string PackageName { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string PackageVersion { get; set; }

        [JsonProperty("include", NullValueHandling = NullValueHandling.Ignore)]
        public string Include { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public List<KeyValuePair<string, string>> Metadata { get; set; }
    }
}