# Milestone Report: Fix History Duration for Fast-Completing Messages

**Completed:** 2026-04-05
**Phases:** 1/1 complete
**GitHub Issue:** #94

## Milestone Summary

A single-phase cosmetic fix addressing GitHub issue #94: messages completing in under 1 millisecond (or before `StartedUtc` could be persisted) displayed blank or inconsistent `Duration` values in the Dashboard history view. Root cause was a race condition between `RecordProcessingStart` and `RecordComplete` across all transports, with the user-visible symptom being `DurationMs = NULL` in storage.

The fix normalizes `DurationMs = 0` at the write-side across all transports, preserves `0` correctly on the read-side (fixing transports that silently converted `0` to `null`), and updates the Dashboard UI to render `< 1 ms` for sub-millisecond completions.

## Phase Summaries

### Phase 1: Normalize DurationMs Across Transports and Fix Dashboard Display

**Status:** Complete

**Scope (expanded beyond roadmap):** Per CONTEXT-1 user decision, fix applied to BOTH `RecordComplete` AND `RecordError` paths (roadmap covered Complete only).

**Two waves:**

- **Wave 1 — Write-side normalization** (PLAN-1.1, 3 tasks)
  - Memory, RelationalDatabase, LiteDb `WriteMessageHistoryHandler`: both `RecordComplete` and `RecordError` now store `DurationMs = 0` when `StartedUtc` is missing
  - Two critical fixes applied post-review: removed `StartedUtc IS NOT NULL` guard from RelationalDatabase `RecordComplete` SQL UPDATE, and deleted a dead first-UPDATE block that contained the same guard pattern

- **Wave 2 — Read-side + UI** (PLAN-1.2, 3 tasks)
  - Redis and LiteDb `QueryMessageHistoryHandler`: discriminator changed from `DurationMs > 0` to `CompletedUtc > 0` (semantically correct — distinguishes "never completed" from "sub-ms completion")
  - Dashboard UI `FormatDuration` in `HistoryTab.razor`: renders `< 1 ms` for `DurationMs == 0`, preserves `-` for null (unchanged)

## Key Decisions

1. **Scope expansion to Error path** — fixed RecordError alongside RecordComplete to avoid shipping half a fix (CONTEXT-1 Decision 1)
2. **TDD discipline** — every fix landed with a failing test first, then production change (CONTEXT-1 Decision 2)
3. **Skip researcher step** — ROADMAP.md was exhaustive enough to plan directly (CONTEXT-1 Decision 3)
4. **Semantic read-side improvement** — architect chose `CompletedUtc > 0` discriminator over the roadmap's `DurationMs > 0` (architect deviation)
5. **Null UI rendering preserved** — `-` for null, non-breaking

## Documentation Status

- **CHANGELOG.md:** Updated with `0.9.17 — 2026-04-05` entry covering the fix, UI change, and SQL guard removal
- **Public API docs:** No changes (all changes to internal handlers)
- **Architecture docs:** No changes (fix is below architecture abstraction)
- **User guides:** No changes (bug restoration, not new feature)

## Known Issues

None. Two issues caught during review (ISSUE-014, ISSUE-015) were fully resolved within the phase.

Three low-priority suggestions from the audit/simplification agents were deferred as non-blocking:
- Add SQL comment explaining why the `StartedUtc IS NOT NULL` guard was dropped
- Add inline comment documenting the `commandCallCount == 2` coupling in the RelationalDatabase test
- Guard against negative durations in `FormatDuration` (clock skew edge case)

## Quality Gates

| Gate | Result |
|------|--------|
| Phase Verification | PASS (after dead-code fix `03a356db`) |
| Security Audit | CLEAN — no critical/important findings |
| Simplification Review | LOW_PRIORITY — 1 comment suggestion |
| Documentation Review | MINOR_GAPS — CHANGELOG added |

## Metrics

- **Phases:** 1
- **Plans executed:** 2 (PLAN-1.1, PLAN-1.2)
- **Tasks:** 6 (3 per plan)
- **Critical post-review fixes:** 2
- **Commits on branch:** 11 (3 shipyard + 8 fix commits)
- **Files modified:** 13 (6 production, 5 tests, CHANGELOG, shipyard artifacts)
- **LOC delta:** ~40 production, ~200 tests
- **Tests added/updated:** 147 tests passing (across 5 unit test suites + 45 integration)
- **Bugs caught during review:** 2 real ones (SQL WHERE guard no-op, dead SQL block)
