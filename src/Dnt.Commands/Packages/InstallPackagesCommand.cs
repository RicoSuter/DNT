using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NConsole;

namespace Dnt.Commands.Packages
{
    [Command(Name = "install-packages")]
    public class InstallPackagesCommand : ProjectCommandBase
    {
        [Argument(Position = 1, IsRequired = true)]
        public string Package { get; set; }

        [Argument(Position = 2, IsRequired = false)]
        public string Version { get; set; }

        [Argument(Name = nameof(EnforceRanges), IsRequired = false)]
        public bool EnforceRanges { get; set; } = true;

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var version = Version;
            if (!string.IsNullOrWhiteSpace(version))
            {
                if (EnforceRanges)
                {
                    var segments = Version.Split('.');
                    version =
                        segments.Length == 2 && segments[1] == "*" ? "[" + segments[0] + "," + (int.Parse(segments[0]) + 1) + ")" :
                        segments.Length == 3 && segments[2] == "*" ? "[" + segments[0] + "." + segments[1] + "," + segments[0] + "," + (int.Parse(segments[1]) + 1) + ")" :
                        Version;
                }

                version = " -v " + version;
            }

            await Task.WhenAll(GetProjectPaths()
                .Select(projectPath => Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteCommandAsync("dotnet", "add \"" + projectPath + "\" package " + Package + version, false, host, CancellationToken.None);
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