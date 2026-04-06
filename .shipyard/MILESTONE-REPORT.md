# Milestone Report: Fix History Status for Errored Messages (Issue #97)

**Completed:** 2026-04-06
**Phases:** 1/1 complete
**GitHub Issue:** #97

## Milestone Summary

A single-phase bug fix addressing GitHub issue #97: Dashboard history shows `Status=Processing` for messages that exhausted retries and moved to the error queue. Two distinct bugs contributed:

**Bug A:** `ReceiveMessagesErrorHistoryDecorator` read `context.MessageId` after the inner handler cleared it via `SetMessageAndHeaders(null, ...)`, so `RecordError` was never called for terminal errors.

**Bug B:** Redis and Memory `RecordProcessingStart` unconditionally set `Status=Processing`, overwriting Error status on retries. RelationalDatabase and LiteDb already guarded this.

## Phase Summaries

### Phase 1: Fix History Error Recording and Retry Status Guard

**Status:** Complete

**Wave 1 (3 parallel plans):**

- **PLAN-1.1 (Decorator fix):** Captures `messageId` before delegating to inner handler, ensuring `RecordError` is called with the correct value even when the inner handler clears context.
- **PLAN-1.2 (RecordProcessingStart guard):** Redis and Memory transports now only transition from Enqueued to Processing, matching RelationalDatabase/LiteDb patterns.
- **PLAN-1.3 (Regression tests):** 5 new tests covering both bugs across all affected transports.

**Review fix:** Redis guard additionally checks `rawStatus.HasValue` before integer cast to prevent null-cast collision (`RedisValue.Null` casts to `0` = `MessageHistoryStatus.Enqueued`).

## Key Decisions

1. Fix applied in decorator/shared layer, not per-transport (Bug A)
2. Redis guard uses `HasValue` check before integer comparison (caught in review)
3. Memory guard adds `&& r.Status == MessageHistoryStatus.Enqueued` to existing conditional
4. All 3 plans executed in parallel (Wave 1) — disjoint file sets

## Documentation Status

- CHANGELOG updated with bug fix entry
- No public API changes — no API/architecture doc updates needed

## Known Issues

- Filed #104: Redis `RecordComplete`/`RecordError` have same unchecked `(long)` cast on `StartedUtc` HashGet (pre-existing)

## Quality Gates

| Gate | Result |
|------|--------|
| Phase Verification | PASS — 878 core + 166 Redis tests |
| Security Audit | PASS — no critical findings |
| Simplification Review | Defer — no high-priority findings |
| Documentation Review | Complete — CHANGELOG updated |

## Metrics

- **Phases:** 1
- **Plans executed:** 3 + 1 review fix
- **Commits:** 4 implementation + 1 artifacts
- **Files modified:** 6 (3 production, 3 test)
- **Tests added:** 5 new regression tests
- **Bugs caught during review:** 1 (Redis null-cast collision)
