# Roadmap: Publish Aq.ExpressionJsonSerializer as NuGet Package (issue #102)

## Overview

Two-repo project to publish the vendored Aq.ExpressionJsonSerializer as a proper NuGet package, then replace the bundled DLL references in DotNetWorkQueue. This eliminates NuGet Package Explorer health warnings and enables Source Link debugging for consumers.

## Dependency Graph

```
Phase 1 (expression-json-serializer repo)
    |
    v
--- MANUAL GATE: push v1.0.0 tag, verify nuget.org publish ---
    |
    v
Phase 2 (DotNetWorkQueue repo)
```

Strictly sequential. Phase 2 cannot begin until the package is live on nuget.org.

---

## Phase 1: Prepare Fork for NuGet Publishing

**Repo:** `F:\Git\expression-json-serializer` (github.com/blehnen/expression-json-serializer)
**Risk:** Low -- no consumers yet; mistakes can be corrected with a v1.0.1 patch.
**Scope:** ~40% of total project effort.

### What Changes

1. **Merge upstream** -- Merge `upstream/master` (aquilae/expression-json-serializer) to incorporate loop and goto expression support (2 commits, 69 lines across 5 files). Clean merge expected — fork changes are in csproj/tests only.
2. **Update csproj** -- Add NuGet metadata (PackageId `DotNetWorkQueue.Aq.ExpressionJsonSerializer`, version `1.0.0`, license, repository URL, description, readme), enable deterministic build, Source Link, XML doc generation, `.snupkg` symbol package. Bump `Newtonsoft.Json` from `13.0.1` to `13.0.4` to align with DotNetWorkQueue. Keep assembly name and root namespace as `Aq.ExpressionJsonSerializer` (unchanged).
3. **Add GitHub Actions CI** -- Workflow at `.github/workflows/ci.yml`: build + test on PR/push to `main`, publish to nuget.org on `v*` tag using `NUGET_API_KEY` secret.
4. **Add Jenkinsfile** -- Build + test on all 4 TFMs (`net10.0`, `net8.0`, `net48`, `netstandard2.0`) for internal CI. Follow the pattern from DotNetWorkQueue's Jenkinsfile (agent label `docker`, environment variables `DOTNET_CLI_TELEMETRY_OPTOUT`, `DOTNET_NOLOGO`).

### Files Touched (in expression-json-serializer repo)

- `Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj`
- `.github/workflows/ci.yml` (new)
- `Jenkinsfile` (new)

### Success Criteria

1. `dotnet build Aq.ExpressionJsonSerializer.sln -c Release` succeeds on all 4 TFMs with no warnings
2. `dotnet test Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj` -- all tests pass
3. `dotnet pack Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj -c Release` produces `DotNetWorkQueue.Aq.ExpressionJsonSerializer.1.0.0.nupkg` and `.snupkg`
4. NuGet Package Explorer shows: XML docs present, Source Link valid, deterministic build, no health warnings
5. GitHub Actions workflow file passes `actionlint` or manual review
6. Jenkinsfile is syntactically valid

---

## Manual Gate: Publish to nuget.org

**Owner:** User (Brian Lehnen)
**Prerequisite:** Phase 1 complete and merged to `main`.

### Steps

1. Create `NUGET_API_KEY` secret in the `expression-json-serializer` GitHub repository settings
2. Push a `v1.0.0` tag to `main`: `git tag v1.0.0 && git push origin v1.0.0`
3. Verify GitHub Actions publish job completes successfully
4. Verify `DotNetWorkQueue.Aq.ExpressionJsonSerializer` v1.0.0 appears on [nuget.org](https://www.nuget.org/packages/DotNetWorkQueue.Aq.ExpressionJsonSerializer/)
5. Verify Source Link works: `sourcelink test DotNetWorkQueue.Aq.ExpressionJsonSerializer.1.0.0.nupkg` or confirm in NuGet Package Explorer

### Gate Criteria

Package is listed on nuget.org and `dotnet add package DotNetWorkQueue.Aq.ExpressionJsonSerializer --version 1.0.0` succeeds.

---

## Phase 2: Swap DotNetWorkQueue to PackageReference

**Repo:** `F:\Git\dotnetworkqueue` (this repo)
**Risk:** Medium -- touches the core project's build; any mistake breaks all downstream projects. Mitigated by: the change is mechanical (reference swap), and all existing tests serve as verification.
**Scope:** ~60% of total project effort (more verification surface area).

### What Changes

1. **Add to Central Package Management** -- Add `<PackageVersion Include="DotNetWorkQueue.Aq.ExpressionJsonSerializer" Version="1.0.0" />` to `Source/Directory.Packages.props`.
2. **Replace references in csproj** -- In `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`:
   - Remove all 4 per-TFM `<Reference Include="Aq.ExpressionJsonSerializer">` + `<HintPath>` blocks (from the `net8.0`, `net10.0`, `net48`, and `netstandard2.0` conditional ItemGroups)
   - Remove all 4 `<_PackageFiles Include="..\..\Lib\Aq.ExpressionJsonSerializer\...">` entries from the `IncludeVendoredDllsInPack` target
   - Add a single `<PackageReference Include="DotNetWorkQueue.Aq.ExpressionJsonSerializer" />` in the unconditional `<ItemGroup>` alongside the other PackageReferences
3. **Delete vendored DLLs** -- Remove `Lib/Aq.ExpressionJsonSerializer/` directory entirely (12 files across 4 TFM subdirectories + README.md).

### Files Touched (in DotNetWorkQueue repo)

- `Source/Directory.Packages.props`
- `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`
- `Lib/Aq.ExpressionJsonSerializer/` (deleted)

### Success Criteria

1. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds with no errors
2. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` succeeds with no warnings (TreatWarningsAsErrors)
3. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"` -- all unit tests pass
4. `dotnet pack "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" -c Release` produces a valid nupkg
5. NuGet Package Explorer on the DotNetWorkQueue nupkg shows `DotNetWorkQueue.Aq.ExpressionJsonSerializer` as a proper package dependency (not a bundled DLL) with no health warnings
6. `Lib/Aq.ExpressionJsonSerializer/` directory no longer exists
7. Full CI (Jenkins + GitHub Actions) passes

---

## Risk Summary

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Newtonsoft.Json version mismatch | Low | High | Fork pinned to 13.0.4, same as DotNetWorkQueue |
| nuget.org publish failure | Low | Blocks Phase 2 | Validate locally with `dotnet nuget push --source https://api.nuget.org/v3/index.json --dry-run` first |
| Assembly name change breaks runtime | N/A | N/A | Assembly name stays `Aq.ExpressionJsonSerializer` -- no change |
| net48 build breaks on Linux CI | Low | Medium | GitHub Actions uses `windows-latest` for net48; Jenkins Docker image has net48 targeting pack |
