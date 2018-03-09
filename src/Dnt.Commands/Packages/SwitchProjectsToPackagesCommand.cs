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
    public class SwitchProjectsToPackagesCommand : IConsoleCommand
    {
        [Argument(Position = 1)]
        public string Configuration { get; set; }

        public Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var configuration = ReferenceSwitcherConfiguration.Load(Configuration);

            SwitchToPackages(host, configuration);
            //RemoveProjectsFromSolution(configuration); // TODO: This also removes some unrelated solution folders

            configuration.Save();

            return Task.FromResult<object>(null);
        }

        private void SwitchToPackages(IConsoleHost host, ReferenceSwitcherConfiguration configuration)
        {
            var solution = SolutionFile.Parse(configuration.ActualSolution);

            foreach (var mapping in configuration.Mappings)
            {
                var projectPath = mapping.Value.ActualPath;
                var packageName = mapping.Key;
                var packageVersion = mapping.Value.Version;

                var switchedProjects = SwitchToPackage(solution, projectPath, packageName, packageVersion, host);
                foreach (var s in switchedProjects)
                {
                    host.WriteMessage(Path.GetFileName(s) + ": \n");
                    host.WriteMessage("   " + Path.GetFileName(projectPath) + " => " + packageName + " v" + packageVersion + "\n");
                }
            }
        }

        private void RemoveProjectsFromSolution(ReferenceSwitcherConfiguration configuration)
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
                ProcessUtilities.Execute("dotnet sln \"" + configuration.ActualSolution + "\" remove " + string.Join(" ", projects));
            }
        }

        private IReadOnlyList<string> SwitchToPackage(SolutionFile solution, string projectPath, string packageName, string packageVersion, IConsoleHost host)
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
                            var projectDirectory = Path.GetDirectoryName(solutionProject.AbsolutePath);

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