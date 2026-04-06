# Phase 1 Verification Report

**Phase:** Fix History Duration for Fast-Completing Messages (GitHub issue #94)  
**Date:** 2026-04-05  
**Type:** build-verify

## Overall Status

**PASS** (after remediation commit `03a356db`)

**Initial verdict:** FAIL — Dead SQL block detected by hardened regression test (contained `StartedUtc IS NOT NULL` CASE expression in overwritten CommandText).

**Remediation (2026-04-05):** Dead first-UPDATE block removed from `WriteMessageHistoryHandler.RecordComplete`. Test `RecordComplete_WithoutStartedUtc_PassesDurationZero` now passes. 16/16 RelationalDatabase WriteMessageHistoryHandler tests pass on net10.0. No other regressions observed.

## Success Criteria Coverage

| # | Criterion | Status | Evidence |
|---|-----------|--------|----------|
| 1 | Build `Source/DotNetWorkQueue.sln` succeeds | PASS | `dotnet build ... -c Debug` completed successfully with no errors. |
| 2 | Core unit tests pass | PASS | `dotnet test DotNetWorkQueue.Tests ... --filter "FullyQualifiedName~WriteMessageHistoryHandler"` — 29 tests passed, 0 failed, 116ms. |
| 3 | RelationalDatabase WriteMessageHistoryHandler tests pass | FAIL | Test `RecordComplete_WithoutStartedUtc_PassesDurationZero` failed. Assertion at line 223 of WriteMessageHistoryHandlerTests.cs detected `StartedUtc IS NOT NULL` guard in UPDATE SQL. Error: "The duration UPDATE WHERE clause must not contain 'StartedUtc IS NOT NULL' — that guard makes the UPDATE a no-op when StartedUtc was never persisted." |
| 4 | LiteDb WriteMessageHistoryHandler tests pass | PASS | `dotnet test ... LiteDb.Tests ... --filter "FullyQualifiedName~WriteMessageHistoryHandler"` — 20 tests passed, 0 failed, 286ms. |
| 5 | Redis WriteMessageHistoryHandler tests pass | PASS | `dotnet test ... Redis.Tests ... --filter "FullyQualifiedName~WriteMessageHistoryHandler"` — 19 tests passed, 0 failed, 181ms. |
| 6 | Dashboard UI FormatDuration correctly renders sub-ms | PASS | Inspected `HistoryTab.razor:151-158`. Line 154: `if (ms == 0) return "< 1 ms";` correctly handles 0 duration. Null case (line 153): returns `"-"` (unchanged from previous behavior, preserving null rendering). |

## Critical Review Findings Resolution

| Issue | Status | Evidence |
|-------|--------|----------|
| **ISSUE-014:** RelationalDatabase RecordComplete WHERE clause blocks DurationMs=0 write | **UNRESOLVED** | Commit b538823a claims fix but incomplete. Only the THIRD UPDATE statement (lines 129-131) had the guard removed. The FIRST UPDATE statement (lines 107-110) still contains dead code: `DurationMs = CASE WHEN StartedUtc IS NOT NULL THEN @DurationPlaceholder ELSE NULL END` on line 109. This SQL text is never executed (CommandText is overwritten at line 119), but the test correctly detects it via SQL inspection. Root cause: b538823a removed the guard from one UPDATE but left the problematic CASE statement in the dead-code first UPDATE. |
| **ISSUE-015:** Dead local function MakeTrackingParam | **RESOLVED** | Commit b538823a removed unused `MakeTrackingParam()` helper function from test. |

## Integration Round-Trip Verification

**Memory Transport (Complete pipeline):**
- Write-side: `a2d2337e` sets `durationMs = 0` when StartedUtc missing (in RecordComplete and RecordError)
- Read-side: Reads and preserves the `0` value as-is
- UI-side: `a79cec3c` renders `0 → "< 1 ms"` per line 154 of HistoryTab.razor
- **Status:** Round-trip works correctly for Memory transport.

**Redis Transport:**
- Write-side: `686117bc` write-side + read-side handling; correctly sets `durationMs = 0` (line 155: `var durationMs = startTime.HasValue ? ... : 0L`)
- Read-side: `686117bc` preserves the value
- UI-side: Routes through FormatDuration
- **Status:** Round-trip works correctly for Redis transport.

**LiteDb Transport:**
- Write-side: `8cf57c0c` sets `durationMs = 0` when missing
- Read-side: `08ce80be` preserves the value
- UI-side: Routes through FormatDuration
- **Status:** Round-trip works correctly for LiteDb transport.

**RelationalDatabase Transport:**
- Write-side: `171c796f` attempted the fix, but the FIRST UPDATE statement still contains the problematic CASE guard with `StartedUtc IS NOT NULL`. The THIRD UPDATE (line 131) correctly lacks the guard and should set DurationMs. However, the test detects the dead code and fails, blocking verification.
- **Status:** Code artifact prevents test passage; actual runtime behavior may be correct but cannot be verified.

## CONTEXT-1 Decisions Honored

| Decision | Status | Evidence |
|----------|--------|----------|
| **Scope (RecordError + RecordComplete)** | PARTIAL | Most transports (Memory, Redis, LiteDb) have both paths fixed with `durationMs = 0` when StartedUtc missing. RelationalDatabase: both are affected by the same code cleanup issue. |
| **TDD discipline** | PASS | Tests were added/updated (e.g., `RecordComplete_WithoutStartedUtc_PassesDurationZero`, `RecordError_WithoutStartedUtc_PassesDurationZero`). Test assertions changed from `== null` to `== 0`. |
| **UI null behavior preserved** | PASS | `FormatDuration(null)` still returns `"-"` (line 153). No change from prior behavior. Only `0` rendering changed to `"< 1 ms"` (line 154). |

## Gaps Identified

1. **CRITICAL:** RelationalDatabase WriteMessageHistoryHandler.cs line 109 still contains dead code with the problematic `StartedUtc IS NOT NULL` guard in a CASE statement. This code is never executed (CommandText overwritten at line 119), but the test detects it during SQL text inspection. Must be removed or refactored to pass the test.

2. **Test Blocking Verification:** The failing RelationalDatabase test (`RecordComplete_WithoutStartedUtc_PassesDurationZero`) prevents full phase completion. The test is correct; the code cleanup is incomplete.

## Recommendations

1. **FIX REQUIRED:** Edit `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs` lines 107-116. Either:
   - Option A: Remove the dead FIRST UPDATE statement entirely (lines 107-124), leaving only the SECOND UPDATE (lines 119-121) and THIRD UPDATE (lines 127-141).
   - Option B: If the FIRST UPDATE is needed, remove the CASE statement and set `DurationMs` directly (though this is redundant given the THIRD UPDATE).
   - Recommended: **Option A** (remove the dead code block).

2. **Verification After Fix:** Re-run the RelationalDatabase test to confirm it passes:
   ```bash
   dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" \
     --filter "FullyQualifiedName~WriteMessageHistoryHandler" -c Debug
   ```

3. **Final Integration Test:** Run all phase 1 success criteria tests to confirm no regressions.

## File References

- **Failing Test:** `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs` line 223
- **Code Issue:** `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs` lines 107-116 (dead CASE statement)
- **UI Implementation:** `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue.Dashboard.Ui/Components/Shared/HistoryTab.razor` lines 151-158 (FormatDuration)

## Verdict

**FAIL** — Phase 1 cannot be considered complete while the RelationalDatabase transport test fails. The code cleanup from commit b538823a was incomplete; dead code containing the problematic `StartedUtc IS NOT NULL` guard remains in the FIRST UPDATE statement. This must be removed to pass the test. All other transports (Memory, Redis, LiteDb) pass their tests and show correct round-trip behavior. The Dashboard UI is correctly implemented. The issue is isolated to RelationalDatabase code cleanup.
