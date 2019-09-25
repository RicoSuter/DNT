using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dnt.Commands.Infrastructure
{
    public static class ProcessUtilities
    {
        public static async Task ExecuteAsync(string command, string arguments, bool verbose)
        {
            var taskSource = new TaskCompletionSource<object>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sendingProcess, args) =>
            {
                if (args.Data != null && verbose)
                    ConsoleUtilities.Write(args.Data + "\n");
            };

            process.ErrorDataReceived += (sendingProcess, args) =>
            {
                if (args.Data != null)
                    ConsoleUtilities.WriteError(args.Data + "\n");
            };

            ConsoleUtilities.WriteInfo("Executing: " + command + "\n");
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Process execution failed: " + command);
            }
        }
    }
}
