---
phase: integration-tests
plan: 2.1
wave: 2
dependencies: [1.1, 1.2]
must_haves:
  - PostgreSqlOutboxIntegrationTestBase exists with atomic-commit helpers (PG-specific)
  - 8 method-matrix tests pass against real PostgreSQL (Send + SendAsync × single + batch × commit + rollback)
  - All tests use queue-per-test isolation via "q" + Guid.NewGuid().ToString("N")
  - Each test uses try/finally with oCreation.RemoveQueue() teardown
  - Capability-cast IRelationalProducerQueue<FakeMessage> verified at runtime on PostgreSQL
  - Atomic commit: both queue row AND business row visible after tx.Commit
  - Atomic rollback: neither queue row NOR business row present after tx.Rollback
  - No regressions in existing PostgreSQL integration test suite
files_touched:
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxIntegrationTestBase.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxSendTests.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxSendAsyncTests.cs
tdd: false
risk: medium
---

# Plan 2.1: PostgreSQL Outbox Method-Matrix Integration Tests

## Context

Mirrors PLAN-1.1 (SqlServer) for PostgreSQL. Phase 4 shipped the PG `HandleExternalTx` /
`HandleExternalTxAsync` forks and the `PostgreSqlRelationalProducerQueue<T>` capability-cast
wiring. This plan exercises the full runtime path against a real PostgreSQL instance for
the four caller-tx public methods × commit/rollback outcomes.

**Wave-2 dependency rationale (Decision 3 in CONTEXT-6):** Phase 6 Wave 2 ships only
after the Wave-1 (SqlServer) draft PR achieves Jenkins-green on the `SqlServer` stage.
This catches transport-specific flakes early and reflects the user's CI-gating decision.

**Path convention NOTE:** PostgreSQL integration tests live in
`Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/` (DOT before "Integration").
This differs from SqlServer's `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/`
(no dot). Builder MUST use the correct path — RESEARCH §1 confirms both.

## Dependencies

- Wave 1 (PLAN-1.1 + PLAN-1.2) merged and Jenkins SqlServer stage green on draft PR.
- Phase 4 complete (PG handler forks + DI wiring + unit tests).
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/connectionstring.txt` configured.
- `Npgsql` already referenced in the PG integration test csproj.

No new NuGet dependencies. No Jenkinsfile changes (`Outbox/` is a subfolder of an
existing project that the `PostgreSQL` Jenkins stage already runs).

---

## Tasks

### Task 1: Create `PostgreSqlOutboxIntegrationTestBase` with atomic-commit harness

**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxIntegrationTestBase.cs` (NEW)

**Action:** create

**Description:**

Mirror `SqlServerOutboxIntegrationTestBase` (PLAN-1.1 Task 1) with PostgreSQL-specific
types and SQL:

- `NpgsqlConnection` instead of `SqlConnection`.
- `NpgsqlTransaction` instead of `SqlTransaction`.
- `TableNameHelper` (in `DotNetWorkQueue.Transport.PostgreSQL.Basic`) instead of
  `SqlServerTableNameHelper`.
- `PostgreSqlMessageQueueInit` / `PostgreSqlMessageQueueCreation` instead of the
  SqlServer equivalents.
- Business table DDL uses PostgreSQL syntax:
  - `CREATE TABLE` (no `dbo.` prefix; PG schema is public by default).
  - `VARCHAR(100)` not `NVARCHAR`.
  - `DROP TABLE IF EXISTS` (PG natively supports this).
- Parameter prefix is `@` for both providers (Npgsql accepts `@name`).
- The `GuardNpgsqlTransaction` check in `PostgreSqlRelationalProducerQueue<T>` throws on
  non-`NpgsqlTransaction`; tests pass real `NpgsqlTransaction` so the guard is silent.

**Shape — `PostgreSqlOutboxIntegrationTestBase.cs`:**

```csharp
// LGPL-2.1 license header (copy from
// Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/SharedClasses.cs)

using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Outbox
{
    public abstract class PostgreSqlOutboxIntegrationTestBase
    {
        protected static string NewQueueName() => "q" + Guid.NewGuid().ToString("N");

        // PostgreSQL identifier max length is 63; "outboxbusiness_" + 32-char hex = 47. OK.
        protected static string NewBusinessTableName() => "outboxbusiness_" + Guid.NewGuid().ToString("N");

        protected sealed class QueueScope : IDisposable
        {
            public QueueCreationContainer<PostgreSqlMessageQueueInit> QueueCreator { get; init; }
            public PostgreSqlMessageQueueCreation OCreation { get; init; }
            public ICreationScope Scope { get; init; }

            public void Dispose()
            {
                try { OCreation?.RemoveQueue(); } catch { /* swallow */ }
                OCreation?.Dispose();
                Scope?.Dispose();
                QueueCreator?.Dispose();
            }
        }

        protected QueueScope CreateQueue(QueueConnection queueConnection)
        {
            var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(queueConnection);
            oCreation.Options.EnableStatus = true;
            oCreation.Options.EnableStatusTable = false;
            oCreation.Options.EnableHeartBeat = false;
            oCreation.Options.EnableDelayedProcessing = false;
            oCreation.Options.EnableMessageExpiration = false;
            oCreation.Options.EnableHoldTransactionUntilMessageCommitted = false;
            oCreation.Options.EnablePriority = false;

            var result = oCreation.CreateQueue();
            Assert.IsTrue(result.Success, result.ErrorMessage);

            return new QueueScope
            {
                QueueCreator = queueCreator,
                OCreation = oCreation,
                Scope = oCreation.Scope
            };
        }

        protected sealed class ProducerScope : IDisposable
        {
            public QueueContainer<PostgreSqlMessageQueueInit> Creator { get; init; }
            public IProducerQueue<FakeMessage> Producer { get; init; }
            public IRelationalProducerQueue<FakeMessage> RelationalProducer { get; init; }

            public void Dispose()
            {
                Producer?.Dispose();
                Creator?.Dispose();
            }
        }

        protected ProducerScope CreateRelationalProducer(QueueConnection queueConnection)
        {
            var creator = new QueueContainer<PostgreSqlMessageQueueInit>();
            var producer = creator.CreateProducer<FakeMessage>(queueConnection);
            Assert.IsInstanceOfType(producer, typeof(IRelationalProducerQueue<FakeMessage>),
                "PostgreSQL producer must implement IRelationalProducerQueue<T> (PROJECT.md §SC #3)");
            var rp = (IRelationalProducerQueue<FakeMessage>)producer;
            return new ProducerScope { Creator = creator, Producer = producer, RelationalProducer = rp };
        }

        // ---- Business table lifecycle (PostgreSQL) ----
        protected static void CreateBusinessTable(NpgsqlConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE TABLE {tableName} (Id INT NOT NULL, Val VARCHAR(100) NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        protected static void DropBusinessTable(NpgsqlConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
            cmd.ExecuteNonQuery();
        }

        protected static void InsertBusinessRow(NpgsqlConnection conn, NpgsqlTransaction tx, string tableName, int id, string val)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = $"INSERT INTO {tableName} (Id, Val) VALUES (@id, @val)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@val", val);
            cmd.ExecuteNonQuery();
        }

        protected static long CountBusinessRows(NpgsqlConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {tableName}";
            return (long)cmd.ExecuteScalar();
        }

        protected static long CountQueueMessages(QueueConnection queueConnection)
        {
            var info = new SqlConnectionInformation(queueConnection);
            var helper = new TableNameHelper(info);
            using var conn = new NpgsqlConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {helper.MetaDataName}";
            return (long)cmd.ExecuteScalar();
        }

        protected static void AssertQueueRowCount(QueueConnection queueConnection, long expected, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            long actual = -1;
            while (DateTime.UtcNow < deadline)
            {
                actual = CountQueueMessages(queueConnection);
                if (actual == expected) return;
                System.Threading.Thread.Sleep(100);
            }
            Assert.AreEqual(expected, actual,
                $"Queue row count did not converge to {expected} within {timeoutMs}ms (last observed: {actual}).");
        }

        protected static void AssertBusinessRowExists(NpgsqlConnection conn, string tableName, long expectedCount)
        {
            var actual = CountBusinessRows(conn, tableName);
            Assert.AreEqual(expectedCount, actual,
                $"Business table {tableName} expected {expectedCount} rows, observed {actual}.");
        }

        protected static List<QueueMessage<FakeMessage, IAdditionalMessageData>> BuildBatch(int count)
        {
            var list = new List<QueueMessage<FakeMessage, IAdditionalMessageData>>(count);
            for (var i = 0; i < count; i++)
            {
                list.Add(new QueueMessage<FakeMessage, IAdditionalMessageData>(
                    GenerateMessage.Create<FakeMessage>(), null));
            }
            return list;
        }
    }
}
```

**Notes for builder:**

- `TableNameHelper` (PG) lives in `DotNetWorkQueue.Transport.PostgreSQL.Basic`. Same
  shape as `SqlServerTableNameHelper`.
- `SqlConnectionInformation` is the same class name in both transports (intentional —
  see RESEARCH §1 / VerifyQueueData.cs PG variant); namespace differs.
- PostgreSQL identifier max length is 63 chars (vs. 128 on SQL Server). `outboxbusiness_`
  + 32-char GUID-hex = 47 chars; safe.
- `NpgsqlCommand.ExecuteScalar()` returns `long` for `SELECT COUNT(*)` on PG (vs. `int`
  on SqlServer); cast accordingly.
- PostgreSQL is case-sensitive for unquoted identifiers but folds them to lowercase. The
  business table name uses all-lowercase to avoid quoting headaches.

**Acceptance Criteria:**

- File exists at the path above with the LGPL-2.1 header.
- Project builds clean: `dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" -c Debug`
- No regressions in non-Outbox tests.

---

### Task 2: `PostgreSqlOutboxSendTests` — 4 sync tests (single + batch × commit + rollback)

**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxSendTests.cs` (NEW)

**Action:** create + test

**Description:**

Mirror PLAN-1.1 Task 2 with `NpgsqlConnection` / `NpgsqlTransaction` and the PG-specific
base class.

**Required test names (exact):**

- `Send_Commit_BothRowsVisible`
- `Send_Rollback_NeitherRowVisible`
- `SendBatch_Commit_AllRowsVisible`
- `SendBatch_Rollback_NeitherRowVisible`

**Shape — `PostgreSqlOutboxSendTests.cs`:**

```csharp
// LGPL-2.1 license header

using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Outbox
{
    [TestClass]
    public class PostgreSqlOutboxSendTests : PostgreSqlOutboxIntegrationTestBase
    {
        [TestMethod]
        public void Send_Commit_BothRowsVisible()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var tx = conn.BeginTransaction())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = producer.RelationalProducer.Send(msg, tx);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, tx, businessTable, 1, "first");
                    tx.Commit();
                }

                AssertQueueRowCount(qc, 1);
                AssertBusinessRowExists(conn, businessTable, 1);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        public void Send_Rollback_NeitherRowVisible()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var tx = conn.BeginTransaction())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = producer.RelationalProducer.Send(msg, tx);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, tx, businessTable, 1, "first");
                    tx.Rollback();
                }

                AssertQueueRowCount(qc, 0);
                AssertBusinessRowExists(conn, businessTable, 0);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        public void SendBatch_Commit_AllRowsVisible()
        {
            const int batchSize = 5;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var tx = conn.BeginTransaction())
                {
                    var batch = BuildBatch(batchSize);
                    var result = producer.RelationalProducer.Send(batch, tx);
                    Assert.IsFalse(result.HasErrors);
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, tx, businessTable, i, $"row{i}");
                    tx.Commit();
                }

                AssertQueueRowCount(qc, batchSize);
                AssertBusinessRowExists(conn, businessTable, batchSize);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        public void SendBatch_Rollback_NeitherRowVisible()
        {
            const int batchSize = 5;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var tx = conn.BeginTransaction())
                {
                    var batch = BuildBatch(batchSize);
                    var result = producer.RelationalProducer.Send(batch, tx);
                    Assert.IsFalse(result.HasErrors);
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, tx, businessTable, i, $"row{i}");
                    tx.Rollback();
                }

                AssertQueueRowCount(qc, 0);
                AssertBusinessRowExists(conn, businessTable, 0);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }
    }
}
```

**Notes for builder:**

- `NpgsqlConnection.BeginTransaction()` returns `NpgsqlTransaction`, which implements
  `DbTransaction` — the producer's `GuardNpgsqlTransaction` check accepts this.
- PostgreSQL transactions on the same connection are sequential by ADO.NET contract,
  same as SqlServer. Batch-Send sequencing inside `PostgreSqlRelationalProducerQueue<T>`
  iterates one INSERT at a time on the caller's tx.

**Acceptance Criteria:**

- File exists with LGPL-2.1 header.
- `dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" -c Debug --filter "FullyQualifiedName~PostgreSqlOutboxSendTests"` — 4 tests pass.
- No regressions in the full project test run.

---

### Task 3: `PostgreSqlOutboxSendAsyncTests` — 4 async tests

**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxSendAsyncTests.cs` (NEW)

**Action:** create + test

**Description:**

Mirror PLAN-1.1 Task 3 with async + Npgsql.

**Why a separate file:** `SendMessageCommandHandlerAsync` is in its own file in the PG
transport (per RESEARCH §10 and CLAUDE.md async-handler lesson). Codecov coverage of
the async path requires the async test to run; it cannot be inferred from sync coverage.

**Required test names (exact):**

- `SendAsync_Commit_BothRowsVisible`
- `SendAsync_Rollback_NeitherRowVisible`
- `SendBatchAsync_Commit_AllRowsVisible`
- `SendBatchAsync_Rollback_NeitherRowVisible`

**Shape — `PostgreSqlOutboxSendAsyncTests.cs`:**

```csharp
// LGPL-2.1 license header

using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Outbox
{
    [TestClass]
    public class PostgreSqlOutboxSendAsyncTests : PostgreSqlOutboxIntegrationTestBase
    {
        [TestMethod]
        public async Task SendAsync_Commit_BothRowsVisible()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            await using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = await conn.BeginTransactionAsync())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, tx);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, (NpgsqlTransaction)tx, businessTable, 1, "first");
                    await tx.CommitAsync();
                }

                AssertQueueRowCount(qc, 1);
                AssertBusinessRowExists(conn, businessTable, 1);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        public async Task SendAsync_Rollback_NeitherRowVisible()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            await using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = await conn.BeginTransactionAsync())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, tx);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, (NpgsqlTransaction)tx, businessTable, 1, "first");
                    await tx.RollbackAsync();
                }

                AssertQueueRowCount(qc, 0);
                AssertBusinessRowExists(conn, businessTable, 0);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        public async Task SendBatchAsync_Commit_AllRowsVisible()
        {
            const int batchSize = 5;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            await using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = await conn.BeginTransactionAsync())
                {
                    var batch = BuildBatch(batchSize);
                    var result = await producer.RelationalProducer.SendAsync(batch, tx);
                    Assert.IsFalse(result.HasErrors);
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, (NpgsqlTransaction)tx, businessTable, i, $"row{i}");
                    await tx.CommitAsync();
                }

                AssertQueueRowCount(qc, batchSize);
                AssertBusinessRowExists(conn, businessTable, batchSize);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }

        [TestMethod]
        public async Task SendBatchAsync_Rollback_NeitherRowVisible()
        {
            const int batchSize = 5;
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            await using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = await conn.BeginTransactionAsync())
                {
                    var batch = BuildBatch(batchSize);
                    var result = await producer.RelationalProducer.SendAsync(batch, tx);
                    Assert.IsFalse(result.HasErrors);
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, (NpgsqlTransaction)tx, businessTable, i, $"row{i}");
                    await tx.RollbackAsync();
                }

                AssertQueueRowCount(qc, 0);
                AssertBusinessRowExists(conn, businessTable, 0);
            }
            finally
            {
                DropBusinessTable(conn, businessTable);
            }
        }
    }
}
```

**Notes for builder:**

- `NpgsqlConnection.BeginTransactionAsync()` returns `ValueTask<NpgsqlTransaction>` —
  the `await` resolves to `NpgsqlTransaction` directly (no cast required for the
  `using` declaration, but a cast IS required when calling `InsertBusinessRow` which
  takes the concrete `NpgsqlTransaction`).
- `await using` works on `NpgsqlConnection` and `NpgsqlTransaction` (Npgsql 10.x).

**Acceptance Criteria:**

- File exists with LGPL-2.1 header.
- `dotnet test ... --filter "FullyQualifiedName~PostgreSqlOutboxSendAsyncTests"` — 4 tests pass.
- Full PG integration suite still green.

---

## Verification

```bash
# Build
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" -c Debug

# Run only the 8 PLAN-2.1 tests
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" \
  -c Debug \
  --filter "FullyQualifiedName~PostgreSqlOutboxSendTests|FullyQualifiedName~PostgreSqlOutboxSendAsyncTests"

# Regression
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" -c Debug
```

## PROJECT.md Success Criteria coverage

Same mapping as PLAN-1.1 but for PostgreSQL — see PLAN-1.1 §SC coverage table.
