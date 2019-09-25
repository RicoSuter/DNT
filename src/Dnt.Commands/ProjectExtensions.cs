using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Locator;
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

            var globalProperties = GetGlobalProperties(projectPath);

            var isSdkStyle = false;

            using (var reader = XmlReader.Create(projectPath))
            {
                if (reader.MoveToContent() == XmlNodeType.Element && reader.HasAttributes)
                {
                    isSdkStyle = reader.MoveToAttribute("Sdk");
                }
            }

            try
            {
                var projectCollection = new ProjectCollection(globalProperties);
                var project = projectCollection.LoadProject(projectPath);

                return new ProjectInformation(projectCollection, project, !isSdkStyle);
            }
            catch (InvalidProjectFileException projectFileException)
            {
                throw new InvalidOperationException("Not a project: " + projectPath, projectFileException);
            }
        }

        private static Dictionary<string, string> GetGlobalProperties(string projectPath)
        {
            var solutionDir = Path.GetDirectoryName(projectPath);

            return new Dictionary<string, string>
            {
                { "SolutionDir", solutionDir }
            };
        }
    }
}
