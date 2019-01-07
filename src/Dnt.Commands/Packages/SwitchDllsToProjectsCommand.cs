using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dnt.Commands.Infrastructure;
using Dnt.Commands.Packages.Switcher;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using NConsole;

namespace Dnt.Commands.Packages
{
    [Command(Name = "switch-dll-references-to-projects")]
    public class SwitchDllReferencesToProjectsCommand : CommandBase
    {
        [Argument(Position = 1)]
        public string Solution { get; set; }

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var projects = GetProjects(Solution, host);

            ReplaceDllReferencesWithProjects(projects, host);

            return null;
        }

        private IEnumerable<ProjectInSolution> GetProjects(string solutionPath, IConsoleHost host)
        {
            var solution = SolutionFile.Parse(solutionPath);
            return solution.ProjectsInOrder;
        }

        private static void ReplaceDllReferencesWithProjects(IEnumerable<ProjectInSolution> projects, IConsoleHost host)
        {
            var projectNames = projects.Where(p => p.ProjectType != SolutionProjectType.SolutionFolder).Select(p => p.ProjectName);

            foreach (var solutionProject in projects.Where(p => p.ProjectType != SolutionProjectType.SolutionFolder))
            {
                try
                {
                    using (var projectInformation = ProjectExtensions.LoadProject(solutionProject.AbsolutePath))
                    {
                        var newProjectsToReference = new List<ProjectInSolution>();
                        var dllReferencesToRemove = new List<ProjectItem>();
                        foreach (var reference in projectInformation.Project.Items.Where(r => r.ItemType == "Reference"
                                    && projectNames.Contains(r.UnevaluatedInclude.Split(',').First())))
                        {
                            dllReferencesToRemove.Add(reference);
                            var projectToReference = projects.First(p => p.ProjectType != SolutionProjectType.SolutionFolder && p.ProjectName == reference.EvaluatedInclude.Split(',').First());
                            newProjectsToReference.Add(projectToReference);
                        }

                        foreach (var item in dllReferencesToRemove)
                        {
                            projectInformation.Project.RemoveItem(item);
                        }

                        foreach (var projectToReference in newProjectsToReference)
                        {
                            var refProjectDirectory = Path.GetFullPath(Path.GetDirectoryName(projectToReference.AbsolutePath));
                            var relativePath = PathUtilities.ToRelativePath(refProjectDirectory, Path.GetFullPath(Path.GetDirectoryName(solutionProject.AbsolutePath)));

                            var path = Path.Combine(relativePath, Path.GetFileName(projectToReference.AbsolutePath));
                            var metaData = new List<KeyValuePair<string, string>>
                                {
                                    new KeyValuePair<string, string>("Project", projectToReference.ProjectGuid),
                                    new KeyValuePair<string, string>("Name", projectToReference.ProjectName)
                                };
                            projectInformation.Project.AddItem("ProjectReference", path, metaData);
                        }
                        projectInformation.Project.Save();
                    }
                }
                catch (Exception e)
                {
                    host.WriteError("The project '" + solutionProject.AbsolutePath + "' could not be loaded: " +
                                    e.Message + "\n");
                }
            }
        }
    }
}