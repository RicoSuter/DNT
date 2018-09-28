using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using NConsole;

namespace Dnt.Commands.Projects
{
    [Command(Name = "bump-versions", Description = "Bumps the major, minor or patch version of the given projects. " +
                                                   "Only projects with GeneratePackageOnBuild set are being processed.")]
    public class BumpVersionsCommand : ProjectCommandBase
    {
        [Argument(Position = 1, Description = "The version part to update (major|minor|patch|revision).", IsRequired = true)]
        public string Action { get; set; }

        [Argument(Position = 2, Description = "The specified version number of the given action.", IsRequired = false)]
        public int Number { get; set; } = -1;

        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var collection = new ProjectCollection();
            foreach (var projectPath in GetProjectPaths())
            {
                try
                {
                    var project = collection.LoadProject(projectPath);
                    if (project.GeneratesPackage() || project.HasVersion())
                    {
                        var versions = BumpVersion(project, "Version", "1.0.0");

                        host.WriteMessage("[x] Bumped version of " + System.IO.Path.GetFileName(projectPath) +
                                          " from " + versions.Item2 +
                                          " to " + versions.Item1 + "\n");

                        if (!Simulate)
                        {
                            project.Save();
                        }

                        collection.UnloadProject(project);
                    }
                    else
                    {
                        host.WriteMessage("[ ] Ignoring " + System.IO.Path.GetFileName(projectPath) + ": Not GeneratePackageOnBuild\n");
                    }
                }
                catch (Exception e)
                {
                    host.WriteError(e + "\n");
                }
            }

            return Task.FromResult<object>(null);
        }

        private Tuple<string, string> BumpVersion(Project project, string propertyName, string previousVersion)
        {
            var versionItem = project.Properties.FirstOrDefault(i => i.Name == propertyName);
            if (versionItem != null && !versionItem.IsImported)
            {
                previousVersion = versionItem.EvaluatedValue;
                versionItem.UnevaluatedValue = GetBumpedVersion(versionItem.EvaluatedValue);
            }
            else
            {
                versionItem = project.SetProperty(propertyName, GetBumpedVersion(previousVersion));
            }

            return new Tuple<string, string>(versionItem.EvaluatedValue, previousVersion);
        }

        private string GetBumpedVersion(string version)
        {
            var segments = version.Split('.');

            if (Action == "major")
            {
                return $"{(Number != -1 ? Number : int.Parse(segments[0]) + 1)}.0.0{(segments.Length >= 4 ? ".0" : "")}";
            }
            else if (Action == "minor")
            {
                return $"{segments[0]}.{(Number != -1 ? Number : int.Parse(segments[1]) + 1)}.0{(segments.Length >= 4 ? ".0" : "")}";
            }
            else if (Action == "patch")
            {
                return $"{segments[0]}.{segments[1]}.{(Number != -1 ? Number : int.Parse(segments[2]) + 1)}{(segments.Length >= 4 ? ".0" : "")}";
            }
            else
            {
                return $"{segments[0]}.{segments[1]}.{segments[2]}.{(Number != -1 ? Number : (segments.Length >= 4 ? int.Parse(segments[3]) + 1 : 1))}";
            }
        }
    }
}