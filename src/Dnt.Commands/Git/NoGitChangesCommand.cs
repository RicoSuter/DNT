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
        private readonly Regex _changedRegex = new Regex("\t((.*):  )?(.*)\n");

        [Argument(Position = 1, IsRequired = false)]
        public string ErrorText { get; set; }

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var output = await ExecuteCommandAsync("git", "status", true, host, CancellationToken.None);
          
            var changesMatches = _changedRegex.Matches(output);
            var changes = changesMatches
                .OfType<Match>()
                .Select(m => new
                {
                    Type = m.Groups[2].Value == "" ? "added" : m.Groups[2].Value,
                    File = m.Groups[3].Value
                })
                .ToArray();           

            if (changes.Any())
            {
                if (!string.IsNullOrEmpty(ErrorText))
                {
                    host.WriteError(ErrorText + "\n\n");
                }

                foreach (var change in changes)
                {
                    host.WriteError("Change not allowed: " + change.Type + ": " + change.File + "\n");
                }

                throw new Exception(
                    "Changes in Git are not allowed (i.e. build is not allowed to change repository files).\n" +
                    "Possible fixes:\n" +
                    "- Merge target branch with the source branch (auto-merge changes files)\n" +
                    "- Try to rebuild the project locally and commit all changes");
            }

            return changes.Length;
        }
    }
}