using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dnt.Commands.Infrastructure;
using Fluid;
using Fluid.Values;
using Mono.Cecil;
using Namotion.Reflection;
using NConsole;
using Newtonsoft.Json.Linq;

namespace Dnt.Commands.Markdown
{
    [Command(Name = "jsonmark", Description = "TBD.")]
    public class JsonMarkCommand : CommandBase
    {
        public class ReflectMarkdownOptions
        {
            public string JsonFile { get; set; }

            public string MarkdownFile { get; set; }

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
            options.MarkdownFile = PathUtilities.ToAbsolutePath(options.MarkdownFile, directory);

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

                templateOptions.Filters.AddFilter("properties", (FluidValue input, FilterArguments arguments, TemplateContext ctx) =>
                {
                    var obj = input.ToObjectValue();
                    if (obj is IFluidIndexable dictionary)
                        return new ValueTask<FluidValue>(new ArrayValue(dictionary
                            .Keys
                            .Select(k => new ObjectValue(new
                            {
                                Name = k,
                                Value = dictionary.TryGetValue(k, out var x) ? x : default
                            }))));
                    else
                        return new ValueTask<FluidValue>(new ObjectValue(((JObject)obj).Properties()));
                });

                var context = new TemplateContext(json, templateOptions);
                var output = template.Render(context);

                var markdown = File.ReadAllText(options.MarkdownFile);
                var firstIndex = markdown.IndexOf(options.Placeholder);
                var secondIndex = markdown.IndexOf(options.Placeholder, firstIndex + 1);

                markdown = markdown.Substring(0, firstIndex) +
                    options.Placeholder +
                    "\n" + output.Trim() +
                    "\n" + options.Placeholder +
                    markdown.Substring(secondIndex + options.Placeholder.Length);

                if (string.IsNullOrWhiteSpace(options.MarkdownFile))
                {
                    return Task.FromResult(markdown);
                }
                else
                {
                    File.WriteAllText(options.MarkdownFile, markdown);
                    Console.WriteLine($"Updated {options.MarkdownFile} in {nameof(AssemblyMarkCommand)}.");
                }
            }
            else
            {
                Console.WriteLine($"Error in {nameof(AssemblyMarkCommand)}: {error}");
            }

            return Task.FromResult<object>(null);
        }

        private static object GetDefaultValue(IMemberDefinition memberDefinition)
        {
            var attribute = memberDefinition?
                .CustomAttributes
                .FirstOrDefault(a => a.AttributeType.FullName == "System.ComponentModel.DefaultValueAttribute");

            if (attribute != null && attribute.ConstructorArguments.Count > 0)
            {
                var arg = attribute.ConstructorArguments[0];
                if (arg.Value is CustomAttributeArgument customArg)
                {
                    if (customArg.Type is TypeDefinition definition)
                    {
                        var field = definition.Fields.FirstOrDefault(f => f.Constant?.Equals(customArg.Value) == true);
                        if (field != null)
                        {
                            return field.Name;
                        }
                    }
                }

                return arg.Value;
            }

            return null;
        }

        private static bool GetIsRequired(IMemberDefinition memberDefinition)
        {
            if (memberDefinition != null)
            {
                var attribute = memberDefinition
                    .CustomAttributes
                    .FirstOrDefault(a => a.AttributeType.FullName == "System.ComponentModel.DataAnnotations.RequiredAttribute");

                if (attribute != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetXmlDocsDescription(IMemberDefinition memberDefinition, XmlDocsOptions xmlDocsOptions, XDocument xmlDocs)
        {
            if (xmlDocs != null && memberDefinition != null)
            {
                var docs = Namotion.Reflection.Cecil.XmlDocsExtensions.GetXmlDocsSummary(memberDefinition, xmlDocs, xmlDocsOptions);
                if (docs != null)
                {
                    return docs
                        .Replace("\n", " ")
                        .Replace("\r", string.Empty);
                }
            }

            return string.Empty;
        }
    }
}