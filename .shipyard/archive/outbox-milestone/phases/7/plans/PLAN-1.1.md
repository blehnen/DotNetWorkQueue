---
phase: documentation
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - Add Release|net8.0 condition block with DocumentationFile + TreatWarningsAsErrors to Transport.RelationalDatabase.csproj
  - Add WarningsNotAsErrors NU1902 to Transport.SQLite.csproj to unblock full-solution Release build (closes ISSUE-032 inline)
  - Confirm zero XML-doc warnings (CS1591) across the four outbox-touching projects under Release+CI=true
files_touched:
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj
  - Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj
tdd: false
risk: low
---

# Plan 1.1: csproj XML-doc gate fix + per-project verification

## Context

RESEARCH.md §2 found that all public types added in Phases 2–4 already carry XML doc
comments (zero CS1591 gaps on direct file inspection). Phase 7's XML-doc work is therefore
a **verification pass**, not a write-from-scratch pass.

Two csproj gaps stand between the current state and a clean Release build:

1. **`Transport.RelationalDatabase.csproj`** has a `Release|net10.0` condition block with
   `<DocumentationFile>` + `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` but is
   **missing the matching `Release|net8.0` block**. The net8.0 TFM of the shared foundation
   layer is therefore not gated by CS1591 in Release. If any net8.0-only doc gap exists
   it ships undetected.

2. **`Transport.SQLite.csproj`** escalates NU1902 (pre-existing OpenTelemetry advisory) to
   an error under `TreatWarningsAsErrors`, which makes the full-solution Release build fail
   before Phase 7's XML-doc gate can be evaluated (ISSUE-032). ROADMAP §Phase 7's strict
   success criterion is "`dotnet build -c Release -p:CI=true` of `DotNetWorkQueueNoTests.sln`
   produces no XML-doc warnings" — that requires the full-solution build to complete.
   Adding `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to the SQLite csproj keeps
   the advisory visible as a warning while letting the full-solution build run, satisfying
   the strict reading and closing ISSUE-032 inline.

Both edits are mechanical, single-file, and risk-low.

## Dependencies

None. Plan 1.1 is independent of Plan 1.2 (doc file) and Plan 1.3 (README bullet) — no
file overlap and no logical ordering. All three can run in parallel.

## Tasks

### Task 1: Add Release|net8.0 condition block to Transport.RelationalDatabase.csproj

**Files:** `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj`
**Action:** modify
**Description:** Insert a new `<PropertyGroup>` after the existing `Release|net10.0` block
(line 27) mirroring its three settings for the net8.0 TFM. The block shape:

```xml
<PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Release|net8.0|AnyCPU'">
    <DefineConstants></DefineConstants>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <DocumentationFile>DotNetWorkQueue.Transport.RelationalDatabase.xml</DocumentationFile>
</PropertyGroup>
```

Preserve the existing tab-vs-space indentation of the surrounding blocks (the file uses
tab indentation on the `Release|net10.0` block at lines 22–27 — match that, do not
convert to spaces). Do not modify any other line.

**Acceptance Criteria:**
- File contains two `Release|...` `<PropertyGroup>` blocks (one per TFM) with identical
  property contents.
- `dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release -p:CI=true` succeeds with no CS1591 warnings.
- `git diff` shows only the new block added; no whitespace churn elsewhere.

### Task 2: Add WarningsNotAsErrors NU1902 to Transport.SQLite.csproj (closes ISSUE-032)

**Files:** `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj`
**Action:** modify
**Description:** Add `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to each of the
three Release-condition `<PropertyGroup>` blocks in the SQLite csproj (the per-TFM
`Release|net10.0` block at lines 27–32, the per-TFM `Release|net8.0` block at lines 34–39,
and the all-platforms `Release|AnyCPU` block at lines 41–45). NU1902 continues to surface
as a warning so the OpenTelemetry advisory remains visible; it just no longer escalates to
an error under `TreatWarningsAsErrors`. Rationale: ROADMAP §Phase 7 success criterion
requires the full-solution Release build to be clean for XML-doc warnings; ISSUE-032's
pre-existing NU1902 error obscures that signal. Inline fix is cheaper than scoping
verification to per-project builds and re-opening ISSUE-032 later.

**Acceptance Criteria:**
- All three Release `<PropertyGroup>` blocks contain a `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` line.
- `dotnet build "Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj" -c Release -p:CI=true` succeeds. NU1902 appears as a warning, not an error.
- Build output line count for NU1902 is unchanged from pre-edit baseline (the advisory is still surfaced for visibility, not suppressed).

### Task 3: Per-project Release+CI=true verification of the four outbox projects

**Files:** none modified; verification only
**Action:** verify
**Description:** Run a Release+CI=true build against each of the four outbox-touching
projects individually to confirm zero CS1591 (XML-doc) warnings on each. This is the
authoritative Phase 7 XML-doc gate; Wave 2 (PLAN-2.1) runs the full-solution build
afterwards. Per-project builds isolate any XML-doc gap to a specific assembly without the
noise of unrelated transports.

**Acceptance Criteria:**
- Each of the four `dotnet build ... -c Release -p:CI=true` commands below exits 0.
- Each command's output reports `0 Warning(s)` for CS1591 (other categories acceptable, e.g., NU1902 on SQLite was excluded as it does not apply to these projects).

## Verification

```bash
# Task 1 — net8.0 RelationalDatabase build now produces CS1591 if any doc gap exists
dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release -p:CI=true

# Task 2 — SQLite no longer breaks on NU1902 under TreatWarningsAsErrors
dotnet build "Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj" -c Release -p:CI=true

# Task 3 — per-project XML-doc gate verification on all four outbox projects
dotnet build "Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj" -c Release -p:CI=true
dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release -p:CI=true
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release -p:CI=true
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release -p:CI=true
```

All four must report `Build succeeded` with `0 Warning(s)` for CS1591. If any project
emits CS1591, **stop and report the specific member** — the architect's RESEARCH inspected
all known new public types and found none missing, so any CS1591 is an unexpected finding
that the builder should surface rather than silently document.

## PROJECT.md Success Criteria coverage

| Plan element | §SC |
|---|---|
| Per-project Release+CI=true builds clean | §SC #10 (precondition) |
| net8.0 TFM XML-doc gate enabled on Transport.RelationalDatabase | §Non-Functional Multi-targeting |
| ISSUE-032 closed inline (NU1902 no longer ship-blocking) | Out-of-band — unblocks Wave 2 full-solution verification |
