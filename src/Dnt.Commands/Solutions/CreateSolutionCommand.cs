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

            InstallTemplates();

            var rootDirectory = Directory.GetCurrentDirectory();
            var repositoryDirectory = Path.Combine(rootDirectory, Name);
            var srcDirectory = Path.Combine(repositoryDirectory, "src");

            var appDirectory = Path.Combine(repositoryDirectory, "src", Name);
            var clientsDirectory = Path.Combine(repositoryDirectory, "src", Name + ".Clients");

            Directory.CreateDirectory(repositoryDirectory);
            Directory.CreateDirectory(srcDirectory);
            Directory.CreateDirectory(appDirectory);
            Directory.CreateDirectory(clientsDirectory);

            CreateApplicationProject(appDirectory);
            CreateClientsProject(clientsDirectory);

            return null;
        }

        private static void InstallTemplates()
        {
            Console.Write("Install templates? [yes|no]");
            if (Console.ReadLine() == "yes")
                ProcessUtilities.Execute("dotnet new --install Microsoft.AspNetCore.SpaTemplates::*");
        }

        private void CreateApplicationProject(string appDirectory)
        {
            Directory.SetCurrentDirectory(appDirectory);
            ProcessUtilities.Execute("dotnet new " + Type);
            ProcessUtilities.Execute("dotnet restore");
            if (File.Exists("package.json"))
                ProcessUtilities.Execute("npm i");
        }

        private static void CreateClientsProject(string clientsDirectory)
        {
            Directory.SetCurrentDirectory(clientsDirectory);
            ProcessUtilities.Execute("dotnet new classlib");
            ProcessUtilities.Execute("dotnet restore");
        }
    }
}