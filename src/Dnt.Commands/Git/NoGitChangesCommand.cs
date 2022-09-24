using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NConsole;

namespace Dnt.Commands.Git
{
    [Command(Name = "nogitchanges")]
    public class NoGitChangesCommand : CommandBase
    {
        private readonly Regex _regex = new Regex("\t(.*):  (.*)");

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var output = await ExecuteCommandAsync("git", "status", true, host, CancellationToken.None);
            var matches = _regex.Matches(output);
            var changes = matches
                .OfType<Match>()
                .Select(m => new
                {
                    Type = m.Groups[1].Value,
                    File = m.Groups[2].Value
                })
                .ToArray();

            if (changes.Any())
            {
                foreach (var change in changes)
                {
                    host.WriteError("Change not allowed: " + change.Type + ": " + change.File + "\n");
                }

                throw new Exception("Changes in Git are not allowed \n" +
                    "(e.g. build is not allowed to change the repository files, \n" +
                    "rebuild the project locally and commit all changes).");
            }

            return changes.Length;
        }
    }
}