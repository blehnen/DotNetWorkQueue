# Review: Plan 01

## Verdict: MINOR_ISSUES

---

## Stage 1: Spec Compliance

**Verdict:** PASS

### Task 1: Merge upstream, update csproj with NuGet metadata, update test TFMs

- Status: PASS
- Evidence:
  - `Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj`: `PackageId`, `Version`, `Authors`, `Description`, `PackageLicenseExpression`, `PackageProjectUrl`, `RepositoryUrl`, `RepositoryType`, `PackageReadmeFile`, `GenerateDocumentationFile`, `Deterministic`, `IncludeSymbols`, `SymbolPackageFormat`, `PublishRepositoryUrl`, `EmbedUntrackedSources`, and `Microsoft.SourceLink.GitHub` all present. Newtonsoft.Json pinned to 13.0.4.
  - `Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj`: TFMs are `net10.0;net8.0;net48`. Newtonsoft.Json 13.0.4.
  - `git log` shows upstream merge commit `9edeaaa` ("Merge remote-tracking branch 'upstream/master'") followed by `dbb844f` ("shipyard(phase-1): merge upstream and add NuGet metadata").
  - `Deserializer.cs` contains `goto` case in the switch dispatch (`case "goto"`) and `loop` case (`case "loop"`), confirming the upstream loop/goto support was merged cleanly.
  - `TypeAs` test uses `#if NETFULL` guard to call `TestExpression` on .NET Framework and `Assert.ThrowsAny<Exception>` on all other TFMs — appropriate for the known JSON.NET limitation.
- Notes: `TargetFrameworks` in the library csproj includes `netstandard2.0` (in addition to net10.0, net8.0, net48). The plan did not explicitly require netstandard2.0 to be preserved, but it was present before and keeping it is conservative and correct for downstream consumers. Not a deviation.

### Task 2: GitHub Actions CI workflow

- Status: PASS
- Evidence: `.github/workflows/ci.yml` exists. Matrix has three entries: `ubuntu-latest/net10.0`, `ubuntu-latest/net8.0`, `windows-latest/net48` — matching the user decision (CONTEXT-1.md). Triggers on push to `master`/`main` and tags `v*`, and on pull_request to `master`/`main`. Both build-and-test and publish jobs install .NET 8.0.x and 10.0.100 SDKs. Publish job is gated on `startsWith(github.ref, 'refs/tags/v')` and uses `secrets.NUGET_API_KEY`. Both `.nupkg` and `.snupkg` are pushed.

### Task 3: Jenkinsfile

- Status: PASS
- Evidence: `Jenkinsfile` exists at repo root. Uses `agent { label 'docker' }`. Stages: Build, Test net10.0, Test net8.0. No net48 stage present. `DOTNET_CLI_TELEMETRY_OPTOUT` and `DOTNET_NOLOGO` set in environment block.

---

## Stage 2: Code Quality

### Critical
None.

### Minor

- **GitHub Actions: `dotnet-version` pin for .NET 10 is overly specific** — `.github/workflows/ci.yml` line 37: `10.0.100` is a specific SDK build. When .NET 10 SDK patches (e.g., 10.0.101, 10.0.200), this pin will silently install the outdated SDK or fail if that exact build is no longer available on the runner. The standard pattern for floating to the latest patch is `10.0.x`.
  - Remediation: Change `10.0.100` to `10.0.x` in both the `build-and-test` job (line 37) and the `publish` job (line 63).

- **GitHub Actions: publish job has no `fetch-depth` or tag fetch guarantee** — `.github/workflows/ci.yml` line 54: the `publish` job uses `actions/checkout@v4` without `fetch-depth: 0`. When the workflow is triggered by a `v*` tag push, the default shallow clone may not include full history needed for Source Link to embed correct commit SHAs. The `build-and-test` job has the same gap.
  - Remediation: Add `with: { fetch-depth: 0 }` to both `actions/checkout@v4` steps, or at minimum to the `publish` job's checkout step.

- **GitHub Actions: `dotnet pack` runs without `--no-restore` after an explicit restore** — `.github/workflows/ci.yml` line 69: the publish job runs `dotnet restore` then `dotnet pack --no-restore`. This is actually correct — it IS using `--no-restore`. No action needed; noted for completeness.

- **Jenkinsfile: no `--no-restore` on `dotnet build`** — `Jenkinsfile` line 11: `dotnet build ... --no-restore` IS present. Correct. However the `dotnet test` commands (lines 16, 21) use `--no-build`, which is correct since build already ran. No issue.

### Suggestions

- **`PackageReleaseNotes` metadata absent** — `Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj`: a `PackageReleaseNotes` or `PackageTags` element would improve NuGet discoverability. Not required by the spec but standard practice for published packages.
  - Remediation: Add `<PackageTags>expression;linq;json;serialization;newtonsoft</PackageTags>` and optionally `<PackageReleaseNotes>Initial fork release with .NET 10/8/4.8/Standard 2.0 support.</PackageReleaseNotes>`.

- **GitHub Actions: no `permissions` block** — Best practice for public repos is to explicitly declare `permissions: contents: read` at the job level to limit the GITHUB_TOKEN scope. Not a functional issue for this workflow since it only reads and pushes to NuGet (not GitHub), but consistent with hardened CI hygiene.

---

## Findings

### Critical
None.

### Minor
- `.github/workflows/ci.yml` lines 37 and 63: `10.0.100` should be `10.0.x` to track SDK patch releases.
- `.github/workflows/ci.yml` lines 30 and 56: `actions/checkout@v4` should add `fetch-depth: 0` for correct Source Link SHA embedding on tag-triggered publish builds.

### Positive
- NuGet metadata is complete and accurate: Source Link, snupkg symbols, deterministic build, README packaging, license expression — all present and correctly configured.
- `TypeAs` test guard (`#if NETFULL` / `Assert.ThrowsAny<Exception>`) is the right approach; it documents the known JSON.NET limitation rather than silently skipping the test.
- Upstream merge correctly incorporated loop/goto dispatch cases in `Deserializer.cs` with no conflict residue.
- Jenkinsfile is clean and minimal — only net10.0 and net8.0 stages as required, no net48 leak.
- Commit history is coherent: merge first, then metadata, then CI configs — correct ordering.
