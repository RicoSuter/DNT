using System;
using System.Diagnostics;

namespace Dnt.Commands.Infrastructure
{
    public static class ProcessUtilities
    {
        public static void Execute(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.ErrorDataReceived += (sendingProcess, args) =>
            {
                if (args.Data != null)
                    ConsoleUtilities.WriteError(args.Data + "\n");
            };

            process.OutputDataReceived += (sendingProcess, args) =>
            {
                if (args.Data != null)
                    Console.Write(args.Data + "\n");
            };

            ConsoleUtilities.WriteInfo("Executing: " + command + "\n");
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidOperationException("Process execution failed: " + command);
        }
    }
}
