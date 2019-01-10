using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dnt.Commands.Infrastructure;
using Microsoft.Build.Evaluation;
using NConsole;

namespace Dnt.Commands.Projects
{
    [Command(Name = "switch-assemblies-to-projects", Description = "Updates assembly references to project references in the given projects.")]
    public class SwitchAssembliesToProjectsCommand : ProjectCommandBase
    {
        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            using (var projects = LoadProjects())
            {
                ReplaceAssemblyReferencesWithProjects(projects, host);
            }

            return Task.FromResult<object>(null);
        }

        private ProjectCollection LoadProjects()
        {
            var collection = new ProjectCollection();
            foreach (var projectPath in GetProjectPaths())
            {
                collection.LoadProject(projectPath);
            }

            return collection;
        }

        private static void ReplaceAssemblyReferencesWithProjects(ProjectCollection projects, IConsoleHost host)
        {
            var projectNames = projects.LoadedProjects.Select(p => System.IO.Path.GetFileNameWithoutExtension(p.FullPath));
            foreach (var project in projects.LoadedProjects)
            {
                try
                {
                    using (var projectInformation = ProjectExtensions.LoadProject(project.FullPath))
                    {
                        var newProjectsToReference = new List<Project>();
                        var assemblyReferencesToRemove = new List<ProjectItem>();

                        foreach (var reference in projectInformation.Project.Items
                            .Where(r => r.ItemType == "Reference" && projectNames.Contains(r.UnevaluatedInclude.Split(',').First())))
                        {
                            assemblyReferencesToRemove.Add(reference);

                            var projectToReference = projects.LoadedProjects.First(p => System.IO.Path.GetFileNameWithoutExtension(p.FullPath) == reference.EvaluatedInclude.Split(',').First());
                            newProjectsToReference.Add(projectToReference);
                        }

                        foreach (var item in assemblyReferencesToRemove)
                        {
                            projectInformation.Project.RemoveItem(item);
                        }

                        foreach (var projectToReference in newProjectsToReference)
                        {
                            var refProjectDirectory = System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(projectToReference.FullPath));
                            var relativePath = PathUtilities.ToRelativePath(refProjectDirectory, System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(project.FullPath)));

                            var path = System.IO.Path.Combine(relativePath, System.IO.Path.GetFileName(projectToReference.FullPath));
                            var metaData = new List<KeyValuePair<string, string>>
                            {
                                new KeyValuePair<string, string>("Name", System.IO.Path.GetFileNameWithoutExtension(projectToReference.FullPath))
                            };

                            projectInformation.Project.AddItem("ProjectReference", path, metaData);
                        }

                        projectInformation.Project.Save();
                    }
                }
                catch (Exception e)
                {
                    host.WriteError("The project '" + project.FullPath + "' could not be loaded: " + e.Message + "\n");
                }
            }
        }
    }
}