# Project: Publish Aq.ExpressionJsonSerializer as NuGet Package (issue #102)

## Description

Aq.ExpressionJsonSerializer is currently bundled as pre-compiled DLLs in `/Lib` with no XML docs, no symbols, and no Source Link. This causes NuGet Package Explorer health warnings on every DotNetWorkQueue package. The library's source code lives in a fork at `github.com/blehnen/expression-json-serializer` (local: `F:\Git\expression-json-serializer`).

This project publishes the fork as `DotNetWorkQueue.Aq.ExpressionJsonSerializer` on nuget.org, then replaces the bundled DLL reference in DotNetWorkQueue with a proper PackageReference.

## Goals

1. Publish `DotNetWorkQueue.Aq.ExpressionJsonSerializer` v1.0.0 to nuget.org with deterministic build, Source Link, XML docs, and `.snupkg` symbols
2. Set up GitHub Actions CI in the fork: build + test on PR/push, publish to nuget.org on version tag (`v*`)
3. Set up Jenkinsfile in the fork for internal CI (build + test)
4. Replace bundled DLL references in DotNetWorkQueue with a proper PackageReference
5. Remove `/Lib/Aq.ExpressionJsonSerializer/` from DotNetWorkQueue
6. Resolve NuGet Package Explorer health warnings

## Non-Goals

- Renaming the assembly or namespace (stays `Aq.ExpressionJsonSerializer`)
- Changing any source code in the serializer library
- Updating DotNetWorkQueue's own NuGet package version
- Publishing any other `/Lib` libraries (Schyntax #100, JpLabs #101 are separate)

## Requirements

### NuGet Package (expression-json-serializer repo)
- Package ID: `DotNetWorkQueue.Aq.ExpressionJsonSerializer`
- Version: `1.0.0`
- Assembly name and namespace: `Aq.ExpressionJsonSerializer` (unchanged)
- Target frameworks: `net10.0;net8.0;net48;netstandard2.0`
- Dependency: `Newtonsoft.Json` aligned to `13.0.4` (matches DotNetWorkQueue)
- Deterministic build, Source Link, XML doc generation, `.snupkg` symbol package
- Full NuGet metadata: license expression, repository URL, description, readme

### CI Pipelines (expression-json-serializer repo)
- GitHub Actions: build + test on PR/push to main, publish to nuget.org on `v*` tag
- Uses `NUGET_API_KEY` GitHub secret (user sets up before first publish)
- Jenkinsfile: build + test on all 4 TFMs

### DotNetWorkQueue Integration (this repo)
- Replace 4 per-TFM `<Reference>` + `<HintPath>` blocks with single `<PackageReference>`
- Remove `<_PackageFiles>` manual packing entries
- Add to `Directory.Packages.props` (Central Package Management)
- Delete `/Lib/Aq.ExpressionJsonSerializer/` directory

## Non-Functional Requirements

- Package must be published to nuget.org before DotNetWorkQueue can reference it
- All existing DotNetWorkQueue tests must pass after the swap

## Success Criteria

1. `DotNetWorkQueue.Aq.ExpressionJsonSerializer` v1.0.0 available on nuget.org
2. `dotnet build "Source/DotNetWorkQueue.sln"` succeeds with PackageReference (no `/Lib` DLLs)
3. All unit and integration tests pass
4. NuGet Package Explorer shows no health warnings for the serializer dependency
5. Source Link works (consumers can step into serializer code)

## Constraints

- Two-repo project: Phase 1 in `expression-json-serializer`, Phase 2 in `DotNetWorkQueue`
- Package must be published to nuget.org before Phase 2 can begin
- User must create `NUGET_API_KEY` secret in GitHub before first tag push
- Newtonsoft.Json version must align with DotNetWorkQueue (13.0.4)
