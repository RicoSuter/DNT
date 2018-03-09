using System.IO;
using NConsole;

namespace Dnt.Commands
{
    public abstract class ProjectCommandBase : CommandBase
    {
        [Argument(Name = "path", Description = "The path to a single .csproj, directory containing .csprojs or empty for the current directory.", IsRequired = false)]
        public string Path { get; set; }

        protected string[] GetProjectPaths()
        {
            if (File.Exists(Path))
            {
                return new[] { Path };
            }

            var path = Directory.Exists(Path) ? Path : Directory.GetCurrentDirectory();
            return Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);
        }
    }
}