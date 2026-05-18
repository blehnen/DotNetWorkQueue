# Research: Phase 4 — PostgreSQL Inbox `IWorkerNotification` Implementation

Scope: Direct PostgreSQL counterpart of Phase 3's SqlServer work. Same shape, Npgsql substituted for SqlClient. The five Phase 3 lessons (see `.shipyard/phases/3/VERIFICATION.md` §"Phase-3-specific lessons") apply verbatim.

---

## §1 PostgreSQL transport file inventory

| Concern | File | Notes |
|---|---|---|
| ConnectionHolder (internal) | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/ConnectionHolder.cs` | Line 32: `internal class ConnectionHolder : IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>`. Line 89: `public NpgsqlTransaction Transaction { get; set; }`. Same shape as SqlServer's `ConnectionHolder`. |
| Init (DI registrations) | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` | `RegisterImplementations(IContainer, RegistrationTypes, QueueConnection)` at line 55. Outbox-block `RegisterConditional` ends at line 74. `//**all` comment + general registrations start at line 76. **Insert point for the new inbox factory-delegate block: between lines 74 and 76.** |
| Receive path | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueReceive.cs` | `GetConnectionAndSetOnContext(IMessageContext)` at line 153. Returns the `IConnectionHolder`. **Insert point for the new ConnectionHolder setter: just before `return connection;` at line ~170** — identical placement to Phase 3. |
| Tests directory | `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/` | Existing sibling files: `PostgreSqlExternalDbNameExtractorTests.cs`, `PostgreSqlRelationalProducerQueueTests.cs`. Mirror Phase 3's `SqlServerRelationalWorkerNotification[Registration]Tests.cs` shape. |

## §2 Transport-option type names (Npgsql ↔ SqlClient substitution table)

| SqlServer (Phase 3) | PostgreSQL (Phase 4) |
|---|---|
| `SqlServerMessageQueueTransportOptions` | `PostgreSqlMessageQueueTransportOptions` |
| `ISqlServerMessageQueueTransportOptionsFactory` | `IPostgreSqlMessageQueueTransportOptionsFactory` |
| `SqlConnection` | `NpgsqlConnection` |
| `SqlTransaction` | `NpgsqlTransaction` |
| `SqlCommand` | `NpgsqlCommand` |
| `using Microsoft.Data.SqlClient;` | `using Npgsql;` |
| `Microsoft.Data.SqlClient.SqlException` (in error-path tests) | `NpgsqlException` (if needed) |

Note: file/folder name is `PostgreSQL` (all-caps "SQL") but type-name prefix is `PostgreSql` (lowercase "q"). This is the existing convention — see `PostgreSqlExternalDbNameExtractor` in `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/`.

`ITransportOptionsFactory` (the shared abstraction) is the same on both transports — the factory-delegate's `container.GetInstance<ITransportOptionsFactory>().Create()` works identically, the only change is the cast target on the returned options instance.

## §3 Receive-path edit location (confirmed)

`Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueReceive.cs` lines 153-171:

```csharp
private IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> GetConnectionAndSetOnContext(IMessageContext context)
{
    var connection = _connectionFactory.Create();
    context.Set(_sqlHeaders.Connection, connection);

    if (!_configuration.Options().EnableHoldTransactionUntilMessageCommitted)
    {
        context.Commit += ContextOnCommit;
        context.Rollback += ContextOnRollback;
    }
    else
    {
        context.Commit += ContextOnCommitTransaction;
        context.Rollback += ContextOnRollbackTransaction;
    }
    context.Cleanup += Context_Cleanup;
    return connection;  // ← insert PLAN-2.1 pattern-match BEFORE this line
}
```

Identical shape to Phase 3's SqlServer counterpart. Pattern-match insertion is mechanically identical with `SqlServerRelationalWorkerNotification` → `PostgreSqlRelationalWorkerNotification` substitution.

## §4 Init insert point (confirmed)

`Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` line 74 ends the outbox `RegisterConditional` block. Line 76 begins the `//**all` general registrations block. Insertion of the inbox factory-delegate goes between these — identical to where Phase 3 placed the SqlServer inbox block (after outbox `RegisterConditional`, before general registrations).

## §5 ITransportOptionsFactory access — same as SqlServer

Line 87 of `PostgreSQLMessageQueueInit.cs`:
```csharp
container.Register<ITransportOptionsFactory, TransportOptionsFactory>(LifeStyles.Singleton);
```

Line 99-103 (the `IBaseTransportOptions` precedent that PLAN-1.1 should mirror):
```csharp
container.Register<IBaseTransportOptions>(() =>
{
    try { return (IBaseTransportOptions)container.GetInstance<ITransportOptionsFactory>().Create(); }
    catch { return new PostgreSqlMessageQueueTransportOptions(); }
}, LifeStyles.Singleton);
```

The Phase 4 inbox factory-delegate uses the same shape:
- `container.GetInstance<ITransportOptionsFactory>().Create()` returns `ITransportOptions` (the shared base).
- Cast to `PostgreSqlMessageQueueTransportOptions`.
- Read `EnableHoldTransactionUntilMessageCommitted`.
- Wrap in try/catch with fallback to `false` (per Phase 3 lesson 1).

## §6 Test seam — `QueueContainer<PostgreSQLMessageQueueInit>(register, setOptions)` with mocked `ITransportOptionsFactory`

Identical to Phase 3 PLAN-2.2's seam, with `SqlServerMessageQueueInit` → `PostgreSQLMessageQueueInit` and stub-options-type `SqlServerMessageQueueTransportOptions` → `PostgreSqlMessageQueueTransportOptions`.

Existing test pattern reference: `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/PostgreSqlConnectionInformationTests.cs` (or `PostgreSqlExternalDbNameExtractorTests.cs`) for MSTest 3.x conventions and NSubstitute usage.

## §7 Risks / pitfalls for Phase 4 architect

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Re-discovering Phase 3 lesson 1 (factory-delegate try/catch) | Low if plan author follows CONTEXT-4 | High — would break existing PG tests at container.Verify time | Bake try/catch into PLAN-1.1 Task 2 from the outset |
| Sealed-type cast `(NpgsqlConnection)` or `(NpgsqlTransaction)` in handler code | Medium (Npgsql tutorials often cast) | High — breaks NSubstitute mocking + CLAUDE.md rule + ROADMAP success criterion #4 | Grep guard in PLAN-1.1 verification: `grep -nE "\(NpgsqlConnection\)\|\(NpgsqlTransaction\)" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs` → exit 1 |
| `NpgsqlTransaction` NSubstitute proxy fail (sealed) | Certain | Low — handled in Phase 3 via test rename pattern | Name the contract test for what it actually proves; defer non-null-return coverage to Phase 7 |
| Naming drift: using `Postgres` or `PostgreSQL` (uppercase) instead of `PostgreSql` (lowercase q) for type prefixes | Low | Low — caught at compile time | CONTEXT-4 §"Decisions" §2 explicitly locks `PostgreSqlRelationalWorkerNotification` |
| `Tx` token in identifiers or commit messages | Low | Low | Grep guard in PLAN-1.1 verification |

## Phase 4 architect handoff summary

1. **Mirror Phase 3 verbatim.** Same wave layout (Wave 1: PLAN-1.1; Wave 2: PLAN-2.1 + PLAN-2.2 parallel), same task counts (2 + 1 + 2 = 5), same verification gates. Substitute `SqlServer`/`SqlClient` → `PostgreSql`/`Npgsql` throughout.
2. **Bake try/catch into PLAN-1.1 from the outset** — Phase 3's mid-build self-fix becomes Phase 4's first commit. Saves a retry cycle.
3. **Insert points:** init = `PostgreSQLMessageQueueInit.cs` between lines 74 and 76; receive = `PostgreSQLMessageQueueReceive.cs` just before `return connection;` at line ~170.
4. **Options-cast target:** `PostgreSqlMessageQueueTransportOptions`. Fallback type for try/catch: `new PostgreSqlMessageQueueTransportOptions()` (matches `IBaseTransportOptions` precedent at line 100).
5. **Test seam = `QueueContainer<PostgreSQLMessageQueueInit>(registerService, setOptions)` + mocked `ITransportOptionsFactory`** — identical to Phase 3.
