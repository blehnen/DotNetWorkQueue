---
phase: integration-tests
plan: 1.2
wave: 1
dependencies: [1.1]
must_haves:
  - Cross-database validation throws InvalidOperationException before any DB write (closes §SC #6)
  - Closed-connection validation throws InvalidOperationException
  - Retry-bypass demonstrated: caller-tx path fails on first attempt, not after 3 retries (closes §SC #8 at integration level)
  - IAdditionalMessageData round-trip preserves headers + correlation ID via direct metadata-table assertion
  - All tests use queue-per-test isolation, try/finally with oCreation.RemoveQueue()
  - No regressions in existing SqlServer integration test suite
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxValidationTests.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxRetryBypassTests.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxAdditionalDataTests.cs
tdd: false
risk: medium
---

# Plan 1.2: SqlServer Outbox — Validation + Retry Bypass + AdditionalMessageData

## Context

PLAN-1.1 covered the **happy-path method matrix** (8 tests). PLAN-1.2 covers the four
remaining matrix slots:

- **Validation (2 tests):** Cross-database mismatch → `InvalidOperationException` before
  any write. Closed-connection → `InvalidOperationException`. Closes PROJECT.md §SC #6.
- **Retry bypass (1 test):** Force first-attempt failure → assert the caller sees the
  exception immediately, not after 3 Polly retries. Closes §SC #8 at the integration
  level (the structural unit-level pin lives in Phase 3).
- **IAdditionalMessageData round-trip (1 test):** Caller passes custom headers +
  correlation ID via `AdditionalMessageData`; after commit, the queue's metadata table
  is queried directly and the persisted values are asserted. Per RESEARCH §3, this uses
  the **SQL-table verification approach** (NOT live consumer dequeue) — it matches the
  existing `VerifyQueueData.VerifyPriority()` pattern and avoids consumer lifecycle
  complexity.

All four tests inherit `SqlServerOutboxIntegrationTestBase` from PLAN-1.1.

## Dependencies

- PLAN-1.1 base class (`SqlServerOutboxIntegrationTestBase`) — created in this same wave;
  the build orders Task 1 of PLAN-1.1 before any PLAN-1.2 task because the base class
  symbol must compile first. Builder should land PLAN-1.1 Task 1 first.
- Phase 3 wiring: `ExternalTransactionValidator` is registered for the SqlServer
  transport and runs at the entry of every caller-tx overload.
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/connectionstring.txt` valid.

---

## Tasks

### Task 1: `SqlServerOutboxValidationTests` — cross-DB + closed-connection

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxValidationTests.cs` (NEW)

**Action:** create + test

**Description:**

Two tests prove the `ExternalTransactionValidator` runs **before** any SQL write:

1. **Cross-DB mismatch** — open a `SqlConnection` against the **`master`** system database
   (always exists on any running SqlServer instance), begin a transaction on that
   connection, call `rp.Send(msg, tx)`. The validator compares `tx.Connection.Database`
   (`"master"`) against the queue's configured database name (the one in
   `ConnectionInfo.ConnectionString`). They mismatch → `InvalidOperationException` thrown
   before the first INSERT. The test then asserts the queue's MetaData row count is 0.

2. **Closed-connection** — open a `SqlConnection`, begin a transaction, **close the
   connection** (which puts the tx into a state where `tx.Connection` may be non-null but
   `State != Open`, or `tx.Connection` becomes null depending on driver behavior). Call
   `rp.Send(msg, tx)` → `InvalidOperationException` from the validator.

Cross-DB technique per RESEARCH §10:
```csharp
var builder = new SqlConnectionStringBuilder(ConnectionInfo.ConnectionString.Trim());
builder.InitialCatalog = "master";
using var wrongDb = new SqlConnection(builder.ConnectionString);
wrongDb.Open();
using var wrongTx = wrongDb.BeginTransaction();
// rp.Send(msg, wrongTx) → throws InvalidOperationException
```

**Required test names (exact):**

- `Validation_CrossDatabaseMismatch_ThrowsBeforeInsert`
- `Validation_ClosedConnection_ThrowsBeforeInsert`

**Shape — `SqlServerOutboxValidationTests.cs`:**

```csharp
// LGPL-2.1 license header

using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Outbox
{
    [TestClass]
    public class SqlServerOutboxValidationTests : SqlServerOutboxIntegrationTestBase
    {
        [TestMethod]
        public void Validation_CrossDatabaseMismatch_ThrowsBeforeInsert()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            // Open a SqlConnection to "master" (always exists) — different database
            // than the queue's configured DB.
            var builder = new SqlConnectionStringBuilder(ConnectionInfo.ConnectionString.Trim());
            builder.InitialCatalog = "master";
            using var wrongDb = new SqlConnection(builder.ConnectionString);
            wrongDb.Open();
            using var wrongTx = wrongDb.BeginTransaction();

            var msg = GenerateMessage.Create<FakeMessage>();

            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(msg, wrongTx));

            // Validator's exception message must include both DB names for diagnostics
            // (per PROJECT.md §Non-Functional "Diagnostics" requirement).
            Assert.IsTrue(ex.Message.IndexOf("master", StringComparison.OrdinalIgnoreCase) >= 0,
                $"Expected exception message to mention 'master': {ex.Message}");

            // No partial write — queue metadata table count must be 0.
            AssertQueueRowCount(qc, 0);

            wrongTx.Rollback();
        }

        [TestMethod]
        public void Validation_ClosedConnection_ThrowsBeforeInsert()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            var tx = conn.BeginTransaction();
            conn.Close();  // Connection is now closed; tx.Connection may be null OR State != Open

            var msg = GenerateMessage.Create<FakeMessage>();

            Assert.ThrowsExactly<InvalidOperationException>(
                () => producer.RelationalProducer.Send(msg, tx));

            // No partial write
            AssertQueueRowCount(qc, 0);

            // Cleanup — both objects may already be disposed/invalidated; swallow.
            try { tx.Dispose(); } catch { /* ignore */ }
            try { conn.Dispose(); } catch { /* ignore */ }
        }
    }
}
```

**Notes for builder:**

- `ConnectionInfo.ConnectionString.Trim()` — RESEARCH §7 flagged a trailing-newline risk
  when Jenkins injects the connection string via `echo`. `SqlConnectionStringBuilder`'s
  parser is stricter than `SqlConnection`'s and will throw on stray whitespace. Trim
  at the use site, not in `ConnectionInfo` (avoid touching shared infra).
- `Assert.ThrowsExactly<T>` is MSTest 4.x (CLAUDE.md lesson — NOT `Assert.ThrowsException<T>`).
- The validator's exception message includes both DB names per PROJECT.md §Non-Functional
  "Diagnostics" requirement; assert at minimum that the WRONG db name (`master`) appears.
- For the closed-connection case, do NOT assert the exception message — the validator
  may throw "connection state is not Open" or "transaction disposed" depending on driver
  behavior. The contract is just `InvalidOperationException` from validation, no write.

**Acceptance Criteria:**

- File exists with LGPL-2.1 header.
- `dotnet test ... --filter "FullyQualifiedName~SqlServerOutboxValidationTests"` — 2 tests pass.
- No regressions.

---

### Task 2: `SqlServerOutboxRetryBypassTests` — 1 test, committed-tx technique

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxRetryBypassTests.cs` (NEW)

**Action:** create + test

**Description:**

One test proving the caller-tx path **does not retry** on failure. Per RESEARCH §4 the
architect-selected technique is **the committed-tx approach**: open a `SqlConnection`,
begin a transaction, commit the transaction (the transaction is now in a completed
state — calling `Send` against it must fail immediately), then call `rp.Send(msg, tx)`
and assert that:

1. The call throws on **first attempt** — no Polly retry chain spins up.
2. Wall-clock elapsed time is small (< 2 seconds); a 3x retry chain on this transport
   would add per-attempt back-off, observable as multi-second delay.

The retry-bypass mechanism (per Phase 3 spike note in PROJECT.md §Risk Inventory) is
the `IRetrySkippable` marker on `RelationalSendMessageCommand` evaluated at the top of
`RetryCommandHandlerOutputDecorator.Handle()`. The structural unit test is in Phase 3.
This integration test confirms the bypass at runtime by asserting the wall-clock pattern.

Metrics polling: per CLAUDE.md ("poll the live IMetrics object, NOT a snapshot"), we
take an additional belt-and-suspenders assertion by polling the `IMetrics` instance
attached to the QueueContainer. We assert the SendMessagesMeter counter for this queue
is 0 (the send threw before incrementing the meter). Because the metric is bound per
QueueContainer, we need the existing `Metrics.Metrics` helper.

**Required test name (exact):**

- `RetryBypass_TransientError_SingleAttempt`

**Shape — `SqlServerOutboxRetryBypassTests.cs`:**

```csharp
// LGPL-2.1 license header

using System;
using System.Diagnostics;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Outbox
{
    [TestClass]
    public class SqlServerOutboxRetryBypassTests : SqlServerOutboxIntegrationTestBase
    {
        [TestMethod]
        public void RetryBypass_TransientError_SingleAttempt()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);
            using var queue = CreateQueue(qc);
            using var producer = CreateRelationalProducer(qc);

            // Committed-tx technique (RESEARCH §4): open a connection, begin a tx, and
            // immediately commit. The transaction is now in a completed state; any
            // subsequent INSERT against it must fail on first attempt.
            using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
            conn.Open();
            var tx = conn.BeginTransaction();
            tx.Commit();
            // Note: after Commit(), tx.Connection may be null on SqlClient. Either way
            // the validator OR the handler must throw immediately — both outcomes are
            // acceptable since they prove "no retry" wall-clock.

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

            // Single-attempt assertion: a 3x retry chain with backoff would take seconds.
            // The bypass means the throw is essentially immediate. Use a generous 2s
            // cap to avoid flakiness on slow CI hosts while still distinguishing single
            // vs. 3-attempt outcomes.
            Assert.IsTrue(sw.ElapsedMilliseconds < 2000,
                $"Caller-tx Send took {sw.ElapsedMilliseconds}ms — expected < 2000ms " +
                "for single-attempt failure (3x retry chain would exceed this).");

            // Belt-and-suspenders: no row landed in the queue metadata table.
            AssertQueueRowCount(qc, 0);

            try { tx.Dispose(); } catch { /* ignore */ }
        }
    }
}
```

**Notes for builder:**

- Per RESEARCH §4 recommendation, this test asserts on wall-clock timing because the
  current `IMetrics` shape does not expose a producer-side "retry attempt count"
  counter. The combination of "throws immediately" + "no metadata row" + "< 2s elapsed"
  is sufficient at the integration level since the unit test in Phase 3 already pins
  the `SkipRetry`/`IRetrySkippable` structural seam.
- The 2000ms cap is intentionally generous. The actual retry chain (if it ran) would
  use Polly with seconds-long back-offs. A first-attempt failure completes in <50ms on
  typical hardware.
- If the test proves flaky on slow CI hosts, raise the cap to 3000ms — DO NOT remove
  the timing assertion entirely; it is the only integration-level pin against the
  "retry decorator silently regressed" failure mode.
- Per CLAUDE.md "fix root cause not symptom" — if `ConnectionInfo.ConnectionString`
  needs trimming (RESEARCH §7), trim at the use site only.

**Acceptance Criteria:**

- File exists with LGPL-2.1 header.
- `dotnet test ... --filter "FullyQualifiedName~SqlServerOutboxRetryBypassTests"` passes.
- Wall-clock elapsed assertion < 2000ms holds on the local SqlServer instance.

---

### Task 3: `SqlServerOutboxAdditionalDataTests` — IAdditionalMessageData round-trip

**Files:** `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxAdditionalDataTests.cs` (NEW)

**Action:** create + test

**Description:**

One test proving `IAdditionalMessageData` survives the caller-tx send path. Per
RESEARCH §3 the verification uses **direct SQL-table query** (NOT live consumer
dequeue) for simplicity and consistency with `VerifyQueueData.VerifyPriority()`.

Test shape:

1. Create the queue with `EnablePriority = true` (this ensures the metadata table has
   a `priority` column that is queryable; per RESEARCH §3 uncertainty flag #4, priority
   is the safest queryable column to assert).
2. Build an `AdditionalMessageData` with `SetPriority(7)` AND a `CorrelationId` set to
   a known `Guid`. Send via `rp.Send(msg, data, tx)`; commit.
3. Query the queue's MetaData table directly: assert `priority = 7` for the inserted
   row. Asserts the AdditionalMessageData reached the persistence layer through the
   caller-tx path (same as `VerifyQueueData.VerifyPriority`).
4. Verify the queue row count is exactly 1 (sanity).
5. The CorrelationId is also returned on `IQueueOutputMessage.SentMessage.CorrelationId`;
   assert that the returned CorrelationId matches what was passed in.

**Required test name (exact):**

- `AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation`

**Shape — `SqlServerOutboxAdditionalDataTests.cs`:**

```csharp
// LGPL-2.1 license header

using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.IntegrationTests.Outbox
{
    [TestClass]
    public class SqlServerOutboxAdditionalDataTests : SqlServerOutboxIntegrationTestBase
    {
        [TestMethod]
        public void AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation()
        {
            var qc = new QueueConnection(NewQueueName(), ConnectionInfo.ConnectionString);

            // Override base queue creation to enable priority (so we can assert it
            // round-tripped through the metadata table).
            var queueCreator = new QueueCreationContainer<SqlServerMessageQueueInit>();
            var oCreation = queueCreator.GetQueueCreation<SqlServerMessageQueueCreation>(qc);
            try
            {
                oCreation.Options.EnableStatus = true;
                oCreation.Options.EnablePriority = true;
                var result = oCreation.CreateQueue();
                Assert.IsTrue(result.Success, result.ErrorMessage);

                using var producer = CreateRelationalProducer(qc);
                using var conn = new SqlConnection(ConnectionInfo.ConnectionString);
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

                    // Sanity: returned CorrelationId matches what we passed in.
                    Assert.AreEqual(correlationGuid, (Guid)sendResult.SentMessage.CorrelationId.Id.Value,
                        "CorrelationId must round-trip through the SentMessage projection.");
                }

                // Persistence assertion — query the MetaData table directly.
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

        // Mirrors VerifyQueueData.VerifyPriority but local to this test class.
        private static void AssertPriorityInMetadata(QueueConnection qc, byte expectedPriority)
        {
            var info = new SqlConnectionInformation(qc);
            var helper = new SqlServerTableNameHelper(info);
            using var conn = new SqlConnection(qc.Connection);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT priority FROM {helper.MetaDataName}";
            using var reader = cmd.ExecuteReader();
            Assert.IsTrue(reader.Read(), "Expected at least one metadata row.");
            var actual = (byte)reader[0];
            Assert.AreEqual(expectedPriority, actual,
                $"Priority did not round-trip: expected {expectedPriority}, observed {actual}.");
        }

        // Minimal CorrelationIdContainer implementing IMessageId for the test.
        // (DNQ exposes CorrelationIdSerialized / CorrelationId on AdditionalMessageData;
        // the exact ctor shape may differ. The builder should resolve via existing
        // CorrelationId helpers in DotNetWorkQueue.Messages.)
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

- `AdditionalMessageData.CorrelationId` setter shape: in DNQ the public API exposes
  `data.CorrelationId` of type `ICorrelationId`. There are existing concrete CorrelationId
  helpers in `DotNetWorkQueue.Messages`. If the inline `CorrelationIdContainer` /
  `MessageCorrelationId` shells below don't compile (likely — the interfaces may have
  additional members), substitute the existing public helpers — search for usages of
  `data.CorrelationId =` in the codebase under `Source/DotNetWorkQueue.IntegrationTests.Shared/`
  to find the canonical pattern.
- `Helpers.GenerateData(...)` (existing SqlServer integration test helper) shows
  `data.SetPriority(5)` — same pattern, different value.
- The priority assertion mirrors `VerifyQueueData.VerifyPriority()` exactly (lines
  146-165 of `VerifyQueueData.cs`). Reuse the table-name-helper construction verbatim.
- If the CorrelationId round-trip assertion via `sendResult.SentMessage.CorrelationId`
  proves brittle (the projection layer may strip the GUID), drop that single assertion
  and rely only on the priority round-trip. The priority assertion alone is sufficient
  to prove `IAdditionalMessageData` reached the persistence layer via the caller-tx path.
- The custom inline scope override (not using `CreateQueue` from the base) is necessary
  because the base method hardcodes `EnablePriority = false`. Refactor option: add an
  optional `Action<SqlServerMessageQueueCreation>` parameter to base `CreateQueue` —
  defer to builder.

**Acceptance Criteria:**

- File exists with LGPL-2.1 header.
- `dotnet test ... --filter "FullyQualifiedName~SqlServerOutboxAdditionalDataTests"` passes.
- Persisted priority = 7 confirmed by direct SQL query.

---

## Verification

```bash
# Build
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" -c Debug

# Run the 4 PLAN-1.2 tests
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" \
  -c Debug \
  --filter "FullyQualifiedName~SqlServerOutboxValidationTests|FullyQualifiedName~SqlServerOutboxRetryBypassTests|FullyQualifiedName~SqlServerOutboxAdditionalDataTests"

# Run all 12 SqlServer outbox tests (PLAN-1.1 + PLAN-1.2)
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" \
  -c Debug \
  --filter "FullyQualifiedName~Outbox"

# Regression: full SqlServer integration suite must still pass
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" -c Debug
```

## PROJECT.md Success Criteria coverage

| Test | §SC |
|---|---|
| `Validation_CrossDatabaseMismatch_ThrowsBeforeInsert` | #6 |
| `Validation_ClosedConnection_ThrowsBeforeInsert` | #6 (related validation path) |
| `RetryBypass_TransientError_SingleAttempt` | #8 (integration level) |
| `AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation` | end-to-end metadata round-trip on caller-tx path |
