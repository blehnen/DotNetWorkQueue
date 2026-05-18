# Review: Plan 3.2 — SQLite Outbox Tests + Empty-String Fix

**Verdict:** PASS (with deferred fork-test scope item)

4 extractor tests cover spike §3 semantics directly. Empty-string guard added to extractor + wrapper (symmetric).

## Deferred (intentional)
`HandleExternalTransaction` fork unit tests deferred to Phase 7 integration. Documented in SUMMARY-3.2 rationale section. Phase 7's real-DB caller-tx paths cover PROJECT.md §SC #8.

## Positives
- Case-sensitivity test (`:Memory:` NOT a keyword) explicit and clear.
- Empty-string fix symmetric across extractor + wrapper.

## Minor
- None blocking.
