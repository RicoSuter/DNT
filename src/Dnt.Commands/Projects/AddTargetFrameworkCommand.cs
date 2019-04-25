using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using NConsole;

namespace Dnt.Commands.Projects
{
    [Command(Name = "add-target-framework", Description = "Add a target framework.")]
    public class AddTargetFrameworkCommand : ProjectCommandBase
    {
        [Argument(Position = 1, Description = "The target framework to add (e.g. 'netstandard2.0').", IsRequired = true)]
        public string TargetFramework { get; set; }

        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            if (string.IsNullOrEmpty(TargetFramework))
            {
                return Task.FromResult<object>(null);
            }

            using (var collection = new ProjectCollection())
            {
                foreach (var projectPath in GetProjectPaths())
                {
                    try
                    {
                        var project = collection.LoadProject(projectPath);

                        ProjectProperty targetFrameworksProperty = null;
                        List<string> targetFrameworks = null;

                        var targetFrameworkProperty = project.GetProperty("TargetFramework");
                        if (targetFrameworkProperty != null)
                        {
                            var value = targetFrameworkProperty.EvaluatedValue;
                            if (!string.IsNullOrEmpty(value))
                            {
                                targetFrameworks = new List<string> { targetFrameworkProperty.EvaluatedValue };
                            }

                            project.RemoveProperty(targetFrameworkProperty);
                        }
                        else
                        {
                            targetFrameworksProperty = project.GetProperty("TargetFrameworks");
                            if (targetFrameworksProperty != null)
                            {
                                targetFrameworks = targetFrameworksProperty.EvaluatedValue
                                    .Split(';')
                                    .Where(f => !string.IsNullOrEmpty(f))
                                    .ToList();
                            }
                        }

                        if (targetFrameworks != null)
                        {
                            if (!targetFrameworks.Contains(TargetFramework))
                            {
                                targetFrameworks.Add(TargetFramework);

                                if (targetFrameworksProperty != null)
                                {
                                    targetFrameworksProperty.UnevaluatedValue = string.Join(";", targetFrameworks);
                                }
                                else
                                {
                                    project.SetProperty("TargetFrameworks", string.Join(";", targetFrameworks));
                                }

                                host.WriteMessage("[x] Added target framework " + TargetFramework + " to " +
                                    System.IO.Path.GetFileName(projectPath) + "\n");
                            }
                            else
                            {
                                host.WriteMessage("[ ] Target framework " + TargetFramework + " already in project " +
                                    System.IO.Path.GetFileName(projectPath) + "\n");
                            }
                        }
                        else
                        {
                            host.WriteMessage("[ ] Could not add target framework " + TargetFramework + " to " +
                                System.IO.Path.GetFileName(projectPath) + "\n");
                        }

                        project.Save();

                        collection.UnloadProject(project);
                    }
                    catch (Exception e)
                    {
                        host.WriteError(e + "\n");
                    }
                }
            }

            return Task.FromResult<object>(null);
        }
    }
}