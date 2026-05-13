# Phase 3 Research: SqlServer Implementation + Unit Tests

Purpose: equip the architect to write three Phase 3 plans (foundation + sync handler fork + async handler fork) without re-reading the codebase. All findings here override the original ROADMAP/CONTEXT-3 assumptions where they conflict — discrepancies are flagged in §11.

---

## §1. `SendMessageCommandHandler` (sync) anatomy

**File:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs` (226 lines).

**Class:** `internal class SendMessageCommandHandler : ICommandHandlerWithOutput<SendMessageCommand, long>` (line 39).

**Critical typing discovery — the handler is `SqlConnection`/`SqlTransaction`/`SqlCommand`-typed end-to-end**:

```csharp
private readonly ICommandHandler<SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>> _sendJobStatus;
private readonly IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses> _jobExistsHandler;
```

Constructor (line 64) takes 9 deps:
1. `ITableNameHelper`
2. `ICompositeSerialization`
3. `ISqlServerMessageQueueTransportOptionsFactory` (wrapped in `Lazy<SqlServerMessageQueueTransportOptions>`)
4. `IHeaders`
5. `SqlServerCommandStringCache`
6. `TransportConfigurationSend`
7. `ICommandHandler<SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>>`
8. `IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses>`
9. `IJobSchedulerMetaData`

Plus a mutable `bool? _messageExpirationEnabled` lazy-init'd inside `Handle()`.

### `Handle(SendMessageCommand commandSend)` body (lines 101–181)

Two-phase structure:

**Phase A — pre-amble (lines 103–115):**
- Lazy-init `_messageExpirationEnabled` from `_options.Value.EnableMessageExpiration`.
- Extract `jobName` from `commandSend.MessageData` via `_jobSchedulerMetaData.GetJobName(...)`.
- If `jobName` is non-empty, also extract `scheduledTime` and `eventTime`.

**Phase B — connection + tx + inserts (lines 117–180):**
```csharp
using (var connection = new SqlConnection(_configurationSend.ConnectionInfo.ConnectionString))
{
    connection.Open();
    using (var trans = connection.BeginTransaction())
    {
        // 1. Job-uniqueness check (skipped when jobName is empty)
        if (jobName empty || _jobExistsHandler.Handle(new DoesJobExistQuery<SqlConnection, SqlTransaction>(jobName, scheduledTime, connection, trans)) == QueueStatuses.NotQueued)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = trans;
                command.CommandText = _commandCache.GetCommand(CommandStringTypes.InsertMessageBody);
                // … serialize body + headers, add @body/@headers params (lines 130-143)
                var id = Convert.ToInt64(command.ExecuteScalar());
                if (id > 0)
                {
                    // 2. CreateMetaDataRecord (always)
                    CreateMetaDataRecord(delay, expiration, connection, id, msg, data, trans);
                    // 3. CreateStatusRecord (conditional on _options.Value.EnableStatusTable)
                    if (_options.Value.EnableStatusTable) CreateStatusRecord(connection, id, msg, data, trans);
                    // 4. SetJobLastKnownEventCommand (conditional on jobName non-empty)
                    if (jobName non-empty) _sendJobStatus.Handle(new SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>(jobName, eventTime, scheduledTime, connection, trans));
                }
                else throw new DotNetWorkQueueException("Failed to insert record - …");
                trans.Commit();
                return id;
            }
        }
        throw new DotNetWorkQueueException("…job has already been queued or processed");
    }
}
```

### `CreateMetaDataRecord` and `CreateStatusRecord` helpers (lines 183–224)

Both private methods take `SqlConnection` and `SqlTransaction` directly:

```csharp
private void CreateStatusRecord(SqlConnection connection, long id, IMessage message, IAdditionalMessageData data, SqlTransaction trans)
{
    using (var command = connection.CreateCommand())
    {
        SendMessage.BuildStatusCommand(command, _tableNameHelper, _headers, data, message, id, _options.Value);
        command.Transaction = trans;
        command.ExecuteNonQuery();
    }
}

private void CreateMetaDataRecord(TimeSpan? delay, TimeSpan expiration, SqlConnection connection, long id, IMessage message, IAdditionalMessageData data, SqlTransaction trans)
{
    using (var command = connection.CreateCommand())
    {
        SendMessage.BuildMetaCommand(command, _tableNameHelper, _headers, data, message, id, _options.Value, delay, expiration);
        command.Transaction = trans;
        command.ExecuteNonQuery();
    }
}
```

`connection.CreateCommand()` on `SqlConnection` returns `SqlCommand`. `SendMessage.BuildStatusCommand` and `BuildMetaCommand` take **`SqlCommand`** (not `IDbCommand`) — see §2.

### Insertion point for `HandleExternalTx` fork

The fork goes at the **top of `Handle()`, immediately after the closing `}` of the lazy-init block (line 106) OR — cleaner — as the very first statement of the method (line 102), before the lazy-init. Recommendation: **immediately after line 106** so the lazy-init runs (`_messageExpirationEnabled` and `_options.Value`) before the fork branches; the fork relies on `_options.Value.EnableStatusTable` and `_options.Value.EnableMessageExpiration`.

```csharp
public long Handle(SendMessageCommand commandSend)
{
    if (!_messageExpirationEnabled.HasValue)
        _messageExpirationEnabled = _options.Value.EnableMessageExpiration;

    if (commandSend.ExternalTransaction != null)
        return HandleExternalTx(commandSend);

    // … existing self-managed-tx body (lines 108-180) unchanged
}
```

---

## §2. `SendMessage` static builders

**File:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessage.cs` (194 lines).

**Class:** `internal static class SendMessage` (line 28).

### Builder signatures — both take `SqlCommand`

```csharp
internal static void BuildStatusCommand(SqlCommand command,
    ITableNameHelper tableNameHelper, IHeaders headers, IAdditionalMessageData data,
    IMessage message, long id, SqlServerMessageQueueTransportOptions options); // line 31

internal static void BuildMetaCommand(SqlCommand command,
    ITableNameHelper tableNameHelper, IHeaders headers, IAdditionalMessageData data,
    IMessage message, long id, SqlServerMessageQueueTransportOptions options,
    TimeSpan? delay, TimeSpan expiration); // line 79
```

These mutate the passed `SqlCommand` (set `CommandText`, add `SqlParameters` with `SqlDbType` types). They DO NOT touch `command.Connection` or `command.Transaction` — that's the handler's job.

**Implication:** the `HandleExternalTx` fork can reuse these builders verbatim **if and only if** it gets a `SqlCommand` to pass. To get a `SqlCommand` from `command.ExternalTransaction`, the fork must downcast:

```csharp
var sqlTx = (SqlTransaction)commandSend.ExternalTransaction!;        // safe — see §10
var sqlConn = (SqlConnection)sqlTx.Connection!;                       // safe — validator checked Connection != null
using var insertCmd = sqlConn.CreateCommand();                        // returns SqlCommand
insertCmd.Transaction = sqlTx;
```

The downcasts are **internally consistent with the existing handler** which already uses `SqlConnection`/`SqlTransaction`/`SqlCommand` throughout (lines 117, 120, 125 of `SendMessageCommandHandler.cs`). The handler's typing contract is "SqlServer types end-to-end" — the fork must honour that contract.

This **CONTRADICTS CONTEXT-3.md "Hard Rules"** which says "operate on `IDbConnection`/`DbConnection` only" — that rule applies to *new* handlers per the CLAUDE.md lesson, but the existing `SendMessageCommandHandler` predates that lesson and is irretrievably SqlServer-typed. See §11 Discrepancy #1.

---

## §3. `SendMessageCommandHandlerAsync` anatomy

**File:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs` (232 lines).

**Class:** `internal class SendMessageCommandHandlerAsync : ICommandHandlerWithOutputAsync<SendMessageCommand, long>` (line 39).

Mirrors sync exactly in shape and typing. Same 9 constructor deps. Same `SqlConnection`/`SqlTransaction`/`SqlCommand` typing throughout. Same two-phase `HandleAsync(SendMessageCommand commandSend)` structure (lines 100–184).

Async-specific calls:
- `connection.Open()` (NOT `OpenAsync()` — line 118; the connection is already open before this in the caller-tx path, so this matters only for the self-managed path)
- `command.ExecuteScalarAsync().ConfigureAwait(false)` (line 144)
- `command.ExecuteNonQueryAsync().ConfigureAwait(false)` in the `CreateMetaDataRecordAsync`/`CreateStatusRecordAsync` helpers (lines 195–230)

Note: `_jobExistsHandler.Handle(...)` on line 121 is **sync** (no `HandleAsync` variant exists for this query). `_sendJobStatus.Handle(...)` on line 166 is also sync. The async handler calls these synchronously — Phase 3's async fork inherits this pattern.

### Insertion point for `HandleExternalTxAsync` fork

Same as sync: immediately after the lazy-init block (line 105), before line 107:

```csharp
public async Task<long> HandleAsync(SendMessageCommand commandSend)
{
    if (!_messageExpirationEnabled.HasValue)
        _messageExpirationEnabled = _options.Value.EnableMessageExpiration;

    if (commandSend.ExternalTransaction != null)
        return await HandleExternalTxAsync(commandSend).ConfigureAwait(false);

    // … existing self-managed-tx body unchanged
}
```

---

## §4. `RelationalProducerQueue<T>` virtual hook signatures (Phase 2 PLAN-2.2)

Per `.shipyard/phases/2/plans/PLAN-2.2.md` lines 204–207:

```csharp
protected virtual IQueueOutputMessage SendWithExternalTransaction(TMessage message, IAdditionalMessageData? data, DbTransaction tx);
protected virtual Task<IQueueOutputMessage> SendWithExternalTransactionAsync(TMessage message, IAdditionalMessageData? data, DbTransaction tx);
protected virtual IQueueOutputMessages SendWithExternalTransactionBatch(List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction tx);
protected virtual Task<IQueueOutputMessages> SendWithExternalTransactionBatchAsync(List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction tx);
```

**All four default to throwing `InvalidOperationException("…not implemented for this transport…")`** per CONTEXT-2 Decision 3. Phase 3 SqlServer subclass overrides all four. Phase 4 PostgreSQL subclass mirrors.

`RelationalProducerQueue<T>` itself derives from `ProducerQueue<T>` (Phase 2 PLAN-2.2 §Context point 3). Its constructor — per the PLAN-2.2 reading at line 70 — forwards 6 params to `base(...)` and adds no new constructor deps in Phase 2 (the validator and extractor get injected by transport-specific subclasses in Phase 3/4).

### `ProducerQueue<T>` base constructor (verified)

**File:** `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (line 60).

```csharp
public ProducerQueue(
    QueueProducerConfiguration configuration,
    ISendMessages sendMessages,
    IMessageFactory messageFactory,
    ILogger log,
    GenerateMessageHeaders generateMessageHeaders,
    AddStandardMessageHeaders addStandardMessageHeaders);
```

`ProducerQueue<T>` is `public class ProducerQueue<T> : IProducerQueue<T> where T : class` (line 38). Non-sealed, so subclassing is supported.

### SqlServer producer subclass constructor — recommended shape

```csharp
public sealed class SqlServerRelationalProducerQueue<TMessage> : RelationalProducerQueue<TMessage>
    where TMessage : class
{
    private readonly ICommandHandlerWithOutput<SendMessageCommand, long> _sendHandler;
    private readonly ICommandHandlerWithOutputAsync<SendMessageCommand, long> _sendHandlerAsync;
    private readonly ExternalTransactionValidator _validator;
    private readonly ISentMessageFactory _sentMessageFactory;

    public SqlServerRelationalProducerQueue(
        // 6 base deps:
        QueueProducerConfiguration configuration,
        ISendMessages sendMessages,
        IMessageFactory messageFactory,
        ILogger log,
        GenerateMessageHeaders generateMessageHeaders,
        AddStandardMessageHeaders addStandardMessageHeaders,
        // 4 new deps:
        ICommandHandlerWithOutput<SendMessageCommand, long> sendHandler,
        ICommandHandlerWithOutputAsync<SendMessageCommand, long> sendHandlerAsync,
        ExternalTransactionValidator validator,
        ISentMessageFactory sentMessageFactory)
        : base(configuration, sendMessages, messageFactory, log, generateMessageHeaders, addStandardMessageHeaders)
    {
        _sendHandler = sendHandler;
        _sendHandlerAsync = sendHandlerAsync;
        _validator = validator;
        _sentMessageFactory = sentMessageFactory;
    }

    protected override IQueueOutputMessage SendWithExternalTransaction(TMessage message, IAdditionalMessageData? data, DbTransaction tx)
    {
        _validator.Validate(tx);
        var imsg = _messageFactory.Create(message);  // need to expose _messageFactory from base, OR pass it via field
        var amd = data ?? _additionalMessageDataFactory.Create();  // ditto
        var cmd = new RelationalSendMessageCommand(imsg, amd, tx);
        var id = _sendHandler.Handle(cmd);
        return new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId<long>(id), amd.CorrelationId));
    }
    // … other 3 overrides analogously
}
```

**Open question (Architect):** the base `ProducerQueue<T>` has private fields for `_messageFactory`, `_generateMessageHeaders`, `_addStandardMessageHeaders`. The Phase 2 PLAN-2.2 `RelationalProducerQueue<T>` either needs to expose these via `protected` getters OR the Phase 3 subclass needs to accept its own copies via DI. Architect should pick one approach in PLAN-1.1 task design. The cleaner choice is **protected getters on the Phase 2 base** — but that's a Phase 2 change. Since Phase 2 isn't built yet, Phase 3 plan can include a "Phase 2 amendment" note in CONTEXT-3 OR Phase 3 task design can pass these via DI in the SqlServer subclass without touching Phase 2. **Recommended: pass via DI in Phase 3** to avoid coupling phases.

---

## §5. `SendMessageCommand` + `RelationalSendMessageCommand` shapes

**`SendMessageCommand`** (file `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs`, modified by Phase 2 PLAN-1.1):
- Public, non-sealed, namespace `DotNetWorkQueue.Transport.Shared.Basic.Command`.
- Existing constructor: `(IMessage messageToSend, IAdditionalMessageData data)`.
- New property added by PLAN-1.1: `public DbTransaction ExternalTransaction { get; init; }` — defaults to null.
- Does NOT implement `IRetrySkippable` (per PLAN-1.1 Task 2 final paragraph).

**`RelationalSendMessageCommand`** (file `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs`, new in Phase 2 PLAN-2.2):
- Public class, namespace `DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command`.
- `: SendMessageCommand, IRetrySkippable`.
- Constructor: `(IMessage messageToSend, IAdditionalMessageData data, DbTransaction externalTransaction)` — forwards first two to base, sets `ExternalTransaction = externalTransaction` in body (init-property cross-assembly write is legal — confirmed in PLAN-2.2 architect note).
- `public bool SkipRetry => ExternalTransaction != null;`

Phase 3 producer subclass overrides construct `new RelationalSendMessageCommand(imsg, amd, tx)` and dispatch through the registered handler. The handler receives the base type `SendMessageCommand` (handler signature is `ICommandHandlerWithOutput<SendMessageCommand, long>`), so the fork inside the handler reads `commandSend.ExternalTransaction` (the inherited property), not the derived type. This works because `ExternalTransaction` lives on the base; the derived class only adds `IRetrySkippable.SkipRetry`.

---

## §6. `SQLServerMessageQueueInit` registration topology

**File:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs`.

`RegisterImplementations(IContainer, RegistrationTypes, QueueConnection)` (line 55) calls `base.RegisterImplementations(...)` first (line 58), then explicitly registers ~50 transport-specific types.

### Existing `SendMessageCommand` registration topology

- **Concrete handler** `SendMessageCommandHandler` is **auto-scanned** by `RelationalDatabaseMessageQueueInit.RegisterCommands(container, caller)` (line 91 of that init), which uses reflection to register any `ICommandHandlerWithOutput<,>` implementation in the calling assembly. So Phase 3 does NOT need an explicit `Register<ICommandHandlerWithOutput<SendMessageCommand, long>, SendMessageCommandHandler>` line — the auto-scanner picks it up. Same for the async variant.
- **Retry decorator** registered at lines 156–157: `container.RegisterDecorator(typeof(ICommandHandlerWithOutput<,>), typeof(RetryCommandHandlerOutputDecorator<,>), LifeStyles.Singleton);`. Phase 2 PLAN-3.1 adds the `IRetrySkippable` bypass branch inside this decorator's `Handle()`. No registration change in Phase 3.
- **Async retry decorator** at lines 162–163: `container.RegisterDecorator(typeof(ICommandHandlerWithOutputAsync<,>), typeof(RetryCommandHandlerOutputDecoratorAsync<,>), …)`.
- **Trace decorator (sync)** at lines 182–184: `container.RegisterDecorator(typeof(ICommandHandlerWithOutput<SendMessageCommand, long>), typeof(…SendMessageCommandHandlerDecorator), …)`. Decorator chain on the registered handler resolves outermost→innermost as: **Trace → Retry → Concrete handler**. Phase 1 spike confirmed this order.
- **Trace decorator (async)** at lines 186–188: mirrors sync.

### Producer factory registration

Base `ProducerQueue<T>` is registered as a fallback in `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs:385` via `RegisterConditional`:

```csharp
container.RegisterConditional(typeof(IProducerQueue<>), typeof(ProducerQueue<>), LifeStyles.Singleton);
```

`RegisterConditional` in SimpleInjector terms means "register this only if no other registration exists for the type". A transport CAN preempt by registering its own producer first. Phase 3 SqlServer init must register:

```csharp
container.Register(typeof(IProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton);
// also register the concrete so capability-cast works:
container.Register(typeof(RelationalProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton);
// and the derived interface:
container.Register(typeof(IRelationalProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton);
```

**Architect must verify with the actual SimpleInjector wrapper** that `IContainer.Register(typeof(IProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>))` is the correct call shape (the wrapper may have an open-generic-overload that differs). If the wrapper exposes only `Register<TService, TImpl>()`, the Phase 3 init may need a different shape — e.g., a factory lambda or a typed extension method. **This is a code-detail confirmation, not an architectural risk**; the registration WILL work, the exact call shape needs a spot-check during build.

### Where Phase 3 must add new registrations

Inside `SQLServerMessageQueueInit.RegisterImplementations`, after line 58 (`base.RegisterImplementations(...)`) but before line 64 (the existing block of `container.Register<...>` calls), add:

```csharp
// Outbox-pattern producer wiring
container.Register<IExternalDbNameExtractor, SqlServerExternalDbNameExtractor>(LifeStyles.Singleton);
container.Register<ExternalTransactionValidator>(LifeStyles.Singleton);  // if not already registered by base
container.Register(typeof(IProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton);
container.Register(typeof(IRelationalProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton);
container.Register(typeof(RelationalProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton);
```

**Critical:** the `SqlServerRelationalProducerQueue<>` registration must come BEFORE the fallback `RegisterConditional` runs in `ComponentRegistration.RegisterFallbacks`. The fallback is gated on `RegistrationTypes.Send` and the transport init runs first — so this is naturally ordered. Architect should confirm during build.

### `ExternalTransactionValidator` registration site

Per Phase 2 PLAN-2.1 (line 117 of its plan body, paraphrased): the validator is "registered transient or scoped per CONVENTIONS.md — architect decides". PLAN-2.1 does NOT contain an explicit registration in `Transport.RelationalDatabase`'s init helper. Phase 3 needs to either:
- (a) Add the registration in `SQLServerMessageQueueInit` (and Phase 4 mirrors for PostgreSQL).
- (b) Push the registration into `RelationalDatabaseMessageQueueInit.RegisterStandardImplementations` (which would be a Phase 2 amendment, currently not in PLAN-2.1).

**Recommended (a):** keep transport-specific registration in transport-specific init. Phase 3 PLAN-1.1 adds the validator registration to `SQLServerMessageQueueInit`. Phase 4 PLAN-1.1 will mirror.

---

## §7. Existing unit-test seams for `SendMessageCommandHandler`

**Finding:** there are **no existing unit tests** for `SendMessageCommandHandler` or `SendMessageCommandHandlerAsync` in `Source/DotNetWorkQueue.Transport.SqlServer.Tests/`.

```
$ find Source/DotNetWorkQueue.Transport.SqlServer.Tests -name "*.cs" | xargs grep -l "SendMessageCommandHandler"
Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs   # Phase 1 PoC, will be deleted by Phase 2 PLAN-1.1
```

Coverage of these handlers currently comes entirely from the SqlServer **integration test suite**, not unit tests. Phase 3 introduces the first unit tests against these handlers. This matters for two reasons:

1. **No prior test pattern to mirror.** The unit test plans must establish the SqlServer handler test convention from scratch. Mock strategy decisions land in Phase 3.
2. **Mocking constraint** (from CLAUDE.md "Casting `IDbConnection` to a sealed transport-specific type… breaks NSubstitute"): the existing handler **already** uses sealed `SqlConnection`/`SqlTransaction`/`SqlCommand` types in its private helpers. NSubstitute **cannot** mock these. Therefore Phase 3's unit tests **cannot** use NSubstitute to verify the connection/transaction state on the existing self-managed path — they can only verify behavior reachable WITHOUT mocking SqlConnection.

### Implications for Phase 3 unit test plans

For the **HandleExternalTx fork**, the caller-tx path takes a `DbTransaction` as input. The test can:
- Create a **real `SqlConnection`** pointed at LocalDB or a test connection string that doesn't need to be queryable (the fork executes commands, so a real DB is needed unless we abstract differently).
- Pass `null` `ExternalTransaction` to verify the fork is NOT entered (sync branch returns from existing self-managed path — but the existing path needs a real connection too).

**The realistic Phase 3 unit-test scope, given the SqlConnection mocking constraint:**

1. **Producer subclass tests** — these CAN be unit-tested cleanly because the subclass takes the validator + handler interfaces as DI deps. NSubstitute mocks the handler interface (not `SqlConnection`), and the validator can be mocked or instantiated with mocked `IExternalDbNameExtractor` + `IConnectionInformation`. **Suggested tests in Phase 3:**
   - `SendWithExternalTransaction_NullTx_ThrowsArgumentNullException` (via validator)
   - `SendWithExternalTransaction_ValidatorRejects_ThrowsInvalidOperation` (validator throws → producer surfaces)
   - `SendWithExternalTransaction_HappyPath_DispatchesRelationalSendMessageCommand` (mock handler interface, capture command, assert `command.ExternalTransaction == tx` and `command is RelationalSendMessageCommand`)
   - Batch variant: `SendWithExternalTransactionBatch_HappyPath_DispatchesPerMessage` (mock handler, capture N commands, assert all have the same `ExternalTransaction` and the loop is sequential)
   - Async variants of the above

2. **Handler fork tests** — limited by SqlConnection mocking. Two viable patterns:
   - **(a) LocalDB / SQL connection string from `connectionstring.txt`** — instantiate a real `SqlConnection`, open it, begin a transaction, pass to the fork, then **rollback** the test transaction at the end. Verifies the fork executes against a real database without leaving state. This is **a unit test in spirit** (no other moving parts) but requires LocalDB/SQL to be reachable, which doesn't match the existing unit-test gate (no `connectionstring.txt`). Architect SHOULD NOT pick this option for Phase 3 — it crosses into integration test territory (Phase 6).
   - **(b) Reflection-based fork unit test** — invoke `HandleExternalTx` via reflection, pass real `SqlConnection`/`SqlTransaction` instances that point at `Server=(localdb)\\MSSQLLocalDB` and rely on `_options.Value.EnableStatusTable = false` etc. to skip the actual SQL execution. **Brittle and not really a unit test.**
   - **(c) NO direct fork unit tests in Phase 3.** Defer fork-level behavior to Phase 6 integration tests. Phase 3 only tests the producer subclass (which IS unit-testable). Architect chooses this.

**Strong recommendation:** option (c). Phase 3's "fork happens correctly" coverage comes from the producer subclass tests (which prove the right command shape reaches the registered handler) + Phase 2 PLAN-3.1 tests (which prove the bypass branch fires when `IRetrySkippable.SkipRetry == true`) + Phase 6 integration tests (which prove the fork executes end-to-end against real SqlServer). Phase 3's unit-test surface is 6–8 producer-subclass tests, NOT direct fork tests.

This **revises CONTEXT-3.md Decision 4** which assumed ~3-4 sync handler unit tests in Plan 2.1 and 3-4 async tests in Plan 2.2. The architect should restructure to: producer-tests in PLAN-1.1 (or a dedicated PLAN-1.2), and W2 plans add the fork code itself with no direct fork unit tests, just smoke tests that the fork compiles + the producer subclass dispatches correctly. See §11 Discrepancy #2.

---

## §8. Naming + namespace conventions

### `SqlServerExternalDbNameExtractor`

Existing analog in this transport: `SqlServerCommandStringCache`, `SqlServerTime`, `SqlServerTableNameHelper` — all named with `SqlServer` prefix and live under `Source/DotNetWorkQueue.Transport.SqlServer/Basic/`.

**Recommended location:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs`.

**Implementation (~15 lines):**

```csharp
using System.Data.Common;
using DotNetWorkQueue.Transport.RelationalDatabase;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// SqlServer implementation of <see cref="IExternalDbNameExtractor"/>.
    /// Uses <c>DbConnection.Database</c> for case-insensitive comparison
    /// (SqlServer identifier semantics).
    /// </summary>
    public sealed class SqlServerExternalDbNameExtractor : IExternalDbNameExtractor
    {
        public string Extract(DbConnection connection) => connection.Database;
        public StringComparer Comparer => StringComparer.OrdinalIgnoreCase;
    }
}
```

**Architect must verify** the `IExternalDbNameExtractor` interface shape from Phase 2 PLAN-2.1. PLAN-2.1 line 72 shows the interface; the architect should re-read PLAN-2.1's Task 1 to confirm whether `Comparer` is a property or whether the comparer is supplied separately. The above sketch may need to align with the Phase 2 contract.

### `SqlServerRelationalProducerQueue<T>`

Existing analog: `SqlServerMessageQueueReceive`, `SqlServerJobQueueCreation` — `SqlServer<feature>` naming. Phase 2's base is `RelationalProducerQueue<T>`, so the SqlServer subclass follows the same word ordering:

**Recommended location:** `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs`.

The `T` type constraint `where T : class` is inherited from `IProducerQueue<T>` → `ProducerQueue<T>`.

### License header

All new `.cs` files MUST include the LGPL-2.1 header exactly as it appears in `SendMessageCommandHandler.cs` lines 1–18. Architect tasks should include "copy header verbatim from any existing file in the same project" as a step.

---

## §9. Batch path: existing failure-aggregation pattern

**File:** `Source/DotNetWorkQueue.Transport.Shared/Basic/SendMessages.cs` lines 79–148.

The existing `Send(List<>)` and `SendAsync(List<>)` paths catch per-item exceptions and convert them into `QueueOutputMessage(... , error)` entries in the result `IQueueOutputMessages`:

```csharp
try
{
    var id = _sendMessage.Handle(new SendMessageCommand(m.Message, m.MessageData));
    rc.Add(new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId<T>(id), m.MessageData.CorrelationId)));
}
catch (Exception error)
{
    rc.Add(new QueueOutputMessage(_sentMessageFactory.Create(null, m.MessageData.CorrelationId), error));
}
```

The sync version uses `Parallel.ForEach` over a `ConcurrentBag<IQueueOutputMessage>`. The async version uses sequential `foreach` (lines 130–142).

**Phase 3 batch override pattern:** the SqlServer subclass's `SendWithExternalTransactionBatch` and `SendWithExternalTransactionBatchAsync` MUST use **sequential** `foreach` (NOT `Parallel.ForEach`) per CONTEXT-3 Decision 1. Per-item exceptions go into the result list exactly as above; the loop continues to the next message rather than throwing. **Caller-tx semantics:** if any insert fails partway through a batch, the caller can choose to rollback the whole tx — the producer doesn't make that decision.

The existing async `Send(List<>)` is already sequential (`foreach`, not `Parallel.ForEach`). Phase 3 async batch override mirrors that pattern verbatim, only swapping `SendMessageCommand` for `RelationalSendMessageCommand` and adding pre-loop `validator.Validate(tx)`.

---

## §10. Grep gates Phase 3 must NOT trip

### Existing SqlServer handlers already use sealed types — that is OK

The CLAUDE.md no-sealed-cast lesson applies to **new** handlers. `SendMessageCommandHandler` predates that lesson and is irretrievably typed on SqlServer types. Phase 3's fork is internally consistent with the host file's typing model — it casts `command.ExternalTransaction` to `SqlTransaction` and `.Connection` to `SqlConnection` at the top of the fork. **This is acceptable** because:

1. The existing handler ALREADY uses these types.
2. The cast is contained to the SqlServer handler fork — no spillover to `Transport.RelationalDatabase` or any cross-transport code.
3. The producer subclass validates the tx is a `SqlTransaction` BEFORE dispatch (recommended addition to CONTEXT-3 Decision 3), giving a clean error message rather than a raw `InvalidCastException` for misuse.

### Grep gates Phase 3 MUST NOT trip

- **No `SqlConnection` / `SqlTransaction` / `SqlCommand` casts in `Transport.RelationalDatabase`** — these types must not appear in the base producer or validator. Verified clean per Phase 2 PLAN-2.1 + PLAN-2.2.
- **No `using Microsoft.Data.SqlClient;` in any `Transport.RelationalDatabase` file.**
- **No `Microsoft.Data.SqlClient.SqlConnection` reference in `SqlServerExternalDbNameExtractor`** — the extractor uses `DbConnection.Database` (abstract base property) and the comparer; it does not need to know it's SqlServer.

### Cast guard in the producer (recommended addition)

The `SqlServerRelationalProducerQueue<T>` override should include this provider-specific guard between `validator.Validate(tx)` and command construction:

```csharp
if (tx is not SqlTransaction sqlTx)
    throw new InvalidOperationException(
        $"Expected SqlTransaction but received {tx.GetType().FullName}. " +
        "The transaction must be opened on a SqlConnection from Microsoft.Data.SqlClient.");
```

This protects against the case where a caller passes (e.g.) an Npgsql transaction to a SqlServer producer — the boundary validator's database-name check might pass by coincidence, but the handler's cast would throw `InvalidCastException` deep in the call. The guard surfaces a clear error at the API boundary instead.

**Note:** the producer doesn't NEED to consume `sqlTx`. The cast guard is purely for error-message quality. The producer still dispatches `new RelationalSendMessageCommand(imsg, amd, tx)` with the `DbTransaction` base type. The handler fork performs its own cast.

---

## §11. Open questions / discrepancies from CONTEXT-3.md

### Discrepancy #1 — "Operates on `IDbConnection`" is wrong for this transport

CONTEXT-3.md "Hard Rules" line: *"No `Microsoft.Data.SqlClient.SqlConnection` casts inside the handler fork. Operate on `IDbConnection` / `DbConnection` only."*

**Reality:** the existing `SendMessageCommandHandler` is already `SqlConnection`-typed end-to-end. The fork must downcast to reuse the existing builders. **Resolution:** treat this rule as "no NEW patterns of sealed-type cast in code that didn't already have them" — the SqlServer handler is grandfathered. The architect should rewrite the CONTEXT-3 hard rule before plans are written, or the planner addresses it in plan-critique. **Action:** update CONTEXT-3.md hard rules to reflect the SqlServer typing reality.

### Discrepancy #2 — Phase 3 unit test count is over-stated in CONTEXT-3

CONTEXT-3 Decision 4 paragraph: *"PLAN-2.1 — Sync handler fork + tests. Modify SendMessageCommandHandler.cs to add the HandleExternalTx fork. Add 3–4 sync unit tests (branch selection, no-Commit/Rollback/Dispose/Close on mocked tx, …)"*

**Reality:** SqlConnection is sealed; NSubstitute cannot mock it. The "no-Commit/Rollback/Dispose/Close on mocked tx" test is **not feasible** at the handler-fork level via unit test. It IS feasible at the producer-subclass level (mock the handler interface and observe what the handler is called with).

**Resolution:** the producer subclass tests cover "producer dispatches the right `RelationalSendMessageCommand` with the right `ExternalTransaction` and validator is called first." The "no Commit/Rollback/Dispose/Close" semantics is enforced by the fork's code (no such calls present) and verified by Phase 6 integration tests with a real SqlServer + caller-owned tx, NOT by SqlServer.Tests unit tests. **Action:** architect restructures Phase 3 plans so unit tests live primarily in PLAN-1.1 (producer subclass tests) with W2 plans adding the fork code and ~1–2 light smoke tests each (verifying compile + that the fork is reachable).

### Discrepancy #3 — `ExternalTransactionValidator` `Validate` should NOT throw `ArgumentNullException`

Phase 2 PLAN-2.1 sketches validator checking `transaction != null` and throwing `ArgumentNullException`. The producer override calls `validator.Validate(tx)` — at that point the producer's own input was already nullable, so an `ArgumentNullException` would surface to the caller. This is consistent with .NET conventions. **No action needed.** Just flagging that the producer should NOT do its own null check before calling `Validate` — let the validator handle it.

### Discrepancy #4 — `ExternalTransactionValidator` registration site

Phase 2 PLAN-2.1 does not specify where the validator is registered in DI. It says "transient or scoped — architect decides per CONVENTIONS.md". For Phase 3, the validator must be registered in `SQLServerMessageQueueInit` so the producer subclass can resolve it. **Action:** Phase 3 PLAN-1.1 adds `container.Register<ExternalTransactionValidator>(LifeStyles.Singleton)` to `SQLServerMessageQueueInit.RegisterImplementations`. Phase 4 will mirror for PostgreSQL.

### Open question — `SqlServerRelationalProducerQueue` access to base private fields

The Phase 2 `RelationalProducerQueue<T>` (per PLAN-2.2) does not expose protected getters for `_messageFactory`, `_generateMessageHeaders`, `_addStandardMessageHeaders`. The Phase 3 subclass needs these to construct `IMessage` and run header generation, OR it needs to receive its own copies via DI.

**Recommended:** Phase 3 subclass takes its own `IMessageFactory` reference via DI (constructor param) and uses that. The base class's `_sendMessages` field handles the existing self-managed path; the subclass's `_messageFactory` handles the caller-tx path. No Phase 2 amendment required.

### Open question — Async batch parallelism inside caller-tx

Existing async batch (`SendMessages<T>.SendAsync(List<>)`) is already sequential (`foreach`, not `Parallel.ForEach`). Phase 3 async batch override mirrors that. The sync batch override (`Send(List<>)`) is the one that **changes** from `Parallel.ForEach` to sequential `foreach`. This is documented in CONTEXT-3 Decision 1; no architect action beyond honouring it in PLAN-1.1 / PLAN-2.x.

---

## Summary for the architect

- **Three plans, two waves** as decided in CONTEXT-3, but the W2 plans become **smaller** than originally framed because direct handler-fork unit tests are not feasible (§7 + §11 Discrepancy #2). The bulk of Phase 3 unit tests lives in W1 around the producer subclass.
- **Handler fork** is mechanical — replicate the existing handler body, but skip `new SqlConnection(...)`, `Open()`, `BeginTransaction()`, `Commit()`, and the `using` blocks for connection and transaction. Reuse `CreateMetaDataRecord`/`CreateStatusRecord` private helpers verbatim by casting `command.ExternalTransaction` to `SqlTransaction` and `.Connection` to `SqlConnection`.
- **`SQLServerMessageQueueInit`** gets ~5 new registrations: extractor, validator, and three producer-shape registrations (`IProducerQueue<>`, `RelationalProducerQueue<>`, `IRelationalProducerQueue<>` all → `SqlServerRelationalProducerQueue<>`).
- **Producer subclass** carries the validator + cast guard + handler dispatch. Takes 10 DI deps (6 from base + 4 new).
- **The CLAUDE.md no-sealed-cast rule** does not block Phase 3 — the existing handler is grandfathered. Phase 3 must not introduce sealed-type casts in `Transport.RelationalDatabase` (clean by Phase 2 design) but is permitted to cast inside the SqlServer-specific handler fork.

**Architect: update CONTEXT-3.md hard-rules section before writing plans** (or note the discrepancies in plan-critique). The §11 items are the only architectural surprises; everything else aligns with CONTEXT-3.
