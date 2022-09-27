using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NConsole;

namespace Dnt.Commands.Solutions
{
    [Command(Name = "full-rename")]
    public class FullRenameCommand : CommandBase
    {
        [Argument(Position = 1)]
        public string Search { get; set; }

        [Argument(Position = 2)]
        public string Replace { get; set; }

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            if (string.IsNullOrEmpty(Search))
                throw new ArgumentNullException(nameof(Search));

            if (string.IsNullOrEmpty(Replace))
                throw new ArgumentNullException(nameof(Replace));

            var rootDirectory = Directory.GetCurrentDirectory();

            foreach (var file in Directory.GetFiles(rootDirectory, "*", SearchOption.AllDirectories)
                .Where(d => !d.Contains("\\."))
                .Where(d => !d.Contains("/.")))
            {
                var text = File.ReadAllText(file);
                var newText = text.Replace(Search, Replace);
                if (text != newText)
                {
                    File.WriteAllText(file, newText);
                    host.WriteMessage("File " + file + " updated.\n");
                }
            }

            foreach (var directory in Directory.GetDirectories(rootDirectory, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
            {
                var newDirectory = directory.Replace(Search, Replace);
                if (directory != newDirectory)
                {
                    Directory.Move(directory, newDirectory);
                    host.WriteMessage("Directory " + directory + " renamed to\n    " + newDirectory + "\n");
                }
            }

            foreach (var file in Directory.GetFiles(rootDirectory, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
            {
                var newFile = file.Replace(Search, Replace);
                if (file != newFile)
                {
                    Directory.Move(file, newFile);
                    host.WriteMessage("File " + file + " renamed to\n    " + newFile + "\n");
                }
            }

            return null;
        }
    }
}