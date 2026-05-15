# Research: Phase 4 — PostgreSQL Implementation + Unit Tests

## Context

Phase 4 mirrors Phase 3 (SqlServer) for PostgreSQL. The outbox feature foundation (Phase 2)
and SqlServer reference implementation (Phase 3) are both shipped. This document provides
the PG-specific concrete intel the architect needs before authoring PLAN-1.1, PLAN-2.1,
and PLAN-2.2 for Phase 4. Phase 3 SUMMARYs/REVIEWs serve as structural templates; this
research focuses on PG-specific deviations and integration points.

---

## §1. PostgreSQL Handler File Layout

### Confirmed paths

| File | Absolute path |
|------|--------------|
| Sync handler | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs` |
| Async handler | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` |
| Shared SQL builders | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessage.cs` |

### Sync handler (`SendMessageCommandHandler.cs`) — insertion points

The **sync** `Handle(SendMessageCommand commandSend)` method runs from **line 99**.

Structure (current, pre-Phase-4):
```
line  99 :  public long Handle(SendMessageCommand commandSend)
line 101 :  {
line 101-104 :  lazy-init block (_messageExpirationEnabled.HasValue check)
line 106-113 :  job-scheduler metadata extraction
line 115 :  using (var connection = new NpgsqlConnection(...))  ← self-managed-tx path starts
...
line 179 :  }  ← end of Handle()
line 181-224 :  CreateStatusRecord / CreateMetaDataRecord helpers (use NpgsqlConnection/NpgsqlTransaction)
```

**Insertion point for early-branch:** After the lazy-init block ends (line 104), BEFORE the
`var jobName = ...` line (line 106). Insert:
```csharp
if (commandSend.ExternalTransaction != null)
    return HandleExternalTx(commandSend);
```

**Insertion point for `HandleExternalTx` private method:** Before `CreateStatusRecord` at line 189
(i.e., between the closing brace of `Handle()` at line 179 and the `CreateStatusRecord` XML-doc at ~line 181).

### Async handler (`SendMessageCommandHandlerAsync.cs`) — insertion points

The **async** `HandleAsync(SendMessageCommand commandSend)` method runs from **line 101**.

Structure (current, pre-Phase-4):
```
line 101 :  public async Task<long> HandleAsync(SendMessageCommand commandSend)
line 103 :  {
line 103-106 :  lazy-init block
line 108-115 :  job-scheduler metadata extraction
line 117 :  using (var connection = new NpgsqlConnection(...))  ← self-managed-tx path
...
line 185 :  }  ← end of HandleAsync()
line 187-232 :  CreateStatusRecordAsync / CreateMetaDataRecordAsync helpers
```

**Insertion point for early-branch:** After line 106 (lazy-init end), before line 108 (`var jobName`):
```csharp
if (commandSend.ExternalTransaction != null)
    return await HandleExternalTxAsync(commandSend).ConfigureAwait(false);
```

**Insertion point for `HandleExternalTxAsync`:** Before `CreateStatusRecordAsync` at ~line 196.

### Critical structural difference from SqlServer: `_getTime` + `currentTime` parameter

**This is the single most important PG-vs-SqlServer divergence.**

- PG `SendMessageCommandHandler` has an additional injected field: `private readonly IGetTime _getTime` (line 49).
- PG `SendMessage.BuildMetaCommand` takes a `DateTime currentDateTime` parameter that SqlServer's version does NOT.
- In the self-managed-tx path, the handler materializes: `_getTime.GetCurrentUtcDate()` and passes it into `CreateMetaDataRecord(...)`.
- The `HandleExternalTx` fork MUST also call `_getTime.GetCurrentUtcDate()` and pass the result through to `CreateMetaDataRecord`.
- `IGetTime` is **already injected** in the PG handler constructor (line 72, parameter `getTimeFactory`) — no ctor change needed. `_getTime` is set at line 94: `_getTime = getTimeFactory.Create()`.

Failing to pass `currentTime` to `CreateMetaDataRecord` in the fork body is a **compile error**
because `CreateMetaDataRecord(TimeSpan?, TimeSpan, NpgsqlConnection, long, IMessage, IAdditionalMessageData, NpgsqlTransaction, DateTime)` has 8 parameters.

### Handler DI interfaces (both handlers)

Both handlers are registered under the same command-handler-with-output interfaces as SqlServer:
- Sync: `ICommandHandlerWithOutput<SendMessageCommand, long>` → `SendMessageCommandHandler`
- Async: `ICommandHandlerWithOutputAsync<SendMessageCommand, long>` → `SendMessageCommandHandlerAsync`

(Confirmed by inspection of `PostgreSQLMessageQueueInit.cs` decorator registrations at lines 179–214.)

### Job-exists query type parameters (PG-specific)

PG uses `NpgsqlConnection` and `NpgsqlTransaction` as type parameters:
```csharp
IQueryHandler<DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>, QueueStatuses>
ICommandHandler<SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>>
```

The `HandleExternalTx` fork must construct:
```csharp
new DoesJobExistQuery<NpgsqlConnection, NpgsqlTransaction>(jobName, scheduledTime, npgsqlConn, npgsqlTx)
new SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>(jobName, eventTime, scheduledTime, npgsqlConn, npgsqlTx)
```

### Parameter type differences (PG vs SqlServer)

PG uses `NpgsqlDbType` enum (from `NpgsqlTypes`) NOT `SqlDbType`. Body / header binary parameters use:
```csharp
command.Parameters.Add("@body", NpgsqlDbType.Bytea, -1);
command.Parameters.Add("@headers", NpgsqlDbType.Bytea, -1);
```
SqlServer used `SqlDbType.VarBinary`. The fork body must use `NpgsqlDbType.Bytea`.

### NpgsqlConnection / NpgsqlTransaction sealed-type confirmation (§5)

Both `NpgsqlConnection` and `NpgsqlTransaction` are sealed types. Per CLAUDE.md
"sync vs async handler mocking split" lesson:
- Sync handler tests use `IDbConnection`/`IDbCommand`/`IDbDataParameter` interfaces — feasible.
- Async handler tests must mock `DbConnection`/`DbCommand`/`DbDataReader` abstract base classes.
- Phase 4 structural smoke tests (reflection + source-grep) avoid this entirely — same approach as Phase 3.

---

## §2. PostgreSQL Init File

**Confirmed path:** `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs`

**Class name:** `PostgreSqlMessageQueueInit` (note mixed-case "Sql" in the class vs "SQL" in the filename).

### Insertion point for 5 new DI registrations

Insert immediately after line 62:
```csharp
init.RegisterStandardImplementations(container, Assembly.GetAssembly(GetType()));
```

And immediately before line 64 (the `//**all` section comment block).

**Exact insertion (mirroring SQLServerMessageQueueInit.cs lines 63–74):**
```csharp
// Phase 4: outbox-pattern producer wiring (PostgreSQL side)
container.Register<IExternalDbNameExtractor, PostgreSqlExternalDbNameExtractor>(LifeStyles.Singleton);
container.Register<ExternalTransactionValidator>(LifeStyles.Singleton);
// RegisterConditional preempts the open-generic IProducerQueue<> fallback in
// ComponentRegistration.RegisterFallbacks (also conditional) and preserves
// SimpleInjector's lazy-verification semantics for these open generics — plain
// Register triggers eager verification that surfaces pre-existing repo-wide
// diagnostic warnings on transient IDisposable types.
container.RegisterConditional(typeof(IProducerQueue<>), typeof(PostgreSqlRelationalProducerQueue<>), LifeStyles.Singleton);
container.RegisterConditional(typeof(IRelationalProducerQueue<>), typeof(PostgreSqlRelationalProducerQueue<>), LifeStyles.Singleton);
container.RegisterConditional(typeof(RelationalProducerQueue<>), typeof(PostgreSqlRelationalProducerQueue<>), LifeStyles.Singleton);
```

There is no `//override so that we can use schema as needed` comment block in
`PostgreSQLMessageQueueInit.cs` (unlike SqlServer which has one for `ITableNameHelper`).
The PG init goes directly into the `//**all` block. Insert BEFORE `//**all` at line 64.

**Required using directives to add to PostgreSQLMessageQueueInit.cs:**
```csharp
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
```
Both namespaces are already referenced by existing imports in the file (lines 33, 35 in the
pre-Phase-4 baseline) — no new using directives needed.

---

## §3. PostgreSQL Test Project Layout

**Test project root:** `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/`

### Confirmed existing folder structure

```
Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/
├── Basic/
│   ├── Command/                   (CreateQueueTablesAndSaveConfigurationCommandTests.cs)
│   ├── CommandHandler/            (SetJobLastKnownEventCommandHandlerTests.cs — ONLY existing test here)
│   ├── Factory/                   (SqlServerMessageQueueTransportOptionsFactoryTests.cs)
│   ├── QueueCreatorTests.cs       (pattern: asserts NpgsqlException)
│   ├── PostgreSqlJobQueueCreationTests.cs
│   ├── PostgreSqlJobSchemaTests.cs
│   ├── PostgreSqlSendJobToQueueTests.cs
│   ├── QueueQueueConsumerConfigurationExtensionsTests.cs
│   ├── RetrySqlPolicyCreationTests.cs
│   ├── SqlServerCommandStringCacheTests.cs
│   ├── SqlServerMessageQueueSchemaTests.cs
│   └── SqlServerMessageQueueTransportOptionsTests.cs
├── Decorator/
│   ├── RetryCommandHandlerDecoratorTests.cs
│   ├── RetryCommandHandlerOutputDecoratorAsyncTests.cs
│   ├── RetryCommandHandlerOutputDecoratorBypassTests.cs  ← Phase 2 PLAN-3.2 (shipped)
│   ├── RetryCommandHandlerOutputDecoratorTests.cs
│   └── RetryQueryHandlerDecoratorTests.cs
├── Schema/
│   ├── ColumnTests.cs
│   ├── ColumnsTests.cs
│   ├── ConstraintTests.cs
│   └── TableTests.cs
├── Helpers.cs
├── QueueCreatorTests.cs           ← RISK: will break with plain Register (see §3 notes)
└── SqlConnectionInformationTests.cs
```

### Phase 4 new test files — placement plan (mirroring Phase 3)

| New file | Path |
|----------|------|
| Extractor tests | `Basic/PostgreSqlExternalDbNameExtractorTests.cs` |
| Producer subclass tests | `Basic/PostgreSqlRelationalProducerQueueTests.cs` |
| Sync fork smoke tests | `Basic/CommandHandler/SendMessageCommandHandlerForkSmokeTests.cs` |
| Async fork smoke tests | `Basic/CommandHandler/SendMessageCommandHandlerAsyncForkSmokeTests.cs` |

### QueueCreatorTests — RegisterConditional risk (Rule A validation)

`Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs` has 6 test methods
(lines 14–127), each calling `test.CreateProducer<FakeMessage>(...)` or similar and asserting
`Assert.ThrowsExactly<Npgsql.NpgsqlException>(...)`. This is the exact pattern that Phase 3
broke with plain `Register`. Using `RegisterConditional` for the 3 open-generic producer
mappings is **mandatory** to preserve these 6 tests. See CONTEXT-4 Rule A.

---

## §4. Existing PG Retry Decorator Bypass Tests

**Confirmed path:** `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs`

File is present (Phase 2 PLAN-3.2 shipped). Contains 2 `[TestMethod]` entries:
- `Handle_WhenCommandSkipsRetry_InvokesInnerOnce_AndDoesNotAccessRegistry` (sync)
- `HandleAsync_WhenCommandSkipsRetry_InvokesInnerOnce_AndDoesNotAccessRegistry` (async)

Both use `RelationalSendMessageCommand` (from `DotNetWorkQueue.Transport.Shared.Basic.Command`)
with a `DbTransaction` mock. Phase 4 does not modify this file; it is cross-referenced in
the DI smoke test plan to confirm end-to-end bypass behaviour.

---

## §5. NpgsqlConnection / NpgsqlTransaction Sealed-Type Concerns

Covered under §1. Summary:
- `NpgsqlConnection` is sealed → cannot mock with NSubstitute/Castle DynamicProxy.
- `NpgsqlTransaction` is sealed → same.
- Consequence: Phase 4 handler-fork unit tests use reflection + source-grep (structural smoke tests), identical to Phase 3 Wave 2. No execution-path unit test is feasible at the unit-test level for PG handlers.
- Runtime coverage lives in Phase 6 integration tests against a real PostgreSQL instance.

---

## §6. SimpleInjector `RegisterConditional` API Confirmation

**Confirmed.** `ContainerWrapper.RegisterConditional(Type serviceType, Type implementationType, LifeStyles lifestyle)` (lines 179–183 of `Source/DotNetWorkQueue/IoC/ContainerWrapper.cs`) is the correct overload for open-generic registrations. Internal predicate is `c => !c.Handled`.

Phase 3 used this successfully with:
```csharp
container.RegisterConditional(typeof(IProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton);
```

Phase 4 mirrors identically with `PostgreSqlRelationalProducerQueue<>`. No API changes detected.

---

## §7. ICommandHandlerWithOutput Signatures

Confirmed from PostgreSQLMessageQueueInit.cs decorator registrations (lines 179–214):
- Sync: `ICommandHandlerWithOutput<SendMessageCommand, long>` (with `RetryCommandHandlerOutputDecorator<,>`)
- Async: `ICommandHandlerWithOutputAsync<SendMessageCommand, long>` (with `RetryCommandHandlerOutputDecoratorAsync<,>`)

`PostgreSqlRelationalProducerQueue<T>` constructor will receive both as injected parameters,
same as SqlServer's 11-param constructor. The registered handlers are the decorated versions
(trace + retry decorators wrap them); the bypass is via `IRetrySkippable` on `RelationalSendMessageCommand`.

---

## §8. PG `connection.Database` Semantics

From Npgsql documentation and PG source semantics:
- `NpgsqlConnection.Database` returns the database name **as specified in the connection string** (verbatim, no normalization by Npgsql).
- For unquoted PG identifiers, PostgreSQL stores names in lowercase, so `Database=mydb` → connection reports `"mydb"`.
- For quoted identifiers (`"MyDb"`), PG preserves the case, so the connection string must use `Database=MyDb` and `connection.Database` returns `"MyDb"`.
- Pass-through extraction (`connection.Database` verbatim) is correct for PG's `Ordinal` comparison semantics per Decision 2.

**Decision 2 validation:** `PostgreSqlExternalDbNameExtractor.Extract(connection)` returns `connection.Database` with NO normalization. This matches what Npgsql reports, which matches what PG stores, which matches `IConnectionInformation.Container` (see §9 below). The Ordinal compare in `ExternalTransactionValidator` is correct.

---

## §9. PG-Side `IConnectionInformation.Container` — Critical Quirk

**File:** `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs`

The PG `SqlConnectionInformation` class has this structure (lines 50–53):
```csharp
public override string Server => _server;
public override string Container => Server;
```

And in `ValidateConnection` (line 88):
```csharp
_server = builder.Database;   // NpgsqlConnectionStringBuilder.Database
```

**The property named `Server` actually stores the Database name.** This is a pre-existing
naming quirk specific to PG's `SqlConnectionInformation`. `Container` → `Server` → `Database`.

**Implication for Phase 4:** `IConnectionInformation.Container` for PG returns the **database
name from the connection string** (as parsed by `NpgsqlConnectionStringBuilder`). This is
semantically correct for outbox validation — the validator compares `connection.Database`
(extractor output) against `_connectionInfo.Container` (which is `builder.Database`). Both
sides of the `StringComparison.Ordinal` check are sourced from the same Npgsql database-name
value, making the comparison consistent.

**Implication for the case-sensitive extractor test (Risk #3 / Decision 2):** When building
the test, the architect must configure `IConnectionInformation.Container` with a specific
case (e.g., `"mydb"`) and mock the extractor to return a different case (e.g., `"MyDb"`) to
trigger the validator's `InvalidOperationException`. The `SqlConnectionInformation` cannot
be easily constructed without a real `QueueConnection`+connection-string in tests — use a
substituted `IConnectionInformation` mock directly.

**NOTE:** There is NO equivalent to SqlServer's `ITableNameHelper` override in PG's init.
SqlServer has `//override so that we can use schema as needed` at line 75. PG init does NOT
have this comment or registration — the insertion point for the 5 new registrations is
between `init.RegisterStandardImplementations(...)` (line 62) and `//**all` (line 64), with
NO intervening comment-sentinel to anchor against.

---

## §10. Forward References (Phase 5 + Phase 6)

### Phase 5 (negative-path) dependencies

Phase 5 will assert that resolving `IProducerQueue<T>` from a Memory/Redis/LiteDb/SQLite
container does NOT yield `IRelationalProducerQueue<T>`. Phase 4's `RegisterConditional`
registrations in PG make this trivially true for PG (correct transport) and leave non-PG
transports unchanged. No Phase 4 action required for Phase 5 prep.

### Phase 6 (integration) scaffolding

Integration tests live in `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/`
(separate project, not touched in Phase 4). Queue-per-test isolation via `Guid.NewGuid().ToString("N")`.
Phase 4 unit tests must NOT attempt to connect to a real PG instance — all tests are pure
unit tests using mocks or type-system assertions. Phase 6 is the sole home for live-database
coverage of the `HandleExternalTx` code paths.

---

## §11. Cross-Cutting Risks and API Differences

### Confirmed structural differences from SqlServer

| Area | SqlServer (Phase 3) | PostgreSQL (Phase 4) | Impact |
|------|--------------------|-----------------------|--------|
| Binary parameter type | `SqlDbType.VarBinary` | `NpgsqlDbType.Bytea` | Fork body must use `NpgsqlDbType.Bytea` |
| `CreateMetaDataRecord` signature | 7 params (no time) | 8 params (`DateTime currentTime`) | Fork MUST call `_getTime.GetCurrentUtcDate()` and pass result |
| `IGetTime` injection | NOT injected in handler | Injected (`_getTime` field, set via `getTimeFactory.Create()`) | No ctor change needed; `_getTime` already available |
| Connection/tx types in query | `SqlConnection`/`SqlTransaction` | `NpgsqlConnection`/`NpgsqlTransaction` | All `DoesJobExistQuery<,>` / `SetJobLastKnownEventCommand<,>` instantiations use PG types |
| `ITableNameHelper` override in Init | Present (`SqlServerTableNameHelper`) | NOT present | PG insertion point is at line 62→64, no schema-comment anchor |
| `Container` semantics | Direct DB name from connection string | `Server` property (quirk) → same DB name | Functionally equivalent; see §9 |
| `SqlConnectionInformation` file casing | `SqlConnectionInformation.cs` | `SQLConnectionInformation.cs` (capital SQL) | Linux-safe path requires exact casing |

### Not a structural difference (same in both)

- `SendMessage.BuildStatusCommand` signature is identical (no time param in either transport).
- Both handlers are `internal` classes.
- Both use `NpgsqlCommandStringCache` / `SqlServerCommandStringCache` typed caches.
- Both have `ExecuteScalar()` (sync) / `ExecuteScalarAsync()` (async) for INSERT RETURNING.
- `JobName` / `scheduledTime` / `eventTime` extraction code is identical.
- Guard pattern (`GuardNpgsqlTransaction` / `GuardSqlTransaction`) is per-transport identical in shape.
- 11-param constructor shape is identical (Rule C confirmed for PG).

### `NpgsqlDbType` import

The fork body in `SendMessageCommandHandler.cs` needs `NpgsqlTypes.NpgsqlDbType`. The existing
file already has `using NpgsqlTypes;` at line 32 — no new import needed.

---

## Comparison Matrix

| Criteria | SqlServer (Phase 3, shipped) | PostgreSQL (Phase 4, this phase) |
|----------|------------------------------|----------------------------------|
| Handler paths | `Transport.SqlServer/Basic/CommandHandler/Send*.cs` | `Transport.PostgreSQL/Basic/CommandHandler/Send*.cs` |
| Init file | `SQLServerMessageQueueInit.cs` | `PostgreSQLMessageQueueInit.cs` |
| Test project | `Transport.SqlServer.Tests/` | `Transport.PostgreSQL.Tests/` |
| Test folder convention | `Basic/`, `Basic/CommandHandler/`, `Decorator/` | Same (confirmed) |
| Insertion anchor in Init | After `RegisterStandardImplementations`, before `//override...schema` comment | After `RegisterStandardImplementations`, before `//**all` |
| Connection type in fork | `SqlConnection`/`SqlTransaction` (sealed) | `NpgsqlConnection`/`NpgsqlTransaction` (sealed) |
| Binary param DbType | `SqlDbType.VarBinary` | `NpgsqlDbType.Bytea` |
| `CreateMetaDataRecord` currentTime | NOT required | REQUIRED (`_getTime.GetCurrentUtcDate()`) |
| `IGetTime` in handler | Not injected | Already injected (`_getTime`) |
| `Container` source | `builder.InitialCatalog` (SqlServer) | `builder.Database` via `_server` alias (PG quirk) |
| Extractor normalization | `.ToUpperInvariant()` (case-insensitive) | Verbatim pass-through (case-sensitive, Decision 2) |
| QueueCreatorTests risk | 6 tests assert `SqlException` | 6 tests assert `NpgsqlException` — same risk, same fix (RegisterConditional) |
| RegisterConditional API | `ContainerWrapper.RegisterConditional(Type, Type, LifeStyles)` | Same — confirmed unchanged |

---

## Recommendation

Phase 4 is a structural mirror of Phase 3 with four concrete deviations the architect must
encode in the plans:

1. **`_getTime.GetCurrentUtcDate()` in both `HandleExternalTx` and `HandleExternalTxAsync`** — non-negotiable compile requirement. Pass result as `currentTime` argument to `CreateMetaDataRecord` / `CreateMetaDataRecordAsync`.

2. **`NpgsqlDbType.Bytea` not `SqlDbType.VarBinary`** — fork body parameter types.

3. **Init insertion anchor is `//**all` not `//override so that we can use schema as needed`** — PG init has no schema-override comment.

4. **Pass-through extractor (no `.ToUpperInvariant()`)** — PG case-sensitive semantics, Decision 2.

Everything else is a literal find-replace of `SqlServer` → `PostgreSql`/`Npgsql`.

---

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Fork body omits `_getTime.GetCurrentUtcDate()` call | High (easy to miss — SqlServer has no equivalent) | High (compile error) | Architect explicitly calls out `currentTime` param in PLAN-2.1 and PLAN-2.2 task body |
| Fork uses `SqlDbType.VarBinary` instead of `NpgsqlDbType.Bytea` | Medium | High (compile error) | PLAN-2.1/2.2 must specify `NpgsqlDbType.Bytea` |
| Plain `Register` used instead of `RegisterConditional` | Low (Rule A encoded in CONTEXT-4) | High (6 QueueCreatorTests fail) | Rule A is a hard rule; architect repeats it in PLAN-1.1 |
| Init insertion at wrong line (schema-comment doesn't exist) | Medium | Low (compile OK, wrong placement) | Architect anchors to `init.RegisterStandardImplementations(...)` line, not a comment |
| Case-sensitive extractor test misconfigured | Medium | Low (test passes vacuously) | Test must use two strings that differ only in case and assert `InvalidOperationException` |
| `IConnectionInformation.Container` in tests | Low | Low | Use `Substitute.For<IConnectionInformation>()` directly; never construct `SqlConnectionInformation` in unit tests |

---

## Implementation Considerations

- No new `using` directives required in `SendMessageCommandHandler.cs` or `SendMessageCommandHandlerAsync.cs` — `NpgsqlTypes` is already imported in both files.
- No new `using` directives required in `PostgreSQLMessageQueueInit.cs` — `Transport.RelationalDatabase` namespaces already present.
- `PostgreSqlRelationalProducerQueue<T>` needs its own `using Npgsql;` for the `NpgsqlTransaction` type guard.
- `GuardNpgsqlTransaction` replaces `GuardSqlTransaction` — throws `InvalidOperationException` with a message naming `NpgsqlTransaction` and `Npgsql`.
- 11-param constructor (Rule C) is identical in shape to SqlServer — SimpleInjector will inject the same `IMessageFactory` singleton for both `messageFactory` and `ownMessageFactory` slots.

---

## Sources

1. `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandler.cs` — inspected directly (lines 1–225)
2. `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` — inspected directly (lines 1–234)
3. `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/CommandHandler/SendMessage.cs` — inspected (BuildMetaCommand signature, line 79–88, `DateTime currentDateTime` param confirmed)
4. `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` — inspected (lines 1–232; insertion point at line 62→64)
5. `Source/DotNetWorkQueue.Transport.PostgreSQL/SQLConnectionInformation.cs` — inspected (lines 50–89; `Container => Server => _server = builder.Database` quirk)
6. `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs` — inspected (6 tests asserting `NpgsqlException`, lines 1–127)
7. `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Decorator/RetryCommandHandlerOutputDecoratorBypassTests.cs` — confirmed present (Phase 2 PLAN-3.2 output)
8. `Source/DotNetWorkQueue/IoC/ContainerWrapper.cs` lines 179–183 — `RegisterConditional(Type, Type, LifeStyles)` API confirmed
9. `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs` — `StringComparison.Ordinal` and `Container` usage confirmed
10. `Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs` — interface confirmed
11. `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs` — SqlServer uses `.ToUpperInvariant()` (contrast with PG pass-through)
12. `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs` — 11-param ctor, Guard pattern, SendOne/SendOneAsync helpers (Phase 4 template)
13. `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs` — HandleExternalTx shape (Phase 3 reference)
14. `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` — HandleExternalTxAsync shape, lifecycle-comment text confirmed
15. `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerForkSmokeTests.cs` — smoke test pattern (Phase 4 template)
16. `.shipyard/phases/3/results/SUMMARY-1.1.md` — Phase 3 Wave 1 decisions (RegisterConditional, 11-param ctor)
17. `.shipyard/phases/3/results/SUMMARY-2.1.md` — Phase 3 sync fork (line-comment stripping approach)
18. `.shipyard/phases/3/results/SUMMARY-2.2.md` — Phase 3 async fork (rephrase approach — CONTEXT-4 Rule B mandates this for Phase 4)
19. `.shipyard/phases/4/CONTEXT-4.md` — all 6 user decisions and 3 hard rules

---

## Uncertainty Flags

- **`NpgsqlTransaction` sealed confirmation:** Confirmed by CLAUDE.md lesson ("NpgsqlConnection casts in non-fork handler code") and by transitive knowledge that NpgsqlTransaction inherits from `DbTransaction` without being abstract, but a direct reflection check was not run. Architect should treat it as sealed (the smoke-test-only strategy for Wave 2 is correct regardless).

- **`NpgsqlConnectionStringBuilder.Database` case behaviour on server-side folding:** Npgsql's connection string builder returns the value as written in the connection string, not as PostgreSQL stores it. For unquoted identifiers, PG folds to lowercase at CREATE DATABASE time, so `Database=mydb` → server stores `mydb`, `builder.Database` → `"mydb"`, `connection.Database` → `"mydb"`. For quoted names, behavior is pass-through. This matches Decision 2's stated rationale. Not independently verified against Npgsql source — flag as LOW-risk assumption.

- **`_server` property in `SqlConnectionInformation` for the case-sensitive test:** The validator reads `_connectionInfo.Container` which returns `_server` which is `builder.Database`. For the Risk #3 test (case-sensitive mismatch), the test should mock `IConnectionInformation` directly rather than constructing `SqlConnectionInformation` — the quirk is irrelevant to the test design.

- **Phase 3 `CreateMetaDataRecord` time-parameter omission in async fork:** Phase 3's async SqlServer fork calls `CreateMetaDataRecordAsync(delay, expiration, sqlConn, id, message, data, sqlTx)` with 7 params. PG's `CreateMetaDataRecordAsync` takes 8 (`DateTime currentTime` extra). **Decision Required if unclear:** the architect must confirm PG's `HandleExternalTxAsync` materializes `var currentTime = _getTime.GetCurrentUtcDate();` before calling `CreateMetaDataRecordAsync`. This is the most likely source of a PG-specific build error if overlooked.
