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
    [Command(Name = "switch-to-projects")]
    public class SwitchPackagesToProjectsCommand : IConsoleCommand
    {
        [Argument(Position = 1)]
        public string Configuration { get; set; }

        public Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var configuration = ReferenceSwitcherConfiguration.Load(Configuration);

            AddProjectsToSolution(configuration);
            SwitchToProjects(configuration, host);

            configuration.Save();

            return Task.FromResult<object>(null);
        }

        private void AddProjectsToSolution(ReferenceSwitcherConfiguration configuration)
        {
            var solution = SolutionFile.Parse(configuration.ActualSolution);
            var projects = new List<string>();
            foreach (var mapping in configuration.Mappings)
            {
                if (solution.ProjectsInOrder.All(p => p.ProjectName != mapping.Key))
                {
                    projects.Add("\"" + mapping.Value.ActualPath + "\"");
                }
            }

            if (projects.Any())
            {
                ProcessUtilities.Execute("dotnet sln \"" + configuration.ActualSolution + "\" add " + string.Join(" ", projects));
            }
        }

        private void SwitchToProjects(ReferenceSwitcherConfiguration configuration, IConsoleHost host)
        {
            var solution = SolutionFile.Parse(configuration.ActualSolution);
            foreach (var mapping in configuration.Mappings)
            {
                var packageName = mapping.Key;
                var projectPath = mapping.Value.ActualPath;

                var switchedProjects = SwitchToProject(solution, packageName, projectPath, host);
                foreach (var s in switchedProjects)
                {
                    host.WriteMessage(Path.GetFileName(s.Key) + ": \n");
                    host.WriteMessage("   " + packageName + " v" + s.Value + " => " + Path.GetFileName(projectPath) + "\n");
                }

                if (switchedProjects.Any())
                {
                    mapping.Value.Version = switchedProjects.First().Value;
                }
            }
        }

        private IReadOnlyDictionary<string, string> SwitchToProject(SolutionFile solution, string packageName, string projectPath, IConsoleHost host)
        {
            var switchedProjects = new Dictionary<string, string>();
            foreach (var solutionProject in solution.ProjectsInOrder)
            {
                if (solutionProject.ProjectType != SolutionProjectType.SolutionFolder)
                {
                    try
                    {
                        using (var collection = new ProjectCollection())
                        {
                            var project = collection.LoadProject(solutionProject.AbsolutePath);
                            var projectDirectory = Path.GetDirectoryName(solutionProject.AbsolutePath);

                            foreach (var item in project.Items.Where(i => i.ItemType == "PackageReference").ToList())
                            {
                                var packageReference = item.EvaluatedInclude;
                                if (packageReference == packageName)
                                {
                                    project.RemoveItem(item);
                                    project.AddItem("ProjectReference", PathUtilities.ToRelativePath(projectPath, projectDirectory));

                                    var version = item.Metadata.SingleOrDefault(m => m.Name == "Version")?.EvaluatedValue ?? "Any";
                                    switchedProjects[solutionProject.AbsolutePath] = version;
                                }
                            }

                            project.Save();
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
