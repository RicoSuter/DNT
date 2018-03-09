using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using NConsole;

namespace Dnt.Commands.Packages
{
    [Command(Name = "update-package", Description = "Updates the versions of the selected package references in the given projects.")]
    public class UpdatePackageCommand : ProjectCommandBase
    {
        [Argument(Name = nameof(Package), IsRequired = true, Description = "The package(s) to update (allows * wildcards for selecting multiple packages).")]
        public string Package { get; set; }

        [Argument(Name = nameof(Version), IsRequired = true, Description = "The version to update to (wildcards * will be converted to a range ('1.*' => '[1 - 2)').")]
        public string Version { get; set; }

        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var collection = new ProjectCollection();

            var segments = Version.Split('.');
            var version =
                segments.Length == 2 && segments[1] == "*" ? "[" + segments[0] + "," + (int.Parse(segments[0]) + 1) + ")" :
                segments.Length == 3 && segments[2] == "*" ? "[" + segments[0] + "." + segments[1] + "," + segments[0] + "," + (int.Parse(segments[1]) + 1) + ")" :
                Version;

            var packageRegex = new Regex("^" + Package.Replace(".", "\\.").Replace("*", ".*") + "$");

            foreach (var projectPath in GetProjectPaths())
            {
                try
                {
                    var project = collection.LoadProject(projectPath);

                    var packages = project.Items
                        .Where(i => i.ItemType == "PackageReference" && packageRegex.IsMatch(i.EvaluatedInclude))
                        .Select(i => i.EvaluatedInclude);

                    collection.UnloadProject(project);

                    foreach (var package in packages)
                    {
                        ExecuteCommand("dotnet add \"" + projectPath + "\" package " + package + " -v " + version, host);
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