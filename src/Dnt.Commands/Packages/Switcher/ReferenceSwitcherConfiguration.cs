using System.Collections.Generic;
using System.IO;
using Dnt.Commands.Infrastructure;
using NConsole;
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
        [JsonConverter(typeof(SingleOrArrayConverter))]
        public Dictionary<string, List<string>> Mappings { get; set; }

        [JsonProperty("restore", NullValueHandling = NullValueHandling.Ignore)]
        public List<RestoreProjectInformation> Restore { get; set; } = new List<RestoreProjectInformation>();

        [JsonIgnore]
        public string ActualSolution => PathUtilities.ToAbsolutePath(Solution, System.IO.Path.GetDirectoryName(Path));

        [JsonProperty("removeProjects", NullValueHandling = NullValueHandling.Ignore)]
        public bool RemoveProjects { get; set; } = true;

        public string GetActualPath(string path)
        {
            return PathUtilities.ToAbsolutePath(path, System.IO.Path.GetDirectoryName(Path));
        }

        public static ReferenceSwitcherConfiguration Load(string fileName, IConsoleHost host)
        {
            if (!File.Exists(fileName))
            {
                host.WriteError($"File '{fileName}' not found.");
                return null;
            }

            var c = JsonConvert.DeserializeObject<ReferenceSwitcherConfiguration>(File.ReadAllText(fileName));
            c.Path = PathUtilities.ToAbsolutePath(fileName, Directory.GetCurrentDirectory());
            return c;
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(Path, json);
        }
    }
}