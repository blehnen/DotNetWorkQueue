# CONTEXT-7: Phase 7 Decisions (Integration Tests)

Captured 2026-05-18 during `/shipyard:plan 7`.

## Phase 7 framing

Per ROADMAP.md lines 158-214. 36 integration tests total:
- **24 inbox** (8 per transport × 3 transports): SqlServer + PostgreSQL + SQLite
- **12 SQLite-outbox** (method × outcome + validation + retry-bypass)

Requires running real SqlServer + PostgreSQL + SQLite instances + `connectionstring.txt` per project. Largest phase by test count.

## Decisions

### 1. Test-class organization: mirror outbox-milestone shape

Per transport (Inbox half — 3 transports × 4 test files):
- `*InboxIntegrationTestBase.cs` — shared base: queue lifecycle, business-table helpers, consumer + handler setup, ActivityListener registration ([ClassInitialize]), atomic-visibility assertion helper.
- `*InboxSyncHandlerTests.cs` — 2 tests: `Sync_Commit_BothRowsVisible` + `Sync_Rollback_NeitherRowVisible`.
- `*InboxAsyncHandlerTests.cs` — 2 tests: `Async_Commit_BothRowsVisible` + `Async_Rollback_NeitherRowVisible`.
- `*InboxOptionFalseTests.cs` — 2 tests: `Sync_OptionFalse_CapabilityCastFails_DiscoverableError` + `Async_OptionFalse_CapabilityCastFails_DiscoverableError`. The handler attempts the cast; with option=false, `is IRelationalWorkerNotification` returns `false` → the handler surfaces a discoverable error (not NRE).
- `*InboxAtomicVisibilityTests.cs` — 2 tests: explicit "verify business row visible from separate connection AFTER queue commit" + "verify business row NOT visible from separate connection AFTER queue rollback". These overlap conceptually with the SyncHandler tests but explicitly read from a second connection mid-commit to prove cross-connection visibility semantics.

Total: 8 tests × 3 transports = 24 inbox tests.

SQLite outbox half (mirrors `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/` structure):
- `SqliteOutboxIntegrationTestBase.cs` — shared base
- `SqliteOutboxSendTests.cs` — 2 tests (Send single, commit + rollback)
- `SqliteOutboxSendAsyncTests.cs` — 2 tests (SendAsync single, commit + rollback)
- `SqliteOutboxBatchTests.cs` — 4 tests (Send batch + SendAsync batch, each commit + rollback)
- `SqliteOutboxAdditionalDataTests.cs` — 1 test (IAdditionalMessageData round-trip)
- `SqliteOutboxValidationTests.cs` — 2 tests (cross-database file-path mismatch + closed connection)
- `SqliteOutboxRetryBypassTests.cs` — 1 test (transient error propagates; retry decorator NOT invoked)

Total: 12 outbox tests across 6 files (5 test files + 1 base).

### 2. ActivityListener registration: per-test-class via [ClassInitialize]

Each test base class registers an `ActivityListener` for the relevant `ActivitySource` during `[ClassInitialize]` and disposes during `[ClassCleanup]`. Mandatory per CLAUDE.md trace-decorator coverage lesson — without a listener, `ActivitySource.StartActivity()` returns null and the trace decorator chain short-circuits silently (0% coverage).

### 3. Test file location

| Transport | Integration test project | Folder |
|---|---|---|
| SqlServer | `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/` | `Inbox/` (new subdirectory) |
| PostgreSQL | `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/` | `Inbox/` (new subdirectory) |
| SQLite | `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/` | `Inbox/` (new) AND `Outbox/` (new) subdirectories |

Note: SqlServer's integration project name lacks the dot ("IntegrationTests" not "Integration.Tests"); PG and SQLite have the dot. Match existing conventions per-transport.

### 4. Queue naming + isolation

- Queue names: `Guid.NewGuid().ToString("N")` (CLAUDE.md lesson on DNQ hyphen rejection); helper `NewQueueName()` returns `"q" + N-format-guid` for safety.
- Business table names: `"InboxBusiness_" + Guid.NewGuid().ToString("N")` — parallel-safe; one per test.
- Each test owns its own connection + queue + business table; per-test cleanup in `try/finally` or `using`.

### 5. Metrics assertions: poll, don't snapshot

For the retry-bypass test, use polling on the live `IMetrics` object rather than a single snapshot. CLAUDE.md lesson: handler callbacks signal completion before `CommitMessage.Commit()` increments counters.

### 6. SQLite file-path canonicalization in tests

SQLite tests must explicitly exercise the spike §3 file-path canonicalization decision. The cross-database validation test (`SqliteOutboxValidationTests.cs` test 1) uses a connection to a DIFFERENT SQLite file than the queue's; the validator should reject with the spike-§3 comparison semantics (`Path.GetFullPath()` + uppercase + `:memory:` short-circuit).

### 7. SQLite single-writer concurrency observation

Phase 1 spike Risk #4 flagged SQLite single-writer behavior under hold-tx. Phase 7 tests should observe and document this for Phase 8 docs, NOT treat it as a blocker. If a test deadlocks or has unexpected wait behavior, document the observation in `Phase 7 SUMMARY` and proceed.

## Non-decisions
- All 4 plans (Inbox SqlServer / Inbox PG / Inbox SQLite / SQLite Outbox) are independent; one wave with 4 parallel plans is correct.
- ≤3 tasks per plan: each plan splits its 5 files (1 base + 4 test files) across 3 tasks. Outbox plan splits 6 files across 3 tasks similarly.
- MSTest 3.x assertions throughout.
- No new csproj / no new packages (existing integration test projects).
- Jenkins runs the integration test suite automatically; no Jenkinsfile changes.

## Scope reminders for plan authors
- This is a PLANNING session. Build is deferred to a new session against real DBs.
- The build session needs `connectionstring.txt` files configured for each integration test project.
- Phase 7 build is also the first time SQLite hold-tx is exercised against a real DB — expect to observe (and document for Phase 8) any single-writer concurrency surprises.
- Plans should be specific enough that the build-session builder can author tests with minimal extra investigation.
