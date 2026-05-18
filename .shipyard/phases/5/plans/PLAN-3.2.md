# Plan 3.2: SQLite Outbox Tests — Extractor, Wrapper, HandleExternalTx Fork, Retry-Bypass

## Context

SQLite counterpart of outbox milestone Phase 3+4 unit tests. Six tests covering: extractor round-trip + spike-§3 semantics, normalization wrapper symmetry, `HandleExternalTransaction` fork branch selection in send handlers, the "zero `Commit`/`Rollback`/`Dispose`/`Close` on caller tx" invariant (PROJECT.md §SC #8), and the retry-decorator-NOT-invoked-on-caller-tx-path assertion.

## Dependencies
PLAN-2.2 (extractor, wrapper, producer queue, send-handler forks must exist).

## Tasks

### Task 1: Extractor + wrapper tests
**Files:** `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqLiteExternalDbNameExtractorTests.cs`
**Action:** create
**Description:**

Mirror `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerExternalDbNameExtractorTests.cs` shape. Tests:

1. `Extract_Returns_MemoryLiteral_For_MemorySource` — `conn.DataSource = ":memory:"` → `Extract` returns `":memory:"` verbatim.
2. `Extract_Returns_Canonicalized_Path_For_File_Source` — `conn.DataSource = "./test.db"` → `Extract` returns absolute path via `Path.GetFullPath`.
3. `Extract_Memory_Literal_Comparison_Is_Case_Sensitive` — `conn.DataSource = ":Memory:"` → `Extract` does NOT short-circuit (returns canonicalized path of `:Memory:`).
4. `Normalized_Wrapper_Applies_Symmetric_Canonicalization` — instantiate `SqliteNormalizedConnectionInformation` with a relative-path connection string; assert the wrapper's `Container` (or whatever validator-compared property) returns the canonicalized path matching what the extractor would produce.

Mock `DbConnection` via NSubstitute (`Substitute.For<DbConnection>()`).

**Acceptance Criteria:**
- 4 tests, all pass.

### Task 2: `HandleExternalTransaction` fork tests
**Files:** `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/CommandHandler/SendMessageCommandHandlerExternalTxTests.cs`
**Action:** create
**Description:**

Two tests proving the fork branch selects correctly AND honors the no-mutation invariant on caller's resources (PROJECT.md §SC #8):

1. `HandleExternalTransaction_Path_Selected_When_Command_Has_ExternalTransaction` — mock `IDbConnection`/`IDbTransaction`; build a `SendMessageCommand` with `ExternalTransaction` set; invoke the handler; assert the SQL was executed against the mocked `command.ExternalTransaction.Connection` (not a newly-created connection).
2. `HandleExternalTransaction_Never_Calls_Commit_Rollback_Dispose_Close_On_Caller_Resources` — same setup; after handler returns, assert:
   - `mockTransaction.Received(0).Commit()`
   - `mockTransaction.Received(0).Rollback()`
   - `mockTransaction.Received(0).Dispose()`
   - `mockConnection.Received(0).Close()`
   - `mockConnection.Received(0).Dispose()`

Mirror outbox-milestone equivalent tests for SqlServer (`SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerExternalTxTests.cs` or similar — find via Glob).

**Acceptance Criteria:**
- 2 tests, all pass.
- PROJECT.md §SC #8 directly satisfied for SQLite.

## Verification

```bash
# Gate 1: extractor tests pass.
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --filter "FullyQualifiedName~SqLiteExternalDbNameExtractorTests" --nologo 2>&1 | tail -3
# expect 4/4 pass.

# Gate 2: fork tests pass.
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --filter "FullyQualifiedName~SendMessageCommandHandlerExternalTxTests" --nologo 2>&1 | tail -3
# expect 2/2 pass.

# Gate 3: full SQLite test suite.
dotnet test "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" --nologo 2>&1 | tail -3
# expect 0 failures.

# Gate 4: Tx-token guard (excluding the legitimate ExternalTransaction usage; grep for the bare token only).
grep -nE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/SqLiteExternalDbNameExtractorTests.cs Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/CommandHandler/SendMessageCommandHandlerExternalTxTests.cs
# expect exit 1 (no `Tx` standalone; the `ExternalTransaction` full word doesn't trigger \b(Tx)\b).
```
