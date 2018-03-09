using Dnt.Commands.Infrastructure;
using Newtonsoft.Json;

namespace Dnt.Commands.Packages.Switcher
{
    public class ProjectMapping
    {
        internal ReferenceSwitcherConfiguration Parent { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonIgnore]
        public string ActualPath
        {
            get { return PathUtilities.ToAbsolutePath(Path, System.IO.Path.GetDirectoryName(Parent.Path)); }
        }
    }
}