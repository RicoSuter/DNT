using System;
using System.Linq;
using System.Threading.Tasks;
using NConsole;

namespace Dnt.Commands.Projects
{
    [Command(Name = "nowarn", Description = "Add a no warn id to the projects.")]
    public class NoWarnCommand : ProjectCommandBase
    {
        [Argument(Position = 1, Description = "The diagnostics id (semicolon separated).", IsRequired = true)]
        public string DiagnosticsIds { get; set; }

        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var globalProperties = TryGetGlobalProperties();

            foreach (var projectPath in GetProjectPaths())
            {
                try
                {
                    using (var projectInformation = ProjectExtensions.LoadProject(projectPath, globalProperties))
                    {
                        var project = projectInformation.Project;

                        foreach (var diagnosticsId in DiagnosticsIds.Split(';'))
                        {
                            var noWarnItem = project.Properties.FirstOrDefault(i => i.Name.ToLowerInvariant() == "nowarn");
                            if (noWarnItem == null || noWarnItem.IsImported)
                            {
                                noWarnItem = project.SetProperty("NoWarn", diagnosticsId);
                                host.WriteMessage($"[x] Added no warn '{diagnosticsId}' tag to project " + System.IO.Path.GetFileName(projectPath) + "\n");
                            }
                            else if (!noWarnItem.EvaluatedValue.Split(';').Contains(diagnosticsId))
                            {
                                noWarnItem.UnevaluatedValue = noWarnItem.EvaluatedValue + ";" + diagnosticsId;
                                host.WriteMessage($"[x] Added no warn '{diagnosticsId}' to existing tag to project " + System.IO.Path.GetFileName(projectPath) + "\n");
                            }
                            else
                            {
                                host.WriteMessage($"[ ] No warn '{diagnosticsId}' already exists in project " + System.IO.Path.GetFileName(projectPath) + "\n");
                            }
                        }

                        ProjectExtensions.SaveWithLineEndings(projectInformation);
                    }
                }
                catch (Exception e)
                {
                    host.WriteError(e + "\n");
                }
            }

            return Task.FromResult<object>(null);
        }
    }
}