using System;
using Dnt.Commands.Infrastructure;
using NConsole;

namespace Dnt
{
    public class CoreConsoleHost : IConsoleHost
    {
        public void WriteMessage(string message)
        {
            Console.Write(message);
        }

        public void WriteError(string message)
        {
            ConsoleUtilities.WriteError(message);
        }

        public string ReadValue(string message)
        {
            Console.Write(message);
            return Console.ReadLine();
        }
    }
}