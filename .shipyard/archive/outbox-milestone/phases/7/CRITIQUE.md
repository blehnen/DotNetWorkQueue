# Plan Critique: Phase 7 (Documentation + Wiki Draft)

**Date:** 2026-05-15  
**Critiqued by:** Verification Engineer  
**Plans critiqued:** PLAN-1.1, PLAN-1.2, PLAN-1.3, PLAN-2.1

---

## File Existence & Path Validation

| File Path | Exists | Status | Notes |
|-----------|--------|--------|-------|
| `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj` | YES | PASS | File exists; Release\|net10.0 block present at lines 22–27 (uses tabs); net8.0 block ABSENT as expected per RESEARCH.md §2. |
| `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` | YES | PASS | File exists; three Release PropertyGroups (net10.0, net8.0, AnyCPU) present at lines 27–45. No `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` lines yet (pre-edit baseline confirmed). |
| `Source/DotNetWorkQueueNoTests.sln` | YES | PASS | Solution file present and valid. |
| `docs/jenkins-setup.md` | YES | PASS | Style reference exists (8,988 bytes, last mod 2026-03-31). Used by PLAN-1.2 as voice/heading/fencing reference. |
| `docs/outbox-pattern.md` | NO | PASS | File does not exist (expected pre-execution state). PLAN-1.2 Task 1 creates it. |
| `README.md` | YES | PASS | File exists. Lines 11–15 match old_string context exactly (verified via sed). Line endings are CRLF (`\r\n`). |

**Casing note (CLAUDE.md lesson):** Directory is `DotNetWorkQueue.Transport.SQLite` (with capital "S" and lowercase "QLite"), matching the csproj. Docker Linux path sensitivity risk is mitigated — the directory is already correctly cased.

---

## README Edit String Validation (PLAN-1.3)

**Verification:** Extracted README lines 13–15 (the `old_string` anchor region):

```
- Re-occurring job scheduler

See the [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki) for in-depth documentation.
```

**Result:** PASS. Exact match (including CRLF line endings and blank line spacing).

**Risk:** README file uses CRLF line endings. PLAN-1.3's Edit tool will preserve the file's existing line-ending convention (CRLF). No action needed.

---

## csproj XML Structure Validation (PLAN-1.1)

### Task 1 — Transport.RelationalDatabase.csproj

**Current state:**
- Line 22: `<PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Release|net10.0|AnyCPU'">`
- Lines 23–26: Contains `DefineConstants`, `TreatWarningsAsErrors`, `WarningsAsErrors`, `DocumentationFile`
- No `Release|net8.0` block exists (confirmed)

**Plan expectation:** Add a matching `Release|net8.0` block after line 27 with identical property contents.

**Result:** PASS. The file structure supports the addition. Indentation is tabs (lines 22–27 use `\t`). Plan Task 1 specifies "preserve existing tab indentation" — this is correct.

### Task 2 — Transport.SQLite.csproj

**Current state:**
- Three Release PropertyGroups: `Release|net10.0|AnyCPU` (lines 27–32), `Release|net8.0|AnyCPU` (lines 34–39), `Release|AnyCPU` (lines 41–45)
- All three contain `TreatWarningsAsErrors`, `WarningsAsErrors`, `DocumentationFile`
- None contain `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` (confirmed absent)

**Plan expectation:** Add `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to all three blocks.

**Result:** PASS. The file structure supports the addition. All three blocks have the correct indentation (tabs). Plan Task 2 specifies the exact line to add and rationale (ISSUE-032 closure).

---

## Build Command Validation

### PLAN-1.1 Verification Commands

Three per-project builds + one per-project SQLite build:

```bash
dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/..." -c Release -p:CI=true
dotnet build "Source/DotNetWorkQueue.Transport.SQLite/..." -c Release -p:CI=true
dotnet build "Source/DotNetWorkQueue.Transport.Shared/..." -c Release -p:CI=true
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/..." -c Release -p:CI=true
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/..." -c Release -p:CI=true
```

**Result:** PASS. Syntax is correct. Paths reference valid `.csproj` files. Commands are runnable (tested syntax via MSBuild invocation; actual build not run due to pre-edit state).

### PLAN-2.1 Verification Commands

```bash
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true
dotnet pack "Source/DotNetWorkQueue.Transport.RelationalDatabase/..." -c Release -p:CI=true -o ./pack-verify
```

**Result:** PASS. Syntax is correct. Solution path valid. Pack command uses `-o` to direct output to a transient `./pack-verify` directory (correctly cleaned up per Task 2 acceptance criteria).

---

## Wave 1 Parallel Plan Overlap Analysis

### Files touched per plan:

| Plan | Files Touched | Type |
|------|---------------|------|
| PLAN-1.1 | `Transport.RelationalDatabase.csproj`, `Transport.SQLite.csproj` | Modify (csprojs) |
| PLAN-1.2 | `docs/outbox-pattern.md` | Create (new markdown file) |
| PLAN-1.3 | `README.md` | Modify (single bullet insertion) |

**File overlap:** NONE. Plans are cleanly disjoint:
- PLAN-1.1 edits `.csproj` files (binary XML, not source).
- PLAN-1.2 creates a new documentation file in `docs/`.
- PLAN-1.3 edits README (single bullet, no csproj or docs/ overlap).

**Verdict:** PASS. All three Wave 1 plans can execute in parallel with zero file conflicts.

---

## Wave 2 Dependency Chain (PLAN-2.1)

**Declared dependencies:** `[1.1, 1.2, 1.3]`

**Justification analysis:**

| Dependency | Rationale | Valid |
|------------|-----------|-------|
| PLAN-1.1 | Full-solution Release build will fail on SQLite NU1902 error without `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` (PLAN-1.1 Task 2). Plan-2.1 Task 1 runs this build. | YES |
| PLAN-1.2 | PLAN-2.1 Task 3 verifies the README bullet's link target (`docs/outbox-pattern.md`). File must exist. | YES |
| PLAN-1.3 | PLAN-2.1 Task 3 verifies the README bullet itself exists. Plan-1.3 inserts it. | YES |

**Verdict:** PASS. Dependencies are minimal and necessary. All three Wave 1 plans must complete before PLAN-2.1 can verify the final state.

---

## Critical Findings

### Net8.0 XML-doc gate rationale (PLAN-1.1 Task 1)

RESEARCH.md §2 identified that `Transport.RelationalDatabase.csproj` has a `Release|net10.0` condition block but no `Release|net8.0` block. This means:

- **Current:** If a CS1591 gap exists only in the net8.0 TFM, it goes undetected (no XML-doc generation enabled for net8.0).
- **After PLAN-1.1:** Both TFMs will have `TreatWarningsAsErrors + DocumentationFile`, so any gap is caught.

Plan explicitly states this rationale in Task 1 context. Correct.

### ISSUE-032 Inline Closure (PLAN-1.1 Task 2)

NU1902 (pre-existing OpenTelemetry advisory) escalates to error under `TreatWarningsAsErrors`. PLAN-1.1 Task 2 adds `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to exempt this advisory from error escalation while keeping it visible as a warning.

ROADMAP §Phase 7 success criterion requires "no XML-doc warnings" (CS1591, not NU1902). The full-solution build fails on NU1902 BEFORE the XML-doc gate can be evaluated. Task 2 unblocks this inline. Correct.

### PLAN-1.2 File Size Target

Plan specifies: "File length target: 120–200 lines. If draft exceeds 250 lines, trim."

This is reasonable for a reference page. No pre-written content to validate; Task 1 is write-from-scratch. Builder will check line count and section presence during execution.

### WSL Line Ending Context

README and any markdown in Phase 7 will use the repo's existing convention (CRLF, as evidenced by `README.md` inspection). CLAUDE.md notes WSL can introduce UTF-16 or confusing line-ending warnings. The plans don't explicitly warn about this, but it's low-risk for a markdown-only phase.

---

## Summary

| Category | Finding | Status |
|----------|---------|--------|
| File existence | All prerequisite files exist (csprojs, solution, style reference). Outbox doc doesn't exist yet (expected). | PASS |
| README edit string | Exact match verified including whitespace and blank lines. | PASS |
| csproj structure | Both csprojs have correct XML structure and indentation to support planned edits. | PASS |
| Build commands | All verification commands have correct syntax; paths are valid. | PASS |
| Wave 1 overlap | No file conflicts between PLAN-1.1, PLAN-1.2, PLAN-1.3. | PASS |
| PLAN-2.1 dependencies | All three Wave 1 dependencies are minimal and necessary. | PASS |
| Rationale | Net8.0 gate and ISSUE-032 closure rationales are well-justified. | PASS |

---

## Verdict

**READY**

All four plans pass feasibility validation. File paths are correct, XML structure supports the edits, build commands are syntactically valid, and dependencies are minimal. No file conflicts exist between Wave 1 plans. The plans are ready for builder execution.

### Greenlight Conditions Met

✅ No missing files or mismatched paths  
✅ README old_string matches verbatim  
✅ csproj blocks have correct structure and indentation  
✅ All verification commands are runnable (syntax valid)  
✅ Wave 1 plans are truly parallel (no file overlap)  
✅ Wave 2 dependencies are all necessary (not redundant)  
✅ Context-specific lessons (SQLite casing, CRLF line endings) acknowledged and mitigated  

**Recommendation:** Proceed with builder execution.
