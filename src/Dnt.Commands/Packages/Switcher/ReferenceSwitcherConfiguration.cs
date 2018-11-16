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
        public Dictionary<string, string> Mappings { get; } = new Dictionary<string, string>();

        [JsonProperty("restore", NullValueHandling = NullValueHandling.Ignore)]
        public List<RestoreProjectInformation> Restore { get; set; } = new List<RestoreProjectInformation>();

        [JsonIgnore]
        public string ActualSolution => PathUtilities.ToAbsolutePath(Solution, System.IO.Path.GetDirectoryName(Path));

        public string GetActualPath(string path)
        {
            return PathUtilities.ToAbsolutePath(path, System.IO.Path.GetDirectoryName(Path));
        }

        public static ReferenceSwitcherConfiguration Load(string filePath)
        {
            var c = JsonConvert.DeserializeObject<ReferenceSwitcherConfiguration>(File.ReadAllText(filePath));
            c.Path = PathUtilities.ToAbsolutePath(filePath, Directory.GetCurrentDirectory()); ; ;
            return c;
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Path, json);
        }
    }
}