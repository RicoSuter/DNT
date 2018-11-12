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

        // Based on https://daveaglick.com/posts/running-a-design-time-build-with-msbuild-apis
        public static ProjectInformation GetProject(string projectPath)
        {
            Project result = null;
            bool sdkStyle = false;
            using (XmlReader reader = XmlReader.Create(projectPath))
            {
                if (reader.MoveToContent() == XmlNodeType.Element && reader.HasAttributes)
                {
                    sdkStyle = reader.MoveToAttribute("Sdk");
                    if (sdkStyle)
                    {
                        result = GetCoreProject(projectPath);
                    }
                    else
                    {
                        result = GetLegacyProject(projectPath);
                    }
                }
            }

            return new ProjectInformation() { Project = result, IsLegacyProject = !sdkStyle };
        }

        public static Project GetLegacyProject(string projectPath)
        {
            string toolsPath = GetToolsPath();
            Dictionary<string, string> globalProperties = GetGlobalProperties(projectPath, toolsPath);

            ProjectCollection projectCollection = new ProjectCollection(globalProperties);
            projectCollection.AddToolset(new Toolset(ToolLocationHelper.CurrentToolsVersion, toolsPath, projectCollection, string.Empty));
            Project project = projectCollection.LoadProject(projectPath);
            return project;
        }

        public static Project GetCoreProject(string projectPath)
        {
            string toolsPath = GetCoreBasePath(projectPath);
            Dictionary<string, string> globalProperties = GetCoreGlobalProperties(projectPath, toolsPath);


            Environment.SetEnvironmentVariable(
                "MSBuildExtensionsPath",
                globalProperties["MSBuildExtensionsPath"]);
            Environment.SetEnvironmentVariable(
                "MSBuildSDKsPath",
                globalProperties["MSBuildSDKsPath"]);

            ProjectCollection projectCollection = new ProjectCollection(globalProperties);
            projectCollection.AddToolset(new Toolset(ToolLocationHelper.CurrentToolsVersion, toolsPath, projectCollection, string.Empty));
            return projectCollection.LoadProject(projectPath);
        }

        public static string GetToolsPath()
        {
            string toolsPath = ToolLocationHelper.GetPathToBuildToolsFile("msbuild.exe", ToolLocationHelper.CurrentToolsVersion);
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

        public static string[] PollForToolsPath()
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

        public static Dictionary<string, string> GetGlobalProperties(string projectPath, string toolsPath)
        {
            string solutionDir = Path.GetDirectoryName(projectPath);
            string extensionsPath = Path.GetFullPath(Path.Combine(toolsPath, @"..\..\"));
            string sdksPath = Path.Combine(extensionsPath, "Sdks");
            string roslynTargetsPath = Path.Combine(toolsPath, "Roslyn");

            return new Dictionary<string, string>
            {
                { "SolutionDir", solutionDir },
                { "MSBuildExtensionsPath", extensionsPath },
                { "MSBuildSDKsPath", sdksPath },
                { "RoslynTargetsPath", roslynTargetsPath }
            };
        }

        public static Dictionary<string, string> GetCoreGlobalProperties(string projectPath, string toolsPath)
        {
            string solutionDir = Path.GetDirectoryName(projectPath);
            string extensionsPath = toolsPath;
            string sdksPath = Path.Combine(toolsPath, "Sdks");
            string roslynTargetsPath = Path.Combine(toolsPath, "Roslyn");

            return new Dictionary<string, string>
            {
                { "SolutionDir", solutionDir },
                { "MSBuildExtensionsPath", extensionsPath },
                { "MSBuildSDKsPath", sdksPath },
                { "RoslynTargetsPath", roslynTargetsPath }
            };
        }

        public static string GetCoreBasePath(string projectPath)
        {
            // Ensure that we set the DOTNET_CLI_UI_LANGUAGE environment variable to "en-US" before
            // running 'dotnet --info'. Otherwise, we may get localized results.
            string originalCliLanguage = Environment.GetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE");
            Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", "en-US");

            try
            {
                // Create the process info
                ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", "--info")
                {
                    // global.json may change the version, so need to set working directory
                    WorkingDirectory = Path.GetDirectoryName(projectPath),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                // Execute the process
                using (Process process = Process.Start(startInfo))
                {
                    List<string> lines = new List<string>();
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            lines.Add(e.Data);
                        }
                    };
                    process.BeginOutputReadLine();
                    process.WaitForExit();
                    return ParseCoreBasePath(lines);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE", originalCliLanguage);
            }
        }

        public static string ParseCoreBasePath(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                throw new Exception("Could not get results from `dotnet --info` call");
            }

            foreach (string line in lines)
            {
                int colonIndex = line.IndexOf(':');
                if (colonIndex >= 0
                    && line.Substring(0, colonIndex).Trim().Equals("Base Path", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(colonIndex + 1).Trim();
                }
            }

            throw new Exception("Could not locate base path in `dotnet --info` results");
        }

    }

    public class ProjectInformation
    {
        public Project Project { get; set; }
        public bool IsLegacyProject { get; set; }
    }

}
