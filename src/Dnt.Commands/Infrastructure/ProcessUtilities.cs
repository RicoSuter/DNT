using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dnt.Commands.Infrastructure
{
    public static class ProcessUtilities
    {
        public static async Task ExecuteAsync(string command)
        {
            var taskSource = new TaskCompletionSource<object>();
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
                    ConsoleUtilities.Write(args.Data + "\n");
            };

            ConsoleUtilities.WriteInfo("Executing: " + command + "\n");
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode != 0)
                throw new InvalidOperationException("Process execution failed: " + command);

            //process.Exited += (sender, args) =>
            //{
            //    if (process.ExitCode != 0)
            //        taskSource.SetException(new InvalidOperationException("Process execution failed: " + command));
            //    else
            //        taskSource.SetResult(null);
            //};
            //return taskSource.Task;
        }
    }
}
