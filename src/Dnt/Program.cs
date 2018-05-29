using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Dnt.Commands;
using Dnt.Commands.Infrastructure;
using NConsole;

namespace Dnt
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Debugger.IsAttached)
                Directory.SetCurrentDirectory("C:\\Data\\Projects\\Playground");

            var stopwatch = Stopwatch.StartNew();

            var assembly = Assembly.GetEntryAssembly();
            ConsoleUtilities.Write("DNT (DotNetTools, https://github.com/RSuter/DNT, v" + assembly.GetName().Version + ")\n");
            ConsoleUtilities.Write("Binary: " + assembly.Location + "\n\n");
            try
            {
                var processor = new CommandLineProcessor(new CoreConsoleHost());
                processor.RegisterCommandsFromAssembly(typeof(CommandBase).Assembly);
                processor.Process(args);
            }
            catch (Exception e)
            {
                ConsoleUtilities.WriteError(e.ToString());
            }
            ConsoleUtilities.WriteColor("Elapsed time: " + stopwatch.Elapsed, ConsoleColor.DarkCyan);

            if (Debugger.IsAttached)
                Console.ReadLine();
        }
    }
}