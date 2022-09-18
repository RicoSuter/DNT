using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dnt.Commands.Infrastructure;
using Fluid;
using Mono.Cecil;
using Namotion.Reflection;
using NConsole;

namespace Dnt.Commands.Reflection
{
    [Command(Name = "assemblymark", Description = "TBD.")]
    public class AssemblyMarkCommand : CommandBase
    {
        public class ReflectMarkdownOptions
        {
            public string AssemblyFile { get; set; }

            public string TypeName { get; set; }

            public string MarkdownFile { get; set; }

            public string LiquidFile { get; set; }

            public string Placeholder { get; set; }
        }

        [Argument(Position = 1, IsRequired = true)]
        public string Options { get; set; }

        [Argument(Name = nameof(Configuration), IsRequired = false)]
        public string Configuration { get; set; }

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var xmlDocsOptions = new XmlDocsOptions
            {
                FormattingMode = XmlDocsFormattingMode.Markdown
            };

            var directory = Path.GetDirectoryName(Path.GetFullPath(Options));

            var optionsJson = File
                .ReadAllText(Options)
                .Replace("$(Configuration)", Configuration);

            var options = JsonSerializer.Deserialize<ReflectMarkdownOptions>(optionsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            options.AssemblyFile = PathUtilities.ToAbsolutePath(options.AssemblyFile, directory);
            options.LiquidFile = PathUtilities.ToAbsolutePath(options.LiquidFile, directory);
            options.MarkdownFile = PathUtilities.ToAbsolutePath(options.MarkdownFile, directory);

            XDocument xmlDocs = null;
            var xmlDocsFile = options.AssemblyFile.Replace(".dll", ".xml");
            if (File.Exists(xmlDocsFile))
            {
                xmlDocs = XDocument.Parse(File.ReadAllText(xmlDocsFile));
            }

            var assembly = AssemblyDefinition.ReadAssembly(options.AssemblyFile);
            var type = assembly
                .Modules
                .SelectMany(m => m.Types)
                .Where(t => t.FullName == options.TypeName)
                .FirstOrDefault();

            var parser = new FluidParser();
            var source = File.ReadAllText(options.LiquidFile);

            if (parser.TryParse(source, out var template, out var error))
            {
                var templateOptions = new TemplateOptions
                {
                    MemberAccessStrategy = new UnsafeMemberAccessStrategy(),
                    CultureInfo = CultureInfo.InvariantCulture,
                };

                var context = new TemplateContext(new
                {
                    type.Name,
                    type.FullName,
                    type.Fields,
                    type.Methods,
                    Properties = type.Properties.Select(p => new
                    {
                        p.Name,
                        p.PropertyType,
                        p.CustomAttributes,
                        Description = GetXmlDocsDescription(p, xmlDocsOptions, xmlDocs),
                        DefaultValue = GetDefaultValue(p),
                        IsRequired = GetIsRequired(p)
                    })
                }, templateOptions);

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