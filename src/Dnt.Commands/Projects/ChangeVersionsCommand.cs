using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using NConsole;

namespace Dnt.Commands.Projects
{
    [Command(Name = "change-versions", Description = "Replaces the entire version of the given projects. " +
                                                   "Only projects with GeneratePackageOnBuild set are being processed.")]
    public class ChangeVersionsCommand : ProjectCommandBase
    {
        private const string ACTION_REPLACE = "replace";
        private const string ACTION_FORCE = "force";

        [Argument(Position = 1, Description = "The full version number using the format 'major.minor.patch', must specify at least two parts. " +
            "Final version will be padded to three parts.  Can include additional parts", IsRequired = true)]
        public string Version { get; set; } = string.Empty;

        [Argument(Position = 2, Description = "Change Action (replace|force).  replace (default) = Only set for projects with an existing version" +
            ", force = Set for all projects even if version is missing or blank", IsRequired = false)]
        public string Action { get; set; } = ACTION_REPLACE;

        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {

            if (Version.IndexOf(".") < 0)
            {
                host.WriteError("Version number must include at least two parts \n");
                return Task.FromResult<object>(null);
            }

            bool isReplaceOnly = !ACTION_FORCE.Equals(Action, StringComparison.InvariantCultureIgnoreCase);

            var globalProperties = TryGetGlobalProperties();

            foreach (var projectPath in GetProjectPaths())
            {
                try
                {
                    using (var projectInformation = ProjectExtensions.LoadProject(projectPath, globalProperties))
                    {
                        bool projectHasVersion = projectInformation.Project.HasVersion();
                        bool projectGeneratesPackage = projectInformation.Project.GeneratesPackage();

                        if (projectGeneratesPackage && (projectHasVersion || !isReplaceOnly))
                        {
                            var versions = ChangeVersion(projectInformation.Project, "Version");

                            if (!versions.Item1.Equals(versions.Item2))
                            {
                                host.WriteMessage($"[x] {(projectHasVersion ? "Replaced" : "Set")} version of {System.IO.Path.GetFileName(projectPath)}" +
                                    " from " + versions.Item2 +
                                    " to " + versions.Item1 + "\n");

                                if (!Simulate)
                                {
                                    projectInformation.Project.Save();
                                }
                            }
                            else
                            {
                                host.WriteMessage($"[ ] Skipping {System.IO.Path.GetFileName(projectPath)} is already set to {versions.Item1}\n");
                            }

                        }
                        else
                        {
                            host.WriteMessage($"[ ] Ignoring {System.IO.Path.GetFileName(projectPath)}: {( !projectGeneratesPackage ? "Not GeneratePackageOnBuild" : "No version in Project")}\n");
                        }
                    }
                }
                catch (Exception e)
                {
                    host.WriteError(e + "\n");
                }
            }

            return Task.FromResult<object>(null);
        }

        private Tuple<string, string> ChangeVersion(Project project, string propertyName)
        {
            string previousVersion;

            var versionItem = project.Properties.FirstOrDefault(i => i.Name == propertyName);
            if (versionItem != null && !versionItem.IsImported)
            {
                previousVersion = versionItem.EvaluatedValue;
                versionItem.UnevaluatedValue = ValidateVersion(this.Version);
            }
            else
            {
                previousVersion = "<no version>";
                versionItem = project.SetProperty(propertyName, ValidateVersion(this.Version));
            }

            return new Tuple<string, string>(versionItem.EvaluatedValue, previousVersion);
        }

        private string ValidateVersion(string version)
        {
            var segments = version.Split('.', '-', '+');

            if(segments.Length < 3)
            {
                return $"{segments[0]}.{segments[1]}.0";
            }
            else
            {
                return version;
            }

        }
    }
}