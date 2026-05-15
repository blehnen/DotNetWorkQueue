---
phase: integration-tests
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - SqlServerOutboxIntegrationTestBase exists with atomic-commit helpers
  - 8 method-matrix tests pass against real SqlServer (Send + SendAsync × single + batch × commit + rollback)
  - All tests use queue-per-test isolation via "q" + Guid.NewGuid().ToString("N")
  - Each test uses try/finally with oCreation.RemoveQueue() teardown
  - Capability-cast IRelationalProducerQueue<FakeMessage> verified at runtime (closes PROJECT.md §SC #3)
  - Atomic commit: both queue row AND business row visible after tx.Commit (closes §SC #4)
  - Atomic rollback: neither queue row NOR business row present after tx.Rollback (closes §SC #5)
  - No regressions in existing SqlServer integration test suite
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxIntegrationTestBase.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxSendTests.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxSendAsyncTests.cs
tdd: false
risk: medium
---

# Plan 1.1: SqlServer Outbox Method-Matrix Integration Tests

## Context

Phase 3 shipped the SqlServer `HandleExternalTx` / `HandleExternalTxAsync` forks
(sync + async + batch) and the `SqlServerRelationalProducerQueue<T>` capability-cast wiring.
Unit tests in Phase 3 pinned the structural seams using mocked `IDbConnection` /
`IDbConnectionFactory`. Phase 6 PLAN-1.1 now exercises **the full runtime path against a
real SqlServer instance** for the four caller-tx public methods × commit/rollback outcomes.

This plan covers the **method × outcome matrix only**: 8 tests across two test classes.
Validation, retry-bypass, and `IAdditionalMessageData` round-trip live in PLAN-1.2.

The atomic-commit harness creates a parallel business table per test (a tiny
`OutboxBusiness_<guid>` with `Id INT, Val NVARCHAR(100)`). Each test enlists BOTH the
DNQ queue INSERT and the business-table INSERT on the **same caller-tx** then commits or
rolls back, and asserts both rows are visible (commit) or neither row is visible
(rollback). This is the canonical outbox-pattern proof.

Closes PROJECT.md §Success Criteria #3 (runtime capability cast), #4 (atomic commit), #5
(atomic rollback) for SqlServer.

## Dependencies

- Phase 3 complete (SqlServer handler forks + DI wiring + unit tests).
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/connectionstring.txt`
  configured against a running SqlServer instance.
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj`
  already references `Microsoft.Data.SqlClient` and the transport project.

No new NuGet dependencies. No Jenkinsfile changes (`Outbox/` is a subfolder of an
existing project that the `SqlServer` Jenkins stage already runs).

---

## Tasks

### Task 1: Create `SqlServerOutboxIntegrationTestBase` with atomic-commit harness helpers

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxIntegrationTestBase.cs` (NEW)

**Action:** create

**Description:**

Create the shared test base class that every PLAN-1.1 and PLAN-1.2 test inherits.
It owns the queue lifecycle, the business-table lifecycle, queue-name generation,
producer resolution / capability cast, and the atomic-commit verification helpers.

Existing reference patterns (do not duplicate — call them where possible):

- `SqlServerTableNameHelper` (in `DotNetWorkQueue.Transport.RelationalDatabase.Basic` via
  `SqlConnectionInformation`) — already used in
  `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/VerifyQueueData.cs` to read
  `_tableNameHelper.MetaDataName`. Reuse the same construction.
- Queue lifecycle: `QueueCreationContainer<SqlServerMessageQueueInit>` →
  `GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection)` → `CreateQueue()` /
  `RemoveQueue()`. See
  `Source/DotNetWorkQueue.IntegrationTests.Shared/Producer/Implementation/SimpleProducer.cs`
  lines 24-49 for the canonical try/finally shape.
- Producer resolution: `QueueContainer<SqlServerMessageQueueInit>` then
  `creator.CreateProducer<FakeMessage>(queueConnection)`. Cast to
  `IRelationalProducerQueue<FakeMessage>`.
- `FakeMessage`: `DotNetWorkQueue.IntegrationTests.Shared.FakeMessage`.
- Connection string: `ConnectionInfo.ConnectionString` (existing static).

**Shape — `SqlServerOutboxIntegrationTestBase.cs`:**

```csharp
// LGPL-2.1 license header (copy from
// Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/SharedClasses.cs lines 1-18)

using System;
using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Outbox
{
    /// <summary>Shared helpers for outbox integration tests on SqlServer.</summary>
    public abstract class SqlServerOutboxIntegrationTestBase
    {
        // ---- Queue name (CLAUDE.md lesson: DNQ rejects hyphenated GUIDs) ----
        protected static string NewQueueName() => "q" + Guid.NewGuid().ToString("N");

        // ---- Business table name (parallel-safe; one per test) ----
        protected static string NewBusinessTableName() => "OutboxBusiness_" + Guid.NewGuid().ToString("N");

        // ---- Queue creation / removal ----
        protected sealed class QueueScope : IDisposable
        {
            public QueueCreationContainer<SqlServerMessageQueueInit> QueueCreator { get; init; }
            public SqlServerMessageQueueCreation OCreation { get; init; }
            public ICreationScope Scope { get; init; }

            public void Dispose()
            {
                try { OCreation?.RemoveQueue(); } catch { /* swallow — test teardown */ }
                OCreation?.Dispose();
                Scope?.Dispose();
                QueueCreator?.Dispose();
            }
        }

        protected QueueScope CreateQueue(QueueConnection queueConnection)
        {
            var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>();
            var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(queueConnection);
            // Default options: nothing fancy. Status table off by default keeps the row-count
            // assertions trivial (MetaDataName is always present).
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

        // ---- Producer resolution + capability cast ----
        protected sealed class ProducerScope : IDisposable
        {
            public QueueContainer<SqlServerMessageQueueInit> Creator { get; init; }
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
            var creator = new QueueContainer<SqlServerMessageQueueInit>();
            var producer = creator.CreateProducer<FakeMessage>(queueConnection);
            Assert.IsInstanceOfType(producer, typeof(IRelationalProducerQueue<FakeMessage>),
                "SqlServer producer must implement IRelationalProducerQueue<T> (PROJECT.md §SC #3)");
            var rp = (IRelationalProducerQueue<FakeMessage>)producer;
            return new ProducerScope { Creator = creator, Producer = producer, RelationalProducer = rp };
        }

        // ---- Business table lifecycle ----
        protected static void CreateBusinessTable(SqlConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"CREATE TABLE dbo.{tableName} (Id INT NOT NULL, Val NVARCHAR(100) NOT NULL)";
            cmd.ExecuteNonQuery();
        }

        protected static void DropBusinessTable(SqlConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"IF OBJECT_ID('dbo.{tableName}', 'U') IS NOT NULL DROP TABLE dbo.{tableName}";
            cmd.ExecuteNonQuery();
        }

        protected static void InsertBusinessRow(SqlConnection conn, SqlTransaction tx, string tableName, int id, string val)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = $"INSERT INTO dbo.{tableName} (Id, Val) VALUES (@id, @val)";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@val", val);
            cmd.ExecuteNonQuery();
        }

        protected static int CountBusinessRows(SqlConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM dbo.{tableName}";
            return (int)cmd.ExecuteScalar();
        }

        // ---- Queue row count assertion (polling, NOT snapshot — CLAUDE.md lesson) ----
        protected static int CountQueueMessages(QueueConnection queueConnection)
        {
            var info = new SqlConnectionInformation(queueConnection);
            var helper = new SqlServerTableNameHelper(info);
            using var conn = new SqlConnection(queueConnection.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM {helper.MetaDataName}";
            return (int)cmd.ExecuteScalar();
        }

        /// <summary>Polls the queue MetaData row count until it equals expected, or times out (default 5s).</summary>
        protected static void AssertQueueRowCount(QueueConnection queueConnection, int expected, int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            int actual = -1;
            while (DateTime.UtcNow < deadline)
            {
                actual = CountQueueMessages(queueConnection);
                if (actual == expected) return;
                System.Threading.Thread.Sleep(100);
            }
            Assert.AreEqual(expected, actual,
                $"Queue row count did not converge to {expected} within {timeoutMs}ms (last observed: {actual}).");
        }

        protected static void AssertBusinessRowExists(SqlConnection conn, string tableName, int expectedCount)
        {
            var actual = CountBusinessRows(conn, tableName);
            Assert.AreEqual(expectedCount, actual,
                $"Business table {tableName} expected {expectedCount} rows, observed {actual}.");
        }

        // ---- Convenience: build a batch ----
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

- `QueueCreationContainer<T>` and `QueueContainer<T>` accept an optional `Action<IContainer>`
  registration delegate. For Phase 6 outbox tests, no extra registrations are needed.
- `CreateProducer<T>` is called on `QueueContainer<T>` (NOT on `QueueCreationContainer<T>`).
- `IProducerQueue<FakeMessage>` implements `IDisposable`.
- The runtime cast assertion in `CreateRelationalProducer` closes PROJECT.md §SC #3 at the
  integration level for SqlServer.
- The polling helper `AssertQueueRowCount` is required because metrics/state writes are
  not always synchronous from the perspective of a separate connection.

**Acceptance Criteria:**

- File exists at the path above with the LGPL-2.1 header.
- Project builds clean: `dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" -c Debug`
- No regressions: `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" -c Debug` still passes for non-Outbox tests.

---

### Task 2: `SqlServerOutboxSendTests` — 4 sync tests (single + batch × commit + rollback)

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxSendTests.cs` (NEW)

**Action:** create + test

**Description:**

Four `[TestMethod]`s covering the sync `Send(T, DbTransaction)` and
`Send(List<QueueMessage<T, IAdditionalMessageData>>, DbTransaction)` paths × commit and
rollback outcomes. Each test follows the exact atomic-commit harness:

1. `NewQueueName()` → `QueueConnection`.
2. `CreateQueue(qc)` (try/finally — `using` the `QueueScope` ensures `RemoveQueue()`).
3. Open a real `SqlConnection`, `CreateBusinessTable(conn, name)` (try/finally — drop in finally).
4. Open the same connection's transaction.
5. `CreateRelationalProducer(qc)` → `rp.Send(msg, tx)` AND `InsertBusinessRow(conn, tx, ...)`.
6. `tx.Commit()` (commit tests) or `tx.Rollback()` (rollback tests).
7. Assert: `AssertQueueRowCount(qc, 1)` + `AssertBusinessRowExists(conn, name, 1)` for commit,
   or both `= 0` for rollback.

**Required test names (exact):**

- `Send_Commit_BothRowsVisible`
- `Send_Rollback_NeitherRowVisible`
- `SendBatch_Commit_AllRowsVisible`
- `SendBatch_Rollback_NeitherRowVisible`

**Shape — `SqlServerOutboxSendTests.cs`:**

```csharp
// LGPL-2.1 license header

using System.Data;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Outbox
{
    [TestClass]
    public class SqlServerOutboxSendTests : SqlServerOutboxIntegrationTestBase
    {
        [TestMethod]
        public void Send_Commit_BothRowsVisible()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
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
            using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
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
            using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                using (var tx = conn.BeginTransaction())
                {
                    var batch = BuildBatch(batchSize);
                    var result = producer.RelationalProducer.Send(batch, tx);
                    Assert.IsFalse(result.HasErrors,
                        result.HasErrors ? "batch had errors" : null);
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
            using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
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

- `IQueueOutputMessage.HasError` is singular; `IQueueOutputMessages.HasErrors` is plural.
- `result.SendingException` may be null even on failure paths if `HasError` is false; the
  `Assert.IsFalse(result.HasError, ...)` pattern matches existing tests.
- `conn.BeginTransaction()` returns `SqlTransaction` (which is a `DbTransaction`). The
  `SqlServerRelationalProducerQueue<T>` includes a `GuardSqlTransaction` check that throws
  on non-`SqlTransaction` — the tests pass a real `SqlTransaction` so this guard is silent.

**Acceptance Criteria:**

- File exists at the path above with the LGPL-2.1 header.
- `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" -c Debug --filter "FullyQualifiedName~SqlServerOutboxSendTests"` — 4 tests pass.
- No regressions in the full project test run.

---

### Task 3: `SqlServerOutboxSendAsyncTests` — 4 async tests (single + batch × commit + rollback)

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxSendAsyncTests.cs` (NEW)

**Action:** create + test

**Description:**

Mirror Task 2 with async variants. Each test is an `async Task` method that uses
`OpenAsync`, `BeginTransactionAsync`, `SendAsync`, `CommitAsync`/`RollbackAsync`.

**Why a separate file:** Phase 3 SUMMARY-2.2 + CLAUDE.md async-handler lesson:
`SendMessageCommandHandlerAsync` lives in a separate file and its branch coverage cannot
be inferred from the sync handler's coverage. Codecov-driven coverage requires the async
path to be exercised explicitly by its own integration test.

**Required test names (exact):**

- `SendAsync_Commit_BothRowsVisible`
- `SendAsync_Rollback_NeitherRowVisible`
- `SendBatchAsync_Commit_AllRowsVisible`
- `SendBatchAsync_Rollback_NeitherRowVisible`

**Shape — `SqlServerOutboxSendAsyncTests.cs`:**

```csharp
// LGPL-2.1 license header

using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Outbox
{
    [TestClass]
    public class SqlServerOutboxSendAsyncTests : SqlServerOutboxIntegrationTestBase
    {
        [TestMethod]
        public async Task SendAsync_Commit_BothRowsVisible()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            var businessTable = NewBusinessTableName();

            using var queue = CreateQueue(qc);
            await using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = (SqlTransaction)await conn.BeginTransactionAsync())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, tx);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, tx, businessTable, 1, "first");
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
            await using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = (SqlTransaction)await conn.BeginTransactionAsync())
                {
                    var msg = GenerateMessage.Create<FakeMessage>();
                    var result = await producer.RelationalProducer.SendAsync(msg, tx);
                    Assert.IsFalse(result.HasError, result.SendingException?.ToString());
                    InsertBusinessRow(conn, tx, businessTable, 1, "first");
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
            await using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = (SqlTransaction)await conn.BeginTransactionAsync())
                {
                    var batch = BuildBatch(batchSize);
                    var result = await producer.RelationalProducer.SendAsync(batch, tx);
                    Assert.IsFalse(result.HasErrors);
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, tx, businessTable, i, $"row{i}");
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
            await using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            await conn.OpenAsync();
            try
            {
                CreateBusinessTable(conn, businessTable);
                using var producer = CreateRelationalProducer(qc);

                await using (var tx = (SqlTransaction)await conn.BeginTransactionAsync())
                {
                    var batch = BuildBatch(batchSize);
                    var result = await producer.RelationalProducer.SendAsync(batch, tx);
                    Assert.IsFalse(result.HasErrors);
                    for (var i = 0; i < batchSize; i++)
                        InsertBusinessRow(conn, tx, businessTable, i, $"row{i}");
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

- `SqlConnection.BeginTransactionAsync()` returns `ValueTask<DbTransaction>`; cast to
  `SqlTransaction` since `InsertBusinessRow` accepts the concrete type.
- `await using` for `SqlConnection` + `SqlTransaction` exists on Microsoft.Data.SqlClient
  6.x (already referenced in the project).
- `await tx.CommitAsync()` / `RollbackAsync()` are the right API; `tx.Dispose()` happens
  in the `await using` block.

**Acceptance Criteria:**

- File exists with LGPL-2.1 header.
- `dotnet test ... --filter "FullyQualifiedName~SqlServerOutboxSendAsyncTests"` — 4 tests pass.
- Full integration suite still green: `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" -c Debug`.

---

## Verification

```bash
# Build the project
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" -c Debug

# Run only the 8 PLAN-1.1 tests
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" \
  -c Debug \
  --filter "FullyQualifiedName~SqlServerOutboxSendTests|FullyQualifiedName~SqlServerOutboxSendAsyncTests"

# Regression: full SqlServer integration suite must still pass
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" -c Debug
```

Note: integration tests require `connectionstring.txt` in each project's bin output dir.
Coverlet collection runs by default via Directory.Build.props — no extra flag needed.
`-c Debug` per CLAUDE.md (`-p:CI=true` is NuGet-pack-only and has no effect on tests).

## PROJECT.md Success Criteria coverage

| Test | §SC |
|---|---|
| `Send_Commit_BothRowsVisible` | #3 (capability cast at runtime), #4 (atomic commit) |
| `Send_Rollback_NeitherRowVisible` | #5 (atomic rollback) |
| `SendBatch_Commit_AllRowsVisible` | #4 (batch path) |
| `SendBatch_Rollback_NeitherRowVisible` | #5 (batch path) |
| `SendAsync_Commit_BothRowsVisible` | #4 (async path) |
| `SendAsync_Rollback_NeitherRowVisible` | #5 (async path) |
| `SendBatchAsync_Commit_AllRowsVisible` | #4 (async batch) |
| `SendBatchAsync_Rollback_NeitherRowVisible` | #5 (async batch) |
