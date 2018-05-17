using System;
using System.IO;
using System.Threading.Tasks;
using Dnt.Commands.Infrastructure;
using NConsole;

namespace Dnt.Commands.Solutions
{
    [Command(Name = "create-solution")]
    public class CreateSolutionCommand : IConsoleCommand
    {
        [Argument(Position = 1)]
        public string Type { get; set; }

        [Argument(Name = "Name", IsRequired = true)]
        public string Name { get; set; }

        public async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            if (string.IsNullOrEmpty(Name))
                throw new ArgumentNullException(nameof(Name));

            if (Directory.Exists(Name))
                throw new InvalidOperationException("The project '" + Name + "' already exists.");

            await InstallTemplatesAsync();

            var rootDirectory = Directory.GetCurrentDirectory();
            var repositoryDirectory = Path.Combine(rootDirectory, Name);
            var srcDirectory = Path.Combine(repositoryDirectory, "src");

            var appDirectory = Path.Combine(repositoryDirectory, "src", Name);
            var clientsDirectory = Path.Combine(repositoryDirectory, "src", Name + ".Clients");

            Directory.CreateDirectory(repositoryDirectory);
            Directory.CreateDirectory(srcDirectory);
            Directory.CreateDirectory(appDirectory);
            Directory.CreateDirectory(clientsDirectory);

            await CreateApplicationProjectAsync(appDirectory);
            await CreateClientsProjectAsync(clientsDirectory);

            return null;
        }

        private static async Task InstallTemplatesAsync()
        {
            ConsoleUtilities.Write("Install templates? [yes|no]");
            if (Console.ReadLine() == "yes")
                await ProcessUtilities.ExecuteAsync("dotnet new --install Microsoft.AspNetCore.SpaTemplates::*");
        }

        private async Task CreateApplicationProjectAsync(string appDirectory)
        {
            Directory.SetCurrentDirectory(appDirectory);

            await ProcessUtilities.ExecuteAsync("dotnet new " + Type);
            await ProcessUtilities.ExecuteAsync("dotnet restore");

            if (File.Exists("package.json"))
                await ProcessUtilities.ExecuteAsync("npm i");
        }

        private static async Task CreateClientsProjectAsync(string clientsDirectory)
        {
            Directory.SetCurrentDirectory(clientsDirectory);
            await ProcessUtilities.ExecuteAsync("dotnet new classlib");
            await ProcessUtilities.ExecuteAsync("dotnet restore");
        }
    }
}