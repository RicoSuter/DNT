# DNT (DotNetTools)
## Command line tools to manage .NET Core and Standard projects and solutions

[![NuGet Version](https://img.shields.io/nuget/v/DNT.svg)](https://www.nuget.org/packages?q=DNT)
[![npm](https://img.shields.io/npm/v/dotnettools.svg)](https://www.npmjs.com/package/dotnettools)

**Experimental: Command and parameter names may change**

Install .NET Core global tool (.NET Core 2.1+ only): 

```
dotnet tool install -g dnt
```

Globally install via NPM (.NET 4.6.2+ and .NET Core 2.1+)

```
npm i -g dotnettools
```

Uninstall 

```
dotnet tool uninstall -g dnt
npm uninstall -g dotnettools
```

## Package Commands

By default, all commands search in the current directory for all `*.csproj` files and apply the command to all of them. The targeted projects or solutions can be changed with the `/path:MyProject.csproj` parameter.

To list all currently selected projects, call:

```
dnt list-projects

dnt list-projects /path:MySolution.sln
```

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
dnt bump-version major|minor|patch|revision [number]
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

This command automatically switches NuGet assembly references to project references and vice-versa. This is useful when developing applications which reference own NuGet packages: When developing an application, switch to project references so that all code is editable and debuggable. After finishing the development, create new NuGet package versions, switch back to NuGet references and upgrade to the new NuGet versions.

This is [NuGetReferenceSwitcher](https://github.com/RSuter/NuGetReferenceSwitcher) for .NET Core/Standard.

#### Usage

Create `njs-switch.dnt` file and specify the solution to look for projects, and the NuGet packages to replace with actual projects. The involved projects are only specified by the solution path in the settings file:

**Sample:** Here we create a switcher file for [NSwag](http://nswag.org) which references libraries of [NJsonSchema](http://njsonschema.org) to work on both projects in a single solution: 

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

# DNT developement and testing

It is recommended to add the debug output path "DNT/src/Dnt.NetFx/bin/Debug" to the Path environment variable, or directly start the app with a command.
