# Phase 1 Context — Design Decisions

Captured from discussion capture before planning. Architect agent must honor these decisions.

## Decision 1: Scope — Fix RecordError Path Alongside RecordComplete

**Decision:** Extend the fix beyond the ROADMAP's stated scope to include `RecordError` in all transports.

**Rationale:** `RecordError` has the identical `durationMs > 0 ? x : null` pattern as `RecordComplete`. The user-visible bug (null/blank duration on sub-millisecond messages) applies equally to errored messages. Fixing only Complete would leave the same inconsistency on the Error path, requiring a second PR later. The code changes are mechanically identical — minimal extra work.

**Implication for plan:**
- For every transport's `RecordComplete` fix, apply the mirrored fix to `RecordError`
- Existing tests named like `RecordError_WithoutStarted_DurationIsNull` must be updated to assert `== 0` instead of `== null` (mirroring the Complete path fix)
- UI fix remains scoped to `HistoryTab.razor FormatDuration`; no separate Error-path UI work

## Decision 2: TDD — Failing Tests First

**Decision:** Apply strict TDD discipline: write a failing test first, confirm it fails on current code, then apply the production fix and confirm the test passes.

**Rationale:** The fix touches 6+ files across 5 transports — each change needs its own regression test. TDD forces each fix to be verified against a specific failing scenario before the production change lands. This also produces the test coverage that documents the new expected behavior.

**Implication for plan:**
- Each task should have explicit Red → Green steps:
  1. Write/update failing test asserting `DurationMs == 0` (not null) when StartedUtc missing
  2. Run test, confirm FAIL on current code
  3. Apply production fix
  4. Run test, confirm PASS
  5. Commit
- For Memory transport: existing `*_WithoutStarted_DurationIsNull` tests get their assertions flipped (from `null` to `0`) — this IS the failing test
- For Redis/LiteDb: same pattern for their existing unit tests

## Decision 3: Skip Researcher Agent

**Decision:** Skip Step 4 (research dispatch). Proceed directly from this CONTEXT doc to the architect agent.

**Rationale:** The ROADMAP.md already contains the exhaustive file-path + line-number + code-change-per-transport mapping that a researcher would produce. Dispatching a researcher would rediscover documented facts. The architect can work directly from ROADMAP.md + this CONTEXT-1.md.

**Implication for plan:** No RESEARCH.md will be produced for this phase.

## Dashboard UI Ambiguity (for architect to resolve)

The ROADMAP says "when `DurationMs == 0` and status is Complete, display `< 1 ms`". After the backend fix, the possible states for `DurationMs` in the UI become:
- `0` → sub-millisecond completion (fixed) → show `< 1 ms`
- `> 0` → measured duration → show `{N} ms`
- `null` → message hasn't started yet (Enqueued/Processing rows that never completed) → show what?

**Architect should:** Examine current `FormatDuration` in `HistoryTab.razor` and propose the null-rendering behavior as part of the plan (e.g., preserve existing null rendering, or unify to `-` / blank).
