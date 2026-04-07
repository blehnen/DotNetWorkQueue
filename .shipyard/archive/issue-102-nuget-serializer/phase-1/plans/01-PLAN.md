---
phase: prepare-fork-for-nuget
plan: 01
wave: 1
dependencies: []
must_haves:
  - Upstream loop/goto support merged into fork
  - NuGet metadata in csproj (PackageId, version, license, Source Link, snupkg)
  - Newtonsoft.Json bumped to 13.0.4
  - Test project TFMs updated to match library TFMs
  - GitHub Actions CI with matrix build and NuGet publish on tag
  - Jenkinsfile for local CI (net10.0 + net8.0)
files_touched:
  # ALL paths below are in /mnt/f/Git/expression-json-serializer (the fork repo)
  - Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj
  - Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj
  - .github/workflows/ci.yml
  - Jenkinsfile
tdd: false
---

# Plan 01 -- Prepare Fork for NuGet Publishing

**Target repo**: `/mnt/f/Git/expression-json-serializer` (the fork, NOT dotnetworkqueue)

All file paths in this plan are relative to `/mnt/f/Git/expression-json-serializer`.

## Pre-flight

Before starting any task, verify the fork state:

```bash
cd /mnt/f/Git/expression-json-serializer
git status          # should be clean, on master
git fetch upstream  # ensure upstream/master is current
```

---

<task id="1" files="Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj, Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj" tdd="false">
  <action>
  **Step 1a: Merge upstream.** From the fork's `master` branch, run `git merge upstream/master`. This brings in 2 commits (57408b3, 28a4470) adding loop and goto expression support -- 69 lines across 5 files. The merge should be clean since the fork's changes (build targets, TFMs, Newtonsoft bump) do not touch the same lines. If conflicts arise in Deserializer.cs (the only overlapping file), they will be in the `switch` statement where new cases were added -- accept both sides.

  **Step 1b: Update main csproj with NuGet metadata.** Edit `Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj` to add the following inside a new `PropertyGroup`:
  - `PackageId` = `DotNetWorkQueue.Aq.ExpressionJsonSerializer`
  - `Version` = `1.0.0`
  - `Authors` = `Brian Lehnen`
  - `Description` = `Expression tree JSON serializer for Newtonsoft.Json. Fork of aquilae/expression-json-serializer with .NET 10/8/4.8/Standard 2.0 support.`
  - `PackageLicenseExpression` = `MIT`
  - `PackageProjectUrl` = `https://github.com/blehnen/expression-json-serializer`
  - `RepositoryUrl` = `https://github.com/blehnen/expression-json-serializer.git`
  - `RepositoryType` = `git`
  - `PackageReadmeFile` = `README.md`
  - `GenerateDocumentationFile` = `true`
  - `Deterministic` = `true`
  - `IncludeSymbols` = `true`
  - `SymbolPackageFormat` = `snupkg`
  - `PublishRepositoryUrl` = `true`
  - `EmbedUntrackedSources` = `true`

  Keep `TargetFrameworks`, assembly name, and root namespace unchanged.

  Add Source Link package reference:
  ```xml
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All" />
  ```

  Bump existing Newtonsoft.Json reference from `13.0.1` to `13.0.4`.

  Add an `ItemGroup` for the README to be included in the package:
  ```xml
  <ItemGroup>
    <None Include="..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>
  ```

  **Step 1c: Update test project TFMs.** The test project currently targets `netcoreapp3.1;net48`. Update `Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj`:
  - Change `TargetFrameworks` from `netcoreapp3.1;net48` to `net10.0;net8.0;net48`
  - Remove the 4 conditional `DefineConstants` PropertyGroups (they reference `netcoreapp3.1` conditions that no longer apply)
  - Update `Microsoft.NET.Test.Sdk` from `17.0.0` to `17.12.0`
  - Update `xunit` from `2.4.1` to `2.9.3`
  - Update `xunit.runner.visualstudio` from `2.4.3` to `2.9.3`
  - Update `xunit.runner.console` from `2.4.1` to `2.9.3`
  - Bump `Newtonsoft.Json` from `13.0.1` to `13.0.4`
  </action>
  <verify>cd /mnt/f/Git/expression-json-serializer && git log --oneline -3 | grep -q "loop\|goto\|Merge" && dotnet build Aq.ExpressionJsonSerializer.sln -c Debug && dotnet test Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj -f net10.0 --no-build -c Debug && dotnet pack Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj -c Release --no-build 2>&1 | head -5 || dotnet pack Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj -c Release</verify>
  <done>
  1. `git log` shows upstream merge commit with loop/goto support present.
  2. `dotnet build` succeeds for all TFMs (net10.0, net8.0, net48, netstandard2.0).
  3. `dotnet test -f net10.0` passes all existing tests.
  4. `dotnet pack -c Release` produces `DotNetWorkQueue.Aq.ExpressionJsonSerializer.1.0.0.nupkg` and `.snupkg` in `bin/Release/`.
  5. The csproj contains all NuGet metadata fields listed above.
  </done>
</task>

<task id="2" files=".github/workflows/ci.yml" tdd="false">
  <action>
  Create `.github/workflows/ci.yml` with the following structure:

  **Triggers**: `push` to `master` and `main` branches; `pull_request` to `master` and `main`; `push` of `v*` tags.

  **Job 1: `build-and-test`** (runs on PR and push)
  - Strategy matrix with 2 entries:
    - `{ os: ubuntu-latest, tfm: net10.0 }` with `dotnet-version: ['8.0.x', '10.0.100']`
    - `{ os: ubuntu-latest, tfm: net8.0 }` with `dotnet-version: ['8.0.x', '10.0.100']`
    - `{ os: windows-latest, tfm: net48 }` with `dotnet-version: ['8.0.x', '10.0.100']`
  - Note: `netstandard2.0` has no test TFM -- it is validated by the build step (the library builds for netstandard2.0, but tests cannot target it directly). The build step covers it.
  - Steps:
    1. `actions/checkout@v4`
    2. `actions/setup-dotnet@v4` with both SDK versions (10.0.100 and 8.0.x) -- both are needed because the library multi-targets
    3. `dotnet restore`
    4. `dotnet build Aq.ExpressionJsonSerializer.sln -c Debug --no-restore`
    5. `dotnet test Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj -f ${{ matrix.tfm }} --no-build -c Debug`

  **Job 2: `publish`** (runs only on `v*` tag push)
  - `needs: build-and-test`
  - `runs-on: ubuntu-latest`
  - Steps:
    1. `actions/checkout@v4`
    2. `actions/setup-dotnet@v4` with `dotnet-version: ['8.0.x', '10.0.100']`
    3. `dotnet restore`
    4. `dotnet pack Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj -c Release --no-restore`
    5. `dotnet nuget push` with `**/*.nupkg` and `**/*.snupkg` to `https://api.nuget.org/v3/index.json` using `${{ secrets.NUGET_API_KEY }}`

  Use `DOTNET_CLI_TELEMETRY_OPTOUT: 1` and `DOTNET_NOLOGO: true` as env vars.
  </action>
  <verify>cd /mnt/f/Git/expression-json-serializer && test -f .github/workflows/ci.yml && python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))" 2>/dev/null || (which python3 > /dev/null 2>&1 && python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))") || echo "YAML validation skipped (no pyyaml); verify manually" && grep -q "NUGET_API_KEY" .github/workflows/ci.yml && grep -q "net10.0" .github/workflows/ci.yml && grep -q "net48" .github/workflows/ci.yml && grep -q "windows-latest" .github/workflows/ci.yml</verify>
  <done>
  1. `.github/workflows/ci.yml` exists and is valid YAML.
  2. File contains `build-and-test` job with matrix entries for net10.0 (ubuntu), net8.0 (ubuntu), and net48 (windows).
  3. File contains `publish` job gated on `v*` tags, using `NUGET_API_KEY` secret.
  4. Both jobs install .NET 8 and .NET 10 SDKs.
  5. The publish job runs `dotnet pack` then `dotnet nuget push` for both `.nupkg` and `.snupkg`.
  </done>
</task>

<task id="3" files="Jenkinsfile" tdd="false">
  <action>
  Create `Jenkinsfile` in the fork repo root, following the DotNetWorkQueue pattern but significantly simplified (no integration tests, no coverage, no credentials):

  ```groovy
  pipeline {
      agent { label 'docker' }

      environment {
          DOTNET_CLI_TELEMETRY_OPTOUT = '1'
          DOTNET_NOLOGO = 'true'
      }

      stages {
          stage('Build') {
              steps {
                  sh 'dotnet restore "Aq.ExpressionJsonSerializer.sln"'
                  sh 'dotnet build "Aq.ExpressionJsonSerializer.sln" -c Debug --no-restore'
              }
          }
          stage('Test net10.0') {
              steps {
                  sh 'dotnet test "Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj" -f net10.0 --no-build -c Debug'
              }
          }
          stage('Test net8.0') {
              steps {
                  sh 'dotnet test "Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj" -f net8.0 --no-build -c Debug'
              }
          }
      }

      post {
          failure {
              echo 'Pipeline failed. Check stage logs for details.'
          }
          success {
              echo 'Pipeline completed successfully.'
          }
      }
  }
  ```

  Key decisions:
  - `agent { label 'docker' }` -- same Docker agent label as DotNetWorkQueue (has .NET 8 + .NET 10 SDKs)
  - No net48 stage -- Docker agent lacks .NET Framework targeting pack (per CONTEXT-1.md)
  - No coverage collection -- this is a small library; coverage is not a project goal
  - Sequential test stages (not parallel) -- the project is small enough that parallel adds no value
  </action>
  <verify>cd /mnt/f/Git/expression-json-serializer && test -f Jenkinsfile && grep -q "label 'docker'" Jenkinsfile && grep -q "net10.0" Jenkinsfile && grep -q "net8.0" Jenkinsfile && ! grep -q "net48" Jenkinsfile</verify>
  <done>
  1. `Jenkinsfile` exists in repo root.
  2. Uses `agent { label 'docker' }` matching the DotNetWorkQueue Docker agent.
  3. Has Build, Test net10.0, and Test net8.0 stages.
  4. Does NOT contain net48 (Docker agent limitation).
  5. Sets `DOTNET_CLI_TELEMETRY_OPTOUT` and `DOTNET_NOLOGO` environment variables.
  </done>
</task>

## Sequencing Notes

Tasks must be executed in order:
- **Task 1** must complete first because the merge changes source files, and the csproj/test updates are prerequisites for CI.
- **Task 2** depends on task 1 because the workflow references TFMs and pack targets defined in the updated csproj.
- **Task 3** depends on task 1 for the same reason (test TFMs must match).

Tasks 2 and 3 could technically run in parallel after task 1, but they are small enough that sequential execution is simpler.

## Risk Notes

- **Merge conflicts**: The upstream merge (task 1, step 1a) should be clean. The fork's changes are in csproj files and build targets; the upstream changes are in Serializer/Deserializer source files. The only shared file is `Deserializer.cs` but the changes are in different regions. If a conflict does occur, it will be in the `switch` statement in `Deserializer.cs` -- accept both sides (the fork's refactoring and the upstream's new `case` entries).
- **Test package versions**: The xunit version bump (2.4.1 to 2.9.3) is a minor risk. If tests break, try 2.8.1 as a fallback. The xunit.runner.console package may not be needed at all with modern `dotnet test` -- it can be removed if it causes issues.
- **netstandard2.0 test gap**: There is no test execution for netstandard2.0 in any CI pipeline. This is expected -- netstandard2.0 is a library target, not a test target. The build step validates compilation. Tests run on net10.0, net8.0, and net48 which cover all runtime behaviors.
