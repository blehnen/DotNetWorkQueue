# Build Summary: Plan 3.1

## Status: complete

## Tasks Completed

- Task 1: Add `IRetrySkippable` bypass branch to SqlServer sync retry decorator — complete — `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs` (+1 `using DotNetWorkQueue.Transport.RelationalDatabase;` in alphabetical position; +3-line early-return branch after `Guard.NotNull` in `Handle()`).
- Task 2: Add `IRetrySkippable` bypass branch to SqlServer async retry decorator — complete — `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` (same `using`, mirrored branch using `await ... .ConfigureAwait(false)` to match existing async style).
- Task 3: Add bypass-branch unit tests (sync + async) — complete — `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` (NEW; LGPL header + 1 `[TestClass]` with 2 `[TestMethod]`; uses `RelationalSendMessageCommand` + `Substitute.For<DbTransaction>()` to trigger `SkipRetry = true`; both methods assert `_ = policies.DidNotReceiveWithAnyArgs().Registry;` per Phase 1 SUMMARY-1.1 property-getter pattern).

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `7c6e348a` | 1 | `shipyard(phase-2): add IRetrySkippable bypass branch to SqlServer sync retry decorator` |
| `64eb91b1` | 2 | `shipyard(phase-2): add IRetrySkippable bypass branch to SqlServer async retry decorator` |
| `86a16287` | 3 | `shipyard(phase-2): add IRetrySkippable bypass-branch unit tests for SqlServer retry decorator` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs` — MODIFIED (+5 lines)
- `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` — MODIFIED (+5 lines)
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` — NEW (86 lines)

## Decisions Made

- None. All 3 edits followed plan wording verbatim.

## Issues Encountered

- Pre-existing 14 NU1902 OpenTelemetry advisory warnings on every Release build (same baseline as Wave 1 / Wave 2). Out of scope.
- WSL LF→CRLF git warning on new test file (cosmetic).

## Verification Results

| Gate | Expected | Actual |
|---|---|---|
| grep `IRetrySkippable skippable` sync decorator | 1 match | line 54 |
| grep `IRetrySkippable skippable` async decorator | 1 match | line 55 |
| New test file exists | present | present |
| SqlServer main project Release build | 0 errors | 0 errors, 14 pre-existing NU1902 |
| New bypass tests (filter `~RetryCommandHandlerOutputDecoratorBypassTests`) | 2 passed | 2 passed |
| All `~Decorator` (existing + new) | Failed: 0 | 14 passed (12 prior + 2 new) |
| Full SqlServer.Tests suite | Failed: 0 | 141 passed, 0 failed, 0 skipped |
| Layering invariant on `Transport.RelationalDatabase/` | no matches | no matches |

## Phase 3 Hand-off

- SqlServer retry decorators (sync + async) now honor `IRetrySkippable.SkipRetry`. Any production handler chain decorating `ICommandHandlerWithOutput<RelationalSendMessageCommand, long>` will bypass the Polly pipeline when `ExternalTransaction != null`. Caller owns retry on that path.
- PostgreSQL companion is PLAN-3.2 (parallel, disjoint files).
- No changes to wiring, DI, or producer API in this plan — Phase 3 wires this into `SqlServerRelationalProducerQueue<T>`.
