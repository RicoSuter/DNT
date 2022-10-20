﻿using System;
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

        public static ProjectInformation LoadProject(string projectPath, IDictionary<string, string> globalProperties = null)
        {
            // Based on https://daveaglick.com/posts/running-a-design-time-build-with-msbuild-apis

            if (globalProperties == null)
            {
                globalProperties = GetGlobalProperties(projectPath);
            }

            var isSdkStyle = false;

            using (var reader = XmlReader.Create(projectPath))
            {
                if (reader.MoveToContent() == XmlNodeType.Element && reader.HasAttributes)
                {
                    isSdkStyle = reader.MoveToAttribute("Sdk");
                }
            }

            var projectFileContent = File.ReadAllText(projectPath);
            var lineEndings = projectFileContent.Contains("\r\n") ? "\r\n" : "\n";

            try
            {
                var projectCollection = new ProjectCollection(globalProperties);
                var project = projectCollection.LoadProject(projectPath);

                return new ProjectInformation(projectCollection, project, !isSdkStyle, lineEndings);
            }
            catch (InvalidProjectFileException projectFileException)
            {
                throw new InvalidOperationException("Not a project: " + projectPath, projectFileException);
            }
        }

        public static Dictionary<string, string> GetGlobalProperties(string projectOrSolutionPath)
        {
            // SolutionDir always ends with directory separator character
            var solutionDir = $"{Path.GetDirectoryName(projectOrSolutionPath)}{Path.DirectorySeparatorChar}";

            return new Dictionary<string, string>
            {
                { "SolutionDir", solutionDir }
            };
        }

        public static void SaveWithLineEndings(ProjectInformation projectInformation)
        {
            projectInformation.Project.Save();
            if (projectInformation.LineEndings != Environment.NewLine)
            {
                var path = projectInformation.Project.FullPath;
                File.WriteAllText(path, File.ReadAllText(path).Replace(Environment.NewLine, projectInformation.LineEndings));
            }
        }
    }
}
