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

        private static void SwitchToPackages(IConsoleHost host, ReferenceSwitcherConfiguration configuration)
        {
            var solution = SolutionFile.Parse(configuration.ActualSolution);
            var mappedProjectFilePaths = configuration.Mappings.Select(m => Path.GetFileName(m.Value.ActualPath)).ToList();

            foreach (var mapping in configuration.Mappings)
            {
                var projectPath = mapping.Value.ActualPath;
                var packageName = mapping.Key;
                var defaultPackageVersion = mapping.Value.Version;

                var switchedProjects = SwitchToPackage(configuration, solution, projectPath, packageName, defaultPackageVersion, mappedProjectFilePaths, host);
                foreach (var s in switchedProjects)
                {
                    host.WriteMessage(Path.GetFileName(s) + ": \n");
                    host.WriteMessage("   " + Path.GetFileName(projectPath) + " => " + packageName + " v" + defaultPackageVersion + "\n");
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

        private static IReadOnlyList<string> SwitchToPackage(
            ReferenceSwitcherConfiguration configuration, SolutionFile solution, string projectPath,
            string packageName, string defaultPackageVersion, List<string> mappedProjectFilePaths, IConsoleHost host)
        {
            var switchedProjects = new List<string>();
            var absoluteProjectPath = PathUtilities.ToAbsolutePath(projectPath, Directory.GetCurrentDirectory());

            foreach (var solutionProject in solution.ProjectsInOrder)
            {
                if (solutionProject.ProjectType != SolutionProjectType.SolutionFolder &&
                    ProjectExtensions.IsSupportedProject(solutionProject.AbsolutePath))
                {
                    try
                    {
                        var projectInformation = ProjectExtensions.GetProject(solutionProject.AbsolutePath);
                        var project = projectInformation.Project;
                        var projectFileName = Path.GetFileName(solutionProject.AbsolutePath);
                        var projectDirectory = Path.GetDirectoryName(solutionProject.AbsolutePath);

                        if (!mappedProjectFilePaths.Contains(projectFileName)) // do not modify mapped projects
                        {
                            var count = 0;
                            foreach (var item in project.Items.Where(i => i.ItemType == "ProjectReference").ToList())
                            {
                                var absoluteProjectReferencePath =
                                    PathUtilities.ToAbsolutePath(item.EvaluatedInclude, projectDirectory);
                                if (absoluteProjectReferencePath == absoluteProjectPath)
                                {
                                    project.RemoveItem(item);
                                    
                                    var packageVersion = GetPackageVersion(configuration, solutionProject.AbsolutePath, packageName, defaultPackageVersion);
                                    AddPackage(configuration, solutionProject, project, packageName, packageVersion);
                                    
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
                        host.WriteError("The project '" + solutionProject.AbsolutePath + "' could not be loaded: " +
                                        e.Message + "\n");
                    }
                }
            }

            return switchedProjects;
        }

        private static void AddPackage(ReferenceSwitcherConfiguration configuration, ProjectInSolution solutionProject, Project project, string packageName, string packageVersion)
        {
            var projectName =
                Path.GetFileNameWithoutExtension(solutionProject.AbsolutePath);

            var switchedProject = (
                from r in configuration.SwitchedProjects
                where string.Equals(r.Name, projectName, StringComparison.OrdinalIgnoreCase)
                select r).FirstOrDefault();

            if (switchedProject != null)
            {
                var reference = switchedProject.GetSwitchedPackage(packageName);

                if (!string.IsNullOrEmpty(reference.Include))
                {
                    project.AddItem("Reference", reference.Include, reference.Metadata);
                }
                else
                {
                    project.AddItem("PackageReference", packageName,
                        new[] { new KeyValuePair<string, string>("Version", packageVersion) });
                }

            }
        }

        private static string GetPackageVersion(ReferenceSwitcherConfiguration configuration, string projectFullPath, string packageName, string defaultPackageVersion)
        {
            var projectName = Path.GetFileNameWithoutExtension(projectFullPath);
            string result = null;

            var switchedProject = (
                from p in configuration.SwitchedProjects
                where string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase)
                select p).FirstOrDefault();

            if (switchedProject != null)
            {
                result = (
                    from m in switchedProject.Packages
                    where string.Equals(m.PackageName, packageName, StringComparison.OrdinalIgnoreCase)
                    select m.PackageVersion
                    ).FirstOrDefault();
            }

            if (result is null)
                result = defaultPackageVersion;

            return result;
        }

    }
}