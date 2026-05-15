---
phase: integration-tests
plan: 2.2
wave: 2
dependencies: [1.1, 1.2, 2.1]
must_haves:
  - Cross-database validation (PG) throws InvalidOperationException before any DB write (closes §SC #6)
  - Closed-connection validation (PG) throws InvalidOperationException
  - Retry-bypass demonstrated on PG: caller-tx path fails on first attempt
  - IAdditionalMessageData round-trip on PG preserves priority via direct metadata-table assertion
  - All tests use queue-per-test isolation, try/finally with oCreation.RemoveQueue()
  - No regressions in existing PostgreSQL integration test suite
files_touched:
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxValidationTests.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxRetryBypassTests.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxAdditionalDataTests.cs
tdd: false
risk: medium
---

# Plan 2.2: PostgreSQL Outbox — Validation + Retry Bypass + AdditionalMessageData

## Context

Mirrors PLAN-1.2 (SqlServer) for PostgreSQL. Four tests covering the non-method-matrix
slots:

- **Validation (2):** cross-DB (uses `postgres` system DB per RESEARCH §10) + closed-connection.
- **Retry bypass (1):** committed-tx technique (same as PLAN-1.2 Task 2).
- **IAdditionalMessageData round-trip (1):** direct metadata-table priority assertion.

All four tests inherit `PostgreSqlOutboxIntegrationTestBase` from PLAN-2.1.

**Path NOTE:** PG project path uses DOT before "Integration":
`Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/`.

## Dependencies

- PLAN-2.1 base class (`PostgreSqlOutboxIntegrationTestBase`) — same wave; Task 1 lands first.
- PLAN-1.1 + PLAN-1.2 merged with Jenkins SqlServer stage green (CI gating per Decision 4).
- Phase 4 wiring complete.
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/connectionstring.txt` valid.

---

## Tasks

### Task 1: `PostgreSqlOutboxValidationTests` — cross-DB + closed-connection

**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxValidationTests.cs` (NEW)

**Action:** create + test

**Description:**

Mirror PLAN-1.2 Task 1. PG-specific details:

- Cross-DB candidate is **`postgres`** (always exists on every PG instance — see RESEARCH §10).
- Connection-string builder is `NpgsqlConnectionStringBuilder`; property is `.Database`
  (not `.InitialCatalog`).
- `PostgreSqlExternalDbNameExtractor` compares via `StringComparer.Ordinal` (case-sensitive
  per RESEARCH §10 comparison matrix; this differs from SqlServer's `OrdinalIgnoreCase`).
  The cross-DB test still works because `postgres` and the queue's DB name differ at the
  byte level regardless of case.

**Required test names (exact):**

- `Validation_CrossDatabaseMismatch_ThrowsBeforeInsert`
- `Validation_ClosedConnection_ThrowsBeforeInsert`

**Shape — `PostgreSqlOutboxValidationTests.cs`:**

```csharp
// LGPL-2.1 license header

using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Outbox
{
    [TestClass]
    public class PostgreSqlOutboxValidationTests : PostgreSqlOutboxIntegrationTestBase
    {
        [TestMethod]
        public void Validation_CrossDatabaseMismatch_ThrowsBeforeInsert()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            // Open NpgsqlConnection to "postgres" (always exists) — different DB
            var builder = new NpgsqlConnectionStringBuilder(ConnectionInfo.ConnectionString.Trim());
            builder.Database = "postgres";
            using var wrongDb = new NpgsqlConnection(builder.ConnectionString);
            wrongDb.Open();
            using var wrongTx = wrongDb.BeginTransaction();

            var msg = GenerateMessage.Create<FakeMessage>();

            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(msg, wrongTx));

            // Validator's exception message must include the wrong DB name (PROJECT.md §Diagnostics).
            Assert.IsTrue(ex.Message.IndexOf("postgres", StringComparison.Ordinal) >= 0,
                $"Expected exception message to mention 'postgres': {ex.Message}");

            // No partial write
            AssertQueueRowCount(qc, 0);

            wrongTx.Rollback();
        }

        [TestMethod]
        public void Validation_ClosedConnection_ThrowsBeforeInsert()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            var tx = conn.BeginTransaction();
            conn.Close();  // tx.Connection may be null or State != Open

            var msg = GenerateMessage.Create<FakeMessage>();

            Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(msg, tx));

            AssertQueueRowCount(qc, 0);

            try { tx.Dispose(); } catch { /* ignore */ }
            try { conn.Dispose(); } catch { /* ignore */ }
        }
    }
}
```

**Notes for builder:**

- PG: `NpgsqlConnectionStringBuilder.Database` (not `.InitialCatalog`).
- PG DB-name compare is case-sensitive (`StringComparer.Ordinal`); `postgres` vs. the
  configured queue DB is a guaranteed mismatch on any sane setup.
- Trim the connection string when feeding `NpgsqlConnectionStringBuilder` (RESEARCH §7
  trailing-newline risk).

**Acceptance Criteria:**

- File exists with LGPL-2.1 header.
- `dotnet test ... --filter "FullyQualifiedName~PostgreSqlOutboxValidationTests"` — 2 tests pass.
- No regressions.

---

### Task 2: `PostgreSqlOutboxRetryBypassTests` — 1 test, committed-tx technique

**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxRetryBypassTests.cs` (NEW)

**Action:** create + test

**Description:**

Mirror PLAN-1.2 Task 2 with `NpgsqlConnection` / `NpgsqlTransaction`. The committed-tx
technique is transport-agnostic: open, begin tx, commit, then call `rp.Send(msg, tx)` —
the transaction is in a completed state and the send must fail on first attempt.

PG-specific note: after `NpgsqlTransaction.Commit()`, the transaction is marked completed.
The Npgsql driver behavior for `tx.Connection` after commit may differ from SqlClient's
(some Npgsql versions return the connection, others null). Either way, the validator
or handler throws immediately — both outcomes satisfy "no retry."

**Required test name (exact):**

- `RetryBypass_TransientError_SingleAttempt`

**Shape — `PostgreSqlOutboxRetryBypassTests.cs`:**

```csharp
// LGPL-2.1 license header

using System;
using System.Diagnostics;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Outbox
{
    [TestClass]
    public class PostgreSqlOutboxRetryBypassTests : PostgreSqlOutboxIntegrationTestBase
    {
        [TestMethod]
        public void RetryBypass_TransientError_SingleAttempt()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            var tx = conn.BeginTransaction();
            tx.Commit();  // transaction now completed

            var msg = GenerateMessage.Create<FakeMessage>();

            var sw = Stopwatch.StartNew();
            Exception caught = null;
            try
            {
                producer.RelationalProducer.Send(msg, tx);
                Assert.Fail("Expected an exception from the caller-tx Send on a completed tx.");
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            sw.Stop();

            Assert.IsNotNull(caught, "Caller-tx Send must throw on completed tx.");

            // Single-attempt wall-clock assertion (see PLAN-1.2 Task 2 rationale).
            Assert.IsTrue(sw.ElapsedMilliseconds < 2000,
                $"Caller-tx Send took {sw.ElapsedMilliseconds}ms — expected < 2000ms " +
                "for single-attempt failure (3x retry chain would exceed this).");

            AssertQueueRowCount(qc, 0);

            try { tx.Dispose(); } catch { /* ignore */ }
        }
    }
}
```

**Notes for builder:**

- Same 2000ms cap rationale as PLAN-1.2: generous enough for slow CI hosts, tight enough
  to distinguish single-attempt from 3-retry-with-backoff.
- If flaky in Jenkins, raise to 3000ms; do NOT delete the timing assertion (it is the only
  integration-level pin against retry-decorator regression).

**Acceptance Criteria:**

- File exists with LGPL-2.1 header.
- `dotnet test ... --filter "FullyQualifiedName~PostgreSqlOutboxRetryBypassTests"` passes.
- Wall-clock elapsed < 2000ms.

---

### Task 3: `PostgreSqlOutboxAdditionalDataTests` — IAdditionalMessageData round-trip

**Files:** `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxAdditionalDataTests.cs` (NEW)

**Action:** create + test

**Description:**

Mirror PLAN-1.2 Task 3. PG-specific details:

- Enable `EnablePriority = true` on the `PostgreSqlMessageQueueCreation` options.
- The PG `TableNameHelper` and `SqlConnectionInformation` (PG-namespaced) provide the
  metadata table name.
- Direct SQL query against the PG metadata table reads `priority` (same column name on
  both transports per the shared `RelationalDatabase` schema).
- PostgreSQL stores priority as `INT` (or `SMALLINT`); cast accordingly when reading.
  Existing `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/VerifyQueueData.cs`
  shows the canonical read.

**Required test name (exact):**

- `AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation`

**Shape — `PostgreSqlOutboxAdditionalDataTests.cs`:**

```csharp
// LGPL-2.1 license header

using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.Outbox
{
    [TestClass]
    public class PostgreSqlOutboxAdditionalDataTests : PostgreSqlOutboxIntegrationTestBase
    {
        [TestMethod]
        public void AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);

            // Override base queue creation to enable priority
            var queueCreator = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            var oCreation = queueCreator.GetQueueCreation<PostgreSqlMessageQueueCreation>(qc);
            try
            {
                oCreation.Options.EnableStatus = true;
                oCreation.Options.EnablePriority = true;
                var result = oCreation.CreateQueue();
                Assert.IsTrue(result.Success, result.ErrorMessage);

                using var producer = CreateRelationalProducer(qc);
                using var conn = new NpgsqlConnection(ConnectionInfo.ConnectionString);
                conn.Open();

                var data = new AdditionalMessageData();
                data.SetPriority(7);
                var correlationGuid = Guid.NewGuid();
                data.CorrelationId = new CorrelationIdContainer(correlationGuid);
                var msg = GenerateMessage.Create<FakeMessage>();

                using (var tx = conn.BeginTransaction())
                {
                    var sendResult = producer.RelationalProducer.Send(msg, data, tx);
                    Assert.IsFalse(sendResult.HasError, sendResult.SendingException?.ToString());
                    tx.Commit();

                    Assert.AreEqual(correlationGuid, (Guid)sendResult.SentMessage.CorrelationId.Id.Value,
                        "CorrelationId must round-trip through the SentMessage projection.");
                }

                AssertQueueRowCount(qc, 1);
                AssertPriorityInMetadata(qc, expectedPriority: 7);
            }
            finally
            {
                try { oCreation?.RemoveQueue(); } catch { /* ignore */ }
                oCreation?.Dispose();
                queueCreator.Dispose();
            }
        }

        private static void AssertPriorityInMetadata(QueueConnection qc, int expectedPriority)
        {
            var info = new SqlConnectionInformation(qc);
            var helper = new TableNameHelper(info);
            using var conn = new NpgsqlConnection(qc.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT priority FROM {helper.MetaDataName}";
            using var reader = cmd.ExecuteReader();
            Assert.IsTrue(reader.Read(), "Expected at least one metadata row.");
            // PG priority column type: confirm from existing VerifyQueueData.cs in this project
            // and cast accordingly. Likely int or short.
            var actual = Convert.ToInt32(reader[0]);
            Assert.AreEqual(expectedPriority, actual,
                $"Priority did not round-trip: expected {expectedPriority}, observed {actual}.");
        }

        // See PLAN-1.2 Task 3 note: replace these inline shells with the existing
        // CorrelationId helpers from DotNetWorkQueue.Messages if the interfaces have
        // additional members.
        private sealed class CorrelationIdContainer : ICorrelationId
        {
            public CorrelationIdContainer(Guid id) { Id = new MessageCorrelationId(id); }
            public IMessageId Id { get; }
            public bool HasValue => Id?.HasValue ?? false;
        }

        private sealed class MessageCorrelationId : IMessageId
        {
            public MessageCorrelationId(Guid value) { Value = value; HasValue = true; }
            public object Value { get; }
            public bool HasValue { get; }
        }
    }
}
```

**Notes for builder:**

- Cross-check the PG `priority` column type by reading
  `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/VerifyQueueData.cs`
  (look for `VerifyPriority`). Cast to the matching type. `Convert.ToInt32(reader[0])`
  is a safe universal fallback.
- If the inline `CorrelationIdContainer` / `MessageCorrelationId` shells don't compile
  (likely — the interfaces probably have additional members the test doesn't satisfy),
  substitute the existing public correlation-id helpers used by other integration tests.
  Search for `data.CorrelationId =` in
  `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/` and
  `Source/DotNetWorkQueue.IntegrationTests.Shared/`.
- If the CorrelationId assertion proves brittle, drop it; rely on the priority round-trip.

**Acceptance Criteria:**

- File exists with LGPL-2.1 header.
- `dotnet test ... --filter "FullyQualifiedName~PostgreSqlOutboxAdditionalDataTests"` passes.
- Priority = 7 confirmed by direct SQL.

---

## Verification

```bash
# Build
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" -c Debug

# Run the 4 PLAN-2.2 tests
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" \
  -c Debug \
  --filter "FullyQualifiedName~PostgreSqlOutboxValidationTests|FullyQualifiedName~PostgreSqlOutboxRetryBypassTests|FullyQualifiedName~PostgreSqlOutboxAdditionalDataTests"

# Run all 12 PostgreSQL outbox tests (PLAN-2.1 + PLAN-2.2)
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" \
  -c Debug \
  --filter "FullyQualifiedName~Outbox"

# Regression: full PostgreSQL integration suite must still pass
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" -c Debug
```

## PROJECT.md Success Criteria coverage

| Test | §SC |
|---|---|
| `Validation_CrossDatabaseMismatch_ThrowsBeforeInsert` | #6 |
| `Validation_ClosedConnection_ThrowsBeforeInsert` | #6 (related validation path) |
| `RetryBypass_TransientError_SingleAttempt` | #8 (integration level on PG) |
| `AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation` | end-to-end metadata round-trip on caller-tx path |

## Phase 6 ship gate

After PLAN-2.2 lands and PostgreSQL integration tests pass locally:

1. Push to feature branch.
2. Open / update draft PR.
3. Wait for Jenkins `PostgreSQL` and `SqlServer` stages green (and the rest of the 14-stage matrix).
4. Confirm Codecov: new lines in `SqlServerRelationalProducerQueue<T>`,
   `PostgreSqlRelationalProducerQueue<T>`, and both `HandleExternalTx`/`HandleExternalTxAsync`
   handler forks each show ≥1 hit per branch.
5. Phase 6 PASS criteria satisfied → ready for Phase 7 (documentation).
