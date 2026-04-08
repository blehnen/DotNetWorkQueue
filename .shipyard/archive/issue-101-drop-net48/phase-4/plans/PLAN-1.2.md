---
phase: 4-ci-docs-version
plan: "1.2"
wave: 1
dependencies: []
must_haves:
  - Remove net48 flags and windows-latest from GitHub Actions CI
  - Switch to ubuntu-latest runner
  - Convert backslash paths to forward slashes for Linux
  - Update comments to reflect net48 removal
files_touched:
  - .github/workflows/ci.yml
tdd: false
---

# Plan 1.2 -- GitHub Actions CI Update

## Context

The GitHub Actions workflow (`.github/workflows/ci.yml`) currently:
- Runs on `windows-latest` (was needed for net48)
- Uses backslash paths in all `dotnet` commands (Windows-only)
- Passes `-f net48` to 8 of 10 test steps
- Installs `dotnet-version: 10.0.100` (specific preview version)
- Has comments referencing "net48 compatibility" as the purpose

With net48 removed, the workflow should run on `ubuntu-latest` (faster, cheaper), use forward-slash paths, drop all `-f net48` flags, and run tests on `net10.0`.

## Tasks

<task id="1" files=".github/workflows/ci.yml" tdd="false">
  <action>Rewrite `.github/workflows/ci.yml` with these changes:

**Header comments** (lines 3-5): Replace with:
```
# GitHub Actions runs unit tests on ubuntu (net10.0) for CI validation.
# Integration tests run on Jenkins with Docker agents.
# See docs/jenkins-setup.md for the Jenkins pipeline configuration.
```

**Runner** (line 15): Change `runs-on: windows-latest` to `runs-on: ubuntu-latest`.

**dotnet-version** (lines 22-24): Change to:
```yaml
        with:
          dotnet-version: |
            8.0.x
            10.0.x
```

**Restore step** (line 27): Change backslash to forward slash:
```yaml
      - name: Restore
        run: dotnet restore "Source/DotNetWorkQueue.sln"
```

**Build step** (line 30): Change backslash to forward slash:
```yaml
      - name: Build
        run: dotnet build "Source/DotNetWorkQueue.sln" -c Debug --no-restore
```

**Test steps** (lines 32-63): For all 10 test steps:
1. Convert backslash paths to forward slashes
2. Remove `-f net48` flag from the 8 steps that have it (Core, RelationalDatabase, PostgreSQL, Redis, SQLite, LiteDb, SqlServer, Memory)
3. Update the comment block (lines 32-34) from "Unit tests run on net48 to validate .NET Framework compatibility" to "Unit tests run on net10.0 for CI validation"

The resulting test steps should look like:
```yaml
      # Unit tests run on net10.0 for CI validation.
      # net10.0 integration tests run on Jenkins.
      - name: Unit Tests - Core
        run: dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --no-build -c Debug

      - name: Unit Tests - RelationalDatabase
        run: dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --no-build -c Debug
```
(and so on for all 10 test steps, removing `-f net48` where present, converting `\` to `/`)
</action>
  <verify>grep -c 'net48\|windows-latest\|\\\\' .github/workflows/ci.yml</verify>
  <done>`grep -c 'net48\|windows-latest' .github/workflows/ci.yml` returns 0. `grep -c '\\\\' .github/workflows/ci.yml` returns 0 (no backslashes in paths). File uses `ubuntu-latest`, `10.0.x`, and forward-slash paths throughout. All 10 test steps have `--no-build -c Debug` without `-f net48`.</done>
</task>

## Builder Notes

- This is a single-file edit. The file is 63 lines total -- small enough to rewrite entirely if easier than making 15+ individual edits.
- The backslash-to-forward-slash conversion applies to ALL `dotnet` command paths (restore, build, and all 10 test steps).
- The two Dashboard test steps (Dashboard.Api, Dashboard.Client) did NOT have `-f net48` originally -- they only need path separator fixes.
- Use `10.0.x` wildcard instead of `10.0.100` to pick up patch releases automatically.
