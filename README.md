# DNT (DotNetTools)
## Command line tools to manage .NET projects and solutions

**Experimental: Command and parameter names may change**

## Package Commands

### install-packages

### switch-to-projects

This is [NuGetReferenceSwitcher](https://github.com/RSuter/NuGetReferenceSwitcher) for .NET Core/Standard

### switch-to-packages

This is [NuGetReferenceSwitcher](https://github.com/RSuter/NuGetReferenceSwitcher) for .NET Core/Standard

### update-packages

**Command:**

```
dnt update-packages /package:PackagesToUpdate [/version:TargetPackageVersion]

```

**Parameters:**

- package: The package ID to update, also supports * wildcards

## Project Commands

### bump-version

**Command:**

```
dnt bump-version major|minor|patch [/major:number] [/minor:number] [/patch:number] [/path:ProjectDirectoryPath]
```

**Parameters:**

TBD

**Samples:**

```
dnt bump-version minor
```

Bumps the minor version of all selected projects by 1.

```
dnt bump-version patch /patch:18
```

Sets the patch version of all selected projects to 18.

## Solution Commands

### create-solution
