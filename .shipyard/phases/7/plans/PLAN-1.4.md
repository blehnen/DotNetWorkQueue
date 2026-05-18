# Plan 1.4: SQLite Outbox Integration Tests (12 tests)

## Context

12 SQLite-outbox integration tests mirroring the outbox milestone Phase 6 per-transport coverage. Validates the SQLite outbox wiring + symmetric normalization + retry-bypass semantics shipped in Phase 5.

PROJECT.md §SC #6 + #8 satisfied for SQLite (12 outbox integration tests + zero-mutation-on-caller-resources verification).

## Dependencies
Phase 5 (SQLite outbox sweep shipped). Parallel-safe with PLAN-1.1, PLAN-1.2, PLAN-1.3.

## Tasks

### Task 1: SQLite outbox test base + method-matrix tests
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Outbox/SqLiteOutboxIntegrationTestBase.cs` (create)
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Outbox/SqLiteOutboxSendTests.cs` (create)
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Outbox/SqLiteOutboxSendAsyncTests.cs` (create)

**Action:** create

**Description:**

Mirror `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/SqlServerOutboxIntegrationTestBase.cs` template with SQLite specifics:
- Base class: queue lifecycle helpers, business-table helpers, `CreateRelationalProducer(qc)` returning `IRelationalProducerQueue<FakeMessage>` resolved from `QueueContainer<SqLiteMessageQueueInit>`.
- `SqLiteOutboxSendTests.cs`: 2 tests (`Send_Commit_BothRowsVisible`, `Send_Rollback_NeitherRowVisible`).
- `SqLiteOutboxSendAsyncTests.cs`: 2 tests (`SendAsync_Commit_BothRowsVisible`, `SendAsync_Rollback_NeitherRowVisible`).
- All tests assert PROJECT.md §SC #8: zero `Commit`/`Rollback`/`Dispose`/`Close` calls on the caller's `IDbConnection` or `IDbTransaction` AFTER the producer's Send returns. Use NSubstitute spy or a wrapper class that counts these calls.
- `[ClassInitialize]` ActivityListener registration.

**Acceptance Criteria:**
- 4 tests pass against live SQLite.
- §SC #8 zero-call assertions present in all 4 tests.

### Task 2: Batch + AdditionalData tests
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Outbox/SqLiteOutboxBatchTests.cs` (create)
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Outbox/SqLiteOutboxAdditionalDataTests.cs` (create)

**Action:** create

**Description:**

`SqLiteOutboxBatchTests.cs`: 4 tests covering batch sync + async × commit + rollback:
1. `SendBatch_Commit_AllRowsVisible`
2. `SendBatch_Rollback_NoRowsVisible`
3. `SendBatchAsync_Commit_AllRowsVisible`
4. `SendBatchAsync_Rollback_NoRowsVisible`

`SqLiteOutboxAdditionalDataTests.cs`: 1 test:
- `AdditionalMessageData_RoundTrips_Through_CallerTx_Path` — produce a message with custom headers + correlation ID via the caller-tx path; consume; assert headers + correlation ID match after round-trip.

**Acceptance Criteria:**
- 5 tests pass against live SQLite.

### Task 3: Validation + retry-bypass tests
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Outbox/SqLiteOutboxValidationTests.cs` (create)
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Outbox/SqLiteOutboxRetryBypassTests.cs` (create)

**Action:** create

**Description:**

`SqLiteOutboxValidationTests.cs`: 2 tests:
1. `CrossDatabase_FilePath_Mismatch_ValidatorRejects` — caller tx on `business.db`; queue on `queue.db`; producer.Send(...) throws validator's mismatch exception. Confirms spike §3 path-canonicalization semantics (different absolute paths → reject under Ordinal compare).
2. `ClosedConnection_ValidatorRejects` — caller passes a tx whose `Connection.State != Open`; producer rejects with discoverable error.

`SqLiteOutboxRetryBypassTests.cs`: 1 test:
- `Transient_Error_Propagates_On_First_Attempt_Retry_Decorator_Not_Invoked` — simulate a transient error during the caller-tx send path; assert the exception propagates immediately AND the retry decorator was NOT invoked (poll the `IMetrics` retry counter, CLAUDE.md polling lesson).

**Acceptance Criteria:**
- 3 tests pass against live SQLite.
- Retry-bypass test uses polling (not snapshot) on IMetrics.

## Verification

```bash
dotnet build "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" -c Release --nologo 2>&1 | tail -5
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" --filter "FullyQualifiedName~Outbox" --nologo 2>&1 | tail -3
# expect: 12/12 pass.
grep -rnE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Outbox/
# expect exit 1.
```
