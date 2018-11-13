using System.Collections.Generic;
using System.IO;
using Dnt.Commands.Infrastructure;
using Newtonsoft.Json;

namespace Dnt.Commands.Packages.Switcher
{
    public class ReferenceSwitcherConfiguration
    {
        [JsonIgnore]
        internal string Path { get; set; }

        [JsonProperty("solution")]
        public string Solution { get; set; }

        [JsonProperty("mappings")]
        public Dictionary<string, ProjectMapping> Mappings { get; } = new Dictionary<string, ProjectMapping>();

        [JsonProperty("legacyProjects")]
        public List<LegacyProject> LegacyProjects { get; set; } = new List<LegacyProject>();

        [JsonProperty("sdkProjects")]
        public List<SdkProject> SdkProjects { get; set; } = new List<SdkProject>();

        [JsonIgnore]
        public string ActualSolution => PathUtilities.ToAbsolutePath(Solution, System.IO.Path.GetDirectoryName(Path));

        public static ReferenceSwitcherConfiguration Load(string filePath)
        {
            var c = JsonConvert.DeserializeObject<ReferenceSwitcherConfiguration>(File.ReadAllText(filePath));
            c.Path = PathUtilities.ToAbsolutePath(filePath, Directory.GetCurrentDirectory()); ; ;

            foreach (var p in c.Mappings)
            {
                p.Value.Parent = c;
            }

            return c;
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Path, json);
        }
    }
}