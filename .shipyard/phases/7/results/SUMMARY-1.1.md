# Build Summary: Plan 1.1 (Phase 7 Wave 1 — csproj fixes + per-project XML-doc verification)

## Status: complete

## Tasks Completed

- Task 1: Added `Release|net8.0|AnyCPU` PropertyGroup block (+7 lines) to `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj` mirroring the existing `Release|net10.0` block. Closes the net8.0 XML-doc gate gap surfaced in RESEARCH §2.
- Task 2: Added `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to all 3 Release PropertyGroup blocks in `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj`. Closes ISSUE-032 inline so the full-solution Release build does not fail on the pre-existing OpenTelemetry advisory.
- Task 3: Verified per-project Release build clean on all 4 outbox-touching csprojs with `-c Release -p:CI=true`. All projects: 0 errors, 0 CS1591 warnings. Confirms RESEARCH §1 finding (zero XML-doc gaps on Phase 2-4 public types).

## Commits

| SHA | Task | Subject |
|---|---|---|
| `ba41fef0` | 1 | `shipyard(phase-7): add Release|net8.0 DocumentationFile block to Transport.RelationalDatabase.csproj` |
| `88ff8996` | 2 | `shipyard(phase-7): add WarningsNotAsErrors NU1902 to Transport.SQLite.csproj (closes ISSUE-032)` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj` — +7 lines (new `Release|net8.0|AnyCPU` PropertyGroup)
- `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` — +3 lines (one `<WarningsNotAsErrors>` per Release PropertyGroup block)

## Decisions Made

- **ISSUE-032 closure scope:** PLAN-1.1 Task 2 chose the architect-specified option B (add `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to ALL three Release PropertyGroup blocks: `Release|net10.0`, `Release|net8.0`, `Release|AnyCPU`). Applying to all blocks ensures any Release variant tolerates the advisory uniformly. Alternative considered: a single top-level PropertyGroup application — rejected because the existing csproj structure scopes warnings per-config.
- **No XML doc authoring required.** Per RESEARCH §1, all Phase 2-4 public types already had complete XML doc comments (builders added them as they went). Task 3 verified this empirically: 0 CS1591 warnings across all 4 outbox projects. The Phase 7 "XML docs" goal was reframed as a verification pass, not an authoring pass.

## Verification Results

| Project | Result | CS1591 | Errors |
|---|---|---|---|
| `DotNetWorkQueue.Transport.Shared` | Build succeeded | 0 | 0 |
| `DotNetWorkQueue.Transport.RelationalDatabase` | Build succeeded | 0 | 0 |
| `DotNetWorkQueue.Transport.SqlServer` | Build succeeded | 0 | 0 |
| `DotNetWorkQueue.Transport.PostgreSQL` | Build succeeded | 0 | 0 |

All warnings present were NU1902 OpenTelemetry advisories only (ISSUE-032 baseline carry-forward; now non-fatal post-Task-2). Zero XML-doc warnings confirms the Phase 7 success criterion is satisfied at the per-project level.

## Issues Encountered

- WSL CRLF warnings on csproj edits (CLAUDE.md lesson) — expected, ignored.

## Hand-off to PLAN-2.1 (Wave 2)

PLAN-2.1 will run the full-solution build `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true` to confirm the ROADMAP success criterion ("full-solution build produces no XML-doc warnings"). The SQLite NU1902 fix in this plan is the load-bearing change that lets the full-solution build complete.
