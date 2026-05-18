# Build Summary: Plan 3.1 — SQLite Inbox Tests + Option-Backing Fix

## Status: complete

## Tasks Completed

- 6 contract/behavior tests in `SqLiteRelationalWorkerNotificationTests.cs` (mirror Phase 3/4 with SQLite-specific naming + sealed-type NSubstitute note per lesson 4).
- 2 option-driven SimpleInjector smoke tests in `SqLiteRelationalWorkerNotificationRegistrationTests.cs` — directly satisfies PROJECT.md §SC #2 for SQLite.
- Fix: `SQLiteMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted` backing — property had hardcoded `false` getter and no-op setter; refactored to back with private field.

Commit `f547d442`.

## Decisions Made
- `StubOptionsFactory` concrete class used instead of NSubstitute for `ITransportOptionsFactory` (avoids rabbit hole; cleaner stub).
- Test naming for sealed-type limitation: `Transaction_Returns_Null_When_State_Transaction_Not_DbTransaction` instead of misleading `Transaction_Returns_Underlying_Transaction_When_Set` (Phase 3 SIMPLIFICATION L1 lesson applied from outset).

## Issues Encountered
- The option-backing fix was the key Phase 5 architectural finding beyond RESEARCH §2: even with hold-tx machinery wired (PLAN-1.1), the option's getter returned hardcoded false — making all the new code dead. Caught by `Resolves_Relational_When_HoldTransaction_Enabled` smoke test.

## Verification
| Gate | Result |
|---|---|
| Contract tests | 6/6 pass |
| Smoke tests | 2/2 pass |
| Full SQLite suite | 149/149 |
