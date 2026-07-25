using System.Collections.Generic;
using NConsole;

namespace Dnt.Tests
{
    internal class TestConsoleHost : IConsoleHost
    {
        public List<string> Messages { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();

        public void WriteMessage(string message) => Messages.Add(message);
        public void WriteError(string message) => Errors.Add(message);
        public string ReadValue(string message) => null;
    }
}
