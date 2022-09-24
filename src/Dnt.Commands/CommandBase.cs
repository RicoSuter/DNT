using System.Threading;
using System.Threading.Tasks;
using Dnt.Commands.Infrastructure;
using NConsole;

namespace Dnt.Commands
{
    public abstract class CommandBase : IConsoleCommand
    {
        [Switch(ShortName = "s", LongName = "simulate")]
        public bool Simulate { get; set; }

        [Switch(ShortName = "np", LongName = "no-parallel")]
        public bool NoParallel { get; set; }

        [Switch(ShortName = "v", LongName = "verbose")]
        public bool Verbose { get; set; }

        public abstract Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host);

        protected async Task<string> ExecuteCommandAsync(string command, string arguments, bool writeConsole, IConsoleHost host, CancellationToken cancellationToken)
        {
            if (Simulate)
            {
                host.WriteMessage("SIMULATE " + command);
                return string.Empty;
            }
            else
            {
                return await ProcessUtilities.ExecuteAsync(command, arguments, Verbose, writeConsole, cancellationToken);
            }
        }
    }
}