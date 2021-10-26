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
    [Command(Name = "switch-to-packages", Description = "Switch project references to NuGet references")]
    public class SwitchProjectsToPackagesCommand : CommandBase
    {
        [Argument(Position = 1, IsRequired = false, Description = "Configuration .json file")]
        public string Configuration { get; set; } = "switcher.json";

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var configuration = ReferenceSwitcherConfiguration.Load(Configuration, host);
            if (configuration == null)
            {
                return null;
            }

            SwitchToPackages(host, configuration);

            if (configuration.RemoveProjects)
            {
                await RemoveProjectsFromSolutionAsync(configuration, host);
            }

            configuration.Restore = null; // restore information no longer needed
            configuration.Save();

            return null;
        }

        private static void SwitchToPackages(IConsoleHost host, ReferenceSwitcherConfiguration configuration)
        {
            var solution = SolutionFile.Parse(configuration.ActualSolution);
            var globalProperties = ProjectExtensions.GetGlobalProperties(Path.GetFullPath(configuration.ActualSolution));
            var mappedProjectFilePaths = configuration.Mappings.Values
                     .SelectMany(x => x)
                     .Select(p => Path.GetFileName(p))
                     .ToList();

            foreach (var solutionProject in solution.ProjectsInOrder)
            {
                if (solutionProject.ProjectType != SolutionProjectType.SolutionFolder &&
                    ProjectExtensions.IsSupportedProject(solutionProject.AbsolutePath))
                {
                    try
                    {
                        using (var projectInformation = ProjectExtensions.LoadProject(solutionProject.AbsolutePath, globalProperties))
                        {
                            foreach (var mapping in configuration.Mappings)
                            {
                                var projectPaths = mapping.Value.Select(p => configuration.GetActualPath(p)).ToList();
                                var packageName = mapping.Key;

                                var switchedProjects = SwitchToPackage(
                                    configuration, solutionProject, projectInformation, projectPaths, packageName, mappedProjectFilePaths, host);

                                if (switchedProjects.Count > 0)
                                {
                                    host.WriteMessage("Project " + solutionProject.ProjectName + " with project references:\n");
                                    projectPaths.ForEach(p => host.WriteMessage("    " + Path.GetFileName(p) + "\n"));
                                    host.WriteMessage("    replaced by package: " + packageName + " v" + switchedProjects.First().PackageVersion + "\n");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        host.WriteError($"The project '{solutionProject.AbsolutePath}' could not be loaded: {e}\n");
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
                foreach (var path in mapping.Value)
                {
                    var project = solution.ProjectsInOrder.FirstOrDefault
                        (p => PathUtilities.ToAbsolutePath(p.RelativePath, Path.GetDirectoryName(configuration.ActualSolution)) == configuration.GetActualPath(path));
                    if (project != null)
                    {
                        projects.Add("\"" + configuration.GetActualPath(path) + "\"");
                    }
                }
            }

            if (projects.Any())
            {
                await ExecuteCommandAsync("dotnet", "sln \"" + configuration.ActualSolution + "\" remove " + string.Join(" ", projects), host);
            }
        }

        private static IReadOnlyList<(string ProjectPath, string PackageVersion)> SwitchToPackage(
            ReferenceSwitcherConfiguration configuration,
            ProjectInSolution solutionProject, ProjectInformation projectInformation,
            List<string> switchedProjectPaths, string switchedPackageName,
            List<string> mappedProjectFilePaths, IConsoleHost host)
        {
            var switchedProjects = new List<(string ProjectPath, string PackageVersion)>();
            var absoluteProjectPaths = switchedProjectPaths.Select(p => PathUtilities.ToAbsolutePath(p, Directory.GetCurrentDirectory())).ToList();

            var project = projectInformation.Project;
            var projectName = Path.GetFileNameWithoutExtension(solutionProject.AbsolutePath);
            var projectFileName = Path.GetFileName(solutionProject.AbsolutePath);
            var projectDirectory = Path.GetDirectoryName(solutionProject.AbsolutePath);

            // do not modify mapped projects unless we are always keeping them in the solution
            if (!mappedProjectFilePaths.Contains(projectFileName) || !configuration.RemoveProjects)
            {
                var restoreProjectInformation = (
                    from r in configuration.Restore
                    where string.Equals(r.Name, projectName, StringComparison.OrdinalIgnoreCase)
                    select r).FirstOrDefault();

                if (restoreProjectInformation != null)
                {
                    var count = 0;
                    var matchingProjectReferences = project.Items.Where
                    (
                        i => i.ItemType == "ProjectReference" &&
                        absoluteProjectPaths.Contains(PathUtilities.ToAbsolutePath(i.EvaluatedInclude, projectDirectory))
                    ).ToList();

                    foreach (var item in matchingProjectReferences)
                    {
                        project.RemoveItem(item);

                        var packageVersion = GetPackageVersion(restoreProjectInformation, switchedPackageName);
                        AddPackage(configuration, solutionProject, project, switchedPackageName, packageVersion);

                        switchedProjects.Add((solutionProject.AbsolutePath, packageVersion));
                        count++;
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
                    if (!project.Items.Any(i => i.ItemType == "PackageReference" && i.EvaluatedInclude == packageName)) // check that the reference is not already present
                    {
                        var items = project.AddItem("PackageReference", packageName,
                                                    packageVersion == null ? Enumerable.Empty<KeyValuePair<string, string>>() : // this is the case if CentralPackageVersions is in use
                            new[] { new KeyValuePair<string, string>("Version", packageVersion) });

                        items.ToList().ForEach(item =>
                        {
                            item.Metadata?.ToList().ForEach(metadata =>
                                metadata.Xml.ExpressedAsAttribute = true);
                        });
                    }
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
