### 0.9.41 — Unreleased
- SQL Server transport: `IProducerQueue.Send(List<...>)` now performs a true bulk insert instead of looping the single-message handler. The batch runs in one connection and one transaction spanning all chunks: a multi-row body insert via `MERGE … OUTPUT` (generated ids recovered in caller input order), then per-message meta/status rows. Measured ~21× throughput improvement for a 500-message batch (loop 6402 ms → batch 305 ms). See [ADR 0001](docs/adr/0001-true-bulk-insert-batch-send.md) (GitHub #162)
- SQLite transport: the same true bulk insert for `Send(List<...>)`, one connection and one transaction spanning all chunks. The body insert is a multi-row `INSERT … RETURNING QueueID`; ids are recovered in caller input order by sorting them ascending, which is exact because `QueueID` is `INTEGER PRIMARY KEY AUTOINCREMENT` (ids are assigned monotonically in insert order within the single write transaction). Same whole-batch atomic semantics as SQL Server. New `SqLiteMessageQueueTransportOptions.BatchSize` — optional ceiling for the chunk size, clamped to the SQLite safe-max (0 = use the safe-max). A large batch holds the single write transaction until commit; the default WAL journal mode (`EnableWalMode`) keeps readers and other writers from blocking for that duration, so a database that disabled WAL may see longer writer-blocking on large batches (GitHub #162)
- Behavior change: the batch send path is now whole-batch atomic (all-or-nothing) — any failure rolls back every message in the batch and each returned `IQueueOutputMessage` reports the failure. This differs from the previous per-message isolation (one bad message failed alone, the rest committed). Callers who need per-message isolation should call `Send(message)` in a loop. Scheduled-job messages are not supported on the batch path and are rejected with a clear error
- `Send(List<...>)` now returns generated ids in caller input order (`ProducerQueue` message preparation is order-preserving; previously a `ConcurrentBag`/`Parallel.ForEach` discarded order). This affects all transports
- New `SqlServerMessageQueueTransportOptions.BatchSize` — optional ceiling for the batch chunk size, clamped down to a safe maximum derived from the SQL Server parameter limit (0 = use the safe maximum)
- Transports without a bulk-insert path (Memory, LiteDb, and — until its phase — PostgreSQL) fall back to the previous per-message loop with no change. Redis keeps its existing batch path
- No public API surface changes

### 0.9.40 — 2026-06-25
- Redis transport: migrated `StackExchange.Redis` 2.13.17 → 3.0.7 (the latest stable 3.x release). The 3.0 bump was previously reverted (0.9.39) due to intermittent `Timeout performing SCRIPT/EVAL` under load; root-caused to .NET thread-pool / reply-completion pressure on synchronous Redis paths after SE.Redis 3.0 removed its dedicated socket/completion pool
- Behavior: Redis connections now pin `Protocol = RESP2` (matches the 2.x wire protocol; SE.Redis 3.x would otherwise negotiate RESP3, a behavioral change). RESP3 remains a possible future opt-in
- Diagnostics: Redis connections set a queue-aware `ClientName` (`dnwq-{QueueName}`), surfaced in SE.Redis timeout messages and Redis `CLIENT LIST`
- Known limitation (SE.Redis 3.x): high-concurrency **synchronous** producers may time out under load — the 2.x dedicated reply-completion pool is gone, so many concurrent sync `EVAL`s contend on completion. Prefer the **async** API (`SendAsync`) for concurrent/high-volume producers
- Tests: raised the worker thread-pool floor in the Redis integration-test bootstrap (defensive against pool-injection-lag starvation) and reclassified the deterministic thread-pool-starvation test as a permanent diagnostic (excluded from the default CI run)
- Follow-ups tracked separately: an async receive path (`IReceiveMessagesAsync` across core + transports) and deprecation of the synchronous API both remain future work
- No public API surface changes

### 0.9.39 — 2026-06-23
- Dependency refresh across `Directory.Packages.props` (OpenTelemetry, Polly.Core, Swashbuckle.AspNetCore, Microsoft.Extensions.*, test tooling). `StackExchange.Redis` held at 2.13.17 and `FluentAssertions` at 6.12.2 (last MIT-licensed release). No API surface changes

### 0.9.38 — 2026-06-03
- Fix: `IAdminApi.Count(connId, QueueStatusAdmin.*)` on the PostgreSQL transport threw `InvalidCastException` under Npgsql 10.x — the shared relational `GetQueueCountQueryPrepareHandler` bound the raw `QueueStatusAdmin` enum to an `Int32` parameter, which Npgsql 10.x's stricter writer rejects (pre-10.x silently coerced it). Now casts the enum to `int` (GitHub #155)
- Fix: the same status-filtered admin `Count` path threw `SQLiteException: unknown error — Insufficient parameters supplied to the command` on the SQLite transport — the parameter was bound as `@Status` while the SQL placeholder is lowercase `@status`, and System.Data.SQLite binds parameter names case-sensitively (Npgsql and Microsoft.Data.SqlClient fold case, so PG and SQL Server were unaffected). Parameter name now matches the SQL casing (GitHub #155)
- Test: deterministic status-filtered `Count` coverage across PostgreSQL, SQL Server, and SQLite (the path was previously dead behind a `runTime > 10` guard that never ran, which is why both bugs shipped) plus a unit guard on `GetQueueCountQueryPrepareHandler`
- No API surface changes

### 0.9.37 — 2026-05-28
- CVE fix: `OpenTelemetry` 1.15.2 → 1.15.3 — clears the transitive `OpenTelemetry.Api` advisory CVE-2026-40894 / GHSA-g94r-2vxg-569j (NU1902, moderate: excessive memory allocation when parsing OpenTelemetry propagation headers)
- Removed the now-obsolete `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` from `Transport.SQLite.csproj` (added in 0.9.36 / ISSUE-032 solely to keep that advisory visible without failing the Release build)
- Dependency refresh across `Directory.Packages.props` — shipping: `Microsoft.Data.SqlClient` 7.0.1, `Npgsql` 10.0.3, `SimpleInjector` 5.5.2, `StackExchange.Redis` 2.13.17, `MudBlazor` 9.5.0, `CronExpressionDescriptor` 2.48.0, `Cronos` 0.13.0, `Microsoft.SourceLink.GitHub` 10.0.300, and `Microsoft.Extensions.Caching.Memory` / `Microsoft.Extensions.Configuration.Binder` / `Microsoft.Extensions.Http` / `System.Diagnostics.DiagnosticSource` / `System.Security.Cryptography.Xml` → 10.0.8
- Test tooling: `coverlet.collector` 8.0.1 → 10.0.1 (2-major), `MSTest.TestAdapter` / `MSTest.TestFramework` 4.2.3, `Microsoft.NET.Test.Sdk` 18.6.0, `Microsoft.Testing.Extensions.Retry` 2.2.3, `bunit` 2.7.2, `Microsoft.Playwright` / `Microsoft.Playwright.MSTest` 1.60.0, `Microsoft.AspNetCore.TestHost` (net10) 10.0.8
- `FluentAssertions` intentionally held at 6.12.2 (last MIT-licensed release); `Microsoft.AspNetCore.TestHost` net8 target held on the 8.0.x line
- No API surface changes

### 0.9.36 — 2026-05-16
- Feature: transactional outbox pattern on SqlServer and PostgreSQL transports via opt-in `IRelationalProducerQueue<T>` capability cast; the caller supplies a `DbTransaction` and the queue INSERT joins the caller's business transaction (GitHub #138)
- Memory, Redis, LiteDb, and SQLite are unchanged; callers that don't reach for the new interface see the same `IProducerQueue<T>` they always have
- Retry decorators are skipped on the external-transaction send path so the caller keeps control of both transaction lifecycle and retry policy
- `ExternalTransactionValidator` throws on cross-database mismatches before any write
- `SqlServerExternalDbNameExtractor` and `PostgreSqlExternalDbNameExtractor` compare database names verbatim; neither side does case folding
- Test: 24 integration tests across both transports (Send/SendAsync × single/batch × commit/rollback, validation paths, retry-bypass, `IAdditionalMessageData` round-trip)
- CI: `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` on `Transport.SQLite.csproj` keeps the OpenTelemetry NU1902 advisory visible without failing the Release build (ISSUE-032)
- CI: close the net8.0 XML-doc gate on `Transport.RelationalDatabase.csproj`
- Docs: new `docs/outbox-pattern.md` tutorial + reference page and a README pointer under "High-level features" (GitHub #138 + doc polish in #139)

### 0.9.35 — 2026-04-23
- Fix: retry decorators across all transports (SqlServer, PostgreSQL, SQLite, Redis, LiteDb) tolerate a disposed Polly registry during queue shutdown — previously threw `ObjectDisposedException` when a background operation raced the dispose path (GitHub #121)
- CI: auto-publish dashboard docker image on `v*` tag via GitHub Actions (GitHub #122)
- CI: bump GitHub Actions from v4 to v5 for Node.js 24 compatibility (GitHub #128)
- CI: re-enable JobScheduler integration tests in Jenkins — previously excluded via stale `FullyQualifiedName!~JobScheduler` filter carried over from the TeamCity era (GitHub #127)
- Cleanup: remove unused `ICachePolicy` / `CachePolicy` types (GitHub #124)
- Test: add `SntpTime` base class unit tests (GitHub #125)
- Test: add bUnit component tests for `Dashboard.Ui` (Phase 1 of GitHub #126)
- Test: add `Dashboard.Ui` Playwright E2E test project + Jenkins stage 15 (Phase 2 of GitHub #126)
- Docs: lessons learned on Playwright/bUnit, CI filter re-validation, release flow

### 0.9.34 — 2026-04-20
- Fix: dashboard history reads no longer short-circuit on `IBaseTransportOptions.EnableHistory`; fixes empty history when the dashboard container was started before its queue existed (GitHub #119)
- Fix: Dashboard UI history exceptions now collapse by default with a per-row expand chevron; previously the full stack trace rendered inline and inflated row height on pages with errors
- Relational (PostgreSQL / SQL Server / SQLite) read and purge handlers catch `DbException` when the history table does not exist and return empty rather than 500
- Redis and Memory read/purge paths rely on native empty-on-missing behavior; same net effect
- Test: `PostgreSqlHistoryEnabledTests` + `SqlServerHistoryEnabledTests` close the dashboard integration-test coverage gap (previously only Redis, Memory, LiteDb, SQLite had history-enabled coverage)
- Test: `DashboardStartupTimingTests` regressions the exact startup-before-queue timing bug on SQLite
- Follow-up: GitHub #120 tracks the underlying options-factory caching defect that caused this and affects other transport options

### 0.9.33 — 2026-04-18
- Dependency refresh: 15 low-risk patch/minor bumps + 8 major-version bumps across `Directory.Packages.props` (GitHub #118)
- `Microsoft.Data.SqlClient` 6.1.3 → 7.0.0
- `Npgsql` 8.0.8 → 10.0.2 (2-major leap)
- `Swashbuckle.AspNetCore` 7.2.0 → 10.1.7; required `Microsoft.OpenApi` 1→2 namespace migration in `DashboardExtensions` and swagger tests
- `coverlet.collector` 6.0.4 → 8.0.1, `Microsoft.Testing.Extensions.Retry` 1.6.2 → 2.2.1
- `Microsoft.Extensions.Configuration.Binder` / `Http` / `Caching.Memory` 9.0.3 → 10.0.6
- CVE fix: `System.Security.Cryptography.Xml` overridden to 10.0.6 (NU1903)
- No API surface changes outside the mandatory Swashbuckle/OpenApi namespace migration
- `FluentAssertions` remains pinned at 6.12.2 (MIT); net10.0 + net8.0 multi-targeting preserved

### 0.9.32 — 2026-04-16
- Fix: Redis `PurgeMessageHistoryHandler` eliminates redundant `CompletedUtc` round-trip in orphan cleanup path; `HashGet("CompletedUtc")` now executes only when the hash exists (ISSUE-016)
- Fix: RelationalDatabase `WriteMessageHistoryHandler.RecordComplete` removes `StartedUtc IS NOT NULL` guard from WHERE clause so `DurationMs=0` is written for sub-millisecond completions (ISSUE-014)
- Test: Redis `Purge_Handles_Missing_Hash_Gracefully` asserts `CompletedUtc` is never read in orphan path via `DidNotReceive()` (ISSUE-017)
- Test: LiteDb history `CleanupAsync` documents that double-dispose of `CreationScope` is safe (idempotent via `_disposedValue` guard) (ISSUE-020)
- Cleanup: delete 7 empty shell files left after NETFULL removal, remove dead `MakeTrackingParam` local function, remove no-op `dynamic=true` test cases from JobSchedulerTests (ISSUE-015, ISSUE-021, ISSUE-022, ISSUE-023)
- Archive: create missing SUMMARY-1.1.md for LiteDb history test plan audit trail (ISSUE-019)

### 0.9.31 — 2026-04-09
- **Breaking:** Dashboard UI config changed: `DashboardApi:BaseUrl` / `ApiKey` replaced by `DashboardApi:Sources[]` array; old format throws `InvalidOperationException` with migration example at startup (GitHub #96)
- Dashboard UI connects to multiple Dashboard API instances from one deployment; each source gets a `Name`, `BaseUrl`, and optional `ApiKey` in the config array
- All page URLs now include `/source/{slug}` prefix; single-source deployments redirect automatically
- Background health polling per source (30s interval, 5s timeout); cached state shown on Home page
- Home page groups connections by source in collapsible panels with health status; single source shows a flat list
- Offline or failed sources show a warning without blocking other sources; per-source Retry button
- In-process API auto-registers as "Local" source, resolves its own listen address via `IServer`

### 0.9.30 — 2026-04-08
- **Breaking:** Replace Schyntax schedule format with standard cron expressions via [Cronos](https://github.com/HangfireIO/Cronos); 5-field and 6-field (with seconds) both supported (GitHub #100)
- **Breaking:** `IJobSchedule.Previous()` returns `DateTimeOffset?` instead of `DateTimeOffset`
- **Breaking:** All heartbeat and job schedule strings now use cron format
- Remove vendored `/Lib` directory (last Schyntax DLLs)
- Add `IJobSchedule.Description` property, returns cron description text via [CronExpressionDescriptor](https://github.com/bradymholt/cron-expression-descriptor)
- Log schedule descriptions when jobs are added to the scheduler

### 0.9.19 — 2026-04-07
- **Breaking:** Drop .NET Framework 4.8 and .NET Standard 2.0 targets; now targets .NET 10.0 and .NET 8.0 only (GitHub #101)
- Remove dynamic LINQ expression support (was net48-only via JpLabs.DynamicCode)
- Remove vendored `Lib/JpLabs.DynamicCode` directory
- Remove `#if NETFULL` / `#if NETSTANDARD2_0` conditional compilation from all source files
- Remove vestigial `bool dynamic` parameter from `JobSchedulerTests` shared implementation and all 6 transport callers
- Delete 7 empty shell files left after NETFULL block removal
- GitHub Actions CI: switch from `windows-latest` (net48) to `ubuntu-latest` (net10.0)
- Update README and CLAUDE.md to remove net48/dynamic LINQ references

### 0.9.18 — 2026-04-05
- Deterministic builds, portable debug symbols, `.snupkg` symbol packages, Source Link via `Microsoft.SourceLink.GitHub` (GitHub #95)
- Link sample Grafana dashboard (Prometheus data source) from README (GitHub #98)

### 0.9.17 — 2026-04-05
- Fix: message history `DurationMs` stores `0` (not `null`) when a message completes or errors before `StartedUtc` is persisted; fixes blank duration in Dashboard for sub-millisecond messages (Memory, RelationalDatabase, LiteDB, Redis) (GitHub #94)
- Dashboard UI: history table shows `< 1 ms` for zero-duration completions
- RelationalDatabase: drop `StartedUtc IS NOT NULL` guard from `RecordComplete` UPDATE; the guard made the UPDATE a no-op for sub-ms rows even though the C# computed `DurationMs = 0` correctly

### 0.9.16 — 2026-04-03 (Dashboard.Api, Dashboard.Ui only)
- Fix: pre-load plugin assemblies at startup; needed by Newtonsoft `TypeNameHandling` to resolve user POCO types
- Diagnostic logging in `ResolveMessageBodyType` (debug per stage, warning on failure)

### 0.9.15 — 2026-04-03 (Dashboard.Api, Dashboard.Ui only)
- `DashboardOptions.AssemblyPaths` — directories for user POCO DLLs so the dashboard can deserialize message bodies in Docker without embedding assemblies
- Docker: `/app/plugins` directory created by default; volume-mount or extend the image

### 0.9.14 — 2026-04-03
- Dashboard UI: replace connection/queue cards with compact tables; remove nav drawer, make title clickable
- Dashboard UI: self-contained mode — UI and API in one process (for Docker)
- `IConfiguration` overload for JSON-based transport registration (`DashboardConnectionConfig` POCO)
- Docker image: `blehnen74/dotnetworkqueue-dashboard` on Docker Hub

### 0.9.13 — 2026-03-29
- Fix: `QueueContainer.CreateAdminApi()` crash from missing `QueueConnection` parameter after queue name validation changes in 0.9.12
- **Breaking:** Remove `Thread.Abort()` and `AbortWorkerThreadsWhenStopping` config; worker shutdown uses `CancellationToken` only
- Replace `new Thread(MainLoop)` with `Task.Factory.StartNew(MainLoop, TaskCreationOptions.LongRunning)` in PrimaryWorker and Worker
- Replace `Thread.Sleep(20)` spin-wait in `BaseMonitor.Cancel()` with `ManualResetEventSlim`

### 0.9.12 — 2026-03-29
- Default `DenyListSerializationBinder` blocks 30 known Newtonsoft.Json deserialization gadget types (ObjectDataProvider, WindowsIdentity, Process, DataSet, etc.); registered as the default `ISerializationBinder` via DI
- Optional `AllowListSerializationBinder` for strict type control; only explicitly registered types can be deserialized; register via DI to replace the default
- **Breaking:** Queue name validation on all 6 transports; rejects SQL injection characters at construction time. Allowed: alphanumeric, underscores, dots (Redis also allows hyphens). Max lengths: SQL Server 128, PostgreSQL 63, Redis 512, LiteDB 256
- Fix: `DashboardConsumerClient` implements `IAsyncDisposable`; `DisposeAsync()` awaits HTTP DELETE unregistration; sync `Dispose()` no longer blocks with `.GetAwaiter().GetResult()`
- Remove `DotNetWorkQueue.IntegrationTests.Metrics` project; metric tracking types moved to `IntegrationTests.Shared/Metrics/`
- Add `SECURITY.md` (Dynamic LINQ risks, serialization binder usage, queue backend access, deployment recommendations)

### 0.9.11 — 2026-03-26
- **Breaking:** `IHistoryConfiguration` removed; history decorators and monitors use `IBaseTransportOptions.EnableHistory` and `IBaseTransportOptions.HistoryOptions` directly
- **Breaking:** `IHistoryTransportOptions` added to `IBaseTransportOptions`; all transport options classes expose `HistoryOptions` (RetentionDays, MaxExceptionLength, StoreBody, Track* flags, MonitorTime)
- History query pagination uses database-level `OFFSET/LIMIT` (PostgreSQL, SQLite) or `OFFSET...FETCH NEXT` (SQL Server) instead of in-memory skip/take
- Redis history queries: unfiltered pages use `SortedSetRangeByRank` for server-side pagination; filtered pages use batched scanning
- `IBaseTransportOptions` registered in DI for all 6 transports; decorators inject it directly
- `IDbPaginationSyntax` interface for transport-specific SQL pagination (`LimitOffsetPaginationSyntax`, `FetchNextPaginationSyntax`)
- Fix: history table index names include queue name (e.g., `IX_{historyTable}_QueueID`) to prevent collision from leftover indexes in PostgreSQL and SQL Server
- Redis and Memory transports: persistent history config via saved transport options (Redis Configuration key, Memory static DataStorage)

### 0.9.10 — 2026-03-20
- `EnableHistory` on transport options (SQLite, SQL Server, PostgreSQL, LiteDB) — set during queue creation like other options. Redis and Memory don't need this; they create history storage at runtime when `IHistoryConfiguration.Enabled` is true.
- Dashboard API: read-only mode (`DashboardOptions.ReadOnly`) — blocks all write operations with 403
- Dashboard UI: write buttons hidden in read-only mode
- Dashboard API: `GET /api/v1/dashboard/settings` endpoint

### 0.9.9 — 2026-03-19
- Per-message cancellation: `IMessageCancellation` on `IWorkerNotification` (never null, NoOp when inactive), `ICancelRunningMessage` / `MessageCancellationTracker`
- Dashboard API: `POST .../messages/{messageId}/cancel` — cooperative cancel, in-process only
- Dashboard UI: Cancel button on Processing messages
- Message history tracking (opt-in via `IHistoryConfiguration.Enabled`): records enqueue, processing, complete, error, rollback, delete, expire per message
- History table per queue with configurable body storage (`StoreBody`) and 30-day retention purge
- History for all 6 transports
- `ClearHistoryMonitor` for retention purge, wired into `QueueMonitor`
- Dashboard API: history endpoints (list, detail, count, purge)
- Dashboard UI: History tab with status filter, pagination, inline exceptions, purge
- `MessageId` and `CorrelationId` pushed into `ILogger` scope during handler execution (zero config)

### 0.9.8 — 2026-03-17
- Fix: `InvokeMovedToErrorQueue` now increments the error counter — messages moved to the error queue were not being counted
- Dashboard UI: consumer metrics columns (Processed, Errors, Rollbacks, Poison) added to the Consumers tab
- Dashboard.Client: added README.md to NuGet package
- Core library: fixed broken samples link in NuGet README

### 0.9.7 — 2026-03-17
- Dashboard API: consumer heartbeat now carries running totals for messages processed, errors, rollbacks, and poison messages
- `DashboardConsumerClient`: thread-safe `IncrementProcessed()`, `IncrementErrored()`, `IncrementRolledBack()`, `IncrementPoisonMessage()` methods; counters sent automatically with each heartbeat
- `ConsumerInfoResponse` and `ConsumerEntry`: new `MessagesProcessed`, `MessagesErrored`, `MessagesRolledBack`, `PoisonMessages` fields
- Dashboard API: `GET /api/v1/dashboard/consumers` now returns per-consumer metric counters
- Backwards compatible — heartbeat requests without metrics default to zero

### 0.9.6 — 2026-03-16
- Suppress metrics from internal heartbeat scheduler queue; no more GUID-prefixed metric names in metrics backends

### 0.9.5 — 2026-03-16
- `MaintenanceMode` on `QueueConsumerConfiguration` (`Consumer` / `External`) — set to `External` to skip maintenance monitors in the consumer
- `IQueueMaintenanceService` / `QueueMaintenanceService` — runs the transport's `IQueueMonitor` outside the consumer
- Dashboard API: `HostMaintenance` per-queue option starts maintenance monitors at dashboard startup; status at `GET /api/dashboard/queues/{id}/maintenance`
- SQLite transport: `EnableWalMode` option (default `true`) — sets WAL journal mode on new file-based queues
- Metrics now prefixed with `dotnetworkqueue.` — in Prometheus, search for `dotnetworkqueue_` to find all queue metrics

### 0.9.4 — 2026-03-11
- Switch from forked GuerrillaNtp DLLs to official [GuerrillaNtp 3.1.0](https://www.nuget.org/packages/GuerrillaNtp/) NuGet package
- Move SNTP time provider (`SntpTime`) into the core library; any transport can now use NTP time, not just Redis
- Reuse a single `NtpClient` instance per provider (per official docs)
- Delete `Lib/GuerrillaNtp/` (no longer needed)
- Dashboard API: GZip and TripleDES interceptors can be configured from `appsettings.json`
- Dashboard API: Named interceptor profiles (`AddInterceptorProfile`) — register once, reference per-queue
- Dashboard API: Interceptor misconfiguration and missing message type assemblies now return specific error messages instead of 500

### 0.9.3 — 2026-03-10
- Dashboard API consumer tracking — consumers register via HTTP, send heartbeats, get pruned when stale
- `DotNetWorkQueue.Dashboard.Client` — standalone client library (no core dependency):
  - `DashboardApiClient` — typed C# wrapper for all Dashboard API endpoints
  - `DashboardConsumerClient` — auto-register, heartbeat timer, best-effort unregister on dispose
- Dashboard UI: Consumers tab showing connected consumers per queue (name, machine, PID, uptime)
- Dashboard UI: Consumer count badges on queue cards
- `DashboardOptions.EnableConsumerTracking`, `ConsumerHeartbeatIntervalSeconds`, `ConsumerStaleThresholdSeconds`

### 0.9.1 — 2026-03-09
- **Breaking Change** — Replace `App.Metrics` with built-in `System.Diagnostics.Metrics`; `DotNetWorkQueue.AppMetrics` package removed. Use OpenTelemetry.Metrics exporters instead.
- **Breaking Change** — Remove `SamplingTypes` enum from `IMetrics.Histogram()` and `IMetrics.Timer()`
- **Breaking Change** — Replace `dynamic CollectedMetrics` with typed `MetricsSnapshot GetCollectedMetrics()` on `IMetrics`
- Migrate tests from xUnit to MSTest
- Fix 10 missing `.ConfigureAwait(false)` calls in Trace Decorator classes
- Blazor Server dashboard UI (MudBlazor) for monitoring and managing queues
- Optional login page for dashboard UI with SHA256 hashed password (`DashboardAuth` config)
- Dashboard API fixes:
  - SQL Server: 404 on message detail for error messages (fallback to MetaDataErrors table)
  - SQL Server: requeue failing with NULL ExpirationTime (copy all columns from MetaDataErrors)
  - PostgreSQL/SQLite: requeue leaving wrong status after INSERT (use ON CONFLICT DO NOTHING + UPDATE)
  - Redis: requeue not clearing error tracking in serialized metadata
  - Redis: delayed/scheduled column not populating in message list
  - LiteDB: message body/headers/update queries failing with BsonMapper error (use FindById)
  - Body type showing interceptor name instead of actual message type
- Dashboard API:
  - API key authentication (`DashboardOptions.ApiKey` + `X-Api-Key` header)
  - Swagger API key support
  - Bulk operations: Requeue All Errors, Reset All Stale, Delete All Errors
  - Delayed message visibility (Scheduled column + Delayed chip)
  - Two-click delete confirmation for single-record deletes
  - Edit body for messages in Error status

### 0.9.0 — 2026-03-04
- Dashboard API for viewing and modifying messages in transports
- Polly V7 to V8

### 0.8.1 — 2026-02-22
- Fix various long-standing race conditions
- Fix multiple heartbeat schedulers sharing state in the same process

### 0.8.0 — 2026-01-05
- .NET 10 target
- Remove out-of-support frameworks
- **Breaking Change** — SQL client changed from `System.Data.SqlClient` to `Microsoft.Data.SqlClient`; may affect SQL Server connection strings

### 0.7.6 — 2024-02-02
- Remove connection objects from DataStore when queue is complete

### 0.7.5 — 2024-01-09
- Only verify internal container setup in debug mode, as it pins the memory it uses

### 0.7.4 — 2024-01-08
- Add test for scheduler creation with memory queue
- Move two logging messages from info to debug
- .NET 8.0 target
- Remove queue param for workgroups, as it was no longer being used

### 0.7.3 — 2023-11-28
- Error notification will not happen if a rollback notification is being performed

### 0.7.2 — 2023-11-28
- Add notification of queue events to ConsumerQueues

### 0.7.1 — 2023-11-21
- Add property to obtain creation script from `IQueueCreation`. Supported by SQL Server, SQLite, and PostgreSQL.

### 0.7.0 — 2023-10-26
- Fix retry logic for SQLite commands; changes in `System.Data.Sqlite` required changes in transport
- Switch to `DecorrelatedJitterBackoffV2` for SQL Server, PostgreSQL, and SQLite retries

### 0.6.9 — 2023-10-25
- Update various packages to latest versions
- Replace `OpenTelemetry.Exporter.Jaeger` with `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- Remove .NET 5.0 as a supported version

### 0.6.8 — 2022-07-19
- Update Npgsql
- Add initial admin interface

### 0.6.7 — 2022-06-30
- Update various packages to latest versions

### 0.6.6 — 2022-04-29
- Fix issue with custom default constraints in SQL Server transport

### 0.6.5 — 2022-02-06
- Relational database transports now allow additional columns to be used as part of the dequeue

### 0.6.4 — 2022-01-12
- `ILogger` will now be created using the queue name for the category

### 0.6.3 — 2022-01-11
- Remove Polly Bulkhead; does not correctly work with our task-limited scheduler
- Remove `MaxQueue` feature from async processing, as it depended on Polly Bulkheads
- Switch to `ILogger` from `Microsoft.Extensions.Logging.Abstractions`

### 0.6.2 — 2021-12-19
- .NET 6.0 target

### 0.6.1 — 2021-09-28
- Producer will throw an exception on a non-public class used as a message due to internal delegate handling limitations

### 0.6.0 — 2021-09-07
- Switch from https://opentracing.io/ to https://opentelemetry.io/
  **Breaking Change** — OpenTracing always added an entry to headers; OpenTelemetry only adds entries if enabled. Queues must be empty before updating.

### 0.5.4 — 2021-05-19
- Fix error with adding items to a memory queue that has started shutdown
- Asking for list of error messages should not throw if transport fails; added flag to indicate if errors are loaded

### 0.5.3 — 2021-05-18
- Fix performance issue with in-memory queues

### 0.5.2 — 2021-04-18
- LiteDB transport now supports direct and memory connections; all connections must be made in the same process

### 0.5.1 — 2021-04-00
- LiteDB transport
- .NET 5 target; many references do not yet support 5.0

### 0.5.0 — 2020-12-08
- Change how connections are set up; **breaking change** to support generic connection settings not expressible in connection strings
- .NET 4.8 target
- .NET Standard 2.0 target for SQLite transport; Microsoft SQLite transport deprecated
- SQL Server transport now supports creating queues in schemas other than `dbo`

### 0.4.6 — 2020-09-02
- Redis transport: re-cache LUA scripts when no longer in cache; fixes issue with server restarts

### 0.4.5 — 2020-02-28
- Make previous error types and count available to message processing
- Consumer queues now remove errors by default after 30 days; configurable

### 0.4.4 — 2019-12-23
- Fix issue with SQL Server transport and heartbeat reset

### 0.4.3 — 2019-10-29
- Fix issue with registration of message rollback

### 0.4.2 — 2019-10-29
- .NET 4.6.1 target
- Upgrade packages to latest versions

### 0.4.1 — 2019-06-08
- Fix issue with retry policies using seconds instead of milliseconds

### 0.4.0 — 2019-06-02
- Remove RPC
- Implement OpenTracing https://opentracing.io/
- Fix message interception

### 0.3.1 — 2019-04-26
- Correct versioning for NuGet publish

### 0.3.0 — 2019-04-26
- All modules now target .NET 4.7.2 and .NET Standard 2.0
- **Breaking Change** — changes to metrics interface to switch to AppMetrics
- Deprecated Metrics.NET
- `DotNetWorkQueue.AppMetrics` replaces `DotNetWorkQueue.Metrics.Net`

### 0.2.1 — 2017-09-30
- Refactoring to better share logic between transports
- **Breaking Change** — fixed various spelling mistakes affecting public signatures
- **Breaking Change** — fixed typo with internal Redis property; queues should be drained before upgrading
- **Breaking Change** — replaced SmartThreadPool with `Task.StartNew` and Polly Bulkheads; removed related configuration properties
- Heartbeat workers now use internal job scheduler backed by in-memory queue
- **Breaking Change** — heartbeat configuration now uses Schyntax format instead of timespan
- New SQLite transport using Microsoft driver

### 0.1.10 — 2017-03-19
- Route support for SQL Server, SQLite, Redis, and PostgreSQL transports

### 0.1.9 — 2016-10-08
- Fix issue with deleting messages with errors for SQL Server, SQLite, PostgreSQL transports

### 0.1.8 — 2016-09-24
- Refactor default task scheduler to allow easier extension

### 0.1.7 — 2016-08-16
- Fix issue with PostgreSQL transport returning wrong message body
- Update to msgpack.cli 8.0 for Redis transport

### 0.1.6 — 2016-08-12
- PostgreSQL transport

### 0.1.5 — 2016-08-04
- Recurring job scheduler
- Metrics for LINQ serialization, compiling, and execution

### 0.1.4 — 2016-06-22
- Minor refactor to poison message handling
- Redis-on-Windows integration tests
- Refactor `IConnectionInformation` to be immutable
- Send LINQ expressions as queue items
- Fix scope issue with scheduler and multiple consumer queues

### 0.1.3 — 2016-02-18
- Fix formatting issue with poison message exception
- Fix formatting issue with user/system exception
- Don't run monitor delegates if queue is shutting down
- SQLite transport

### 0.1.2 — 2015-11-22
- Fix issue with removing SQL Server queues
- Fix issue with message expiration module running even if transport doesn't support expiration

### 0.1.0 — 2015-11-03
- Initial release to GitHub


---
