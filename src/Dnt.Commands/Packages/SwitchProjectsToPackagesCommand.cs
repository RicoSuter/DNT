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
    [Command(Name = "switch-to-packages")]
    public class SwitchProjectsToPackagesCommand : CommandBase
    {
        [Argument(Position = 1)]
        public string Configuration { get; set; }

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var configuration = ReferenceSwitcherConfiguration.Load(Configuration);

            SwitchToPackages(host, configuration);
            await RemoveProjectsFromSolutionAsync(configuration, host);

            configuration.Save();

            return null;
        }

        private void SwitchToPackages(IConsoleHost host, ReferenceSwitcherConfiguration configuration)
        {
            var solution = SolutionFile.Parse(configuration.ActualSolution);
            var mappedProjectFilePaths = configuration.Mappings.Select(m => Path.GetFileName(m.Value.ActualPath)).ToList();

            foreach (var mapping in configuration.Mappings)
            {
                var projectPath = mapping.Value.ActualPath;
                var packageName = mapping.Key;
                var packageVersion = mapping.Value.Version;

                var switchedProjects = SwitchToPackage(solution, projectPath, packageName, packageVersion, mappedProjectFilePaths, host);
                foreach (var s in switchedProjects)
                {
                    host.WriteMessage(Path.GetFileName(s) + ": \n");
                    host.WriteMessage("   " + Path.GetFileName(projectPath) + " => " + packageName + " v" + packageVersion + "\n");
                }
            }
        }

        private async Task RemoveProjectsFromSolutionAsync(ReferenceSwitcherConfiguration configuration, IConsoleHost host)
        {
            var solution = SolutionFile.Parse(configuration.ActualSolution);
            var projects = new List<string>();
            foreach (var mapping in configuration.Mappings)
            {
                var project = solution.ProjectsInOrder.FirstOrDefault(p => p.ProjectName == mapping.Key);
                if (project != null)
                {
                    projects.Add("\"" + mapping.Value.ActualPath + "\"");
                }
            }

            if (projects.Any())
            {
                await ExecuteCommandAsync("dotnet sln \"" + configuration.ActualSolution + "\" remove " + string.Join(" ", projects), host);
            }
        }

        private IReadOnlyList<string> SwitchToPackage(SolutionFile solution, string projectPath,
            string packageName, string packageVersion, List<string> mappedProjectFilePaths, IConsoleHost host)
        {
            var switchedProjects = new List<string>();
            var absoluteProjectPath = PathUtilities.ToAbsolutePath(projectPath, Directory.GetCurrentDirectory());

            foreach (var solutionProject in solution.ProjectsInOrder)
            {
                if (solutionProject.ProjectType != SolutionProjectType.SolutionFolder &&
                    solutionProject.AbsolutePath.EndsWith(".csproj"))
                {
                    try
                    {
                        using (var collection = new ProjectCollection())
                        {
                            var project = collection.LoadProject(solutionProject.AbsolutePath);
                            var projectFileName = Path.GetFileName(solutionProject.AbsolutePath);
                            var projectDirectory = Path.GetDirectoryName(solutionProject.AbsolutePath);

                            if (!mappedProjectFilePaths.Contains(projectFileName)) // do not modify mapped projects
                            {
                                var count = 0;
                                foreach (var item in project.Items.Where(i => i.ItemType == "ProjectReference").ToList())
                                {
                                    var absoluteProjectReferencePath = PathUtilities.ToAbsolutePath(item.EvaluatedInclude, projectDirectory);
                                    if (absoluteProjectReferencePath == absoluteProjectPath)
                                    {
                                        project.RemoveItem(item);
                                        project.AddItem("PackageReference", packageName, new[] { new KeyValuePair<string, string>("Version", packageVersion) });

                                        switchedProjects.Add(solutionProject.AbsolutePath);
                                        count++;
                                    }
                                }

                                if (count > 0)
                                {
                                    project.Save();
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        host.WriteError("The project '" + solutionProject.AbsolutePath + "' could not be loaded: " + e.Message + "\n");
                    }
                }
            }

            return switchedProjects;
        }
    }
}