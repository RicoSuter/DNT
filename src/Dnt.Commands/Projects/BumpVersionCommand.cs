using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using NConsole;

namespace Dnt.Commands.Projects
{
    [Command(Name = "bump-version", Description = "Bumps the major, minor or patch version of the given projects. " +
                                                  "Only projects with GeneratePackageOnBuild set are being processed.")]
    public class BumpVersionCommand : ProjectCommandBase
    {
        [Argument(Position = 1, Description = "The version part to update (major|minor|patch).", IsRequired = true)]
        public string Action { get; set; }

        [Argument(Name = nameof(Major), Description = "The forced major version.", IsRequired = false)]
        public int Major { get; set; } = -1;

        [Argument(Name = nameof(Minor), Description = "The forced minor version.", IsRequired = false)]
        public int Minor { get; set; } = -1;

        [Argument(Name = nameof(Patch), Description = "The forced patch version.", IsRequired = false)]
        public int Patch { get; set; } = -1;

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

                        host.WriteMessage("Bumped version of " + System.IO.Path.GetFileName(projectPath) +
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
                        host.WriteMessage("Ignoring " + System.IO.Path.GetFileName(projectPath) + ": Not GeneratePackageOnBuild\n");
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
                return (Major == -1 ? int.Parse(segments[0]) + 1 : Major) + "." +
                       (Minor == -1 ? "0" : Minor.ToString()) + "." +
                       (Patch == -1 ? "0" : Patch.ToString());
            }
            else if (Action == "minor")
            {
                return (Major == -1 ? segments[0] : Major.ToString()) + "." +
                       (Minor == -1 ? int.Parse(segments[1]) + 1 : Minor) + "." +
                       (Patch == -1 ? "0" : Patch.ToString());
            }
            else
            {
                return (Major == -1 ? segments[0] : Major.ToString()) + "." +
                       (Minor == -1 ? segments[1] : Minor.ToString()) + "." +
                       (Patch == -1 ? int.Parse(segments[2]) + 1 : Patch);
            }
        }
    }
}