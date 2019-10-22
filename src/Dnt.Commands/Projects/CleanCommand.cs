using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using NConsole;

namespace Dnt.Commands.Projects
{
    [Command(Name = "clean", Description = "Delete project's bin and obj directories.")]
    public class CleanCommand : ProjectCommandBase
    {
        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            foreach (var projectPath in GetProjectPaths())
            {
                try
                {
                    var projectDirectory = System.IO.Path.GetDirectoryName(projectPath);

                    var binDirectory = System.IO.Path.Combine(projectDirectory, "bin");
                    var objDirectory = System.IO.Path.Combine(projectDirectory, "obj");

                    if (Directory.Exists(binDirectory))
                    {
                        Directory.Delete(binDirectory, true);
                        host.WriteMessage("Deleted directory " + binDirectory + "\n");
                    }

                    if (Directory.Exists(objDirectory))
                    {
                        Directory.Delete(objDirectory, true);
                        host.WriteMessage("Deleted directory " + objDirectory + "\n");
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
