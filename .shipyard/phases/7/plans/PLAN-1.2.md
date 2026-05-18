# Plan 1.2: PostgreSQL Inbox Integration Tests (8 tests)

## Context

PG mirror of PLAN-1.1 with Npgsql substituted. 8 inbox integration tests against a live PostgreSQL instance.

PROJECT.md §SC #4 + #5 satisfied for PostgreSQL.

## Dependencies
Phase 4 (PG inbox wiring shipped). Parallel-safe with PLAN-1.1, PLAN-1.3, PLAN-1.4 (different transport directories).

## Tasks

### Task 1: PG inbox integration test base + sync handler tests
**Files:**
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Inbox/PostgreSqlInboxIntegrationTestBase.cs` (create)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Inbox/PostgreSqlInboxSyncHandlerTests.cs` (create)

**Action:** create

**Description:**

Mirror PLAN-1.1 Task 1 with PG-specific anchors:
- `NewQueueName()` + `NewBusinessTableName()` identical helpers.
- `QueueScope` wraps `QueueCreationContainer<PostgreSQLMessageQueueInit>` + `PostgreSqlMessageQueueCreation`.
- `CreateBusinessTable(IDbConnection conn, string tableName)` — PG syntax for table creation.
- `AssertBothRowsVisible` / `AssertNeitherRowVisible` from a separate `NpgsqlConnection`.
- `RunConsumerWithInboxHandler` configures `EnableHoldTransactionUntilMessageCommitted` per param.
- `[ClassInitialize]` ActivityListener registration (same shape).

Sync tests:
1. `Sync_Commit_BothRowsVisible`
2. `Sync_Rollback_NeitherRowVisible`

**Acceptance Criteria:**
- 2 tests pass against live PostgreSQL.
- Files use `Npgsql.NpgsqlConnection` for separate-connection verification (handlers operate on `IDbConnection`/`IDbTransaction` interface level for casting safety).
- No `Tx` token.

### Task 2: Async + atomic visibility
**Files:**
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Inbox/PostgreSqlInboxAsyncHandlerTests.cs` (create)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Inbox/PostgreSqlInboxAtomicVisibilityTests.cs` (create)

**Action:** create

**Description:**
Mirror PLAN-1.1 Task 2 with PG substitution. 4 tests across 2 files.

**Acceptance Criteria:**
- 4 tests pass against live PostgreSQL.

### Task 3: Option-false negative tests
**Files:**
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Inbox/PostgreSqlInboxOptionFalseTests.cs` (create)

**Action:** create

**Description:**
Mirror PLAN-1.1 Task 3 with PG substitution. 2 tests.

**Acceptance Criteria:**
- 2 tests pass against live PostgreSQL.

## Verification

```bash
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" -c Release --nologo 2>&1 | tail -5
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" --filter "FullyQualifiedName~Inbox" --nologo 2>&1 | tail -3
# expect: 8/8 pass.
grep -rnE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Inbox/
# expect exit 1.
```
