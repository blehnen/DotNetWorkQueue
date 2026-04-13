---
phase: cleanup-docs-version
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - Lib/ directory deleted entirely
  - README.md Schyntax references replaced with Cronos/cron
  - CLAUDE.md Schyntax references replaced with Cronos
  - CHANGELOG.md entry for 0.9.3
  - Version bumped to 0.9.3 in DotNetWorkQueue.csproj
  - Solution builds cleanly
files_touched:
  - Lib/ (deleted)
  - README.md
  - CLAUDE.md
  - CHANGELOG.md
  - Source/DotNetWorkQueue/DotNetWorkQueue.csproj
tdd: false
---

# Plan 1.1: Cleanup, Documentation, and Version Bump

Delete vendored Schyntax DLLs, update all documentation, bump version to 0.9.3.

## Context

All code changes are complete (Phases 1-4). This final phase cleans up vendored files, updates user-facing documentation, and bumps the version for the breaking release.

## Dependencies

None (all prior phases complete).

## Tasks

<task id="1" files="Lib/" tdd="false">
  <action>
  Delete the entire `Lib/` directory:
  ```bash
  rm -rf Lib/
  ```
  This removes `Lib/Schyntax/net8.0/` and `Lib/Schyntax/net10.0/` (the only remaining contents after issue #101 removed JpLabs and issue #102 removed Aq.ExpressionJsonSerializer).

  Verify: `ls Lib/` should fail (directory does not exist).
  </action>
  <verify>! ls Lib/ 2>/dev/null && echo "PASS: Lib/ deleted" || echo "FAIL: Lib/ still exists"</verify>
  <done>Lib/ directory and all vendored DLLs deleted.</done>
</task>

<task id="2" files="README.md, CLAUDE.md, CHANGELOG.md" tdd="false">
  <action>
  Update documentation files:

  **README.md** (3 changes):
  1. Line 91: Replace Schyntax scheduling reference with cron format. Change "Jobs may be scheduled using [Schyntax](...) format" to "Jobs may be scheduled using standard cron expressions (5-field or 6-field with seconds), parsed by [Cronos](https://github.com/HangfireIO/Cronos)."
  2. Line 99: Replace "See [Schyntax](...) for the event scheduling format" with "See [crontab.guru](https://crontab.guru/) for the cron expression format. 6-field expressions (with seconds) are also supported."
  3. Line 144: Replace "Custom libraries in `/Lib`: [Schyntax](...), [Aq.ExpressionJsonSerializer](...)" with updated dependency info. Remove Schyntax, remove Aq.ExpressionJsonSerializer (now NuGet), remove the `/Lib` reference entirely. Add Cronos and CronExpressionDescriptor to the NuGet dependencies section if one exists, or update this line.

  **CLAUDE.md** (2 changes):
  1. Line 69: Replace "Schyntax format" with "cron format" in the JobScheduler description.
  2. Line 98: Replace "Custom libraries in `/Lib`: Schyntax (scheduling), Aq.ExpressionJsonSerializer (LINQ serialization)" — remove both (both are now NuGet packages), add Cronos and CronExpressionDescriptor to Key Dependencies section above.

  **CHANGELOG.md**:
  Add entry at the top for version 0.9.3:
  ```markdown
  ## 0.9.3

  ### Breaking Changes
  - Replaced Schyntax schedule format with standard cron expressions (5-field and 6-field with seconds) using [Cronos](https://github.com/HangfireIO/Cronos)
  - `IJobSchedule.Previous()` now returns `DateTimeOffset?` (nullable) instead of `DateTimeOffset`
  - All heartbeat and job schedule strings must use cron format instead of Schyntax DSL
  - Removed vendored `/Lib` directory (Schyntax DLLs)

  ### Added
  - `IJobSchedule.Description` property — human-readable schedule descriptions via [CronExpressionDescriptor](https://github.com/bradymholt/cron-expression-descriptor)
  - Structured logging of schedule descriptions in `JobScheduler` when jobs are added
  - Auto-detection of 5-field (standard) vs 6-field (with seconds) cron expressions
  ```
  </action>
  <verify>grep -i "Schyntax" README.md CLAUDE.md; echo "Should be 0 matches (excluding Lessons Learned in CLAUDE.md if applicable)"</verify>
  <done>All documentation updated. Schyntax references replaced with Cronos/cron. CHANGELOG entry added for 0.9.3.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue/DotNetWorkQueue.csproj" tdd="false">
  <action>
  In `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`:

  1. Update `<Version>` from current value to `0.9.3`
  2. Update `<Description>` if it still references Schyntax or net48 — ensure it mentions cron scheduling

  Then run full verification:
  1. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` — 0 errors
  2. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` — 0 errors, 0 warnings
  3. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"` — all tests pass
  4. `grep -r "Schyntax" Source/ --include="*.cs" --include="*.csproj"` — 0 matches
  5. `grep -i "Schyntax" README.md CLAUDE.md` — 0 matches
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.sln" -c Release --verbosity quiet 2>&1 | tail -3 && ! grep -rq "Schyntax" Source/ --include="*.cs" --include="*.csproj" && echo "PASS" || echo "FAIL"</verify>
  <done>Version 0.9.3, solution builds clean, zero Schyntax references anywhere.</done>
</task>

## Verification

```bash
# Lib/ gone
! ls Lib/ 2>/dev/null

# Zero Schyntax in source
grep -r "Schyntax" Source/ --include="*.cs" --include="*.csproj"

# Zero Schyntax in docs
grep -i "Schyntax" README.md CLAUDE.md

# Version is 0.9.3
grep "<Version>" Source/DotNetWorkQueue/DotNetWorkQueue.csproj

# Full build
dotnet build "Source/DotNetWorkQueue.sln" -c Release

# Tests pass
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"
```
