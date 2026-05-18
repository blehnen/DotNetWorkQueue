# Review: Plan 3.1 — SQLite Inbox Tests + Option-Backing Fix

**Verdict:** PASS (major lesson surfaced)

8 new tests (6 contract + 2 smoke). Two architectural fixes surfaced by writing the tests — both correct and consequential:

1. `SqLiteRelationalWorkerNotification` refactored to settable-property pattern (matches Phase 3/4).
2. `SQLiteMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted` backed by a real field (was hardcoded `false` — would have made ALL Phase 5 inbox wiring dead even with hold-tx machinery in place).

## Positives
- Smoke test seam works on SQLite (concrete `StubOptionsFactory` simpler than NSubstitute for the options-factory override path).
- Test names follow Phase 3 lesson 4 from the outset.
- Catching the option-backing bug via smoke test (rather than at Phase 7 integration) saved substantial debug time.

## Minor
- The option-backing fix is genuinely a pre-existing bug in the SQLite transport; this milestone exposes it. Could have been flagged separately as ISSUE-NEW for cleaner attribution, but bundling with PLAN-3.1 is pragmatic.

## Lesson captured (for VERIFICATION)
Declared-but-unbacked options ARE a real architectural bug shape. RESEARCH §2 caught "option not read" but missed "option also not backed". For future phases adding hold-tx-style options to a new transport: verify property has a real backing field BEFORE relying on its value.
