using System;
using System.Threading.Tasks;
using NConsole;

namespace Dnt.Commands.Packages
{
    [Command(Name = "install-package")]
    public class InstallPackageCommand : ProjectCommandBase
    {
        [Argument(Name = "package", IsRequired = true)]
        public string Package { get; set; }

        [Argument(Name = "version", IsRequired = true)]
        public string Version { get; set; }

        public override Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var segments = Version.Split('.');
            var version =
                segments.Length == 2 && segments[1] == "*" ? "[" + segments[0] + "," + (int.Parse(segments[0]) + 1) + ")" :
                segments.Length == 3 && segments[2] == "*" ? "[" + segments[0] + "." + segments[1] + "," + segments[0] + "," + (int.Parse(segments[1]) + 1) + ")" :
                Version;

            foreach (var projectPath in GetProjectPaths())
            {
                try
                {
                    ExecuteCommand("dotnet add \"" + projectPath + "\" package " + Package + " -v " + version, host);
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