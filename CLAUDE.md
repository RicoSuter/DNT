# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DNT (DotNetTools) is a .NET CLI tool for managing .NET Core, Standard, and SDK-style projects and solutions. It is distributed as both a NuGet global tool (`DNT`) and an NPM package (`dotnettools`).

## Git Rules

- Never include "Claude", "Co-Authored-By", or AI attribution in commit messages, PR descriptions, or GitHub comments.

## Build Commands

```bash
# Build the entire solution
dotnet build src/Dnt.slnx

# Build a specific project
dotnet build src/Dnt/Dnt.csproj
dotnet build src/Dnt.Commands/Dnt.Commands.csproj

# Run the tool locally (from repo root)
dotnet run --project src/Dnt/Dnt.csproj -- <command> [args]

# Build Release (outputs binaries to Dnt.Npm/bin/binaries/ for NPM packaging)
dotnet build src/Dnt.slnx -c Release
```

```bash
# Run tests
dotnet test src/Dnt.slnx --configuration Release
```

## Architecture

### Solution Structure (`src/Dnt.slnx`)

- **Dnt** — Entry point executable. Registers MSBuild via `MSBuildLocator`, discovers commands from `Dnt.Commands` assembly using NConsole's `RegisterCommandsFromAssembly`. Targets `net8.0;net9.0;net10.0`. Packaged as a .NET global tool (`PackAsTool`).

- **Dnt.Commands** — All command implementations. Targets `net472;net8.0;net9.0;net10.0`. Also published as a standalone NuGet library (`DNT.Commands`) for reuse in other tools.

- **Dnt.NetFx** — .NET Framework 4.7.2 build of the tool. Uses linked source files from `Dnt/` (not a project reference). Release output goes to `Dnt.Npm/bin/binaries/Win`.

- **Dnt.Npm** — NPM package wrapper. `bin/dnt.js` detects the installed .NET runtime and launches the appropriate binary.

### Command Pattern

All commands inherit from `CommandBase` (which implements NConsole's `IConsoleCommand`). Common flags: `--simulate`, `--no-parallel`, `--verbose`.

Commands that operate on projects/solutions inherit from `ProjectCommandBase`, which adds the `/path:` argument and handles resolution of `.csproj`, `.sln`, or directory scanning.

Commands are organized by domain:
- `Packages/` — NuGet operations (install, update, switch between package/project references)
- `Projects/` — Project manipulation (versioning, target frameworks, nowarn, clean)
- `Solutions/` — Solution file creation
- `Generation/` — Code generation via Liquid templates (assembly info, JSON)
- `Git/` — Git utilities (no-changes check)

### Key Dependencies

- **NConsole** — CLI argument parsing and command dispatch
- **Microsoft.Build** + **MSBuildLocator** — Project file manipulation (requires `ExcludeAssets="runtime"` since MSBuild assemblies are loaded at runtime via the locator)
- **NuGet.Client / NuGet.Protocol.Core.v3** — NuGet feed queries
- **Fluid.Core** — Liquid template engine for code generation
- **Microsoft.VisualStudio.SolutionPersistence** — Solution file (.sln/.slnx) parsing
- **Namotion.Reflection.Cecil** — Assembly reflection for code generation

### Reference Switcher

The `Packages/Switcher/` subsystem handles switching between NuGet package references and local project references (and back). Configuration is stored in JSON files (e.g., `dnt-nconsole.json`) with mappings from package IDs to local project paths.
