using System;
using System.Diagnostics;
using System.IO;
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

            if (Debugger.IsAttached)
                Console.ReadLine();
        }
    }
}