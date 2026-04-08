---
phase: 4-ci-docs-version
plan: "2.1"
wave: 2
dependencies: ["1.1", "1.2", "1.3"]
must_haves:
  - Bump version from 0.9.18 to 0.9.19
  - Add CHANGELOG.md entry for 0.9.19
  - Full solution build passes (Debug and Release)
  - All unit tests pass
  - Zero grep hits for NETFULL, net48, netstandard2.0 in Source/
files_touched:
  - Source/DotNetWorkQueue/DotNetWorkQueue.csproj
  - CHANGELOG.md
  - .shipyard/ISSUES.md
tdd: false
---

# Plan 2.1 -- Version Bump, CHANGELOG, and Final Verification

## Context

This is the last plan of the last phase. All code changes, CI updates, and documentation updates are complete from Wave 1 plans. This plan bumps the version, writes the CHANGELOG entry, closes resolved issues, and runs the final verification sweep.

This plan depends on all Wave 1 plans completing first because:
- The CHANGELOG entry summarizes ALL changes made in this phase
- The version bump should be the last code change before shipping
- Final verification must run against the complete state

## Tasks

<task id="1" files="Source/DotNetWorkQueue/DotNetWorkQueue.csproj, CHANGELOG.md" tdd="false">
  <action>**Version bump** in `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`:
- Change `<Version>0.9.18</Version>` to `<Version>0.9.19</Version>`
- The `<Description>` already says "dot net 8.0 and 10.0" (confirmed in research) -- no change needed.

**CHANGELOG entry** -- Prepend the following to the top of `CHANGELOG.md` (before the existing `### 0.9.18` entry):

```markdown
### 0.9.19 — 2026-04-07
- **Breaking:** Drop .NET Framework 4.8 and .NET Standard 2.0 targets; now targets .NET 10.0 and .NET 8.0 only (GitHub #101)
- Remove dynamic LINQ expression support (was net48-only via JpLabs.DynamicCode)
- Remove vendored `Lib/JpLabs.DynamicCode` directory
- Remove `#if NETFULL` / `#if NETSTANDARD2_0` conditional compilation from all source files
- Remove vestigial `bool dynamic` parameter from `JobSchedulerTests` shared implementation and all 6 transport callers
- Delete 7 empty shell files left after NETFULL block removal
- GitHub Actions CI: switch from `windows-latest` (net48) to `ubuntu-latest` (net10.0)
- Update README and CLAUDE.md to remove net48/dynamic LINQ references

```

Commit with message "shipyard(phase-4): bump version to 0.9.19 + CHANGELOG entry (issue #101)".</action>
  <verify>cd /mnt/f/git/dotnetworkqueue && grep '<Version>0.9.19</Version>' Source/DotNetWorkQueue/DotNetWorkQueue.csproj && head -3 CHANGELOG.md</verify>
  <done>`grep` confirms version is 0.9.19. `head -3 CHANGELOG.md` shows the new entry at the top.</done>
</task>

<task id="2" files=".shipyard/ISSUES.md" tdd="false">
  <action>Update `.shipyard/ISSUES.md` to mark ISSUE-021, ISSUE-022, and ISSUE-023 as resolved:

For each issue, change `**Status:** Open` to `**Status:** Resolved — phase 4, 2026-04-07` and add a `**Resolution:**` line describing what was done:
- ISSUE-021: "Deleted all 7 empty shell files."
- ISSUE-022: "Removed `DataRow(true, true)` from PostgreSQL, `DataRow(true)` from LiteDb. Removed vestigial `bool dynamic` parameter from shared `JobSchedulerTests.Run()` and all 6 transport callers."
- ISSUE-023: "Removed stray blank line from PostgreSQL JobSchedulerTests. Removed double blank line from Memory Linq csproj."

Move all three from the `## Open` section to the `## Closed` section (above the existing closed issues, maintaining reverse-chronological order).

Commit with message "shipyard(phase-4): close ISSUE-021, ISSUE-022, ISSUE-023".</action>
  <verify>cd /mnt/f/git/dotnetworkqueue && grep -c "Status.*Open" .shipyard/ISSUES.md</verify>
  <done>ISSUE-021, ISSUE-022, ISSUE-023 all show "Resolved" status. The count of "Status.*Open" issues has decreased by 3 compared to pre-edit (was 9 open: 016-023; now 6 open: 016-020). Issues appear in the Closed section.</done>
</task>

<task id="3" files="" tdd="false">
  <action>Run the full final verification sweep. Do NOT commit -- just verify and report results:

1. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` -- expect 0 errors
2. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` -- expect 0 errors, 0 warnings
3. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --no-build -c Debug` -- all tests pass
4. `grep -r "NETFULL\|NETSTANDARD2_0" Source/ --include="*.cs" --include="*.csproj"` -- 0 matches
5. `grep -r "net48\|netstandard2.0" Source/ --include="*.csproj"` -- 0 matches
6. `grep "JpLabs\|DynamicCode" README.md` -- 0 matches
7. `grep "dynamic LINQ" README.md` -- 0 matches
8. `grep '<Version>0.9.19</Version>' Source/DotNetWorkQueue/DotNetWorkQueue.csproj` -- 1 match
9. `grep -c 'net48\|windows-latest' .github/workflows/ci.yml` -- 0 matches
10. `git status` -- clean working tree (no unstaged/untracked changes except .shipyard/ artifacts)

If any check fails, fix the issue before declaring Phase 4 complete.</action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet build "Source/DotNetWorkQueue.sln" -c Release --verbosity quiet 2>&1 | tail -3</verify>
  <done>All 10 verification checks pass. Solution builds clean in both Debug and Release. Unit tests pass. No stale references to NETFULL, net48, netstandard2.0, JpLabs, DynamicCode, or dynamic LINQ remain. Version is 0.9.19. CI workflow is clean. Phase 4 -- and the entire issue #101 effort -- is complete.</done>
</task>

## Builder Notes

- Task 1 (version + changelog) and Task 2 (issues) can be done in parallel since they touch different files.
- Task 3 (verification) must run last -- it validates the entire phase.
- The Release build is important: it enables `TreatWarningsAsErrors`, catching any unused variable or dead code warnings from the edits in Plan 1.1.
- If the Release build fails with warnings, the most likely cause is unused `using` directives in the edited JobSchedulerTests files. Fix them before re-running.
- The `--no-build` flag on `dotnet test` means it uses the Release build artifacts from step 2. If running Debug tests, rebuild in Debug first or omit `--no-build`.
- After Task 3 passes, the branch `issue-101-drop-net48` is ready for PR to master.
