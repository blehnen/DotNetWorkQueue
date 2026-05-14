# Research: Phase 6 — Integration Tests (SqlServer + PostgreSQL)

## Context

Phases 3 and 4 added `HandleExternalTx`/`HandleExternalTxAsync` forks to both transport
handlers, wired `SqlServerRelationalProducerQueue<T>` and `PostgreSqlRelationalProducerQueue<T>`
into DI via `RegisterConditional`, and validated all paths at the unit-test level. Phase 6
verifies the full runtime path against real databases. This research answers the 10 questions
from CONTEXT-6.md §Notes for Researcher.

---

## §1. Integration Test Project Paths

**SqlServer**

- Folder: `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/`
- Assembly name: `DotNetWorkQueue.Transport.SqlServer.Integration.Tests`
- `.csproj`: `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj`
- Jenkins references the csproj at: `"Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj"` (Jenkinsfile line 78)

**PostgreSQL**

- Folder: `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/`
- Assembly name: `DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests`
- `.csproj`: `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj`
- Jenkins references the csproj at: `"Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj"` (Jenkinsfile line 113)

**Subfolder convention:** No existing `Outbox/` subfolder exists in either project. The current
layout uses flat category subfolders (`Producer/`, `Consumer/`, `ConsumerAsync/`, `Route/`,
`UserDequeue/`, `History/`, `Admin/`). Phase 6 should add:

- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/`

This matches the existing per-category subfolder convention and requires no Jenkinsfile changes.
The tests are picked up automatically because the `.csproj` already targets `net10.0` and
includes all `.cs` files by convention.

---

## §2. Existing Test Class Patterns

### Naming convention

All existing test classes use a plain noun or action-noun name: `SimpleProducer`,
`SimpleProducerAsync`, `SimpleProducerBatch`, `SimpleProducerAsyncBatch`, `SimpleConsumer`,
`ConsumerRollBack`, etc. Classes are `[TestClass]`, methods are `[TestMethod]`. DataRow-driven
tests use `[DataRow(...)]` attributes stacked above `[TestMethod]`.

Phase 6 outbox tests should follow the same convention. Suggested names:

- `SqlServerOutboxTests` (the main [TestClass] file per plan group, under `Outbox/`)
- `SqlServerOutboxIntegrationTestBase` (base class, same folder)
- Mirror for PostgreSQL: `PostgreSqlOutboxTests`, `PostgreSqlOutboxIntegrationTestBase`

### Queue setup pattern (canonical from `SimpleProducer.Run<>` in
`Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/Implementation/SimpleProducer.cs`)

```csharp
using (var queueCreator = new QueueCreationContainer<TTransportInit>(...))
{
    var oCreation = queueCreator.GetQueueCreation<TTransportCreate>(queueConnection);
    try
    {
        setOptions(oCreation);                     // set Options flags
        var result = oCreation.CreateQueue();      // DDL: CREATE TABLE
        Assert.IsTrue(result.Success, result.ErrorMessage);
        scope = oCreation.Scope;
        // ... run test body ...
    }
    finally
    {
        oCreation?.RemoveQueue();   // DDL: DROP TABLE — the teardown
        oCreation?.Dispose();
        scope?.Dispose();
    }
}
```

For SqlServer transport-specific code: `SqlServerMessageQueueInit`, `SqlServerMessageQueueCreation`
(`DotNetWorkQueue.Transport.SqlServer.Basic`).
For PostgreSQL: `PostgreSqlMessageQueueInit`, `PostgreSqlMessageQueueCreation`
(`DotNetWorkQueue.Transport.PostgreSQL.Basic`).

### Teardown

`oCreation.RemoveQueue()` drops all queue tables created by `CreateQueue()`. This is the
authoritative teardown pattern. No manual `DROP TABLE` SQL needed. Every existing test delegates
to this in a `finally` block.

### Producer instantiation within a test

After `CreateQueue()`, the existing tests use the shared `ProducerShared` or call
`creator.CreateProducer<TMessage>(queueConnection)` inside a `QueueContainer<TTransportInit>`.
For Phase 6's outbox tests, the producer is obtained as:

```csharp
using var creator = new QueueContainer<SqlServerMessageQueueInit>(...);
var producer = creator.CreateProducer<FakeMessage>(queueConnection);
var rp = (IRelationalProducerQueue<FakeMessage>)producer;  // capability cast
```

`FakeMessage` is defined in
`Source/DotNetWorkQueue.IntegrationTests.Shared/Messages.cs` (shared across all transports).

### ConnectionString reading

Both projects use the same `File.ReadAllText("connectionstring.txt")` pattern. See:

- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/ConnectionString.cs` — static
  `ConnectionInfo.ConnectionString` property, lazy-cached, throws `NullReferenceException` if
  file missing or empty (lines 1-44).
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/ConnectionString.cs` — identical
  pattern, same class name (lines 1-31).

Both `connectionstring.txt` files are configured as `<CopyToOutputDirectory>Always</CopyToOutputDirectory>`
in their respective `.csproj` files. Phase 6 tests reference `ConnectionInfo.ConnectionString`
directly — no new file reading code needed.

### Queue name generation

Both projects use a local `GenerateQueueName.Create()` helper (MD5 of a new GUID, prefixed
with `"INTTEST"`, hyphens/underscores stripped, lowercased). This produces names like
`"INTTESTa1b2c3d4e5f6..."` — alphanumeric-safe.

**CLAUDE.md lesson conflict:** CONTEXT-6.md specifies `"q" + Guid.NewGuid().ToString("N")`.
The existing helper produces all-lowercase alphanumeric names without hyphens, which is also
safe. **Recommendation for Phase 6:** use the existing project-local `GenerateQueueName.Create()`
for consistency with surrounding tests, rather than introducing a new queue-name scheme. Both
approaches are DNQ-compliant. If the architect prefers CONTEXT-6's scheme for clarity, it is
also valid.

---

## §3. `IAdditionalMessageData` Round-Trip Integration Pattern

DEFERRED to architect — partial findings follow.

The existing integration tests in both projects pass `AdditionalMessageData` to `queue.Send(job, data)` in `ProducerShared.RunProducerInternal` (lines 123-135 of
`Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/ProducerShared.cs`). The data object
is constructed in transport-specific `Helpers.GenerateData(configuration)` (e.g.,
`SharedClasses.cs` line 69 in SqlServer project), which sets expiration, delay, priority, and
custom columns via `data.AdditionalMetaData.Add(new AdditionalMetaData<int>("OrderID", 123))`.

**What was NOT found:** No existing integration test performs a full round-trip — enqueue with
`IAdditionalMessageData`, dequeue with a consumer, then assert the received message's metadata
matches the sent metadata. Existing tests only verify the database row count or column values
via direct SQL (`VerifyQueueData.VerifyPriority()`, `VerifyDelayedProcessing()`, etc.).

**Likely approach for Phase 6 B-test:** The test will need to:

1. Create the queue with `oCreation.CreateQueue()` (enabling status table if needed for dequeue).
2. Enqueue via `rp.Send(msg, additionalData, tx)` then commit.
3. Open a `QueueContainer<TTransportInit>` consumer and call `Consume()` / inspect the
   `IReceivedMessage<FakeMessage>` returned. The received message exposes
   `Context.Get<IMessageId>()` for the ID, and headers via `IReceivedMessage.Headers`.
4. Assert specific header values match the sent `AdditionalMessageData`.

The `IAdditionalMessageData` interface (in `DotNetWorkQueue/Messages/AdditionalMessageData.cs`)
supports custom headers via `SetHeader<T>(IMessageContextData<T>, T)` and
`GetHeader<T>(IMessageContextData<T>)`. Simpler: set a CorrelationId and assert it on the
received message via `message.CorrelationId`.

**Decision Required:** The architect should decide whether:

- The B-test dequeues via a real `IConsumerQueue` (requires message handler, multi-threaded
  consumer setup — significant complexity), OR
- The B-test verifies metadata round-trip by querying the queue's metadata table directly via
  SQL (simpler, matches how `VerifyQueueData.VerifyPriority()` works). For correlation ID:
  query the `QueueMetaData` table column directly.

The simpler SQL-verification approach is strongly recommended — it avoids consumer lifecycle
complexity and aligns with the existing test pattern.

---

## §4. Retry-Bypass Test Mechanism

DEFERRED to architect — findings and recommendation follow.

**What was NOT found:** No existing integration test in either transport project forces a
transient SQL error mid-send. There is no "force transient SQL error" helper in
`DotNetWorkQueue.IntegrationTests.Shared`. The chaos testing (`enableChaos` parameter in
existing tests) operates at the consumer side.

**Metrics structure for attempt counting:** The `IMetrics` / `Metrics` class in
`DotNetWorkQueue.IntegrationTests.Shared/Metrics/Metrics.cs` is a `ConcurrentDictionary`-backed
implementation that tracks meters by name. `GetCollectedMetrics()` returns a `MetricsSnapshot`
with `Counters` and `Meters` dictionaries. The polling pattern is implemented in
`VerifyMetrics.VerifyProcessedCount(string, IMetrics, long, int)` (lines 139-167 of
`VerifyMetrics.cs`) — polls on 100ms intervals until the counter reaches the target or times
out. **This is the established pattern for Phase 6's retry-bypass assertion.**

**Retry-bypass via `IRetrySkippable`:** Phase 2/3 confirmed the retry bypass works via
`RelationalSendMessageCommand.SkipRetry = true`, which causes `RetryCommandHandlerOutputDecorator`
to skip retry. This means **on the caller-tx path, no retry ever happens** — the decorator
exits after one attempt regardless of whether the command succeeds or throws. The retry-bypass
unit tests in Phase 3/4 already assert this at the structural level.

**Recommendation for Phase 6 D-test:** Rather than forcing a transient SQL error (which
requires non-trivial setup — lock contention, short command timeout, etc.), assert the bypass
differently:

- Pass a deliberately invalid `DbTransaction` that causes the first INSERT to throw (e.g., a
  transaction that was already rolled back / committed — its connection is closed). This
  produces an `InvalidOperationException` or `SqlException` on first attempt.
- Assert the exception propagates to the caller (not swallowed).
- Assert the `SendMessagesMeter` counter in `IMetrics` shows **0** successful sends (the throw
  prevents increment), not that the attempt count = 1.

Alternatively, wrap the `rp.Send()` call with a `SqlTransaction` from a connection where the
table doesn't exist (wrong schema) — guaranteed single-attempt failure.

**If the ROADMAP's "attempt count = 1, not 3" requirement must be verified explicitly:** The
`IMetrics` instance would need a counter for retry attempts, and the metrics system does not
appear to expose a "retry attempt count" counter for producers (only consumer-side retry
metrics exist: `MessageFailedProcessingRetryMeter`). **Decision Required:** Either:

(a) Accept that "no retry" is demonstrated by the exception propagating immediately (no delay,
    no 3x retry backoff observable in wall-clock time), OR
(b) Add a dedicated "retry attempt" assertion by instrumenting the handler, which is out of
    scope for Phase 6.

**Recommendation:** Option (a). The structural unit tests in Phases 3/4 already pin the
`SkipRetry = true` path. The integration test need only assert the exception propagates
synchronously. This avoids inventing a new metrics counter and is sufficient for PROJECT.md
§Success Criteria #8.

---

## §5. Jenkinsfile Stage Names

From `Jenkinsfile` lines 69-120:

| Transport | Stage name | csproj path tested |
|---|---|---|
| SqlServer | `'SqlServer'` | `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj` |
| PostgreSQL | `'PostgreSQL'` | `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj` |

Both stages are inside the `parallel { }` block under `stage('Integration Tests')`.

**Confirmation:** Phase 6 tests in `Outbox/` subfolders inside these projects will be picked
up automatically by the existing Jenkins stage invocations. No Jenkinsfile changes needed.
The credential injection (`SQLSERVER_CONN`, `POSTGRESQL_CONN`) and `connectionstring.txt`
placement are already handled by the existing stage definitions. Retry flag (`--retry-failed-tests 1`)
is already applied.

---

## §6. Discrepancy: ROADMAP "22" vs. Math "24"

**Finding:** ROADMAP §Phase 6 explicitly lists the matrix as 8+1+2+1 = **12 per transport**.
The "22" figure in the ROADMAP text and CONTEXT-6.md intro is inconsistent with this. The
CONTEXT-6.md Phase Scope section already calls this out as a discrepancy (line 17).

**Root cause analysis:** The figure 22 likely originated when the batch tests were counted
as 2 per pair (one commit + one rollback combined into one test) for the batch variants but
4 for the single-message variants — or one batch variant was dropped from the matrix. The
current matrix as written produces 12 per transport = 24 total.

**Recommendation for architect:** Accept **24 total (12 per transport)** and update ROADMAP at
ship time. The method-matrix must include all four methods × commit + rollback = 8 tests; the
AdditionalMessageData, validation, and retry tests add 4 more per transport. Dropping any
batch test reduces coverage of the `HandleExternalTx` batch fork — the exact code path that
has no sync-from-async inference per the ROADMAP's own rationale. Do not drop any of the 24.

If the product owner requires exactly 22, the only defensible cut is dropping one batch
rollback test per transport (e.g., keep `SendAsync` batch commit + rollback, drop `Send` batch
rollback), accepting reduced coverage of the sync batch fork. This is not recommended.

---

## §7. `connectionstring.txt` Files

Both files exist and are confirmed in the respective `.csproj` `<None Update>` blocks:

- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/connectionstring.txt` — confirmed
  present, `<CopyToOutputDirectory>Always</CopyToOutputDirectory>` in `.csproj` (line 28-31).
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/connectionstring.txt` — confirmed
  present (Jenkinsfile line 111 writes to
  `bin/Debug/net10.0/connectionstring.txt` for this project).

**Parsing pattern** (both transports, identical code):

```csharp
// Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/ConnectionString.cs, line 20
var connectionString = File.ReadAllText("connectionstring.txt");
```

No `.Trim()` is applied in the source code — the file content is used verbatim. If the
Jenkins credential injection adds a trailing newline (as `echo "..."` does on Linux), the
connection string will include it. Existing tests work with this, so Phase 6 tests should
also use `ConnectionInfo.ConnectionString` directly without trimming. If tests fail due to
whitespace, both projects' `ConnectionString.cs` files would need a `.Trim()` — but this is a
pre-existing condition and not Phase 6's concern.

---

## §8. Queue Table Teardown Pattern

**Confirmed:** `oCreation.RemoveQueue()` in the `finally` block of every test wrapper (e.g.,
`SimpleProducer.Run<>` lines 44-46 in
`Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/Implementation/SimpleProducer.cs`).

This call drops all tables created by `CreateQueue()` — queue body table, metadata table,
status table (if enabled), error tracking table, etc. The implementation is in
`SqlServerMessageQueueCreation.RemoveQueue()` / `PostgreSqlMessageQueueCreation.RemoveQueue()`
in the respective transport projects.

**For Phase 6 base classes:** The `finally` block pattern is:

```csharp
finally
{
    // Drop DNQ queue tables
    oCreation?.RemoveQueue();
    oCreation?.Dispose();
    scope?.Dispose();

    // Drop the business table created for atomic-commit verification
    DropBusinessTable(conn, businessTableName);  // Phase 6 invention — raw SQL DROP TABLE
    conn?.Dispose();
}
```

The business table (`CREATE TABLE OutboxBusiness_<guid>`) is NOT managed by DNQ, so it must be
dropped via a direct `DROP TABLE IF EXISTS` SQL command in the test base class's teardown.

---

## §9. Wired-Up Path Verification (Phase 3/4 DI → Integration Test Runtime)

**Confirmed from Phase 3 SUMMARY-1.1.md:**

> `RegisterConditional(... LifeStyles.Singleton)` ... "This preserves lazy verification
> semantics matching the existing fallback `RegisterConditional` in
> `ComponentRegistration.RegisterFallbacks` (line 385). Phase 3's `RegisterConditional` runs
> first (during transport init) and 'claims' `IProducerQueue<>`; the fallback's later
> conditional registration finds `c.Handled = true` and skips, preserving correct resolution
> to the SqlServer subclass."

The capability-cast unit test in Phase 3 used type-system `IsAssignableFrom` assertions (not
runtime DI resolution) because `EnableAutoVerification` surfaced unrelated diagnostic warnings.
Phase 3 SUMMARY-1.1 explicitly notes:

> "PROJECT.md §Success Criteria #3 satisfied; Phase 6 integration tests will cover runtime
> resolution against real SqlServer."

**For Phase 6 PLAN-1.1/PLAN-2.1:** The first test in each transport's method-matrix suite should
also assert the capability cast works at runtime:

```csharp
var producer = creator.CreateProducer<FakeMessage>(queueConnection);
Assert.IsInstanceOfType(producer, typeof(IRelationalProducerQueue<FakeMessage>));
var rp = (IRelationalProducerQueue<FakeMessage>)producer;
```

This closes PROJECT.md §Success Criteria #3 at the runtime level. The registration path is:

1. `QueueContainer<SqlServerMessageQueueInit>` resolves `IProducerQueue<FakeMessage>`
2. `RegisterConditional` in `SQLServerMessageQueueInit.cs` maps it to
   `SqlServerRelationalProducerQueue<FakeMessage>`
3. `SqlServerRelationalProducerQueue<T>` inherits `RelationalProducerQueue<T>` which inherits
   `ProducerQueue<T>` which implements `IProducerQueue<T>`
4. Cast to `IRelationalProducerQueue<T>` succeeds

PostgreSQL path is identical via `PostgreSQLMessageQueueInit.cs`.

---

## §10. Cross-Database Mismatch Test Setup

DEFERRED to architect — recommendation follows.

**What was NOT found:** Neither transport's integration test project has a second database
connection string or a `connectionstring-other.txt` file. The SqlServer project's
`ConnectionString.cs` defines `Schema1 = "test1"` and `Schema2 = "test2"` (schemas, not
databases), but no second database connection.

**Recommendation:** The cross-database validation test does NOT require a real second database.
The validator (`ExternalTransactionValidator`) runs *before* any SQL is executed — it reads
`tx.Connection.Database` and compares it to the queue's configured database name. The test
can be structured as follows:

1. Create a DNQ queue against the primary database (via `oCreation.CreateQueue()`).
2. Open a `SqlConnection` / `NpgsqlConnection` to the **same server** but pointing at a
   *different database that already exists* (e.g., `master` on SqlServer, `postgres` on
   PostgreSQL — both are guaranteed to exist on any running instance).
3. Begin a transaction on that connection.
4. Call `rp.Send(msg, tx)` — the validator sees `tx.Connection.Database = "master"` (or
   `"postgres"`) vs. the queue's configured database and throws `InvalidOperationException`.
5. Assert the exception and assert queue table row count = 0 (no partial write).

**No `CREATE DATABASE` or second connection string needed.** The system databases (`master`,
`postgres`) are always present. The test just needs to open a connection to the system DB by
replacing the `Database=` segment of the test connection string, or by constructing a
`SqlConnectionStringBuilder` / `NpgsqlConnectionStringBuilder`:

```csharp
// SqlServer
var builder = new SqlConnectionStringBuilder(ConnectionInfo.ConnectionString);
builder.InitialCatalog = "master";  // guaranteed to exist
using var wrongConn = new SqlConnection(builder.ConnectionString);
wrongConn.Open();
using var wrongTx = wrongConn.BeginTransaction();
// rp.Send(msg, wrongTx) should throw InvalidOperationException
```

```csharp
// PostgreSQL
var builder = new NpgsqlConnectionStringBuilder(ConnectionInfo.ConnectionString);
builder.Database = "postgres";  // guaranteed to exist
using var wrongConn = new NpgsqlConnection(builder.ConnectionString);
wrongConn.Open();
using var wrongTx = wrongConn.BeginTransaction();
// rp.Send(msg, wrongTx) should throw InvalidOperationException
```

`SqlConnectionStringBuilder` is available in `Microsoft.Data.SqlClient` (already referenced
in the SqlServer integration test `.csproj`). `NpgsqlConnectionStringBuilder` is in
`Npgsql` (already referenced in the PostgreSQL `.csproj`).

---

## Comparison Matrix (Integration Test Structure)

| Criteria | SqlServer | PostgreSQL |
|---|---|---|
| Folder | `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/` | `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/` |
| csproj | `DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj` | `DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj` |
| Jenkins stage name | `'SqlServer'` (Jenkinsfile line 69) | `'PostgreSQL'` (Jenkinsfile line 105) |
| Connection string source | `ConnectionInfo.ConnectionString` (static, `ConnectionString.cs`) | `ConnectionInfo.ConnectionString` (static, `ConnectionString.cs`) |
| Queue name helper | `GenerateQueueName.Create()` | `GenerateQueueName.Create()` |
| Queue creation class | `SqlServerMessageQueueCreation` | `PostgreSqlMessageQueueCreation` |
| Transport init class | `SqlServerMessageQueueInit` | `PostgreSqlMessageQueueInit` |
| Teardown | `oCreation.RemoveQueue()` in finally | same |
| `IRelationalProducerQueue<T>` impl | `SqlServerRelationalProducerQueue<T>` | `PostgreSqlRelationalProducerQueue<T>` |
| DB name comparison | `OrdinalIgnoreCase` (normalizes to upper) | `Ordinal` (verbatim, case-sensitive) |
| Wrong-DB candidate for §C test | `master` (always exists) | `postgres` (always exists) |
| Cross-DB builder class | `SqlConnectionStringBuilder` | `NpgsqlConnectionStringBuilder` |
| ADO connection type | `Microsoft.Data.SqlClient.SqlConnection` | `Npgsql.NpgsqlConnection` |

---

## Recommendation for Test Count (22 vs. 24)

Accept **24 tests (12 per transport)**. The ROADMAP's "22" is a typo/rounding error — the
matrix as specified produces 12. Update `ROADMAP.md` and `CONTEXT-6.md` at ship time. Do not
cut batch tests; the batch `HandleExternalTx` fork is a distinct code path per ROADMAP's own
coverage rationale.

---

## Implementation Considerations

### Base class structure

Each `*OutboxIntegrationTestBase` should provide:

1. `CreateQueue(queueName)` — wraps `QueueCreationContainer<T>` + `GetQueueCreation<TCreate>`
   + `SetOptions(oCreation)` + `CreateQueue()`. Returns `(oCreation, scope)`.
2. `CreateBusinessTable(SqlConnection conn)` — `CREATE TABLE OutboxBusiness_{guid} (Id INT, Val NVARCHAR(100))`. Returns table name.
3. `InsertBusinessRow(tx, tableName, val)` — `INSERT INTO OutboxBusiness_{guid} VALUES (@id, @val)` within the caller's transaction.
4. `AssertQueueRowCount(conn, queueName, expected)` — `SELECT COUNT(*) FROM {MetaDataName}`.
   Use `SqlServerTableNameHelper` / `TableNameHelper` (already used in `VerifyQueueData.cs`)
   to resolve the table name.
5. `AssertBusinessRowExists(conn, tableName, val)` — `SELECT COUNT(*)` assertion.
6. `DropBusinessTable(conn, tableName)` — `DROP TABLE IF EXISTS {tableName}`.
7. `GetRelationalProducer(queueConnection)` — creates a `QueueContainer<TInit>`, resolves
   `IProducerQueue<FakeMessage>`, casts to `IRelationalProducerQueue<FakeMessage>`.

### `SqlServerTableNameHelper` / `TableNameHelper` access

These internal helpers are already used in test projects (see `VerifyQueueData.cs` line 8 and
`SharedClasses.cs` line 6 in the SqlServer project). They are accessible from test projects
because `InternalsVisibleTo` is configured in both transport projects.

- SqlServer: `SqlServerTableNameHelper` in `DotNetWorkQueue.Transport.RelationalDatabase.Basic`
  (via `SqlConnectionInformation`). Usage: `new SqlConnectionInformation(queueConnection)` →
  `new SqlServerTableNameHelper(connection)` → `.MetaDataName`.
- PostgreSQL: `TableNameHelper` in `DotNetWorkQueue.Transport.PostgreSQL.Basic`
  (via `SqlConnectionInformation`). Usage in `VerifyQueueData.cs` (PostgreSQL project).

### `AdditionalMessageData` for the B-test

```csharp
var data = new AdditionalMessageData();
data.CorrelationId = new CorrelationId(Guid.NewGuid());
// OR use SetExpiration/SetPriority for values verifiable in metadata table
data.SetPriority(7);
```

After commit, query the metadata table directly for `priority = 7` — same pattern as
`VerifyQueueData.VerifyPriority()` (lines 150-165 of `VerifyQueueData.cs`). This avoids
building a consumer in the B-test.

### Retry-bypass (D-test) — concrete approach

Use the "already-committed transaction" technique:

```csharp
using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
conn.Open();
var tx = conn.BeginTransaction();
tx.Commit();   // transaction is now complete; Connection is open but tx is invalid
// Now pass the committed tx to Send — tx.Connection is non-null, State=Open,
// but SqlClient will throw on first command execution since tx is no longer active
await Assert.ThrowsExactlyAsync<Exception>(() =>
    rp.SendAsync(new FakeMessage(), tx).AsTask());
// Assert queue metadata table count = 0
```

**Note:** `DbTransaction.Connection` after `Commit()` may be null on some ADO drivers (it
is null on `SqlTransaction` after commit). If null, the validator throws
`InvalidOperationException` — which is also acceptable for this test's purpose (it proves
the path fails fast on first attempt, not after 3 retry attempts). Either outcome satisfies
PROJECT.md §Success Criteria #8 at the integration level, since the Phase 3/4 structural
tests already pin the `SkipRetry = true` wiring.

**Decision Required:** If the architect wants to distinguish "validator threw" from "handler
threw on first attempt", the test should use a valid open transaction pointing to the queue's
database (passes validation) but lock a row to cause a deadlock/timeout. This requires a
second connection holding a lock, which is more complex but produces a true "first-attempt
SQL error". The simpler committed-tx approach is recommended unless the architect specifically
requires the deadlock scenario.

---

## Sources

All findings are from the local codebase. Key files consulted:

1. `Jenkinsfile` — stage names, csproj paths, credential injection, connection string file
   placement (lines 69-120, 105-120).
2. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj`
3. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/ConnectionString.cs`
4. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/GenerateQueueName.cs`
5. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/SharedClasses.cs`
6. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/VerifyQueueData.cs`
7. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Producer/SimpleProducer.cs`
8. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Producer/SimpleProducerAsync.cs`
9. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Producer/SimpleProducerBatch.cs`
10. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Producer/SimpleProducerAsyncBatch.cs`
11. `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/AssemblyInit.cs`
12. `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/ConnectionString.cs`
13. `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/GenerateQueueName.cs`
14. `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/SharedClasses.cs`
15. `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Producer/SimpleProducer.cs`
16. `Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/Implementation/SimpleProducer.cs`
17. `Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/ProducerShared.cs`
18. `Source/DotNetWorkQueue.IntegrationTests.Shared/Metrics/Metrics.cs`
19. `Source/DotNetWorkQueue.IntegrationTests.Shared/VerifyMetrics.cs`
20. `.shipyard/phases/3/results/SUMMARY-1.1.md` — DI registration decisions (`RegisterConditional`, capability-cast approach)
21. `.shipyard/phases/3/results/SUMMARY-2.1.md` — sync handler fork implementation details
22. `.shipyard/phases/3/results/SUMMARY-2.2.md` — async handler fork implementation details
23. `.shipyard/phases/4/results/SUMMARY-1.1.md` — PG extractor pass-through, PG DI wiring
24. `.shipyard/phases/4/results/SUMMARY-2.1.md` — PG sync handler fork, NpgsqlDbType.Bytea
25. `.shipyard/phases/4/results/SUMMARY-2.2.md` — PG async handler fork

---

## Uncertainty Flags

1. **§3 IAdditionalMessageData round-trip consumer path:** No existing test in either project
   dequeues and inspects `IReceivedMessage<T>` metadata. The B-test's approach (SQL query vs.
   live consumer) is a **Decision Required** for the architect. Recommendation: SQL query on
   the metadata table, same approach as `VerifyPriority()`.

2. **§4 Retry-bypass D-test exact mechanism:** No existing "force transient SQL error"
   infrastructure. The committed-tx approach is recommended; the deadlock/lock-conflict approach
   is more rigorous but higher complexity. **Decision Required.**

3. **`connectionstring.txt` trailing whitespace:** `File.ReadAllText` without `.Trim()` may
   include a trailing newline from Jenkins' `echo "..."` injection. Existing tests live with
   this; if Phase 6 uses `SqlConnectionStringBuilder(ConnectionInfo.ConnectionString)` for
   the §10 cross-DB test, a trailing newline may cause a parse exception. Add `.Trim()` in the
   builder construction, not in `ConnectionString.cs` (to avoid modifying shared infra).

4. **`AdditionalMessageData` CorrelationId column:** Whether the queue's metadata table stores
   the `CorrelationId` as a queryable column depends on which options are enabled during
   `CreateQueue()`. Priority is always stored when `EnablePriority = true`. Recommendation:
   use `data.SetPriority(7)` as the assertable metadata value in the B-test (simpler than
   CorrelationId, column always present when priority is enabled).

5. **`InternalsVisibleTo` coverage for `SqlServerRelationalProducerQueue<T>` from the
   integration test assembly:** The Phase 3 SUMMARY notes that `SqlServerRelationalProducerQueue<T>`
   is wired via `RegisterConditional`. The test project references `DotNetWorkQueue.Transport.SqlServer`
   directly (`.csproj` line 23), so the type is accessible without `InternalsVisibleTo`. The
   `CreateProducer<T>()` call returns `IProducerQueue<T>` — the cast to `IRelationalProducerQueue<T>`
   will succeed at runtime per the DI registration. No additional visibility wiring needed.
