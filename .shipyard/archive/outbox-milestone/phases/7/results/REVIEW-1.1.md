# Review: Plan 1.1 (Phase 7 Wave 1 — csproj fixes + per-project XML-doc verify)

## Verdict: PASS

---

## Stage 1: Spec Compliance

**Verdict: PASS**

### Task 1 — `Release|net8.0|AnyCPU` PropertyGroup in Transport.RelationalDatabase.csproj

- **Status: PASS**
- **Evidence:** `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj` lines 29-34 contain a new PropertyGroup with `Condition='Release|net8.0|AnyCPU'`, holding the same four properties as the existing `Release|net10.0` block (`DefineConstants`, `TreatWarningsAsErrors`, `WarningsAsErrors`, `DocumentationFile`). Tab indentation matches surrounding blocks. Property order identical to the net10 sibling.

### Task 2 — `WarningsNotAsErrors NU1902` in all 3 SQLite Release blocks

- **Status: PASS**
- **Evidence:** `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj`:
  - `Release|net10.0` (lines 27-33): `WarningsNotAsErrors NU1902` at line 32
  - `Release|net8.0` (lines 35-41): `WarningsNotAsErrors NU1902` at line 40
  - `Release|AnyCPU` (lines 43-48): `WarningsNotAsErrors NU1902` at line 47
- Advisory remains visible as a warning (not suppressed via `<NoWarn>`).

### Task 3 — Per-project Release `-p:CI=true` verification

- **Status: PASS (by report)**
- SUMMARY-1.1.md table shows build succeeded / 0 CS1591 / 0 errors across the 4 outbox csprojs (Transport.Shared, Transport.RelationalDatabase, Transport.SqlServer, Transport.PostgreSQL). Reviewer did not re-run `dotnet build`. NU1902 warnings remain present but non-fatal per Task 2.

---

## Stage 2: Code Quality

### Critical
None.

### Important

1. **ISSUE-032 status not updated in `.shipyard/ISSUES.md`** — Plan explicitly says "closes ISSUE-032 inline"; commit message `88ff8996` says "(closes ISSUE-032)"; SUMMARY-1.1 says "Closes ISSUE-032 inline." But `.shipyard/ISSUES.md` line 297 still reads `**Status:** Open. Tracking; no Phase 2 build/test impact.` The fix landed; the tracking record is stale.
   - **Remediation:** Update ISSUE-032 `**Status:**` line to `Resolved 2026-05-15 — Phase 7 PLAN-1.1, commit 88ff8996. Added <WarningsNotAsErrors>NU1902</WarningsNotAsErrors> to all three Release PropertyGroup blocks in Transport.SQLite.csproj.`

### Suggestions

1. **Pre-existing indentation inconsistency in Transport.SQLite.csproj** — `Release|AnyCPU` block uses 2-space indentation; per-TFM Release blocks use tab indentation. Not introduced by this plan; not in scope to fix. Worth normalizing if the file is touched again. Non-blocking.

---

## Summary

**Verdict: APPROVE**

Both csproj edits are mechanically correct and match the spec exactly. The new `Release|net8.0` block mirrors its `Release|net10.0` sibling property-for-property. All three SQLite Release blocks carry the `WarningsNotAsErrors NU1902` element. Only bookkeeping gap: ISSUE-032 status update missed.

Critical: 0 | Important: 1 | Suggestions: 1
