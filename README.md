# DNT (DotNetTools)
## Command line tools to manage .NET Core, Standard and SDK-style projects and solutions

[![NuGet Version](https://img.shields.io/nuget/v/DNT.svg)](https://www.nuget.org/packages/DNT)
[![npm](https://img.shields.io/npm/v/dotnettools.svg)](https://www.npmjs.com/package/dotnettools)

**Command and parameter names may improve or change over time. Please create issues or PRs if you'd like to fix, change or add a command.**

## Installation

### .NET Core global tool
#### Requires .NET 8.0+ and Visual Studio 2019

Install .NET Core global tool:

```
dotnet tool install -g dnt
```

Update the global tool:

```
dotnet tool update -g dnt
```

Uninstall the tool:

```
dotnet tool uninstall -g dnt
```

### NPM CLI package
#### Requires .NET Core 2.2+ or NetFX 4.7.2 and Visual Studio 2019

Globally install/update via NPM (.NET 4.6.2+ and .NET Core 2.1+):

```
npm i -g dotnettools
```

Uninstall global package:

```
npm uninstall -g dotnettools
```

## Usage

By default, all commands search in the current directory for all `*.csproj` files and apply the command to all of them. The targeted projects or solutions can be changed with the `/path:MyProject.csproj` parameter.

To list all currently selected projects, call:

```
dnt list-projects

dnt list-projects /path:MySolution.sln
```

Available commands:

- [Package Commands](#package-commands)
- [Project Commands](#project-commands)
- [Solution Commands](#solution-commands)

Possible scenarios to use the tools: 

- [Azure DevOps: A way to version and publish .NET packages from a GitHub repository](https://blog.rsuter.com/azure-devops-my-versioning-flow-to-publish-dotnet-packages-from-github-repository/)

## Package Commands

### install-packages

Installs a NuGet package in the selected projects.

**Command:**

```
dnt install-packages PackageToInstall [TargetPackageVersion] [/path:ProjectDirectoryPath]
```

**Parameters:**

- PackageToInstall
- TargetPackageVersion
- ProjectDirectoryPath

### update-packages

Updates NuGet packages in the selected projects.

**Command:**

```
dnt update-packages PackagesToUpdate [TargetPackageVersion] [/path:ProjectDirectoryPath]
```

**Parameters:**

- PackagesToUpdate: The package ID to update, also supports * wildcards
- TargetPacketVersion: The targeted package version (default: latest)
- ProjectDirectoryPath

**Samples:**

Update all packages in the selected projects to the latest version:

```
dnt update-packages *
```

Update the `Newtonsoft.Json` packages in the selected projects to version `10.0.1`:

```
dnt update-packages Newtonsoft.Json 10.0.1
```

Update all packages which start with `MyPackages.` to version `2.1.0` in the selected projects:

```
dnt update-packages MyPackages.* 2.1.0
```

### bump-versions

Increases or changes the package version of the selected projects.

**Command:**

```
dnt bump-versions major|minor|patch|revision [number]
```

**Parameters:**

- Action: Specifies the version segment to bump (major|minor|patch|revision) by 1 or by the specified number
- Number: The version to bump up to (if not specified: Increase by 1)

**Samples:**

Bump the minor version of all selected projects by 1:

```
dnt bump-versions minor
```

Set the patch version of all selected projects to 18:

```
dnt bump-versions patch 18
```

### switch-to-projects

This command switches NuGet assembly references to project references and vice-versa. This is useful when developing applications/libraries which reference own NuGet packages: When developing an application, switch to project references so that all code is editable and debuggable. After finishing the development, create new NuGet package versions, switch back to NuGet references and upgrade to the new NuGet versions.

This command supports .csproj, .vbproj, legacy and SDK-style projects (.NET Core/Standard). Previously it was implemented as a Visual Studio extension: [NuGetReferenceSwitcher](https://github.com/RSuter/NuGetReferenceSwitcher).

#### Usage

Create a `switcher.json` file and specify the solution to look for projects, and the NuGet packages to replace with actual projects. The involved projects are only specified by the solution path in the settings file.

The command supports switching to project references where the NuGet package is built from a single project or when it is built from multiple projects, i.e. the package contains multiple libraries.

**Sample - single project per package:** Here we create a switcher file for [NSwag](http://nswag.org) which references libraries of [NJsonSchema](http://njsonschema.org) to work on both projects in a single solution: 

```json
{
  "solution": "NSwag.sln",
  "mappings": {
    "NJsonSchema": "../../NJsonSchema/src/NJsonSchema/NJsonSchema.csproj",
    "NJsonSchema.CodeGeneration": "../../NJsonSchema/src/NJsonSchema.CodeGeneration/NJsonSchema.CodeGeneration.csproj",
    "NJsonSchema.CodeGeneration.CSharp": "../../NJsonSchema/src/NJsonSchema.CodeGeneration.CSharp/NJsonSchema.CodeGeneration.CSharp.csproj",
    "NJsonSchema.CodeGeneration.TypeScript": "../../NJsonSchema/src/NJsonSchema.CodeGeneration.TypeScript/NJsonSchema.CodeGeneration.TypeScript.csproj"
  }
}
```

**Sample - multiple projects per package:** Here we create a switcher file for a sample solution which references multiple libraries included (as dependencies) in the [UA-.NETStandard](https://github.com/OPCFoundation/UA-.NETStandard) [OPCFoundation.NetStandard.Opc.Ua](https://www.nuget.org/packages/OPCFoundation.NetStandard.Opc.Ua/) package in order to work on all the projects in a single solution.

```json
{
  "solution": "MyOpcUaApp.sln",
  "mappings": {
    "OPCFoundation.NetStandard.Opc.Ua": [
      "../UA-.NETStandard/Stack/Opc.Ua.Core/Opc.Ua.Core.csproj",
      "../UA-.NETStandard/Stack/Opc.Ua.Bindings.Https/Opc.Ua.Bindings.Https.csproj",
      "../UA-.NETStandard/Libraries/Opc.Ua.Security.Certificates/Opc.Ua.Security.Certificates.csproj",
      "../UA-.NETStandard/Libraries/Opc.Ua.Configuration/Opc.Ua.Configuration.csproj",
      "../UA-.NETStandard/Libraries/Opc.Ua.Client/Opc.Ua.Client.csproj",
      "../UA-.NETStandard/Libraries/Opc.Ua.Server/Opc.Ua.Server.csproj",
      "../UA-.NETStandard/Libraries/Opc.Ua.Gds.Client.Common/Opc.Ua.Gds.Client.Common.csproj",
      "../UA-.NETStandard/Libraries/Opc.Ua.Gds.Server.Common/Opc.Ua.Gds.Server.Common.csproj",
    ]
  }
}
```

Then switch to projects in the solution: 

```
dnt switch-to-projects
```

The command looks for `switcher.json` configuration file by default, but you can specify your own file:

```
dnt switch-to-projects switch-config.json
```

Now all NJsonSchema, or UA-.NETStandard, package references in the NSwag, or MyOpcUaApp, solution are replaced by local project references and the NJsonSchema, or UA-.NETStandard, projects are added to the solution.

**Optional `switcher.json` Flags:**

- removeProjects: (Default: true) Removes mapped projects from the solution and ignores mapped projects when using switch-to-packages.

### switch-to-packages

After implementing and testing, switch back to NuGet references and update to the latest version: 

```
dnt switch-to-packages
dnt update-packages NJsonSchema*
# or
dnt update-packages OPCFoundation.NetStandard.Opc.Ua.*
```

### used-packages

Lists all used packages, transitive packages in the projects and their licenses.

**Parameters:**

- ExcludeMicrosoft (default: true): Exclude packages which start with Microsoft.*
- ExcludeSystem (default: true): Exclude packages which start with System.*
- IncludeTransitiveDependencies (default: true): Also analyze transitive dependencies (i.e. indirectly referenced packages)

Sample output for [NJsonSchema](http://njsonschema.org):

```
Package                              Version   #   License   License URL
BenchmarkDotNet                      0.10.14   1   MIT       https://github.com/dotnet/BenchmarkDotNet/blob/master/LICENSE.md
BenchmarkDotNet.Core                 0.10.14   4   MIT       https://github.com/dotnet/BenchmarkDotNet/blob/master/LICENSE.md
BenchmarkDotNet.Toolchains.Roslyn    0.10.14   1   MIT       https://github.com/dotnet/BenchmarkDotNet/blob/master/LICENSE.md
DotLiquid                            2.0.254   1   Apache    http://www.apache.org/licenses/LICENSE-2.0
NBench                               1.0.4     2   Apache    https://github.com/petabridge/NBench/blob/master/LICENSE
Newtonsoft.Json                      9.0.1     4   MIT       https://raw.github.com/JamesNK/Newtonsoft.Json/master/LICENSE.md
NodaTime                             2.2.0     2   Apache    http://www.apache.org/licenses/LICENSE-2.0
Pro.NBench.xUnit                     1.0.4     1   MIT       https://raw.githubusercontent.com/Pro-Coded/Pro.NBench.xUnit/master/LICENSE
xunit                                2.3.1     7   Apache    https://raw.githubusercontent.com/xunit/xunit/master/license.txt
xunit.abstractions                   2.0.1     2   Apache    https://raw.githubusercontent.com/xunit/xunit/master/license.txt
xunit.analyzers                      0.7.0     1   Apache    https://raw.githubusercontent.com/xunit/xunit.analyzers/master/LICENSE
xunit.assert                         2.3.1     1   Apache    https://raw.githubusercontent.com/xunit/xunit/master/license.txt
xunit.core                           2.3.1     1   Apache    https://raw.githubusercontent.com/xunit/xunit/master/license.txt
xunit.extensibility.core             2.3.1     3   Apache    https://raw.githubusercontent.com/xunit/xunit/master/license.txt
xunit.extensibility.execution        2.3.1     1   Apache    https://raw.githubusercontent.com/xunit/xunit/master/license.txt
xunit.runner.visualstudio            2.3.1     6   Apache    https://raw.githubusercontent.com/xunit/xunit/master/license.txt
YamlDotNet.Signed                    5.0.1     1   MIT       https://github.com/aaubry/YamlDotNet/blob/master/LICENSE
```

### switch-assemblies-to-projects

Looks through all the projects for assembly/DLL references that could instead be project references in the same solution. 

The command does make the assumption that the assembly output of a project has the same name as the project.

```
dnt switch-assemblies-to-projects
```

Looks for references like

```
<Reference Include="ProjectA">
  <HintPath>..\ProjectA\bin\ProjectA.dll</HintPath>
</Reference>
```

And replaces with

```
<ProjectReference Include="..\ProjectA\ProjectA.csproj">
  <Project>{B12406B0-0468-4809-91E3-7991800E3ECD}</Project>
  <Name>ProjectA</Name>
</ProjectReference>
```

## Project Commands

### enable

Enables a project feature in all selected projects.

**Command:**

```
dnt enable warnaserror|xmldocs
```

**Parameters:**

- Action: Specifies the feature to enable (warnaserror|xmldocs)

**Samples:**

Handle all warnings as errors in all selected projects: 

```
dnt enable warnaserror
```

### nowarn

Adds a diagnostic id to the `<NoWarn>` tag (supports muliple ids, semicolon separated).

**Samples:**

Disable "Missing XML comment for publicly visible type or member" warnings in all selected projects:

```
dnt nowarn CS1591
```

### add-target-framework

Add another target framework to the selected projects.

**Command:**

```
dnt add-target-framework TargetFramework
```

**Parameters:**

- TargetFramework: Specifies the target framework to add

**Samples:**

Add .NET Standard 2.0 target framework to all projects:

```
dnt add-target-framework netstandard2.0
```

### clean

Deletes all /bin and /obj directories of the selected projects.

### change-versions

Replaces or sets the package version of the selected projects.  Projects must have the GeneratePackageOnBuild flag set.

**Command:**

```
dnt change-versions version [replace|force]
```

**Parameters:**

- Version: The full version number using the format 'major.minor[.patch][.revision]'. Version will be padded to three parts.
- Action: Action to perform (replace|force). replace (default) = Only set for projects with an existing version, force = Set for all projects even if version is missing or blank

**Samples:**

Replace projects with an existing version tag with 1.0.1:

```
dnt change-versions 1.0.1
```

Force set all projects to 1.2.0

```
dnt change-versions 1.2 force
```

Replace version with a long version:

```
dnt change-versions 1.2.3.4-PreRelease1 replace
```

# DNT development and testing

It is recommended to add the debug output path "DNT/src/Dnt.NetFx/bin/Debug" to the Path environment variable, or directly start the app with a command.
