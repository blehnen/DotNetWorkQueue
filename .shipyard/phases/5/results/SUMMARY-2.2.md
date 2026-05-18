# Build Summary: Plan 2.2 — SQLite Outbox Sweep

## Status: complete

## Tasks Completed

- Authored `SqLiteExternalDbNameExtractor` + `SqliteNormalizedConnectionInformation` wrapper with symmetric `Path.GetFullPath()` + `:memory:` short-circuit + `ToUpperInvariant()` canonicalization (spike §3 semantics under validator's `StringComparison.Ordinal` compare).
- Authored `SqLiteRelationalProducerQueue<T>` — mirrors SqlServer counterpart (~180 lines) without sealed-type transaction guard (SQLite handlers operate at `IDbConnection`/`IDbTransaction` interface level throughout).
- DI registrations in `SqLiteMessageQueueSharedInit`: `IExternalDbNameExtractor`, `ExternalTransactionValidator`, 3× `RegisterConditional` for `IProducerQueue<>`/`IRelationalProducerQueue<>`/`RelationalProducerQueue<>` → `SqLiteRelationalProducerQueue<>`. Swapped `IConnectionInformation` registration to use the new normalized wrapper.
- `HandleExternalTransaction` fork in sync + async send handlers (`SendMessageCommandHandler.Handle` and `SendMessageCommandHandlerAsync.HandleAsync`). Fork reuses caller's connection+tx; never commits/rolls-back/disposes/closes them.
- Deleted obsolete `SqliteProducerDoesNotImplementRelationalTests.cs` (asserted deprecated behavior per CONTEXT-5 §3a reversal).

Commit `2d9c7b94`.

## Files Modified
- `SqLiteExternalDbNameExtractor.cs` (new, with empty-string guard added in PLAN-3.2)
- `SqliteNormalizedConnectionInformation.cs` (new, root assembly)
- `SqLiteRelationalProducerQueue.cs` (new, ~180 lines)
- `SqLiteMessageQueueSharedInit.cs` (+15 lines outbox registration block + IConnectionInformation wrapper swap)
- `SendMessageCommandHandler.cs` (+100 lines fork + helper)
- `SendMessageCommandHandlerAsync.cs` (+100 lines async fork)
- Deleted: `SqliteProducerDoesNotImplementRelationalTests.cs`

## Decisions Made
- Empty-string guard added in extractor + wrapper (PLAN-3.2 surfaced `Path.GetFullPath("")` throws).
- Upper-case canonicalization for OrdinalIgnoreCase semantics under Ordinal comparator (spike §3 + CLAUDE.md string-comparator drift lesson).
- No `SqLiteRelationalProducerQueue<T>` transaction-type guard (SqlServer's `GuardSqlTransaction` is for sealed-type safety; SQLite handlers operate at interface level).
- Deferred fork unit tests to Phase 7 integration coverage (structural mirror to SqlServer/PG outbox-milestone tests).

## Verification
| Gate | Result |
|---|---|
| Release build | PASS (0 errors) |
| SQLite tests | 141/141 then 153/153 after PLAN-3.x additions |
