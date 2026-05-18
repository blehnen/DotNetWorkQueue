# Research: Phase 5 — SQLite Inbox + SQLite-Outbox Sweep (Combined)

**Date:** 2026-05-18
**Authoring posture:** Inline (subagent dispatches stalled twice; main session completed the investigation directly).
**Most load-bearing finding:** SQLite inbox is **NOT FEASIBLE** under Phase 5's current scope — see §2 verdict. CONTEXT-5 Decision §3 gate fires.

---

## §1 SQLite transport file inventory

| Concern | File | Notes |
|---|---|---|
| Init | `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueInit.cs` | Class: `SqLiteMessageQueueInit : SqLiteMessageQueueSharedInit`. Public. `RegisterImplementations` at ~line 36 forwards to base. |
| Shared init | `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueSharedInit.cs` | Base class. The bulk of registrations happen here. Insertion points likely here, not in the derived init. |
| Receive | `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueReceive.cs` | Internal, 220 lines. **Has NO `GetConnectionAndSetOnContext` method.** Receive happens via `_receiveMessages.GetMessage(context)`. |
| Receive helper | `Source/DotNetWorkQueue.Transport.SQLite/Basic/Message/ReceiveMessage.cs` | Internal class. `GetMessage(context)` calls `_receiveMessage.Handle(new ReceiveMessageQuery<IDbConnection, IDbTransaction>(null, null, ...))` — **passes NULL connection AND NULL transaction**. |
| Query handler | `Source/DotNetWorkQueue.Transport.SQLite/Basic/QueryHandler/ReceiveMessageQueryHandler.cs` | Lines 87-114: creates connection + transaction INSIDE `using` blocks; returns the dequeued message; transaction is disposed at end of `Handle()`. No persistence. |
| Send handler | `Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessageCommandHandler.cs` | Internal. Currently uses `IDbFactory` + interface-level access. **No `HandleExternalTx` fork yet** — outbox milestone didn't ship SQLite. |
| Send handler async | `Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` | Sibling of the sync handler. |
| Send batch | `Source/DotNetWorkQueue.Transport.SQLite/Basic/CommandHandler/SendMessage.cs` | Likely the batch path. Confirm during PLAN-1.2 authoring. |
| HoldConnection | `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteHoldConnection.cs` | UNRELATED to hold-transaction. Keeps `:memory:` DB connections alive across operations to prevent GC-driven DB loss. Don't confuse with inbox hold-tx semantics. |
| Connection info | `Source/DotNetWorkQueue.Transport.SQLite/SqliteConnectionInformation.cs` | Note: in root assembly, NOT under `Basic/`. Public class. |
| Tests directory | `Source/DotNetWorkQueue.Transport.SQLite.Tests/` | Survey at §6. |

**SQLite client library:** `Microsoft.Data.Sqlite` (per `ConfigurationExtensions.cs`, `DbDataSource.cs`, `ReaderAsync.cs`, `DbFactory.cs`). NOT `System.Data.SQLite` (though `System.Data.SQLite` is imported by some legacy types like `ReceiveMessageQueryHandler.cs:22`).

---

## §2 SQLite inbox seam analysis (LOAD-BEARING)

### Verdict: **NOT FEASIBLE within Phase 5's L-sized scope.** REFACTOR REQUIRED.

### Evidence chain

**The `EnableHoldTransactionUntilMessageCommitted` option is DECLARED but NEVER READ in the SQLite transport.**

Grep over `Source/DotNetWorkQueue.Transport.SQLite`:
```
Source/DotNetWorkQueue.Transport.SQLite/Basic/SQLiteMessageQueueTransportOptions.cs:278:
    public bool EnableHoldTransactionUntilMessageCommitted
```
That's the ONLY hit. No reads. No branches. No conditional code paths.

By contrast, SqlServer has 4 hits (`ConnectionHolder`, `SQLServerMessageQueueReceive`, options class). PostgreSQL has 5 hits.

**The SQLite receive pipeline creates and discards the transaction inside the dequeue's `using` blocks** (`ReceiveMessageQueryHandler.cs:94-113`):

```csharp
using (var connection = _dbFactory.CreateConnection(_connectionInformation.ConnectionString, false))
{
    connection.Open();
    using (var transaction = _dbFactory.CreateTransaction(connection).BeginTransaction())
    {
        using (var selectCommand = connection.CreateCommand())
        {
            selectCommand.Transaction = transaction;
            // ... build dequeue, execute, return message
            return _messageDeQueue.HandleMessage(connection, transaction, reader, commandString);
        }
    }
}
```

When `Handle()` returns, the transaction is disposed by the outermost `using` (or whatever happens inside `_messageDeQueue.HandleMessage` — but the `using` enforces disposal at scope exit). The library carries NO transaction state across the boundary between "dequeue the message" and "invoke the user handler."

### What "SQLite inbox" actually requires

Per PROJECT.md §Functional New Public API + the Phase 2 `IRelationalWorkerNotification` contract, the user handler must receive a non-null `DbTransaction` that is committed/rolled back AFTER the handler returns. For SQLite, this means:

1. **Restructure the receive path** so the dequeue transaction is created BEFORE the user handler runs, persists THROUGH the handler invocation, and is committed AFTER the handler returns successfully (or rolled back on throw).
2. **Wire this persistent transaction** into a per-message state object the inbox notification can read from.
3. **Implement `EnableHoldTransactionUntilMessageCommitted` semantics for the first time on SQLite** — this is essentially the "hold transaction" feature that SqlServer/PG already have via `ConnectionHolder<,,>` but SQLite has never had.

This is **NOT** a 1-2 file edit. It's a substantive architectural addition to the SQLite receive pipeline. Likely files affected:
- `SqLiteMessageQueueReceive.cs` — needs to bracket the user-handler invocation with tx lifecycle.
- `ReceiveMessageQueryHandler.cs` — must remove the inner `using` for the transaction; needs a new mechanism to hand the tx ownership upstream.
- `Message/ReceiveMessage.cs` — must surface the tx (or a tx-holder) to the receive caller.
- `IMessageContext` integration — likely new per-context state to hold the tx, mirroring SqlServer's `context.Set(_sqlHeaders.Connection, connection)` pattern.
- DI registrations — new transport-options-driven branches in `SqLiteMessageQueueInit` for the new tx-holding path.

Realistic effort: easily another L-sized phase (8-12 hours) on top of the inbox-notification work that Phase 5 already scopes. Combined with the outbox sweep + tests, full Phase 5 ships at XL+ (20-30 hours).

### CONTEXT-5 Decision §3 gate fires

User's locked decision in CONTEXT-5: "Researcher proves the seam first; if substantive refactor needed, file an ISSUE and ship only outbox in Phase 5 (Recommended)."

**Recommendation: scope-reduce Phase 5 to outbox-only.** File a new ISSUE (`ISSUE-NEW`, will be `ISSUE-043` based on existing numbering) for "SQLite never implemented `EnableHoldTransactionUntilMessageCommitted` semantics; required for SQLite inbox." Address in a follow-up phase or future milestone.

User should approve the scope reduction before architect proceeds. Surfacing now.

---

## §3 SQLite outbox seam

Outbox half remains FEASIBLE — structurally a mirror of SqlServer/PG outbox milestone work, with the additional caveat that SQLite has never had `HandleExternalTx` forks either (the outbox milestone shipped only SqlServer + PG; SQLite was explicit deferred per outbox-milestone PROJECT.md scope).

### Send handlers
- `SendMessageCommandHandler.cs` and `SendMessageCommandHandlerAsync.cs` and `SendMessage.cs` (likely batch) currently use `IDbFactory.CreateConnection(connStr, forMemoryHold)` to create a connection per Send call. The `HandleExternalTx` fork pattern (from SqlServer/PG outbox milestone) adds:

```csharp
if (command.ExternalTransaction != null)
{
    // Use the caller's connection + tx; never Commit/Rollback/Dispose/Close.
    using (var sqlCommand = ((IDbConnection)command.ExternalTransaction.Connection).CreateCommand())
    {
        sqlCommand.Transaction = command.ExternalTransaction;
        // ... execute against caller's tx
    }
    return ...;
}
// existing path: create our own connection + tx, manage lifecycle ourselves.
```

SQLite's existing handlers already use `IDbConnection`/`IDbTransaction` interface-level access — no sealed-type cast risk during the fork. Pattern transfers cleanly.

### DI registration (`SqLiteMessageQueueInit` block)
Mirror outbox milestone's `RegisterConditional` block in SqlServer/PG init:
```csharp
container.Register<IExternalDbNameExtractor, SqliteExternalDbNameExtractor>(LifeStyles.Singleton);
container.Register<ExternalTransactionValidator>(LifeStyles.Singleton);
container.RegisterConditional(typeof(IProducerQueue<>), typeof(SqliteRelationalProducerQueue<>), LifeStyles.Singleton);
container.RegisterConditional(typeof(IRelationalProducerQueue<>), typeof(SqliteRelationalProducerQueue<>), LifeStyles.Singleton);
container.RegisterConditional(typeof(RelationalProducerQueue<>), typeof(SqliteRelationalProducerQueue<>), LifeStyles.Singleton);
```

Insertion point: in `SqLiteMessageQueueSharedInit.cs` `RegisterImplementations` method, after `base.RegisterImplementations(...)` and the standard general-registrations block. Confirm exact line during PLAN-1.2 authoring.

### New per-transport classes
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqliteExternalDbNameExtractor.cs` — implements `IExternalDbNameExtractor`. Per spike §3 semantics.
- `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqliteRelationalProducerQueue.cs` (generic `<T>`) — mirror `SqlServerRelationalProducerQueue<T>` and `PostgreSqlRelationalProducerQueue<T>`.
- `Source/DotNetWorkQueue.Transport.SQLite/SqliteNormalizedConnectionInformation.cs` — symmetric-normalization wrapper. Lives at root (matches `SqliteConnectionInformation.cs` placement).

---

## §4 SQLite outbox extractor + NormalizedConnectionInformation wrapper

Per CONTEXT-5 Decision §2, both bundled in PLAN-1.2.

### `SqliteExternalDbNameExtractor`
```csharp
public string Extract(DbConnection connection)
{
    var raw = connection.DataSource;
    if (string.Equals(raw, ":memory:", StringComparison.Ordinal))
        return ":memory:";
    return Path.GetFullPath(raw);
}
```
Comparison is performed by the validator with `StringComparer.OrdinalIgnoreCase` (must verify — see below).

### Symmetric normalization on the validator side
Need to investigate `ExternalTransactionValidator` from the outbox milestone:
- File: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` (or similar)
- What comparator does it use today?

Earlier scout: PG and SqlServer both registered `ExternalTransactionValidator` as a singleton in their init files. Need to confirm the comparator's string-compare semantics. If `Ordinal`, the wrapper must `.ToUpperInvariant()`. If `OrdinalIgnoreCase`, the wrapper just needs `Path.GetFullPath()`. PLAN-1.2 architect should READ the validator file and decide.

`SqliteNormalizedConnectionInformation`:
- Wraps `IConnectionInformation` (or extends `SqliteConnectionInformation`).
- Overrides `Container` (or whatever property holds the DB-name-equivalent) to apply the SAME `Path.GetFullPath()` + `:memory:` short-circuit.
- Symmetric with the extractor side.

Registration: PLAN-1.2 swaps the default `SqliteConnectionInformation` registration to use the wrapper when the outbox path is active.

---

## §5 Outbox milestone HandleExternalTx pattern reference

**File:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs`
**Pattern:** `if (command.ExternalTransaction != null) { ... external-tx path; never Commit/Rollback/Dispose ... } else { ... existing self-managed path ... }`

Architect should READ that file end-to-end during PLAN-1.2 authoring to confirm the exact branch shape (e.g., where the `using` for the connection is hoisted vs skipped, how cleanup delegates are wired). Lines from outbox milestone Phase 3 commits (already in git log).

Verification grep guard for PLAN-2.2 outbox tests: mock `IDbConnection` and `IDbTransaction`, run the send handler with `command.ExternalTransaction != null`, assert that `mockConnection.Received(0).Dispose()` and `mockTransaction.Received(0).Commit()` etc. — PROJECT.md §Success Criteria #8.

---

## §6 Test framework + helpers in SQLite.Tests

`Source/DotNetWorkQueue.Transport.SQLite.Tests/`:
- Has subdirectory structure mirroring SqlServer (verified). Sibling files include connection-info tests.
- MSTest 3.x + NSubstitute + AutoFixture + FluentAssertions 6.12.2 (project-wide convention; verified in earlier phases).
- No existing `SqliteExternalDbNameExtractor*.cs` (extractor is new) — confirm via Glob during PLAN-2.2 authoring.

For PLAN-2.2 outbox tests, mirror SqlServer's `SqlServerExternalDbNameExtractorTests.cs` (proven pattern from earlier phases).

For inbox tests (if scope is reduced to outbox-only, PLAN-2.1 is skipped — but if user later authorizes the SQLite hold-tx work in a follow-up, the test seam mirrors Phase 3/4 PLAN-2.2 closely).

---

## §7 `SqLiteMessageQueueInit` `using` and registration audit

**Line 30:** `namespace DotNetWorkQueue.Transport.SQLite.Basic` (confirmed).
**Class:** `SqLiteMessageQueueInit : SqLiteMessageQueueSharedInit` at line 33. Lightweight — most registrations are in the SHARED init parent.

**Critical:** `using DotNetWorkQueue.Queue;` is NOT in the SqLiteMessageQueueInit.cs `using` block (matches Phase 4 PG carry-over note). However, since Phase 5 inbox is being scope-reduced, this gap doesn't bite immediately. PLAN-1.2 (outbox) doesn't need `DotNetWorkQueue.Queue` either — it works on `IExternalDbNameExtractor` and `IRelationalProducerQueue<T>`. Note the gap for future SQLite inbox work.

**`SqLiteMessageQueueSharedInit.cs`:** This is where the bulk of DI registrations live. PLAN-1.2 will mostly edit THIS file (the derived `SqLiteMessageQueueInit` may or may not need touching). Architect to confirm during PLAN-1.2 authoring.

---

## §8 Naming convention final survey

**Dominant pattern in `Source/DotNetWorkQueue.Transport.SQLite/Basic/`:** `SqLite*` with capital L.

Survey:
| File | Class name | Pattern |
|---|---|---|
| `SqLiteMessageQueueInit.cs` | `SqLiteMessageQueueInit` | SqLite |
| `SqLiteMessageQueueSharedInit.cs` | `SqLiteMessageQueueSharedInit` | SqLite |
| `SqLiteMessageQueueReceive.cs` | `SqLiteMessageQueueReceive` | SqLite |
| `SqLiteMessageQueueCreation.cs` | `SqLiteMessageQueueCreation` | SqLite |
| `SqLiteMessageQueueSchema.cs` | `SqLiteMessageQueueSchema` | SqLite |
| `SqLiteHoldConnection.cs` | `SqLiteHoldConnection` | SqLite |
| `SQLiteTransaction.cs` | `SqLiteTransactionWrapper` | SqLite (in SQLite-cap file) |
| `SqLiteMessageQueueTransportOptionsFactory.cs` | `SqLiteMessageQueueTransportOptionsFactory` | SqLite |
| `SQLiteMessageQueueTransportOptions.cs` | `SqLiteMessageQueueTransportOptions` | SqLite (in SQLite-cap file) |

**Exceptions** (with lowercase L pattern):
| File | Class | Pattern | Why |
|---|---|---|---|
| `SqliteConnectionInformation.cs` (root assembly) | `SqliteConnectionInformation` | Sqlite | Possibly Microsoft.Data.Sqlite-influenced (their `SqliteConnection` is `Sqlite`). Public surface lives in root assembly. |

**Decision for new Phase 5 types:** **Use `SqLite` (capital L)** for the new internal types in `Transport.SQLite/Basic/`:
- `SqLiteExternalDbNameExtractor` (NOT `SqliteExternalDbNameExtractor` per CONTEXT-5 §4's tentative guess)
- `SqLiteRelationalProducerQueue<T>`
- `SqLiteNormalizedConnectionInformation` (if a wrapper, lives at root — but follow root convention which is `Sqlite` lowercase). Hmm.

**Tricky case:** `SqliteNormalizedConnectionInformation` would extend or compose `SqliteConnectionInformation` (root-assembly type, lowercase pattern). The wrapper should match: `SqliteNormalizedConnectionInformation` (lowercase L), placed in `Source/DotNetWorkQueue.Transport.SQLite/SqliteNormalizedConnectionInformation.cs` (root).

**Per-Basic types** (extractor, producer queue): `SqLite*` capital L for internal consistency. Architect to confirm during PLAN-1.2 authoring.

(This contradicts my CONTEXT-5 §4 tentative — CONTEXT-5 should be amended if user opts to proceed.)

---

## §9 Risks / pitfalls for Phase 5 architect

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Proceeding with inbox half despite §2 verdict | LOW (CONTEXT-5 gate enforces) | HIGH (XL+ scope blowup) | Stop, surface to user, scope-reduce to outbox-only. |
| `SqLite` vs `Sqlite` naming drift in new types | Medium | Low | Use `SqLite*` capital L for new `Basic/` types; reserve `Sqlite*` for root-assembly types. §8 above. |
| `(SqliteConnection)` / `(SqliteTransaction)` sealed-type casts | Low (SQLite handlers already use `IDbConnection`/`IDbTransaction`) | High if introduced | Grep guards in PLAN-1.2 verification: `grep -nE "\(SqliteConnection\)\|\(SqliteTransaction\)"`. |
| Symmetric normalization asymmetry (extractor side normalizes; validator side doesn't) | Medium | High (false-mismatch validator errors) | PLAN-1.2 architect READS `ExternalTransactionValidator` to confirm the comparator's string-compare semantics; ensures wrapper applies identical canonicalization. |
| `forMemoryHold` flag on `IDbFactory.CreateConnection` | Low | Unknown | Investigate during PLAN-1.2; relates to `SqLiteHoldConnection` keepalive for `:memory:` DBs; should not affect outbox semantics (caller's connection is reused, never created by the library). |
| `using System.Data.SQLite;` in `ReceiveMessageQueryHandler.cs:22` | Low | Documentation only | Legacy import, never used (the file uses `IDbConnection` interface throughout). Not material to Phase 5. Could be cleaned up but out of scope. |

---

## Phase 5 architect handoff summary

1. **🛑 INBOX FEASIBILITY GATE FIRES.** SQLite has `EnableHoldTransactionUntilMessageCommitted` declared but never read anywhere. The transport has no hold-transaction semantics; the dequeue tx is created and disposed inside `ReceiveMessageQueryHandler.Handle()`. Implementing SQLite inbox requires building hold-tx semantics first — substantive refactor (~8-12h extra) on top of the inbox-notification work. Combined Phase 5 would push from L (10-14h) to XL+ (20-30h). **CONTEXT-5 Decision §3 mandates: file ISSUE-NEW, scope-reduce Phase 5 to outbox-only.** Surface to user before architect proceeds.

2. **Outbox half remains FEASIBLE.** All SqlServer/PG outbox-milestone patterns transfer to SQLite (already uses `IDbConnection`/`IDbTransaction` interface-level access; `HandleExternalTx` fork mechanism applies cleanly). Reduced Phase 5 scope: PLAN-1.2 (outbox wiring) + PLAN-2.2 (outbox tests).

3. **Naming: `SqLite*` (capital L) for new `Basic/` types** (extractor, producer queue), matching the dominant in-folder convention. `Sqlite*` (lowercase) reserved for root-assembly types (`SqliteConnectionInformation`, future `SqliteNormalizedConnectionInformation` wrapper).

4. **Symmetric normalization is mandatory.** PLAN-1.2 architect must READ `ExternalTransactionValidator` and confirm the string-compare semantics. Wrapper applies identical canonicalization.

5. **`using DotNetWorkQueue.Queue;` gap in SQLite init** noted but immaterial to outbox-only scope. Document for future SQLite inbox work.

6. **No `(SqliteConnection)` / `(SqliteTransaction)` sealed casts** — SQLite handlers already operate at `IDbConnection`/`IDbTransaction` interface level. Grep guards in PLAN-1.2.

7. **Bulk of DI registrations are in `SqLiteMessageQueueSharedInit.cs`**, not the derived `SqLiteMessageQueueInit.cs`. PLAN-1.2 architect to confirm exact edit location.
