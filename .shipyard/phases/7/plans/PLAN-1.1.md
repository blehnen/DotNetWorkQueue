# Plan 1.1: SqlServer Inbox Integration Tests (8 tests)

## Context

8 inbox integration tests against a live SQL Server instance. Tests confirm:
- Atomic dequeue + business-write semantics (sync + async, commit + rollback).
- Cross-connection visibility after queue commit/rollback.
- Option-false negative path (discoverable error, not NRE).

PROJECT.md §SC #4 + #5 satisfied for SqlServer. Mirrors outbox-milestone Phase 6 test shape.

## Dependencies
Phase 3 (SqlServer inbox wiring shipped).

## Tasks

### Task 1: Inbox integration test base + sync handler tests
**Files:**
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Inbox/SqlServerInboxIntegrationTestBase.cs` (create)
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Inbox/SqlServerInboxSyncHandlerTests.cs` (create)

**Action:** create

**Description:**

Base class follows the outbox-milestone template (`SqlServerOutboxIntegrationTestBase.cs`). Includes:
- `NewQueueName()` helper (`"q" + Guid("N")`).
- `NewBusinessTableName()` helper (`"InboxBusiness_" + Guid("N")`).
- `QueueScope : IDisposable` (queue lifecycle).
- `CreateBusinessTable(IDbConnection conn, string tableName)` — creates a 2-column table (Id, Payload).
- `AssertBothRowsVisible(connStr, queueName, businessTable)` — opens a SEPARATE connection, queries both queue table + business table, asserts both have ≥1 row.
- `AssertNeitherRowVisible(...)` — opposite.
- `RunConsumerWithInboxHandler(QueueConnection qc, bool optionEnabled, Action<IRelationalWorkerNotification, IDbConnection> handler)` — configures the consumer with `EnableHoldTransactionUntilMessageCommitted = optionEnabled`, runs the handler exactly once, returns after processing.
- `[ClassInitialize]` registers `ActivityListener` for `ActivitySource` named "DotNetWorkQueue*"; `[ClassCleanup]` disposes.

Sync handler tests in `SqlServerInboxSyncHandlerTests.cs`:
1. `Sync_Commit_BothRowsVisible` — produce 1 message; consumer with option=true; handler casts notification, writes business row, returns normally; assert from separate connection both visible.
2. `Sync_Rollback_NeitherRowVisible` — same setup; handler casts, writes business row, throws; assert neither visible.

**Acceptance Criteria:**
- Files exist with LGPL header, MSTest 3.x, base class structure as specified.
- `[ClassInitialize]` registers ActivityListener.
- Both tests pass against live SQL Server (`connectionstring.txt` configured).
- No `Tx` token.

### Task 2: Async handler tests + atomic visibility tests
**Files:**
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Inbox/SqlServerInboxAsyncHandlerTests.cs` (create)
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Inbox/SqlServerInboxAtomicVisibilityTests.cs` (create)

**Action:** create

**Description:**

`SqlServerInboxAsyncHandlerTests.cs` mirrors Task 1's sync tests with `IConsumerQueueAsync`:
1. `Async_Commit_BothRowsVisible`
2. `Async_Rollback_NeitherRowVisible`

`SqlServerInboxAtomicVisibilityTests.cs` explicitly tests cross-connection visibility:
1. `BusinessRow_Visible_After_QueueCommit_FromSeparateConnection` — produce 1 message; handler writes business row; AFTER handler returns + queue commits, open a fresh connection; assert business row visible (proves the queue tx committed the business write too).
2. `BusinessRow_NotVisible_After_QueueRollback_FromSeparateConnection` — same but handler throws; assert business row NOT visible from separate connection.

**Acceptance Criteria:**
- 4 tests across 2 files, all pass against live SQL Server.

### Task 3: Option-false negative tests
**Files:**
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Inbox/SqlServerInboxOptionFalseTests.cs` (create)

**Action:** create

**Description:**

2 tests covering the option-false negative path:
1. `Sync_OptionFalse_CapabilityCastFails_DiscoverableError` — consumer with option=false; sync handler attempts `notification is IRelationalWorkerNotification r` → false; handler throws `InvalidOperationException("Inbox capability requires EnableHoldTransactionUntilMessageCommitted = true")`. Test asserts queue records the exception and rolls back; NO `NullReferenceException` anywhere.
2. `Async_OptionFalse_CapabilityCastFails_DiscoverableError` — same with `IConsumerQueueAsync`.

**Acceptance Criteria:**
- Both tests pass against live SQL Server.
- Handler exception type is `InvalidOperationException`, NOT `NullReferenceException`.

## Verification

```bash
# Gate 1: Release build of integration test project.
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" -c Release --nologo 2>&1 | tail -5

# Gate 2: New tests run + pass against live SQL Server (requires connectionstring.txt).
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" --filter "FullyQualifiedName~Inbox" --nologo 2>&1 | tail -3
# expect: 8/8 pass.

# Gate 3: existing SqlServer integration tests still pass (no regression).
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" --nologo 2>&1 | tail -3

# Gate 4: Tx-token guard on new files.
grep -rnE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Inbox/
# expect exit 1.
```
