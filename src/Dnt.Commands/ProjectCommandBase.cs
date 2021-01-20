using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using NConsole;

namespace Dnt.Commands
{
    public abstract class ProjectCommandBase : CommandBase
    {
        [Argument(Name = "path", Description = "The path to a single .csproj, an .sln, a directory containing .csprojs or empty for scanning the current directory for .csprojs.", IsRequired = false)]
        public string Path { get; set; }

        protected string[] GetProjectPaths()
        {
            if (File.Exists(Path))
            {
                if (Path.EndsWith(".sln"))
                {
                    var solution = SolutionFile.Parse(System.IO.Path.GetFullPath(Path));
                    return solution.ProjectsInOrder.Where(p => p.ProjectType != SolutionProjectType.SolutionFolder).Select(p => p.AbsolutePath).ToArray();
                }

                return new[] { Path };
            }

            var path = Directory.Exists(Path) ? Path : Directory.GetCurrentDirectory();
            return Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);
        }

        protected IDictionary<string, string> TryGetGlobalProperties()
        {
            if (PathIsSolutionFile())
            {
                return ProjectExtensions.GetGlobalProperties(System.IO.Path.GetFullPath(Path));
            }

            return null;
        }

        private bool PathIsSolutionFile()
        {
            return File.Exists(Path) && Path.EndsWith(".sln");
        }
    }
}