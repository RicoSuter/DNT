using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using NConsole;

namespace Dnt.Commands.Packages
{
    [Command(Name = "update-packages", Description = "Updates the versions of the selected package references in the given projects.")]
    public class UpdatePackagesCommand : ProjectCommandBase
    {
        [Argument(Position = 1, IsRequired = true, Description = "The package(s) to update (allows * wildcards for selecting multiple packages).")]
        public string Package { get; set; }

        [Argument(Position = 2, IsRequired = false, Description = "The version to update to (wildcards * will be converted to a range ('1.*' => '[1 - 2)').")]
        public string Version { get; set; }

        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var collection = new ProjectCollection();

            string version = null;
            if (!string.IsNullOrEmpty(Version))
            {
                var segments = Version.Split('.');
                version =
                    segments.Length == 2 && segments[1] == "*" ? "[" + segments[0] + "," + (int.Parse(segments[0]) + 1) + ")" :
                    segments.Length == 3 && segments[2] == "*" ? "[" + segments[0] + "." + segments[1] + "," + segments[0] + "," + (int.Parse(segments[1]) + 1) + ")" :
                    Version;
            }

            var packageRegex = new Regex("^" + Package.Replace(".", "\\.").Replace("*", ".*") + "$");
            foreach (var projectPath in GetProjectPaths())
            {
                try
                {
                    var project = collection.LoadProject(projectPath);

                    var packages = project.Items
                        .Where(i => i.ItemType == "PackageReference" && 
                                    packageRegex.IsMatch(i.EvaluatedInclude) && 
                                    i.EvaluatedInclude != "Microsoft.NETCore.App")
                        .Select(i => i.EvaluatedInclude)
                        .ToList();

                    collection.UnloadProject(project);

                    foreach (var package in packages)
                    {
                        ExecuteCommand("dotnet add \"" + projectPath + "\" package \"" + package + "\"" + (version != null ? " -v " + version : ""), host);
                    }
                }
                catch (Exception e)
                {
                    host.WriteError(e + "\n");
                }
            }

            return Task.FromResult<object>(null);
        }
    }
}