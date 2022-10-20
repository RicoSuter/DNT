using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dnt.Commands.Infrastructure
{
    public static class ProcessUtilities
    {
        public static async Task<string> ExecuteAsync(string command, string arguments, bool verbose, bool writeConsole, CancellationToken cancellationToken)
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

            var output = string.Empty;
            process.OutputDataReceived += (sendingProcess, args) =>
            {
                output += args.Data + "\n";

                if (args.Data != null && verbose && writeConsole)
                {
                    ConsoleUtilities.Write(args.Data + "\n");
                }
            };

            process.ErrorDataReceived += (sendingProcess, args) =>
            {
                if (args.Data != null)
                {
                    ConsoleUtilities.WriteError(args.Data + "\n");
                }
            };

            ConsoleUtilities.WriteInfo("Executing: " + command + "\n");
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            var registration = cancellationToken.Register(() =>
            {
                process.Close();
            });

            await Task.Run(() => process.WaitForExit());
            registration.Dispose();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Process execution failed: " + command);
            }

            return output;
        }
    }
}
