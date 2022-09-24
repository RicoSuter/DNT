using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dnt.Commands.Infrastructure;
using NConsole;

namespace Dnt.Commands.Solutions
{
    [Command(Name = "create-solution")]
    public class CreateSolutionCommand : CommandBase
    {
        [Argument(Position = 1)]
        public string Type { get; set; }

        [Argument(Name = "Name", IsRequired = true)]
        public string Name { get; set; }

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            if (string.IsNullOrEmpty(Name))
                throw new ArgumentNullException(nameof(Name));

            if (Directory.Exists(Name))
                throw new InvalidOperationException("The project '" + Name + "' already exists.");

            await InstallTemplatesAsync(host);

            var rootDirectory = Directory.GetCurrentDirectory();
            var repositoryDirectory = Path.Combine(rootDirectory, Name);
            var srcDirectory = Path.Combine(repositoryDirectory, "src");

            var appDirectory = Path.Combine(repositoryDirectory, "src", Name);
            var clientsDirectory = Path.Combine(repositoryDirectory, "src", Name + ".Clients");

            Directory.CreateDirectory(repositoryDirectory);
            Directory.CreateDirectory(srcDirectory);
            Directory.CreateDirectory(appDirectory);
            Directory.CreateDirectory(clientsDirectory);

            await CreateApplicationProjectAsync(appDirectory, host);
            await CreateClientsProjectAsync(clientsDirectory, host);

            return null;
        }

        private async Task InstallTemplatesAsync(IConsoleHost host)
        {
            ConsoleUtilities.Write("Install templates? [yes|no]");
            if (Console.ReadLine() == "yes")
                await ExecuteCommandAsync("dotnet", "new --install Microsoft.AspNetCore.SpaTemplates::*", 
                    false, host, CancellationToken.None);
        }

        private async Task CreateApplicationProjectAsync(string appDirectory, IConsoleHost host)
        {
            Directory.SetCurrentDirectory(appDirectory);

            await ExecuteCommandAsync("dotnet", "new " + Type, false, host, CancellationToken.None);
            await ExecuteCommandAsync("dotnet", "restore", false, host, CancellationToken.None);

            if (File.Exists("package.json"))
                await ExecuteCommandAsync("npm", "i", false, host, CancellationToken.None);
        }

        private async Task CreateClientsProjectAsync(string clientsDirectory, IConsoleHost host)
        {
            Directory.SetCurrentDirectory(clientsDirectory);
            await ExecuteCommandAsync("dotnet", "new classlib", false, host, CancellationToken.None);
            await ExecuteCommandAsync("dotnet", "restore", false, host, CancellationToken.None);
        }
    }
}