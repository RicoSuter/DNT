using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dnt.Commands;
using Dnt.Commands.Infrastructure;
using NConsole;

namespace Dnt
{
    class Program
    {
        static void Main(string[] args)
        {
            var stopwatch = Stopwatch.StartNew();

            var assembly = Assembly.GetEntryAssembly();
            ConsoleUtilities.Write("DNT (DotNetTools, https://github.com/RSuter/DNT, v" + assembly.GetName().Version + ")\n");
            ConsoleUtilities.Write("Binary: " + assembly.Location + "\n\n");
            try
            {
                SetMsBuildExePath();

                var processor = new CommandLineProcessor(new CoreConsoleHost());
                processor.RegisterCommandsFromAssembly(typeof(CommandBase).Assembly);
                processor.Process(args);
            }
            catch (Exception e)
            {
                ConsoleUtilities.WriteError(e.ToString());
            }
            ConsoleUtilities.WriteColor("\nElapsed time: " + stopwatch.Elapsed, ConsoleColor.DarkCyan);

            if (Debugger.IsAttached)
                Console.ReadLine();
        }

        private static void SetMsBuildExePath()
        {
            try
            {
                // See https://github.com/Microsoft/msbuild/issues/2532#issuecomment-381096259

                var process = Process.Start(new ProcessStartInfo("dotnet", "--list-sdks") { UseShellExecute = false, RedirectStandardOutput = true });
                process.WaitForExit(1000);

                var output = process.StandardOutput.ReadToEnd();
                var sdkPaths = Regex.Matches(output, "([0-9]+.[0-9]+.[0-9]+) \\[(.*)\\]").OfType<Match>()
                    .Select(m => Path.Combine(m.Groups[2].Value, m.Groups[1].Value, "MSBuild.dll"));

                var sdkPath = sdkPaths.LastOrDefault() ?? Path.Combine(ProjectExtensions.GetToolsPath(), "msbuild.exe");
                Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", sdkPath);
            }
            catch (Exception exception)
            {
                ConsoleUtilities.Write("Could not set MSBUILD_EXE_PATH: " + exception + "\n\n");
            }
        }
    }
}