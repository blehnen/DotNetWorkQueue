### 0.9.1 — ????
- **Breaking Change** — Replace `App.Metrics` with built-in `System.Diagnostics.Metrics`; `DotNetWorkQueue.AppMetrics` package is no longer needed. Users should configure OpenTelemetry.Metrics exporters in their host to collect metrics.
- **Breaking Change** — Remove `SamplingTypes` enum from `IMetrics.Histogram()` and `IMetrics.Timer()` methods
- **Breaking Change** — Replace `dynamic CollectedMetrics` property with typed `MetricsSnapshot GetCollectedMetrics()` on `IMetrics`
- Migrate test framework from xUnit to MSTest
- Fix 10 missing `.ConfigureAwait(false)` calls in Trace Decorator classes across all transports
- Add Blazor Server dashboard UI (MudBlazor) for monitoring and managing queues
- Add optional login page for dashboard UI with SHA256 hashed password (`DashboardAuth` config)
- Dashboard.api
  - Fix SQL Server 404 on message detail for error messages (fallback to MetaDataErrors table)
  - Fix SQL Server requeue failing with NULL ExpirationTime (copy all columns from MetaDataErrors)
  - Fix PostgreSQL/SQLite requeue leaving wrong status after INSERT (use ON CONFLICT DO NOTHING + UPDATE)
  - Fix Redis requeue not clearing error tracking in serialized metadata
  - Fix Redis delayed/scheduled column not populating in message list
  - Fix LiteDB message body/headers/update queries failing with BsonMapper error (use FindById)
  - Fix dashboard body type showing interceptor name instead of actual message type
  - Add API key authentication for dashboard API (`DashboardOptions.ApiKey` + `X-Api-Key` header)
  - Add Swagger API key support when API key is enabled
  - Add bulk operations: Requeue All Errors, Reset All Stale, Delete All Errors
  - Add delayed message visibility (Scheduled column + Delayed chip)cv
  - Add two-click delete confirmation for single-record deletes
  - Add edit body support for messages in Error status

### 0.9.0 — 2026‑03‑04
- Add dashboard.api for viewing and modifiing messages in transports
- Switch from Polly V7 to Polly V8

### 0.8.1 — 2026‑02‑22
- Use Claude code to find and fix various long‑standing race conditions  
- Use Claude code to fix issue with multiple heartbeat schedulers that were inside the same process

### 0.8.0 — 2026‑01‑05
- Add .NET 10 as target  
- Remove frameworks that are out of support  
- **Breaking Change** — SQL client has been changed from `System.Data.SqlClient` to `Microsoft.Data.SqlClient`; this may affect your SQL Server connection strings if you're using the SQL Server transport

### 0.7.6 — 2024‑02‑02
- Remove connection objects from DataStore when queue is complete

### 0.7.5 — 2024‑01‑09
- Only verify internal container setup in debug mode, as it pins the memory it uses

### 0.7.4 — 2024‑01‑08
- Add test for scheduler creation with memory queue  
- Move two logging messages from info → debug  
- Add net8.0 as target  
- Remove queue param for workgroups, as it was no longer being used

### 0.7.3 — 2023‑11‑28
- Error notification will not happen if a rollback notification is being performed

### 0.7.2 — 2023‑11‑28
- Add notification of queue events to ConsumerQueues

### 0.7.1 — 2023‑11‑21
- Add property to obtain creation script from `IQueueCreation`. Supported by SQL Server, SQLite, and PostgreSQL.

### 0.7.0 — 2023‑10‑26
- Fix retry logic for SQLite commands; changes in `System.Data.Sqlite` required changes in transport  
- Switch to `DecorrelatedJitterBackoffV2` for SQL Server, PostgreSQL, and SQLite retries

### 0.6.9 — 2023‑10‑25
- Update various packages to latest versions  
- Remove `OpenTelemetry.Exporter.Jaeger` and replace with `OpenTelemetry.Exporter.OpenTelemetryProtocol`  
- Remove .NET 5.0 as a supported version

### 0.6.8 — 2022‑07‑19
- Update Npgsql  
- Add initial admin interface

### 0.6.7 — 2022‑06‑30
- Update various packages to latest versions

### 0.6.6 — 2022‑04‑29
- Fix issue with custom default constraints in SQL Server transport

### 0.6.5 — 2022‑02‑06
- Relational database transports now allow additional columns to be used as part of the dequeue

### 0.6.4 — 2022‑01‑12
- `ILogger` will now be created using the queue name for the category

### 0.6.3 — 2022‑01‑11
- Remove Polly Bulkhead; does not correctly work with our task‑limited scheduler  
- Remove `MaxQueue` feature from async processing, as it depended on Polly Bulkheads  
- Switch to `ILogger` from `Microsoft.Extensions.Logging.Abstractions`

### 0.6.2 — 2021‑12‑19
- Add .NET 6.0 as a target

### 0.6.1 — 2021‑09‑28
- Producer will throw an exception on a non‑public class used as a message due to internal delegate handling limitations

### 0.6.0 — 2021‑09‑07
- Switch from https://opentracing.io/ to https://opentelemetry.io/  
  **Breaking Change** — OpenTracing always added an entry to headers; OpenTelemetry only adds entries if enabled. Queues must be empty before updating.

### 0.5.4 — 2021‑05‑19
- Fix error with adding items to a memory queue that has started shutdown  
- Asking for list of error messages should not throw if transport fails; added flag to indicate if errors are loaded

### 0.5.3 — 2021‑05‑18
- Fix performance issue with in‑memory queues

### 0.5.2 — 2021‑04‑18
- LiteDB transport now supports direct and memory connections; all connections must be made in the same process

### 0.5.1 — 2021‑04‑00
- Add LiteDB transport  
- Add .NET 5 as a target; many references do not yet support 5.0

### 0.5.0 — 2020‑12‑08
- Change how connections are set up; **breaking change** to support generic connection settings not expressible in connection strings  
- Add .NET 4.8 as a target  
- Add .NET Standard 2.0 as a target for SQLite transport; Microsoft SQLite transport deprecated  
- SQL Server transport now supports creating queues in schemas other than `dbo`

### 0.4.6 — 2020‑09‑02
- Redis transport: re‑cache LUA scripts when no longer in cache; fixes issue with server restarts

### 0.4.5 — 2020‑02‑28
- Make previous error types and count available to message processing  
- Consumer queues now remove errors by default after 30 days; configurable

### 0.4.4 — 2019‑12‑23
- Fix issue with SQL Server transport and heartbeat reset

### 0.4.3 — 2019‑10‑29
- Fix issue with registration of message rollback

### 0.4.2 — 2019‑10‑29
- Add target for .NET 4.6.1  
- Upgrade packages to latest versions

### 0.4.1 — 2019‑06‑08
- Fix issue with retry policies using seconds instead of milliseconds

### 0.4.0 — 2019‑06‑02
- Remove RPC  
- Implement OpenTracing https://opentracing.io/  
- Fix message interception

### 0.3.1 — 2019‑04‑26
- Correct versioning for NuGet publish

### 0.3.0 — 2019‑04‑26
- All modules now target .NET 4.7.2 and .NET Standard 2.0  
- **Breaking Change** — changes to metrics interface to switch to AppMetrics  
- Deprecated Metrics.NET  
- Added `DotNetWorkQueue.AppMetrics` as replacement for `DotNetWorkQueue.Metrics.Net`

### 0.2.1 — 2017‑09‑30
- Refactoring to better share logic between transports  
- **Breaking Change** — fixed various spelling mistakes affecting public signatures  
- **Breaking Change** — fixed typo with internal Redis property; queues should be drained before upgrading  
- **Breaking Change** — replaced SmartThreadPool with `Task.StartNew` and Polly Bulkheads; removed related configuration properties  
- Heartbeat workers now use internal job scheduler backed by in‑memory queue  
- **Breaking Change** — heartbeat configuration now uses Schyntax format instead of timespan  
- Added new SQLite transport using Microsoft driver

### 0.1.10 — 2017‑03‑19
- Add route support to SQL Server, SQLite, Redis, and PostgreSQL transports

### 0.1.9 — 2016‑10‑08
- Fix issue with deleting messages with errors for SQL Server, SQLite, PostgreSQL transports

### 0.1.8 — 2016‑09‑24
- Refactor default task scheduler to allow easier extension

### 0.1.7 — 2016‑08‑16
- Fix issue with PostgreSQL transport returning wrong message body  
- Update to msgpack.cli 8.0 for Redis transport

### 0.1.6 — 2016‑08‑12
- Add PostgreSQL transport

### 0.1.5 — 2016‑08‑04
- Add re‑occurring job scheduler  
- Add metrics for LINQ serialization, compiling, and execution

### 0.1.4 — 2016‑06‑22
- Minor refactor to poison message handling  
- Add Redis‑on‑Windows integration tests  
- Refactor `IConnectionInformation` to be immutable  
- Add ability to send LINQ expressions as queue items  
- Fix scope issue with scheduler and multiple consumer queues

### 0.1.3 — 2016‑02‑18
- Fix formatting issue with poison message exception  
- Fix formatting issue with user/system exception  
- Don't run monitor delegates if queue is shutting down  
- Add SQLite transport

### 0.1.2 — 2015‑11‑22
- Fix issue with removing SQL Server queues  
- Fix issue with message expiration module running even if transport doesn't support expiration

### 0.1.0 — 2015‑11‑03
- Initial release to GitHub

---