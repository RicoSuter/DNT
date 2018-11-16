using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities;

namespace Dnt.Commands
{
    public static class ProjectExtensions
    {
        public static bool GeneratesPackage(this Project project)
        {
            return project.GetProperty("GeneratePackageOnBuild")?.EvaluatedValue.ToLowerInvariant() == "true";
        }

        public static bool HasVersion(this Project project)
        {
            var data = File.ReadAllText(project.FullPath);
            return data.Contains("<Version>");
            //return project.Properties.Any(i => i.Name == "Version" && !string.IsNullOrEmpty(i.UnevaluatedValue));
        }

        public static bool IsSupportedProject(string projectAbsolutePath)
        {
            projectAbsolutePath = projectAbsolutePath.ToLower();
            return (projectAbsolutePath.EndsWith(".csproj") || projectAbsolutePath.EndsWith(".vbproj"));
        }

        public static ProjectInformation LoadProject(string projectPath)
        {
            // Based on https://daveaglick.com/posts/running-a-design-time-build-with-msbuild-apis

            using (XmlReader reader = XmlReader.Create(projectPath))
            {
                if (reader.MoveToContent() == XmlNodeType.Element && reader.HasAttributes)
                {
                    var isSdkStyle = reader.MoveToAttribute("Sdk");
                    if (isSdkStyle)
                    {
                        return GetSdkroject(projectPath);
                    }
                    else
                    {
                        return GetLegacyProject(projectPath);
                    }
                }
            }

            throw new InvalidOperationException("Not a project.");
        }

        private static ProjectInformation GetLegacyProject(string projectPath)
        {
            var toolsPath = GetToolsPath();
            var globalProperties = GetLegacyGlobalProperties(projectPath, toolsPath);

            var projectCollection = new ProjectCollection(globalProperties);
            projectCollection.AddToolset(new Toolset(ToolLocationHelper.CurrentToolsVersion, toolsPath, projectCollection, string.Empty));

            var project = projectCollection.LoadProject(projectPath);
            return new ProjectInformation(projectCollection) { Project = project, IsLegacyProject = false };
        }

        private static ProjectInformation GetSdkroject(string projectPath)
        {
            var toolsPath = GetSdkBasePath(projectPath);
            var globalProperties = GetSdkGlobalProperties(projectPath, toolsPath);

            Environment.SetEnvironmentVariable(
                "MSBuildExtensionsPath",
                globalProperties["MSBuildExtensionsPath"]);
            Environment.SetEnvironmentVariable(
                "MSBuildSDKsPath",
                globalProperties["MSBuildSDKsPath"]);

            var projectCollection = new ProjectCollection(globalProperties);
            projectCollection.AddToolset(new Toolset(ToolLocationHelper.CurrentToolsVersion, toolsPath, projectCollection, string.Empty));

            var project = projectCollection.LoadProject(projectPath);
            return new ProjectInformation(projectCollection) { Project = project, IsLegacyProject = false };
        }

        private static string GetToolsPath()
        {
            var toolsPath = ToolLocationHelper.GetPathToBuildToolsFile("msbuild.exe", ToolLocationHelper.CurrentToolsVersion);
            if (string.IsNullOrEmpty(toolsPath))
            {
                toolsPath = PollForToolsPath().FirstOrDefault();
            }
            if (string.IsNullOrEmpty(toolsPath))
            {
                throw new Exception("Could not locate the tools (MSBuild) path.");
            }

            return Path.GetDirectoryName(toolsPath);
        }

        private static string[] PollForToolsPath()
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            return new[]
            {
                Path.Combine(programFilesX86, @"Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"MSBuild\14.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"MSBuild\12.0\Bin\MSBuild.exe")
            }.Where(File.Exists).ToArray();
        }

        private static Dictionary<string, string> GetLegacyGlobalProperties(string projectPath, string toolsPath)
        {
            var solutionDir = Path.GetDirectoryName(projectPath);
            var extensionsPath = Path.GetFullPath(Path.Combine(toolsPath, @"..\..\"));
            var sdksPath = Path.Combine(extensionsPath, "Sdks");
            var roslynTargetsPath = Path.Combine(toolsPath, "Roslyn");

            return new Dictionary<string, string>
            {
                { "SolutionDir", solutionDir },
                { "MSBuildExtensionsPath", extensionsPath },
                { "MSBuildSDKsPath", sdksPath },
                { "RoslynTargetsPath", roslynTargetsPath }
            };
        }

        private static Dictionary<string, string> GetSdkGlobalProperties(string projectPath, string toolsPath)
        {
            var solutionDir = Path.GetDirectoryName(projectPath);
            var extensionsPath = toolsPath;
            var sdksPath = Path.Combine(toolsPath, "Sdks");
            var roslynTargetsPath = Path.Combine(toolsPath, "Roslyn");

            return new Dictionary<string, string>
            {
                { "SolutionDir", solutionDir },
                { "MSBuildExtensionsPath", extensionsPath },
                { "MSBuildSDKsPath", sdksPath },
                { "RoslynTargetsPath", roslynTargetsPath }
            };
        }

        private static string GetSdkBasePath(string projectPath)
        {
            // Ensure that we set the DOTNET_CLI_UI_LANGUAGE environment variable to "en-US" before
            // running 'dotnet --info'. Otherwise, we may get localized results.
            var originalCliLanguage = Environment.GetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE");

            Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US");
            try
            {
                // Create the process info
                var startInfo = new ProcessStartInfo("dotnet", "--info")
                {
                    // global.json may change the version, so need to set working directory
                    WorkingDirectory = Path.GetDirectoryName(projectPath),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                // Execute the process
                using (var process = Process.Start(startInfo))
                {
                    var lines = new List<string>();
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            lines.Add(e.Data);
                        }
                    };

                    process.BeginOutputReadLine();
                    process.WaitForExit();
                    return ParseSdkBasePath(lines);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", originalCliLanguage);
            }
        }

        private static string ParseSdkBasePath(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                throw new Exception("Could not get results from `dotnet --info` call");
            }

            foreach (string line in lines)
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex >= 0 &&
                    line.Substring(0, colonIndex).Trim().Equals("Base Path", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(colonIndex + 1).Trim();
                }
            }

            throw new Exception("Could not locate base path in `dotnet --info` results");
        }

    }

    public class ProjectInformation : IDisposable
    {
        private readonly ProjectCollection _projectCollection;

        public ProjectInformation(ProjectCollection projectCollection)
        {
            _projectCollection = projectCollection;
        }

        public Project Project { get; set; }

        public bool IsLegacyProject { get; set; }

        public void Dispose()
        {
            _projectCollection.Dispose();
        }
    }
}
