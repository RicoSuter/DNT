# DNT (DotNetTools)
## Command line tools to manage .NET projects and solutions

**Experimental: Command and parameter names may change**

Install (.NET Core 2.1+ only): 

```
dotnet tool install -g dnt
```

## Package Commands

By default, all commands search in the current directory for all `*.csproj` files and applies the command to all of them. The targeted projects or solutions can be changed with the `/path:MyProject.csproj` parameter.

### install-packages

### switch-to-projects

This is [NuGetReferenceSwitcher](https://github.com/RSuter/NuGetReferenceSwitcher) for .NET Core/Standard

### switch-to-packages

This is [NuGetReferenceSwitcher](https://github.com/RSuter/NuGetReferenceSwitcher) for .NET Core/Standard

### update-packages

**Command:**

```
dnt update-packages PackagesToUpdate [TargetPackageVersion]
```

**Parameters:**

- package: The package ID to update, also supports * wildcards

**Samples:**

Update the Newtonsoft.Json packages in the selected projects to version 10.0.1:

```
dnt update-packages Newtonsoft.Json 10.0.1
```

Update all packages in the selected projects to the latest version:

```
dnt update-packages *
```

Update all packages wich start with "MyCommonPackages." in the selected projects to version 2.1.0:

```
dnt update-packages MyCommonPackages.* 2.1.0
```

## Project Commands

### bump-version

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

Sets the patch version of all selected projects to 18:

```
dnt bump-version patch /patch:18
```

## Solution Commands

### create-solution
