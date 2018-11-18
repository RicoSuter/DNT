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

            configuration.Restore = null; // restore information no longer needed
            configuration.Save();

            return null;
        }

        private static void SwitchToPackages(IConsoleHost host, ReferenceSwitcherConfiguration configuration)
        {
            var solution = SolutionFile.Parse(configuration.ActualSolution);
            var mappedProjectFilePaths = configuration.Mappings.Select(m => Path.GetFileName(m.Value)).ToList();

            foreach (var solutionProject in solution.ProjectsInOrder)
            {
                if (solutionProject.ProjectType != SolutionProjectType.SolutionFolder &&
                    ProjectExtensions.IsSupportedProject(solutionProject.AbsolutePath))
                {
                    try
                    {
                        using (var projectInformation = ProjectExtensions.LoadProject(solutionProject.AbsolutePath))
                        {
                            foreach (var mapping in configuration.Mappings)
                            {
                                var projectPath = configuration.GetActualPath(mapping.Value);
                                var packageName = mapping.Key;

                                var switchedProjects = SwitchToPackage(
                                    configuration, solutionProject, projectInformation, projectPath, packageName, mappedProjectFilePaths, host);

                                foreach (var s in switchedProjects)
                                {
                                    host.WriteMessage(Path.GetFileName(s.ProjectPath) + ": \n");
                                    host.WriteMessage("   " + Path.GetFileName(projectPath) + " => "
                                        + packageName + " v" + s.PackageVersion + "\n");
                                }
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
                    projects.Add("\"" + configuration.GetActualPath(mapping.Value) + "\"");
                }
            }

            if (projects.Any())
            {
                await ExecuteCommandAsync("dotnet sln \"" + configuration.ActualSolution + "\" remove " + string.Join(" ", projects), host);
            }
        }

        private static IReadOnlyList<(string ProjectPath, string PackageVersion)> SwitchToPackage(
            ReferenceSwitcherConfiguration configuration,
            ProjectInSolution solutionProject, ProjectInformation projectInformation,
            string switchedProjectPath, string switchedPackageName,
            List<string> mappedProjectFilePaths, IConsoleHost host)
        {
            var switchedProjects = new List<(string ProjectPath, string PackageVersion)>();
            var absoluteProjectPath = PathUtilities.ToAbsolutePath(switchedProjectPath, Directory.GetCurrentDirectory());

            var project = projectInformation.Project;
            var projectName = Path.GetFileNameWithoutExtension(solutionProject.AbsolutePath);
            var projectFileName = Path.GetFileName(solutionProject.AbsolutePath);
            var projectDirectory = Path.GetDirectoryName(solutionProject.AbsolutePath);

            if (!mappedProjectFilePaths.Contains(projectFileName)) // do not modify mapped projects
            {
                var restoreProjectInformation = (
                    from r in configuration.Restore
                    where string.Equals(r.Name, projectName, StringComparison.OrdinalIgnoreCase)
                    select r).FirstOrDefault();

                if (restoreProjectInformation != null)
                {
                    var count = 0;
                    foreach (var item in project.Items.Where(i => i.ItemType == "ProjectReference").ToList())
                    {
                        var absoluteProjectReferencePath =
                            PathUtilities.ToAbsolutePath(item.EvaluatedInclude, projectDirectory);
                        if (absoluteProjectReferencePath == absoluteProjectPath)
                        {
                            project.RemoveItem(item);

                            var packageVersion = GetPackageVersion(restoreProjectInformation, switchedPackageName);
                            AddPackage(configuration, solutionProject, project, switchedPackageName, packageVersion);

                            switchedProjects.Add((solutionProject.AbsolutePath, packageVersion));
                            count++;
                        }
                    }

                    if (count > 0)
                    {
                        project.Save();
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
                from r in configuration.Restore
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
                    var items = project.AddItem("PackageReference", packageName,
                        new[] { new KeyValuePair<string, string>("Version", packageVersion) });

                    items.ToList().ForEach(item =>
                    {
                        item.Metadata?.ToList().ForEach(metadata =>
                            metadata.Xml.ExpressedAsAttribute = true);
                    });
                }

            }
        }

        private static string GetPackageVersion(RestoreProjectInformation restoreProjectInformation, string packageName)
        {
            string result = null;

            if (restoreProjectInformation != null)
            {
                result = (
                    from r in restoreProjectInformation.Packages
                    where string.Equals(r.PackageName, packageName, StringComparison.OrdinalIgnoreCase)
                    select r.PackageVersion
                    ).FirstOrDefault();
            }

            return result;
        }
    }
}