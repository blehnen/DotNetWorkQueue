# Project: Drop net48/netstandard2.0 and Remove JpLabs.DynamicCode (issue #101)

## Description

JpLabs.DynamicCode is a vendored DLL used only for dynamic LINQ expression support on .NET Framework 4.8, guarded by `#if NETFULL`. The source code is no longer available on the web. With the employer's .NET 10 migration removing the net48 blocker, this milestone drops net48 and netstandard2.0 targets entirely, removes all conditional compilation blocks (`#if NETFULL`, `#if NETSTANDARD2_0`), and deletes the JpLabs vendored DLL.

This is a breaking change — version bumps to 0.9.3. The employer stays on the current version until their .NET 10 migration completes.

## Goals

1. Remove `net48` and `netstandard2.0` from all `TargetFrameworks` across the solution (~40+ csproj files)
2. Delete `Lib/JpLabs.DynamicCode/` (DLL, PDB, README)
3. Remove all `#if NETFULL` code blocks (dynamic LINQ, SoapFormatter, GetObjectData) — ~186 occurrences across ~127 files
4. Remove all `#if NETSTANDARD2_0` / `#if !NETFULL` conditional blocks (keep the modern code path only)
5. Remove `NETFULL` and `NETSTANDARD2_0` from any `DefineConstants`
6. Remove `Lib/Schyntax/net48/` and `Lib/Schyntax/netstandard2.0/` (keep net8.0 + net10.0 only)
7. Update `_PackageFiles` in DotNetWorkQueue.csproj to drop net48/netstandard2.0 Schyntax entries
8. Remove net48 from GitHub Actions CI matrix (no more windows-latest leg)
9. Update README.md to remove dynamic LINQ references
10. Bump version to 0.9.3

## Non-Goals

- Schyntax NuGet publishing (issue #100 — separate milestone)
- Functional changes — this is purely dropping dead targets and dead code
- Changing the expression-json-serializer NuGet package (it keeps all 4 TFMs for other consumers)
- Updating the wiki (separate project)

## Requirements

### Target Framework Removal
- All csproj files: remove `net48` and `netstandard2.0` from `TargetFrameworks`
- Remaining targets: `net10.0;net8.0` only
- Remove any TFM-conditional `<ItemGroup>` or `<PropertyGroup>` blocks for net48/netstandard2.0

### Conditional Compilation Cleanup
- Remove all `#if NETFULL` blocks and their contents (dead code)
- Remove all `#if NETSTANDARD2_0` / `#if !NETFULL` guards — keep the `#else` (modern) branch as the only path
- Remove `NETFULL` / `NETSTANDARD2_0` from `DefineConstants` in any csproj
- Remove `CompileException.cs` if it's only used for dynamic LINQ compilation errors

### Vendored DLL Cleanup
- Delete `Lib/JpLabs.DynamicCode/` entirely
- Delete `Lib/Schyntax/net48/` and `Lib/Schyntax/netstandard2.0/`
- Update `_PackageFiles` in DotNetWorkQueue.csproj

### CI Updates
- Remove net48 leg from GitHub Actions CI matrix (`.github/workflows/ci.yml`)
- No Jenkinsfile changes needed (already runs net10.0 only)

### Documentation
- Update README.md to remove references to dynamic LINQ support
- Do NOT update the wiki (separate effort)

## Non-Functional Requirements

- All existing tests must pass on net10.0 and net8.0 after removal
- Solution must build cleanly in both Debug and Release configurations
- No orphaned files or dead references left behind

## Success Criteria

1. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` — 0 errors
2. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` — 0 errors, 0 warnings
3. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"` — all tests pass
4. `grep -r "NETFULL\|NETSTANDARD2_0" Source/ --include="*.cs"` — 0 matches
5. `grep -r "net48\|netstandard2.0" Source/ --include="*.csproj"` — 0 matches
6. `Lib/JpLabs.DynamicCode/` does not exist
7. `Lib/Schyntax/net48/` and `Lib/Schyntax/netstandard2.0/` do not exist
8. GitHub Actions CI passes without net48 leg

## Constraints

- Breaking change — version 0.9.3
- ~127 files affected, ~40+ csproj files — needs phased approach
- More breaking changes coming in future PRs before a release
- Employer stays on current version — no migration pressure
