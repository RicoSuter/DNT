using NConsole;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Dnt.Commands.Packages
{
    [Command(Name = "used-packages", Description = "Lists all used packages and their licenses.")]
    public class ListUsedPackagesCommand : ProjectCommandBase
    {
        [Argument(Name = "ExcludeSystem", Description = "Exclude System.* packages (default: true)", IsRequired = false)]
        public bool ExcludeSystem { get; set; } = true;

        [Argument(Name = "ExcludeMicrosoft", Description = "Exclude Microsoft.* packages (default: true)", IsRequired = false)]
        public bool ExcludeMicrosoft { get; set; } = true;

        [Argument(Name = "IncludeTransitiveDependencies", Description = "Include transitive dependencies (default: true)", IsRequired = false)]
        public bool IncludeTransitiveDependencies { get; set; } = true;

        public class PackageReferenceInfo
        {
            public string Name { get; set; }

            public string Version { get; set; }

            public string Type { get; set; } = "d";

            public int Count { get; set; } = 1;

            public Uri LicenseUri { get; set; }

            public string License { get; set; }
        }

        public override async Task<object> RunAsync(CommandLineProcessor processor, IConsoleHost host)
        {
            var packages = new List<PackageReferenceInfo>();

            LoadProjects(host, packages);
            await LoadAllDependenciesAsync(packages, host);
            await LoadLicensesAsync(packages, host);

            host.WriteMessage($"{"Package",-85} {"Version",-22} {"Type",-5} {"#",-3} {"License",-15} {"License URL"}\n");
            foreach (var entry in packages.OrderBy(p => p.Name).ThenBy(p => p.Version))
            {
                host.WriteMessage($"{entry.Name,-85} {entry.Version,-22} {entry.Type,-5} {entry.Count,-3} {entry.License,-15} {entry.LicenseUri,-10}\n");
            }

            return Task.FromResult<object>(null);
        }

        private void LoadProjects(IConsoleHost host, List<PackageReferenceInfo> packages)
        {
            host.WriteMessage($"Loading projects ");

            var globalProperties = TryGetGlobalProperties();

            foreach (var projectPath in GetProjectPaths())
            {
                try
                {
                    using (var projectInformation = ProjectExtensions.LoadProject(projectPath, globalProperties))
                    {
                        foreach (var item in projectInformation.Project.Items.Where(i => i.ItemType == "PackageReference").ToList())
                        {
                            var packageName = item.EvaluatedInclude;
                            var packageVersion = item.GetVersion() ?? GetCentralPackageVersion(projectInformation, packageName) ?? "Latest";

                            if (packageName != "NETStandard.Library" &&
                                (!ExcludeSystem || !packageName.StartsWith("System.")) &&
                                (!ExcludeMicrosoft || !packageName.StartsWith("Microsoft.")))
                            {
                                var entry = packages.SingleOrDefault(p => p.Name == packageName &&
                                                                          p.Version == packageVersion);

                                if (entry == null)
                                {
                                    entry = new PackageReferenceInfo { Name = packageName, Version = packageVersion, Type = "t" };
                                    packages.Add(entry);
                                }
                                else
                                {
                                    entry.Count++;
                                }

                                host.WriteMessage(".");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    host.WriteError(e + "\n");
                }
            }

            host.WriteMessage($"\n");

            string GetCentralPackageVersion(ProjectInformation projectInformation, string packageName)
                => projectInformation.Project.Items.SingleOrDefault(i => i.ItemType == "PackageVersion" && i.EvaluatedInclude == packageName)?.GetVersion();
        }

        private async Task LoadAllDependenciesAsync(List<PackageReferenceInfo> packages, IConsoleHost host)
        {
            host.WriteMessage($"Load dependencies ");

            var tasks = new List<Task>();
            foreach (var entry in packages.ToList())
            {
                tasks.Add(LoadDependenciesAsync(entry, packages, tasks, host));
            }

            while (true)
            {
                List<Task> list = null;
                lock (packages)
                {
                    list = tasks.ToList();
                }

                await Task.WhenAll(list);

                lock (packages)
                {
                    if (tasks.All(t => t.IsCompleted))
                        break;
                }
            }

            host.WriteMessage($"\n");
        }

        private async Task LoadDependenciesAsync(PackageReferenceInfo entry, List<PackageReferenceInfo> packages, List<Task> tasks, IConsoleHost host)
        {
            var resource = await CreatePackageMetadataResourceAsync();

            try
            {
                var identity = new PackageIdentity(entry.Name, NuGetVersion.Parse(entry.Version));
                var result = await resource.GetMetadataAsync(identity, new NullLogger(), CancellationToken.None);

                if (result != null)
                {
                    entry.LicenseUri = result.LicenseUrl;

                    if (IncludeTransitiveDependencies)
                    {
                        foreach (var dependentPackage in result.DependencySets.SelectMany(s => s.Packages))
                        {
                            lock (packages)
                            {
                                if (dependentPackage.Id != "NETStandard.Library" &&
                                    (!ExcludeSystem || !dependentPackage.Id.StartsWith("System.")) &&
                                    (!ExcludeMicrosoft || !dependentPackage.Id.StartsWith("Microsoft.")))
                                {
                                    var subEntry = packages.SingleOrDefault(p => p.Name == dependentPackage.Id &&
                                                                                 p.Version == dependentPackage.VersionRange.MinVersion.ToFullString());

                                    if (subEntry == null)
                                    {
                                        var package = new PackageReferenceInfo { Name = dependentPackage.Id, Version = dependentPackage.VersionRange.MinVersion.ToFullString() };
                                        packages.Add(package);

                                        tasks.Add(LoadDependenciesAsync(package, packages, tasks, host));
                                    }
                                    else
                                    {
                                        subEntry.Count++;
                                    }
                                }
                            }
                        }
                    }
                }

                host.WriteMessage(".");
            }
            catch (Exception e)
            {
                host.WriteError(e + "\n");
            }
        }

        private async Task LoadLicensesAsync(List<PackageReferenceInfo> packages, IConsoleHost host)
        {
            var resource = await CreatePackageMetadataResourceAsync();

            host.WriteMessage($"Loading licenses ");

            var httpClient = new HttpClient();
            var tasks = new List<Task>();
            foreach (var entry in packages.ToList())
            {
                tasks.Add(LoadLicenseAsync(httpClient, entry, host));
            }

            await Task.WhenAll(tasks);
            host.WriteMessage($"\n\n");
        }

        private async Task<PackageMetadataResource> CreatePackageMetadataResourceAsync()
        {
            var packageFeed = "https://api.nuget.org/v3/index.json";

            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());

            var packageSource = new PackageSource(packageFeed);
            var sourceRepository = new SourceRepository(packageSource, providers);
            var resource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();
            return resource;
        }

        private static async Task LoadLicenseAsync(HttpClient httpClient, PackageReferenceInfo entry, IConsoleHost host)
        {
            try
            {
                if (entry.LicenseUri != null)
                {
                    var licenseUri = entry.LicenseUri?.ToString();
                    if (licenseUri?.Contains("apache.org") == true)
                    {
                        entry.License = "Apache";
                    }
                    if (licenseUri?.Contains("/LGPL-") == true)
                    {
                        entry.License = "LGPL";
                    }
                    if (licenseUri?.Contains("/GPL-") == true)
                    {
                        entry.License = "GPL";
                    }
                    else
                    {
                        var license = await httpClient.GetStringAsync(entry.LicenseUri);
                        if (license.Contains("www.apache.org"))
                        {
                            entry.License = "Apache";
                        }
                        else if (license.Contains("Lesser General Public License"))
                        {
                            entry.License = "LGPL";
                        }
                        else if (license.Contains("General Public License"))
                        {
                            entry.License = "GPL";
                        }
                        else
                        {
                            var n = "";

                            n = string.IsNullOrWhiteSpace(n) ?
                                Regex.Matches(license, "^\\s*([^><*]{1,20}[a-zA-Z] License)", RegexOptions.Multiline)
                                    .OfType<Match>().FirstOrDefault(m => !m.Groups[1].Value.Contains("the License"))?.Groups[1].Value : n;
                            n = string.IsNullOrWhiteSpace(n) ?
                                Regex.Matches(license, ">\\s*([^><*]{1,20}[a-zA-Z] License)", RegexOptions.Multiline)
                                    .OfType<Match>().FirstOrDefault(m => !m.Groups[1].Value.Contains("the License"))?.Groups[1].Value : n;
                            n = string.IsNullOrWhiteSpace(n) ?
                                Regex.Matches(license, "\n\\s*([^><*]{1,20}[a-zA-Z] License)", RegexOptions.Multiline)
                                    .OfType<Match>().FirstOrDefault(m => !m.Groups[1].Value.Contains("the License"))?.Groups[1].Value : n;

                            n = n ?? "";

                            entry.License = n
                                .Trim('\r', '\n', ' ', '?')
                                .Replace("The ", string.Empty)
                                .Replace(" License", string.Empty);
                        }
                    }

                    host.WriteMessage(".");
                }
            }
            catch (Exception e)
            {
                host.WriteError(e + "\n");
            }
        }
    }
}
