# Plan 1.3: SQLite Inbox Integration Tests (8 tests)

## Context

SQLite mirror of PLAN-1.1 with Microsoft.Data.Sqlite substituted. 8 inbox integration tests against a live SQLite instance (file-based; `:memory:` requires `SqLiteHoldConnection` keep-alive).

**SQLite-specific concerns:**
- Single-writer concurrency (Risk #4 from Phase 1 spike). Document observations during build for Phase 8 docs.
- Hold-tx implementation was newly added in Phase 5 — first real-DB exercise of the new code paths.

PROJECT.md §SC #4 + #5 satisfied for SQLite.

## Dependencies
Phase 5 (SQLite hold-tx + inbox wiring shipped). Parallel-safe with PLAN-1.1, PLAN-1.2, PLAN-1.4.

## Tasks

### Task 1: SQLite inbox test base + sync handler tests
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/SqLiteInboxIntegrationTestBase.cs` (create)
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/SqLiteInboxSyncHandlerTests.cs` (create)

**Action:** create

**Description:**

Mirror PLAN-1.1 Task 1 with SQLite-specific anchors:
- Use `Microsoft.Data.Sqlite.SqliteConnection` for the separate-connection verification.
- `QueueScope` wraps `QueueCreationContainer<SqLiteMessageQueueInit>` + `SqLiteMessageQueueCreation`.
- Use file-based SQLite connection strings (NOT `:memory:` for inbox tests — different processes/connections wouldn't share the database).
- Naming convention: `SqLite*` (capital L) per Phase 5 convention.

Sync tests:
1. `Sync_Commit_BothRowsVisible` — verify atomic commit semantics on SQLite.
2. `Sync_Rollback_NeitherRowVisible` — verify rollback.

**Acceptance Criteria:**
- 2 tests pass against live SQLite (file path in `connectionstring.txt`).
- Test connection string uses a per-test file path to avoid single-writer contention between concurrent test methods.

### Task 2: Async + atomic visibility
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/SqLiteInboxAsyncHandlerTests.cs` (create)
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/SqLiteInboxAtomicVisibilityTests.cs` (create)

**Action:** create

**Description:**

Mirror PLAN-1.1 Task 2. 4 tests. SQLite single-writer concurrency observation: if either test deadlocks or shows unexpected wait behavior, capture details in SUMMARY for Phase 8 docs (NOT blocker).

**Acceptance Criteria:**
- 4 tests pass against live SQLite.
- Any concurrency observations documented in SUMMARY-1.3.md.

### Task 3: Option-false negative tests + Phase 8 doc input capture
**Files:**
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/SqLiteInboxOptionFalseTests.cs` (create)

**Action:** create

**Description:**

Mirror PLAN-1.1 Task 3 with SQLite. 2 tests.

Additionally: SUMMARY-1.3.md should document any SQLite hold-tx behavioral observations (e.g., "single-writer waits Xms when consumer is mid-handler with held tx"). These observations feed Phase 8 docs per PROJECT.md Risk #4.

**Acceptance Criteria:**
- 2 tests pass against live SQLite.
- SUMMARY-1.3.md includes a "SQLite concurrency observations" section even if empty.

## Verification

```bash
dotnet build "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" -c Release --nologo 2>&1 | tail -5
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" --filter "FullyQualifiedName~Inbox" --nologo 2>&1 | tail -3
# expect: 8/8 pass.
grep -rnE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/
# expect exit 1.
```
