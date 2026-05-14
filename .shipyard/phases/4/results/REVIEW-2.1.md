# Review: Plan 2.1 (Phase 4 Wave 2 — PG Sync Fork)

## Verdict: PASS

Spec compliance is clean across every plan item. All three PG-specific deviations from Phase 3 SqlServer were applied correctly. CONTEXT-4 Rule B converged to the source-side rephrase (no test-side stripping inherited from Phase 3 PLAN-2.1). Self-managed-tx path is byte-equivalent to pre-change state aside from the 2-line early-branch insertion. No critical or important issues.

## Stage 1 — Correctness / Spec Compliance

| Spec item | Status | Evidence |
|---|---|---|
| Early-branch immediately after lazy-init block | PASS | `SendMessageCommandHandler.cs:106-107` — branch sits between the `_messageExpirationEnabled.HasValue` lazy-init `}` (104) and the existing `var jobName = ...` (109). |
| Branch text is `if (commandSend.ExternalTransaction != null) return HandleExternalTx(commandSend);` | PASS | Lines 106–107 exact match. |
| `HandleExternalTx(SendMessageCommand)` is `private long` | PASS | Line 201. |
| Casts `ExternalTransaction` to `NpgsqlTransaction`, reuses `tx.Connection` as `NpgsqlConnection` | PASS | Lines 206–207. |
| `[SuppressMessage("Microsoft.Security", "CA2100", ...)]` attribute on new method | PASS | Line 200. |
| Job-exists query gated by non-empty `jobName` with negated combined predicate | PASS | Lines 218–224. Uses `DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>`. |
| Body INSERT inline (uses `_commandCache.InsertMessageBody`) | PASS | Lines 227–246. |
| Metadata INSERT via `CreateMetaDataRecord` helper with 8 args | PASS | Lines 263–265 — eighth arg is `_getTime.GetCurrentUtcDate()`. |
| Status INSERT via `CreateStatusRecord` helper, gated on `_options.Value.EnableStatusTable` | PASS | Lines 267–270. |
| `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>` dispatched only for non-empty `jobName` | PASS | Lines 272–276. |
| No `tx.Commit()/Rollback()/Dispose()`, no `conn.Close()/Dispose()` in fork body | PASS | Single `.Commit()` in the file is on line 174 (self-managed-tx path, unchanged). Fork body lines 201–280 are clean. |
| Self-managed-tx path unchanged | PASS | Lines 109–182 byte-equivalent to pre-2.1 state aside from the 2-line early-branch insertion at 106–107. |
| 3 smoke tests in `SendMessageCommandHandlerForkSmokeTests.cs` | PASS | Tests at lines 42–55, 63–85, 92–118. |
| LGPL header on new test file | PASS | Lines 1–18 verbatim copy. |
| XML doc on new private method | PASS | Lines 184–199. |
| Build clean / 143 PG.Tests pass | PASS | Per SUMMARY-2.1 verification table. |

## Stage 2 — Code Quality / Integration

### Critical
- None.

### Important
- None.

### Minor

1. **Fork-body end-bound overreaches into sibling helpers** (`SendMessageCommandHandlerForkSmokeTests.cs:112`).
   `forkBody = content.Substring(forkStart, Math.Min(6000, content.Length - forkStart))` — `forkStart` indexes from `private long HandleExternalTx` (line 201). 6000 chars forward walks past the fork's closing `}` (line 280) and through `CreateStatusRecord` (line 290) + `CreateMetaDataRecord` (line 313) into the class's closing `}` (line 325). Today the helpers are clean of `.Commit()/.Rollback()/.Close()/.Dispose()`, but a future modification to either helper would falsely fail the fork's lifecycle test with a misleading message. **Identical Minor was flagged in Phase 3 REVIEW-2.1 Minor #1 and not yet remediated** — recurring pattern.
   - **Remediation (optional):** scope the slice by locating the next `^        }$` (8-space indent + closing brace) after `forkStart`, or use the next `private`/`public` declaration as the end-bound.

2. **Fragile relative source-file path** (`SendMessageCommandHandlerForkSmokeTests.cs:69-75, 98-104`).
   The `..\..\..\..\` walk-up assumes a 4-level-deep bin output structure. Brittle to TFM changes or test-running tools that copy assemblies to staging directories. Same Minor as Phase 3 REVIEW-2.1 Minor #3 — accepted then, accepted now. Path-resolution failure is itself a useful signal.
   - **Remediation (optional):** use `[CallerFilePath]` on a helper for robust anchoring.

3. **Path-resolution block duplicated across tests 2 and 3** (`SendMessageCommandHandlerForkSmokeTests.cs:69-75` vs `98-104`).
   Identical 7-line block copy-pasted. Same Minor as Phase 3 REVIEW-2.1 Minor #4.
   - **Remediation (optional):** extract `private static string GetHandlerSourcePath()` helper.

### Positive

- Phase 4 converged on the CONTEXT-4 Rule B source-side rephrase from the start — no test-side comment-stripping logic inherited from Phase 3 PLAN-2.1. Test 3 is plain-substring grep with no preprocessing.
- All three PG-specific deviations applied correctly and visibly: 8th-arg `_getTime.GetCurrentUtcDate()` materialization, `NpgsqlDbType.Bytea` (×2 in fork body, 0 `SqlDbType.*`), typed queries parameterized on `NpgsqlConnection`/`NpgsqlTransaction`.
- Self-managed-tx path is byte-equivalent aside from the 2-line branch insertion — minimum-touch posture.
- `_messageExpirationEnabled.Value` materialization preserved on both paths because the branch sits AFTER the lazy-init `}` (line 255 in fork reads `_messageExpirationEnabled.Value` safely).
- Reflection signature test (`HandleExternalTx_PrivateMethod_ExistsWithExpectedSignature`) pins return type and parameter list — future rename/return-change fails loud.
- XML doc on `HandleExternalTx` (lines 184–199) is informative: documents the producer-surface guarantee and the rationale for not re-validating inside the fork.
- SUMMARY-2.1 §Decisions Made explicitly cross-references Phase 3 PLAN-2.2's source-side approach and notes the convergence — review-friendly transparency.

## CONTEXT-4 Rule B audit

- **Comment present at expected location:** YES — `SendMessageCommandHandler.cs:278` carries the exact required text `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.`
- **Exact wording match:** YES — character-for-character match against the required string in CONTEXT-4.md line 89.
- **No forbidden substrings in the rephrased comment:** YES — the comment uses `Commit`, `Rollback`, `Close`, `Dispose` as bare word forms; no `.Commit()` / `.Rollback()` / `.Close()` / `.Dispose()` method-call substrings.
- **Test 3 grep operates without preprocessing:** YES — Test 3 (`HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose`) is a plain `Contains` check with no line-stripping logic, unlike Phase 3 PLAN-2.1's inherited workaround.
- **Convergence with Phase 3:** Phase 4 sync now matches Phase 3 PLAN-2.2's source-side approach (Phase 3 async), eliminating the Phase 3 PLAN-2.1 (sync) test-side stripper. Sync + async are aligned across both phases.

## PG-specific deviation check

- `_getTime.GetCurrentUtcDate()` correctly materialized: **yes** — appears at line 265 as the 8th argument to `CreateMetaDataRecord(...)`. Count in fork body: 1 (matches verification gate expectation).
- `NpgsqlDbType.Bytea` used (not `SqlDbType`): **yes** — appears at lines 235 (`@body`) and 241 (`@headers`) inside the fork; 0 occurrences of `SqlDbType.*` anywhere in the file. Count in fork body: 2 (matches gate).
- Typed queries use `NpgsqlConnection`/`NpgsqlTransaction`: **yes** — `DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>` at line 219, `SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>` at line 274.

## Cross-Phase consistency with Phase 3

- **Branch shape (lines 106–107)**: identical to Phase 3 SqlServer's two-line insertion. Insertion point relative to the lazy-init block is the same.
- **`HandleExternalTx` method shape**: mirrors Phase 3 SqlServer's structure (cast guard → job-name resolution → optional job-exists query → body INSERT → metadata via helper → optional status via helper → optional job-status command → return id).
- **3 smoke tests**: same three-test pattern as Phase 3 (reflection signature, source contains early-branch, no-lifecycle-calls grep). Test names are byte-equivalent.
- **PG deviations are surgical and explicit**:
  1. `CreateMetaDataRecord` 8-arg call (Phase 3 SqlServer was 7-arg)
  2. `NpgsqlDbType.Bytea` (Phase 3 SqlServer used `SqlDbType.VarBinary`)
  3. Typed queries on `NpgsqlConnection`/`NpgsqlTransaction` (Phase 3 SqlServer used `SqlConnection`/`SqlTransaction`)
  No other deviations.
- **Rule B convergence**: Phase 4 sync's source-side rephrase aligns with Phase 3 async (PLAN-2.2). Phase 3 sync (PLAN-2.1) used the test-side stripper, which Phase 4 deliberately did not inherit. CONTEXT-4 Rule B is satisfied.

## Summary

**Verdict:** APPROVE
Critical: 0 | Important: 0 | Minor: 3 (all carryovers from Phase 3 REVIEW-2.1 — not blocking)

PG sync handler fork is complete, faithful to the plan, and the three Phase 3 carryover Minor findings are unchanged in severity by design (the test pattern is intentionally a Phase 3 mirror). All non-blocking findings will be appended to `.shipyard/ISSUES.md` for future hardening. Phase 6 integration tests against a real PostgreSQL instance will exercise the fork at runtime.
