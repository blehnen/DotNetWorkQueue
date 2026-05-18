# Plan 2.2: SQLite Outbox Wiring — Extractor + Wrapper + Producer Queue + HandleExternalTx Forks

## Context

SQLite counterpart of outbox milestone Phases 3 + 4. Adds the `IRelationalProducerQueue<T>` registration, the `SqLiteExternalDbNameExtractor`, the symmetric `SqliteNormalizedConnectionInformation` wrapper (per spike §3), and `HandleExternalTx` forks in all SQLite send handlers (sync, async, batch). PROJECT.md §SC #8 mandates the "zero `Commit`/`Rollback`/`Dispose`/`Close` on caller tx" invariant.

## Dependencies
PLAN-1.1 (parallel-safe with PLAN-2.1 — different files).

## Tasks

### Task 1: SQLite extractor + symmetric normalization wrapper
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteExternalDbNameExtractor.cs` (create)
- `Source/DotNetWorkQueue.Transport.SQLite/SqliteNormalizedConnectionInformation.cs` (create, root assembly — note `Sqlite` lowercase per RESEARCH.md §8)

**Action:** create

**Description:**

`SqLiteExternalDbNameExtractor : IExternalDbNameExtractor` (note: capital L per RESEARCH.md §8 for `Basic/` placement). `Extract(DbConnection conn)` returns:
- `":memory:"` (verbatim) if `conn.DataSource == ":memory:"` (case-sensitive — SQLite keyword).
- `Path.GetFullPath(conn.DataSource)` otherwise.

Spike §3 + ROADMAP success criterion #7. Spec-locked behavior; no platform-conditional logic.

`SqliteNormalizedConnectionInformation` (root assembly, lowercase): extends or composes `SqliteConnectionInformation`. Overrides `Container` (or whatever property the validator compares against — confirm by READING `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` first) to apply identical `Path.GetFullPath()` + `:memory:` short-circuit.

**CRITICAL: Read `ExternalTransactionValidator.cs` BEFORE finalizing this file.** Confirm the comparator's string-compare semantics:
- If `OrdinalIgnoreCase`: wrapper applies `Path.GetFullPath()` only.
- If `Ordinal`: wrapper applies `Path.GetFullPath()` + `.ToUpperInvariant()` (and the extractor in `Basic/` must apply the same upper-casing — symmetric per spike §3 + CLAUDE.md "string-comparator drift" lesson).

LGPL-2.1 18-line header on both new files (byte-copy from `SqLiteMessageQueueInit.cs:1-18` for the `Basic/` extractor; `SqliteConnectionInformation.cs:1-18` for the root wrapper).

**Acceptance Criteria:**
- Extractor exists with the spike-§3 semantics.
- Wrapper exists with symmetric normalization.
- ExternalTransactionValidator's compare semantics CONFIRMED and documented in the wrapper file's XML doc.
- No `Tx` token.
- Release build clean.

### Task 2: `SqLiteRelationalProducerQueue<T>` + DI registrations
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteRelationalProducerQueue.cs` (create)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueSharedInit.cs` (modify — outbox registration block)

**Action:** create + modify

**Description:**

Mirror `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs` and `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs` — likely just inherits the shared `RelationalProducerQueue<T>` from `Transport.RelationalDatabase`. Read the SqlServer/PG versions and copy the shape; substitute `SqLite` (capital L per RESEARCH.md §8) for the prefix.

Outbox DI registration block in `SqLiteMessageQueueSharedInit.RegisterImplementations` (mirror SqlServer/PG init outbox blocks exactly):
```csharp
// Outbox-pattern producer wiring (SQLite side — Phase 5 sweep).
container.Register<IExternalDbNameExtractor, SqLiteExternalDbNameExtractor>(LifeStyles.Singleton);
container.Register<ExternalTransactionValidator>(LifeStyles.Singleton);
container.RegisterConditional(typeof(IProducerQueue<>), typeof(SqLiteRelationalProducerQueue<>), LifeStyles.Singleton);
container.RegisterConditional(typeof(IRelationalProducerQueue<>), typeof(SqLiteRelationalProducerQueue<>), LifeStyles.Singleton);
container.RegisterConditional(typeof(RelationalProducerQueue<>), typeof(SqLiteRelationalProducerQueue<>), LifeStyles.Singleton);
```

Also: change `IConnectionInformation` registration to use the new `SqliteNormalizedConnectionInformation` wrapper. Builder to determine exact registration shape based on existing `SqliteConnectionInformation` binding.

**Acceptance Criteria:**
- `SqLiteRelationalProducerQueue<T>` exists, mirrors SqlServer/PG counterparts.
- 3 `RegisterConditional` calls present matching the outbox-block pattern.
- `IConnectionInformation` registration uses the normalized wrapper.
- Release build clean. No regression in existing tests.

### Task 3: `HandleExternalTx` forks in send handlers (sync + async + batch)
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessageCommandHandler.cs` (modify)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` (modify)
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessage.cs` (modify — batch path; confirm)

**Action:** modify

**Description:**

For each send handler, add a fork at the start:
```csharp
if (command.ExternalTransaction != null)
{
    return HandleExternalTransaction(command);  // new method
}
// existing self-managed connection path unchanged
```

`HandleExternalTransaction(command)`:
- Use `command.ExternalTransaction.Connection` as the connection (do NOT open/close it — caller owns it).
- Use `command.ExternalTransaction` as the tx on the `IDbCommand.Transaction`.
- Execute the SQL.
- **NEVER call `Commit()`, `Rollback()`, `Dispose()`, or `Close()` on the caller's `Connection` or `Transaction`** — caller owns lifecycle (PROJECT.md §SC #8).
- Skip the retry decorator on this path (the caller's tx may not be retry-safe — outbox milestone precedent).

READ `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs` for the canonical `HandleExternalTransaction` shape (from outbox milestone Phase 3) before authoring. Substitute SQLite-specific dependencies.

**Acceptance Criteria:**
- All 3 handlers have the fork branch.
- `HandleExternalTransaction` method present on each, mirroring SqlServer's shape.
- Caller-tx path uses `IDbConnection`/`IDbTransaction` interface-level access only — no sealed casts.
- No `Commit`/`Rollback`/`Dispose`/`Close` calls on `command.ExternalTransaction` or its `.Connection` in any of the 3 forked paths.
- Release build clean.

## Verification

```bash
# Gate 1: Release build clean.
dotnet build "Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj" -c Release -p:CI=true --nologo 2>&1 | tail -5
# expect 0 errors.

# Gate 2: existing tests still pass.
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --nologo 2>&1 | tail -3
# expect 0 failures.

# Gate 3: new extractor + wrapper files exist.
ls Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteExternalDbNameExtractor.cs Source/DotNetWorkQueue.Transport.SQLite/SqliteNormalizedConnectionInformation.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteRelationalProducerQueue.cs

# Gate 4: outbox RegisterConditional block present.
grep -n "RegisterConditional.*SqLiteRelationalProducerQueue" Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueSharedInit.cs
# expect 3 matches.

# Gate 5: HandleExternalTx fork present in all 3 send handlers.
grep -l "command.ExternalTransaction != null" Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessageCommandHandler.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessage.cs
# expect 3 files matched.

# Gate 6: no Tx token; no sealed-type casts.
grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteExternalDbNameExtractor.cs Source/DotNetWorkQueue.Transport.SQLite/SqliteNormalizedConnectionInformation.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteRelationalProducerQueue.cs
grep -nE "\(SqliteConnection\)|\(SqliteTransaction\)" Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessageCommandHandler.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessage.cs
# both expect exit 1.
```
