using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using NConsole;

namespace Dnt.Commands.Projects
{
    [Command(Name = "enable", Description = "Enable a project feature.")]
    public class EnableCommand : ProjectCommandBase
    {
        [Argument(Position = 1, Description = "The feature to enable (warnaserror|xmldocs).", IsRequired = true)]
        public string Action { get; set; }

        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            using (var collection = new ProjectCollection())
            {
                foreach (var projectPath in GetProjectPaths())
                {
                    try
                    {
                        var project = collection.LoadProject(projectPath);

                        var result = false;
                        switch (Action)
                        {
                            case "warnaserror":
                                result = EnableBooleanProperty(project, "WarningsAsErrors");
                                break;
                            case "xmldocs":
                                result = EnableXmlDocs(project);
                                break;
                            default:
                                throw new ArgumentException("The feature " + Action + " is not available.");
                        }

                        if (result)
                        {
                            host.WriteMessage("[x] Enabled feature " + Action + " in project " + System.IO.Path.GetFileName(projectPath) + "\n");

                            if (!Simulate)
                            {
                                project.Save();
                            }
                        }
                        else
                        {
                            host.WriteMessage("[ ] Feature " + Action + " already enabled in project " + System.IO.Path.GetFileName(projectPath) + "\n");
                        }

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

        private static bool EnableXmlDocs(Project project)
        {
            var versionItem = project.Properties.FirstOrDefault(i => i.Name == "DocumentationFile");
            if (versionItem == null || versionItem.IsImported)
            {
                return EnableBooleanProperty(project, "GenerateDocumentationFile");
            }

            return false;
        }

        private static bool EnableBooleanProperty(Project project, string propertyName)
        {
            var versionItem = project.Properties.FirstOrDefault(i => i.Name == propertyName);
            if (versionItem == null || versionItem.IsImported)
            {
                versionItem = project.SetProperty(propertyName, "true");
                return true;
            }
            else if (versionItem.EvaluatedValue != "true")
            {
                versionItem.UnevaluatedValue = "true";
                return true;
            }

            return false;
        }
    }
}