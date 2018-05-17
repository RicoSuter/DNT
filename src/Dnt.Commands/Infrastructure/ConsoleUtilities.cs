using System;

namespace Dnt.Commands.Infrastructure
{
    public static class ConsoleUtilities
    {
        public static void WriteError(string message)
        {
            WriteColor(message, ConsoleColor.Red);
        }

        public static void WriteInfo(string message)
        {
            WriteColor(message, ConsoleColor.Green);
        }

        public static void Write(string message)
        {
            lock (typeof(ConsoleUtilities))
            {
                Console.Write(message);
            }
        }

        public static void WriteColor(string message, ConsoleColor color)
        {
            lock (typeof(ConsoleUtilities))
            {
                var savedColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.Write(message);
                Console.ForegroundColor = savedColor;
            }
        }
    }
}