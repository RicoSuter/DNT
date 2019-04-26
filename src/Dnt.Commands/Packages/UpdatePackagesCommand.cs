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

        [Argument(Name = nameof(EnforceRanges), IsRequired = false)]
        public bool EnforceRanges { get; set; } = true;

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            string version = null;
            if (EnforceRanges && !string.IsNullOrEmpty(Version))
            {
                var segments = Version.Split('.');
                version =
                    segments.Length == 2 && segments[1] == "*" ? "[" + segments[0] + "," + (int.Parse(segments[0]) + 1) + ")" :
                    segments.Length == 3 && segments[2] == "*" ? "[" + segments[0] + "." + segments[1] + "," + segments[0] + "," + (int.Parse(segments[1]) + 1) + ")" :
                    Version;
            }

            var packageRegex = new Regex("^" + Package.Replace(".", "\\.").Replace("*", ".*") + "$");
            if (NoParallel)
            {
                using (var projectCollection = new ProjectCollection())
                {
                    foreach (var projectPath in GetProjectPaths())
                    {
                        await UpgradeProjectPackagesAsync(host, projectPath, packageRegex, version);
                    }
                }
            }
            else
            {
                await Task.WhenAll(GetProjectPaths().Select(projectPath => Task.Run(async () =>
                {
                    using (var projectCollection = new ProjectCollection())
                        await UpgradeProjectPackagesAsync(host, projectPath, packageRegex, version);
                }
                )));
            }

            return null;
        }

        private async Task UpgradeProjectPackagesAsync(IConsoleHost host, string projectPath, Regex packageRegex, string version)
        {
            try
            {
                using (var projectInformation = ProjectExtensions.LoadProject(projectPath))
                {
                    var packages = projectInformation.Project.Items
                        .Where(i => i.ItemType == "PackageReference" &&
                                    packageRegex.IsMatch(i.EvaluatedInclude) &&
                                    i.EvaluatedInclude != "Microsoft.NETCore.App")
                        .Select(i => i.EvaluatedInclude)
                        .ToList();

                    foreach (var package in packages)
                    {
                        await ExecuteCommandAsync("dotnet add \"" + projectPath + "\" package \"" + package + "\"" + (version != null ? " -v " + version : ""), host);
                    }
                }               
            }
            catch (Exception e)
            {
                host.WriteError(e + "\n");
            }
        }
    }
}