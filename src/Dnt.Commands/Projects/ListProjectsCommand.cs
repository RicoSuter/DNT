using System;
using System.IO;
using System.Threading.Tasks;
using Dnt.Commands.Infrastructure;
using NConsole;

namespace Dnt.Commands.Projects
{
    [Command(Name = "list-projects", Description = "Lists all projects which are affected by project commands.")]
    public class ListProjectsCommand : ProjectCommandBase
    {
        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            foreach (var projectPath in GetProjectPaths())
            {
                ConsoleUtilities.WriteColor(System.IO.Path.GetFileName(projectPath) + ":\n", ConsoleColor.Green);
                host.WriteMessage(projectPath.Replace(Directory.GetCurrentDirectory(), "") + "\n");
            }

            return Task.FromResult<object>(null);
        }
    }
}