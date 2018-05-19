# DNT (DotNetTools)
## Command line tools to manage .NET Core and Standard projects and solutions

[![NuGet Version](https://img.shields.io/nuget/v/DNT.svg)](https://www.nuget.org/packages?q=DNT)
[![npm](https://img.shields.io/npm/v/dotnettools.svg)](https://www.npmjs.com/package/dotnettools)

**Experimental: Command and parameter names may change**

Install via NPM (.NET 4.6.2 and .NET Core 2.1+)

```
npm i -g dotnettools
```

Install .NET Core global tool (.NET Core 2.1+ only, not ready): 

```
dotnet tool install -g dnt
```

Uninstall 

```
npm uninstall -g dotnettools
dotnet tool uninstall -g dnt
```

## Package Commands

By default, all commands search in the current directory for all `*.csproj` files and apply the command to all of them. The targeted projects or solutions can be changed with the `/path:MyProject.csproj` parameter.

To list all currently selected project, call:

```
dnt list-projects

// with path:
dnt list-projects [/path:ProjectDirectoryPath]
```

### install-packages

Installs a NuGet package in the selected projects.

**Command:**

```
dnt install-packages PackageToInstall [TargetPackageVersion] [/path:ProjectDirectoryPath]
```

TBD

### update-packages

Updates NuGet packages in the selected projects.

**Command:**

```
dnt update-packages PackagesToUpdate [TargetPackageVersion] [/path:ProjectDirectoryPath]
```

**Parameters:**

- PackagesToUpdate: The package ID to update, also supports * wildcards
- TargetPacketVersion: The targeted package version (default: latest)

**Samples:**

Update the Newtonsoft.Json packages in the selected projects to version 10.0.1:

```
dnt update-packages Newtonsoft.Json 10.0.1
```

Update all packages in the selected projects to the latest version:

```
dnt update-packages *
```

Update all packages which start with "MyPackages." in the selected projects to version 2.1.0:

```
dnt update-packages MyPackages.* 2.1.0
```

### bump-version

Increases or changes the package version of the selected projects.

**Command:**

```
dnt bump-version major|minor|patch [/major:number] [/minor:number] [/patch:number] [/path:ProjectDirectoryPath]
```

**Parameters:**

TBD

**Samples:**

Bump the minor version of all selected projects by 1:

```
dnt bump-version minor
```

Set the patch version of all selected projects to 18:

```
dnt bump-version patch /patch:18
```

### switch-to-projects

Switches from NuGet package references to local project references for refactorings, debugging, etc.

This is [NuGetReferenceSwitcher](https://github.com/RSuter/NuGetReferenceSwitcher) for .NET Core/Standard.

Idea: https://github.com/rsuter/NuGetReferenceSwitcher/wiki/Guide

Create `njs-switch.dnt` file and specify the solution to look for projects, and the NuGet packages to replace with actual projects. The involved projects are only specified by the solution path in the settings file:

```json
{
  "solution": "NSwag.sln",
  "mappings": {
    "NJsonSchema": {
      "path": "../../NJsonSchema/src/NJsonSchema/NJsonSchema.csproj"
    },
    "NJsonSchema.CodeGeneration": {
      "path": "../../NJsonSchema/src/NJsonSchema.CodeGeneration/NJsonSchema.CodeGeneration.csproj"
    },
    "NJsonSchema.CodeGeneration.CSharp": {
      "path": "../../NJsonSchema/src/NJsonSchema.CodeGeneration.CSharp/NJsonSchema.CodeGeneration.CSharp.csproj"
    },
    "NJsonSchema.CodeGeneration.TypeScript": {
      "path": "../../NJsonSchema/src/NJsonSchema.CodeGeneration.TypeScript/NJsonSchema.CodeGeneration.TypeScript.csproj"
    }
  }
}
```

Then switch to projects in the solution: 

```
dnt switch-to-projects njs-switch.dnt
```

Now all NJsonSchema package references in the NSwag solution are now replaced by local project references and the NJsonSchema projects added to the solution.

### switch-to-packages

After implementing and testing, switch back to NuGet references: 

```
dnt switch-to-packages njs-switch.dnt
```

## Solution Commands

### create-solution

TBD
