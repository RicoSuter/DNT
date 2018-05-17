using System;
using System.Linq;
using System.Threading.Tasks;
using NConsole;

namespace Dnt.Commands.Packages
{
    [Command(Name = "install-packages")]
    public class InstallPackagesCommand : ProjectCommandBase
    {
        [Argument(Name = "package", IsRequired = true)]
        public string Package { get; set; }

        [Argument(Name = "version", IsRequired = true)]
        public string Version { get; set; }

        [Argument(Name = nameof(EnforceRanges), IsRequired = false)]
        public bool EnforceRanges { get; set; } = true;

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var version = Version;

            if (EnforceRanges)
            {
                var segments = Version.Split('.');
                version =
                    segments.Length == 2 && segments[1] == "*" ? "[" + segments[0] + "," + (int.Parse(segments[0]) + 1) + ")" :
                    segments.Length == 3 && segments[2] == "*" ? "[" + segments[0] + "." + segments[1] + "," + segments[0] + "," + (int.Parse(segments[1]) + 1) + ")" :
                    Version;
            }

            await Task.WhenAll(GetProjectPaths()
                .Select(projectPath => Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteCommandAsync("dotnet add \"" + projectPath + "\" package " + Package + " -v " + version, host);
                    }
                    catch (Exception e)
                    {
                        host.WriteError(e + "\n");
                    }
                })));

            return null;
        }
    }
}