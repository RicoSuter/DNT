using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Dnt.Commands.Packages.Switcher
{
    public class RestoreProjectInformation
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("packages")]
        public List<SwitchedPackage> Packages { get; set; } = new List<SwitchedPackage>();

        public SwitchedPackage GetSwitchedPackage(string packageName)
        {
            return (
                from p in Packages
                where string.Equals(p.PackageName, packageName, StringComparison.OrdinalIgnoreCase)
                select p).FirstOrDefault();
        }
    }
}