# DNT (DotNetTools) — fork

This repository is a maintained fork of the original DNT CLI with a focus on day‑to‑day solution maintenance. The fork keeps the same command surface but updates the internals to better handle **solution folders** and modern SDK-style layouts.

## Why this fork exists

The upstream tool did not fully support solution folders, which made some commands skip projects or resolve paths incorrectly. This fork updates the solution parsing so commands work as expected in solutions that organize projects into folders.

## What you can do with DNT

DNT is a CLI toolbox for batch operations across .NET projects and solutions:

- Manage NuGet packages (install, update, list).
- Bump or set package versions across many projects.
- Toggle features like `TreatWarningsAsErrors` and XML docs.
- Switch between NuGet references and project references.
- Analyze used packages and licenses.

## Installation

### .NET global tool (recommended)

```
dotnet tool install -g dnt
```

Update the tool:

```
dotnet tool update -g dnt
```

Uninstall the tool:

```
dotnet tool uninstall -g dnt
```

### NPM package

```
npm i -g dotnettools
```

Uninstall:

```
npm uninstall -g dotnettools
```

## Usage basics

By default, commands operate on all `*.csproj` files found under the current directory. You can narrow the scope to a specific project directory or solution:

```
dnt list-projects

dnt list-projects /path:MySolution.sln
```

### Common commands

```
# Packages

dnt install-packages PackageId [Version] [/path:ProjectOrSolution]

dnt update-packages PackageId [Version] [/path:ProjectOrSolution]

# Versions

dnt bump-versions major|minor|patch|revision [number]

dnt change-versions 1.2.3 [replace|force]

# Project settings

dnt enable warnaserror|xmldocs

dnt nowarn CS1591

dnt add-target-framework netstandard2.0

# Solution/project switching

dnt switch-to-projects

dnt switch-to-packages
```

## Solution folder support

When you target a solution (`/path:MySolution.sln`), this fork will traverse all projects even if they are organized inside solution folders. If you were previously seeing missing projects, re-run with the same command and it should now pick them up correctly.

## Use this fork with an existing DNT installation

If you are using the official DNT tool but want the solution-folder fixes from this fork, you can compile this repository and replace the command assembly in your local tool cache. On Windows, copy `Dnt.Commands.dll` and `Dnt.Commands.pdb` into:

```
C:\Users\{{userName}}\.dotnet\tools\.store\dnt\3.0.0\dnt\3.0.0\tools\net10.0\any
```

## Contributing

Issues and PRs are welcome. If you report a bug, include the exact command, your `.sln` layout, and the output you received so we can reproduce it quickly.

## License

See [LICENSE.md](LICENSE.md).
