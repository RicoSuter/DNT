using System.Threading.Tasks;
using Dnt.Commands.Infrastructure;
using NConsole;

namespace Dnt.Commands
{
    public abstract class CommandBase : IConsoleCommand
    {
        [Switch(ShortName = "s", LongName = "simulate")]
        public bool Simulate { get; set; }

        public abstract Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host);

        protected void ExecuteCommand(string command, IConsoleHost host)
        {
            if (Simulate)
            {
                host.WriteMessage("SIMULATE " + command);
            }
            else
            {
                ProcessUtilities.Execute(command);
            }
        }
    }
}