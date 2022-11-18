using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Dnt.Commands.Infrastructure;
using Fluid;
using Namotion.Reflection;
using NConsole;
using Newtonsoft.Json.Linq;

namespace Dnt.Commands.Generation
{
    [Command(Name = "jsongen", Description = "TBD.")]
    public class JsonGeneratorCommand : CommandBase
    {
        public class ReflectMarkdownOptions
        {
            public string JsonFile { get; set; }

            public string OutputFile { get; set; }

            public string LiquidFile { get; set; }

            public string Placeholder { get; set; }
        }

        [Argument(Position = 1, IsRequired = true)]
        public string OptionsFile { get; set; }

        [Argument(Name = nameof(Configuration), IsRequired = false)]
        public string Configuration { get; set; }

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var xmlDocsOptions = new XmlDocsOptions
            {
                FormattingMode = XmlDocsFormattingMode.Markdown
            };

            var directory = Path.GetDirectoryName(Path.GetFullPath(OptionsFile));

            var optionsJson = File
                .ReadAllText(OptionsFile)
                .Replace("$(Configuration)", Configuration);

            var options = JsonSerializer.Deserialize<ReflectMarkdownOptions>(optionsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            options.JsonFile = PathUtilities.ToAbsolutePath(options.JsonFile, directory);
            options.LiquidFile = PathUtilities.ToAbsolutePath(options.LiquidFile, directory);
            options.OutputFile = PathUtilities.ToAbsolutePath(options.OutputFile, directory);

            var jsonData = File.ReadAllText(options.JsonFile);
            dynamic json = JObject.Parse(jsonData);

            var parser = new FluidParser();
            var source = File.ReadAllText(options.LiquidFile);

            if (parser.TryParse(source, out var template, out var error))
            {
                var templateOptions = new TemplateOptions
                {
                    MemberAccessStrategy = new UnsafeMemberAccessStrategy(),
                    CultureInfo = CultureInfo.InvariantCulture,
                };

                var context = new TemplateContext(json, templateOptions);
                var output = template.Render(context);

                if (!string.IsNullOrEmpty(options.Placeholder))
                {
                    var currentContent = File.ReadAllText(options.OutputFile);
                    var firstIndex = currentContent.IndexOf(options.Placeholder);
                    var secondIndex = currentContent.IndexOf(options.Placeholder, firstIndex + 1);

                    // Hint: Looping through object gives indexers for key (property name) and value
                    // https://github.com/sebastienros/fluid/blob/main/Fluid/Values/DictionaryValue.cs#L126

                    output = currentContent.Substring(0, firstIndex) +
                        options.Placeholder +
                        "\n" + output.Trim() +
                        "\n" + options.Placeholder +
                        currentContent.Substring(secondIndex + options.Placeholder.Length);
                }
              
                if (string.IsNullOrWhiteSpace(options.OutputFile))
                {
                    return Task.FromResult(output);
                }
                else
                {
                    File.WriteAllText(options.OutputFile, output);
                    Console.WriteLine($"Updated {options.OutputFile} in {nameof(JsonGeneratorCommand)}.");
                }
            }
            else
            {
                Console.WriteLine($"Error in {nameof(JsonGeneratorCommand)}: {error}");
            }

            return Task.FromResult<object>(null);
        }
    }
}
