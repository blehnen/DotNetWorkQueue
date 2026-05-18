# PLAN-1.3 Summary — SQLite Inbox Integration Tests

## Status

Tests authored, compile-clean, **awaiting Jenkins execution** (per the build flow revision — Jenkins runs the integration matrix when the PR opens).

## Files Created

- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/SqLiteInboxIntegrationTestBase.cs`
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/SqLiteInboxSyncHandlerTests.cs` (2 tests)
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/SqLiteInboxAsyncHandlerTests.cs` (2 tests)
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/SqLiteInboxAtomicVisibilityTests.cs` (2 tests)
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/SqLiteInboxOptionFalseTests.cs` (2 tests)

8 tests total.

## Decisions Made

- **SQLite database library:** Used `System.Data.SQLite` (matching the existing `IntegrationConnectionInfo` and the SQLite production code references), not `Microsoft.Data.Sqlite` as the plan's prose loosely suggested. The repo's existing `System.Data.SQLite.Core` PackageReference is what's actually available; the production `SqLiteRelationalWorkerNotification` XML-doc text mentioning `Microsoft.Data.Sqlite.SqliteTransaction` is descriptive of a possible alternative driver, not the one in use.
- **Per-test file path:** Each test owns its own `IntegrationConnectionInfo(inMemory: false)` — gives a fresh `.db` file with WAL journaling enabled at file creation, and disposes/deletes the file at test end. WAL is essential here because the inbox test holds a write transaction on one connection while a verification SELECT runs on a separate connection; without WAL the verification reader would block (or in older SQLite, fail with SQLITE_BUSY).
- **Pattern parity with SqlServer/PG inbox bases:** Same `[ClassInitialize]` ActivityListener registration, same retry-delay-behavior bounding for the rollback tests, same cross-connection assertion polling.
- **Async handler return shape:** Lambdas that go down the throw path do not need an explicit `Task.CompletedTask`; lambdas that complete normally explicitly `return Task.CompletedTask;` to satisfy the `Func<..., Task>` signature.

## SQLite Concurrency Observations (input to Phase 8 / PROJECT.md Risk #4)

Awaiting Jenkins integration run. Observations to capture from the Jenkins log:

- **Single-writer serialization behavior under hold-tx.** Whether two concurrent inbox tests on different DB files run in parallel cleanly (expected — each test gets its own file), and whether the per-file SQLite write lock during the inbox transaction causes any unexpected wait time on the verification SELECT under WAL mode.
- **Handler-throw rollback timing.** Time between handler-throw and the verification connection being able to read from the business table (proxied by `AssertBusinessRowCountStaysAt`'s 1500ms settle window — adjust if Jenkins shows the rollback completes faster or slower).
- **Retry-bypass behavior.** Whether the bounded `RetryDelayBehavior` (100ms × 1 attempt) on `InvalidOperationException` keeps the rollback-path tests under the 30s handler-invocation wait without flakiness.
- **WAL vs DELETE journal under the queue's transaction.** The queue creates the metadata/status tables in the same DB file; confirm that the WAL pragma set by `IntegrationConnectionInfo` survives queue creation and applies to the queue's internal connections too.

**If Jenkins shows any of these as a behavioral surprise**, write a short doc-facing note into `docs/inbox-pattern.md` (Phase 8 deliverable) calling out the SQLite-specific characteristic for users.

## Verification

```bash
dotnet build "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" -c Debug --nologo
# → 0 Error(s); only pre-existing NU1902 advisory warnings + SYSLIB0012 on Assembly.CodeBase (unrelated, ConnectionString.cs).

grep -rnE "\b(Tx|TX)\b" Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/Inbox/
# → no matches.
```

Jenkins gate runs `dotnet test ... --filter "FullyQualifiedName~Inbox"`; expected outcome 8/8 pass.
