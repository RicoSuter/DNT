using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dnt.Commands.Infrastructure;
using Dnt.Commands.Packages.Switcher;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using NConsole;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Dnt.Commands.Packages
{
    [Command(Name = "switch-to-packages", Description = "Switch project references to NuGet references")]
    public class SwitchProjectsToPackagesCommand : CommandBase
    {
        private const string SolutionFolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

        private static readonly Regex ProjectLineRegex = new Regex(
            @"^Project\(""(?<typeGuid>[^""]+)""\)\s*=\s*""(?<name>[^""]+)"",\s*""(?<path>[^""]+)"",\s*""(?<projectGuid>[^""]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex NestedProjectRegex = new Regex(
            @"^\s*\{(?<child>[A-F0-9\-]+)\}\s*=\s*\{(?<parent>[A-F0-9\-]+)\}\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [Argument(Position = 1, IsRequired = false, Description = "Configuration .json file")]
        public string Configuration { get; set; } = "switcher.json";

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var configuration = ReferenceSwitcherConfiguration.Load(Configuration, host);
            if (configuration == null)
            {
                return null;
            }

            await SwitchToPackagesAsync(host, configuration);

            if (configuration.RemoveProjects)
            {
                await RemoveProjectsFromSolutionAsync(configuration, host);
            }

            configuration.Restore = null; // restore information no longer needed
            configuration.Save();

            return null;
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
                    .Select(p => Path.GetFileName(p))
                    .ToList();

                foreach (var solutionProject in solution.SolutionProjects)
                {
                    if (ProjectExtensions.IsSupportedProject(solutionProject.FilePath))
                    {
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
                CaptureSolutionProjectFolders(configuration);
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

        private static void CaptureSolutionProjectFolders(ReferenceSwitcherConfiguration configuration)
        {
            var solutionFolderMap = LoadSolutionFolderMap(configuration.ActualSolution);
            if (solutionFolderMap.Count == 0)
            {
                return;
            }

            if (configuration.SolutionProjectFolders == null)
            {
                configuration.SolutionProjectFolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var mapping in configuration.Mappings)
            {
                foreach (var path in mapping.Value)
                {
                    var actualPath = configuration.GetActualPath(path);
                    if (solutionFolderMap.TryGetValue(actualPath, out var folder))
                    {
                        configuration.SolutionProjectFolders[actualPath] = folder;
                    }
                }
            }
        }

        private static Dictionary<string, string> LoadSolutionFolderMap(string solutionPath)
        {
            if (solutionPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                return LoadSolutionFolderMapFromSlnx(solutionPath);
            }

            if (solutionPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                return LoadSolutionFolderMapFromSln(solutionPath);
            }

            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> LoadSolutionFolderMapFromSlnx(string solutionPath)
        {
            var solutionDir = Path.GetDirectoryName(solutionPath);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            XDocument document;
            try
            {
                document = XDocument.Load(solutionPath);
            }
            catch
            {
                return result;
            }

            var folderElements = document.Descendants().Where(e => e.Name.LocalName == "Folder");
            foreach (var folderElement in folderElements)
            {
                var fullFolderPath = GetFullFolderPath(folderElement);
                var normalizedFolder = NormalizeSolutionFolderName(fullFolderPath);
                if (string.IsNullOrWhiteSpace(normalizedFolder))
                {
                    continue;
                }

                var projectElements = folderElement.Elements().Where(e => e.Name.LocalName == "Project");
                foreach (var projectElement in projectElements)
                {
                    var projectPath = projectElement.Attribute("Path")?.Value;
                    if (string.IsNullOrWhiteSpace(projectPath))
                    {
                        continue;
                    }

                    var absolutePath = PathUtilities.ToAbsolutePath(projectPath, solutionDir);
                    result[absolutePath] = normalizedFolder;
                }
            }

            return result;
        }

        private static Dictionary<string, string> LoadSolutionFolderMapFromSln(string solutionPath)
        {
            var solutionDir = Path.GetDirectoryName(solutionPath);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var folderGuidToName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var projectGuidToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var nestedProjects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string[] lines;
            try
            {
                lines = File.ReadAllLines(solutionPath);
            }
            catch
            {
                return result;
            }

            var inNestedProjects = false;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Project(", StringComparison.OrdinalIgnoreCase))
                {
                    var match = ProjectLineRegex.Match(trimmed);
                    if (match.Success)
                    {
                        var typeGuid = match.Groups["typeGuid"].Value;
                        var name = match.Groups["name"].Value;
                        var path = match.Groups["path"].Value;
                        var projectGuid = match.Groups["projectGuid"].Value.Trim('{', '}');

                        if (string.Equals(typeGuid, SolutionFolderTypeGuid, StringComparison.OrdinalIgnoreCase))
                        {
                            var normalized = NormalizeSolutionFolderName(name);
                            if (!string.IsNullOrWhiteSpace(normalized))
                            {
                                folderGuidToName[projectGuid] = normalized;
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(path))
                        {
                            projectGuidToPath[projectGuid] = path;
                        }
                    }
                }
                else if (trimmed.StartsWith("GlobalSection(NestedProjects)", StringComparison.OrdinalIgnoreCase))
                {
                    inNestedProjects = true;
                }
                else if (inNestedProjects)
                {
                    if (trimmed.StartsWith("EndGlobalSection", StringComparison.OrdinalIgnoreCase))
                    {
                        inNestedProjects = false;
                    }
                    else
                    {
                        var match = NestedProjectRegex.Match(trimmed);
                        if (match.Success)
                        {
                            nestedProjects[match.Groups["child"].Value] = match.Groups["parent"].Value;
                        }
                    }
                }
            }

            foreach (var projectEntry in projectGuidToPath)
            {
                var folderPath = BuildFolderPath(projectEntry.Key, nestedProjects, folderGuidToName);
                if (!string.IsNullOrWhiteSpace(folderPath))
                {
                    var absolutePath = PathUtilities.ToAbsolutePath(projectEntry.Value, solutionDir);
                    result[absolutePath] = folderPath;
                }
            }

            return result;
        }

        private static string BuildFolderPath(
            string projectGuid,
            Dictionary<string, string> nestedProjects,
            Dictionary<string, string> folderGuidToName)
        {
            if (!nestedProjects.TryGetValue(projectGuid, out var currentGuid))
                return null;

            var segments = new List<string>();
            while (folderGuidToName.TryGetValue(currentGuid, out var folderName))
            {
                segments.Add(folderName);
                if (!nestedProjects.TryGetValue(currentGuid, out currentGuid))
                    break;
            }

            if (segments.Count == 0)
                return null;

            segments.Reverse();
            return NormalizeSolutionFolderName(string.Join(Path.DirectorySeparatorChar.ToString(), segments));
        }

        private static string NormalizeSolutionFolderName(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return null;

            var parts = folderName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? string.Join(Path.DirectorySeparatorChar.ToString(), parts) : null;
        }

        private static string GetFullFolderPath(XElement folderElement)
        {
            var segments = new List<string>();
            var current = folderElement;

            while (current != null && current.Name.LocalName == "Folder")
            {
                var name = current.Attribute("Name")?.Value;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    segments.Add(name);
                }
                current = current.Parent;
            }

            segments.Reverse();
            return string.Join(Path.DirectorySeparatorChar.ToString(), segments);
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
