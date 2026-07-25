using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dnt.Commands.Infrastructure;
using Dnt.Commands.Packages.Switcher;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using NConsole;

namespace Dnt.Commands.Packages
{
    [Command(Name = "switch-to-packages", Description = "Switch project references to NuGet references")]
    public class SwitchProjectsToPackagesCommand : SwitchCommandBase
    {
        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var configuration = LoadConfiguration(host);
            if (configuration == null)
            {
                return null;
            }

            ReconcileRestoreVariables(configuration, host);

            await SwitchToPackagesAsync(host, configuration);

            if (configuration.RemoveProjects)
            {
                await RemoveProjectsFromSolutionAsync(configuration, host);
            }

            configuration.Restore = null; // restore information no longer needed
            configuration.RestoreVariables = null; // transient switch snapshot no longer needed
            configuration.Save();

            return null;
        }

        /// <summary>If switch-to-projects recorded a different set of variables than the current run resolves,
        /// warns and re-applies the recorded snapshot so references restore against the paths that were written.</summary>
        private static void ReconcileRestoreVariables(ReferenceSwitcherConfiguration configuration, IConsoleHost host)
        {
            var recorded = configuration.RestoreVariables;
            if (recorded == null || recorded.Count == 0)
            {
                return;
            }

            var current = configuration.EffectiveVariables ?? new Dictionary<string, string>();
            var differing = new List<string>();
            foreach (var entry in recorded)
            {
                current.TryGetValue(entry.Key, out var currentValue);
                if (!string.Equals(currentValue, entry.Value, StringComparison.Ordinal))
                {
                    differing.Add(entry.Key);
                }
            }

            if (differing.Count == 0)
            {
                return;
            }

            host.WriteMessage("Warning: switch-to-packages is running with different switcher variables than switch-to-projects used.\n");
            foreach (var key in differing)
            {
                recorded.TryGetValue(key, out var recordedValue);
                current.TryGetValue(key, out var currentValue);
                host.WriteMessage($"    {key}: switched in with '{recordedValue}', now '{currentValue ?? "(unset)"}'. Using '{recordedValue}' to restore.\n");
            }

            // Re-apply the recorded snapshot so reference matching targets exactly what switch-to-projects wrote.
            configuration.ApplyVariableOverrides(recorded);
        }

        private static async Task SwitchToPackagesAsync(IConsoleHost host, ReferenceSwitcherConfiguration configuration)
        {
            // See if the file is a known solution file.
            var serializer = SolutionSerializers.GetSerializerByMoniker(configuration.ActualSolution);
            if (serializer is null)
            {
                host.WriteError("Solution " + configuration.ActualSolution + " could not be loaded as it's not recognized by the serializer");
                return;
            }

            try
            {
                var solution = await serializer.OpenAsync(configuration.ActualSolution, CancellationToken.None);
                var globalProperties = ProjectExtensions.GetGlobalProperties(Path.GetFullPath(configuration.ActualSolution));
                var mappedProjectFilePaths = configuration.Mappings.Values
                    .SelectMany(x => x)
                    .Select(p => Path.GetFileName(configuration.GetActualPath(p)))
                    .ToList();
                foreach (var solutionProject in solution.SolutionProjects)
                {
                    if (!ProjectExtensions.IsSupportedProject(solutionProject.FilePath))
                    {
                        ConsoleUtilities.Write("Skipping unsupported project: " + solutionProject.FilePath + "\n");
                        continue;
                    }

                    try
                    {
                        using (var projectInformation = ProjectExtensions.LoadProject(solutionProject.FilePath, globalProperties))
                        {
                            foreach (var mapping in configuration.Mappings)
                            {
                                var projectPaths = mapping.Value.Select(p => configuration.GetActualPath(p)).ToList();
                                var packageName = mapping.Key;

                                var switchedProjects = SwitchToPackage(
                                    configuration, solutionProject, projectInformation, projectPaths, packageName, mappedProjectFilePaths, host);

                                if (switchedProjects.Count > 0)
                                {
                                    host.WriteMessage("Project " + solutionProject.ActualDisplayName + " with project references:\n");
                                    projectPaths.ForEach(p => host.WriteMessage("    " + Path.GetFileName(p) + "\n"));
                                    host.WriteMessage("    replaced by package: " + packageName + " v" + switchedProjects.First().PackageVersion + "\n");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        host.WriteError($"The project '{solutionProject.FilePath}' could not be loaded: {e}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                host.WriteError("Solution " + configuration.ActualSolution + " could not be loaded. " + ex.Message);
            }
        }

        private async Task RemoveProjectsFromSolutionAsync(ReferenceSwitcherConfiguration configuration, IConsoleHost host)
        {
            // See if the file is a known solution file.
            var serializer = SolutionSerializers.GetSerializerByMoniker(configuration.ActualSolution);
            if (serializer is null)
            {
                host.WriteError("Solution " + configuration.ActualSolution + " could not be loaded as it's not recognized by the serializer");
                return;
            }

            try
            {
                var solution = await serializer.OpenAsync(configuration.ActualSolution, CancellationToken.None);
                var projects = new List<string>();
                foreach (var mapping in configuration.Mappings)
                {
                    foreach (var path in mapping.Value)
                    {
                        var project = solution.SolutionProjects.FirstOrDefault
                        (p => PathUtilities.ToAbsolutePath(p.FilePath, Path.GetDirectoryName(configuration.ActualSolution)) == configuration.GetActualPath(path));
                        if (project != null)
                        {
                            projects.Add("\"" + configuration.GetActualPath(path) + "\"");
                        }
                    }
                }

                if (projects.Any())
                {
                    await ExecuteCommandAsync("dotnet",
                        "sln \"" + configuration.ActualSolution + "\" remove " + string.Join(" ", projects), false,
                        host, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                host.WriteError("Solution " + configuration.ActualSolution + " could not be loaded. " + ex.Message);
            }
        }

        private static IReadOnlyList<(string ProjectPath, string PackageVersion)> SwitchToPackage(
           ReferenceSwitcherConfiguration configuration,
           SolutionProjectModel solutionProject, ProjectInformation projectInformation,
           List<string> switchedProjectPaths, string switchedPackageName,
           List<string> mappedProjectFilePaths, IConsoleHost host)
        {
            var switchedProjects = new List<(string ProjectPath, string PackageVersion)>();
            var absoluteProjectPaths = switchedProjectPaths.Select(p => PathUtilities.ToAbsolutePath(p, Directory.GetCurrentDirectory())).ToList();

            var project = projectInformation.Project;
            var projectName = Path.GetFileNameWithoutExtension(solutionProject.FilePath);
            var projectFileName = Path.GetFileName(solutionProject.FilePath);
            var projectDirectory = Path.GetDirectoryName(solutionProject.FilePath);

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

                        switchedProjects.Add((solutionProject.FilePath, packageVersion));
                        count++;
                    }

                    if (count > 0)
                    {
                        ProjectExtensions.SaveWithLineEndings(projectInformation);
                    }
                }
            }

            return switchedProjects;
        }

        private static void AddPackage(ReferenceSwitcherConfiguration configuration, SolutionProjectModel solutionProject, Project project, string packageName, string packageVersion)
        {
            var projectName =
                Path.GetFileNameWithoutExtension(solutionProject.FilePath);

            var switchedProject = (
                from r in configuration.Restore
                where string.Equals(r.Name, projectName, StringComparison.OrdinalIgnoreCase)
                select r).FirstOrDefault();

            if (switchedProject != null)
            {
                var reference = switchedProject.GetSwitchedPackage(packageName);

                if (reference != null && !string.IsNullOrEmpty(reference.Include))
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
