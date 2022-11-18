using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dnt.Commands.Infrastructure;
using Fluid;
using Microsoft.Extensions.FileProviders;
using Mono.Cecil;
using Namotion.Reflection;
using NConsole;

namespace Dnt.Commands.Generation
{
    [Command(Name = "assemblygen", Description = "TBD.")]
    public class AssemblyGeneratorCommand
        : CommandBase
    {
        public class ReflectMarkdownOptions
        {
            public string AssemblyFile { get; set; }

            public string TypeName { get; set; }

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

            options.AssemblyFile = PathUtilities.ToAbsolutePath(options.AssemblyFile, directory);
            options.LiquidFile = PathUtilities.ToAbsolutePath(options.LiquidFile, directory);
            options.OutputFile = PathUtilities.ToAbsolutePath(options.OutputFile, directory);

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
                    FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(options.LiquidFile))
                };

                var context = new TemplateContext(GetTypeModel(type, xmlDocsOptions, xmlDocs), templateOptions);
                var output = template.Render(context);

                if (!string.IsNullOrEmpty(options.Placeholder))
                {
                    var currentContent = File.ReadAllText(options.OutputFile);
                    var firstIndex = currentContent.IndexOf(options.Placeholder);
                    var secondIndex = currentContent.IndexOf(options.Placeholder, firstIndex + 1);

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
                    Console.WriteLine($"Updated {options.OutputFile} in {nameof(AssemblyGeneratorCommand)}.");
                }
            }
            else
            {
                Console.WriteLine($"Error in {nameof(AssemblyGeneratorCommand)}: {error}");
            }

            return Task.FromResult<object>(null);
        }

        private object GetTypeModel(TypeDefinition type, XmlDocsOptions xmlDocsOptions, XDocument xmlDocs)
        {
            return new
            {
                type.Name,
                type.FullName,
                type.IsClass,

                type.Methods,

                Fields = type
                    .Fields
                    .Select(p => new
                    {
                        p.Name,
                        PropertyType = GetTypeModel(p.FieldType.Resolve(), xmlDocsOptions, xmlDocs),
                        p.CustomAttributes,
                        Description = GetXmlDocsDescription(p, xmlDocsOptions, xmlDocs),
                        DefaultValue = GetDefaultValue(p),

                        IsRequired = GetIsRequired(p)
                    }),

                Properties = type
                    .Properties
                    .Select(p => new
                    {
                        p.Name,
                        PropertyType = GetTypeModel(p.PropertyType.Resolve(), xmlDocsOptions, xmlDocs),
                        p.CustomAttributes,
                        Description = GetXmlDocsDescription(p, xmlDocsOptions, xmlDocs),
                        DefaultValue = GetDefaultValue(p),

                        IsRequired = GetIsRequired(p)
                    })
            };
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