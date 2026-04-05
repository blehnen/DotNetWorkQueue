# DotNetWorkQueue — Deep Codebase Research

Comprehensive analysis of the DotNetWorkQueue codebase covering architecture, data models, transport schemas, the Dashboard API, handler implementations, DI container wiring, and integration test infrastructure. Updated after completion of Dashboard Phases 1–6 and Memory transport dashboard integration tests.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Core Abstractions & Interfaces](#2-core-abstractions--interfaces)
3. [Message Lifecycle & Statuses](#3-message-lifecycle--statuses)
4. [Transport Layer — Schema Reference](#4-transport-layer--schema-reference)
5. [Legacy Admin API](#5-legacy-admin-api)
6. [Dashboard API Project](#6-dashboard-api-project)
7. [Dashboard Handler Implementations](#7-dashboard-handler-implementations)
8. [Container & Factory Pattern](#8-container--factory-pattern)
9. [Job Scheduler System](#9-job-scheduler-system)
10. [Configuration & Feature Flags](#10-configuration--feature-flags)
11. [Heartbeat System](#11-heartbeat-system)
12. [Error Handling & Retry](#12-error-handling--retry)
13. [Observability (Tracing & Metrics)](#13-observability-tracing--metrics)
14. [Message Interceptors](#14-message-interceptors)
15. [Transport Feature Matrix](#15-transport-feature-matrix)
16. [Test Infrastructure & Coverage](#16-test-infrastructure--coverage)
17. [Integration Test Patterns](#17-integration-test-patterns)
18. [Dashboard Integration Test Strategy](#18-dashboard-integration-test-strategy)

---

## 1. Architecture Overview

DotNetWorkQueue is a layered producer/distributed consumer library:

```
┌──────────────────────────────────────────────────────────────┐
│  Application Code (Producers, Consumers, Schedulers)          │
├──────────────────────────────────────────────────────────────┤
│  Dashboard.Api (ASP.NET Core library)                         │
│  - Controllers, Service layer, Configuration, Models          │
│  - Resolves handlers from admin containers per queue          │
├──────────────────────────────────────────────────────────────┤
│  Core Library (DotNetWorkQueue)                               │
│  - Interfaces, Configuration, Queue implementations           │
│  - IoC (SimpleInjector), Policies (Polly)                    │
│  - JobScheduler, HeartBeat, Tracing, Metrics                 │
│  - Memory transport (in-core, no external project)           │
├──────────────────────────────────────────────────────────────┤
│  Transport.Shared                                             │
│  - Command/Query pattern (ICommandHandler, IQueryHandler)     │
│  - Dashboard query/command types (12 queries, 5 commands)     │
│  - Dashboard DTO types (7 result classes)                     │
├──────────────────────────────────────────────────────────────┤
│  Transport.RelationalDatabase                                 │
│  - SQL-specific abstractions, TableNameHelper                 │
│  - Shared query/command handlers for all SQL transports       │
│  - Dashboard handlers (12 read + 5 write) with SQL prepare   │
├────────┬──────────┬────────┬─────────┬─────────┬────────────┤
│SqlServer│PostgreSQL│ SQLite │  Redis  │ LiteDB  │  Memory    │
│  (SQL) │  (SQL)   │ (SQL)  │ (Lua)   │ (LINQ)  │ (static)   │
└────────┴──────────┴────────┴─────────┴─────────┴────────────┘
```

**Key design principles:**
- Generic containers parameterized by `TTransportInit` (e.g., `QueueContainer<SqlServerMessageQueueInit>`)
- Command/Query separation for all data access (`ICommandHandler<T>`, `IQueryHandler<T,TR>`, `IQueryHandlerAsync<T,TR>`)
- SimpleInjector IoC with heavy use of decorator pattern
- Multi-targeting: .NET 10.0, .NET 8.0, .NET Framework 4.8, .NET Standard 2.0
- Conditional compilation: `NETFULL` for Framework-specific code (dynamic LINQ, thread abort)
- Dashboard types centralized in `Transport.Shared`; all message IDs are `string` at the API boundary

---

## 2. Core Abstractions & Interfaces

### Queue Types

| Interface | Purpose |
|---|---|
| `IProducerQueue<T>` | Send POCO messages of type T |
| `IProducerMethodQueue` | Send compiled LINQ expressions / delegates |
| `IConsumerQueue` | Synchronous consumer (dedicated worker threads) |
| `IConsumerQueueScheduler` | Async consumer (SmartThreadPool-based task scheduler) |
| `IConsumerMethodQueue` | Consume LINQ expression messages (sync) |
| `IConsumerMethodQueueScheduler` | Consume LINQ expression messages (async) |

### Message Types

| Interface | Purpose |
|---|---|
| `IMessage<T>` | Outgoing message wrapper (Body + Headers) |
| `IReceivedMessage<T>` | Incoming message (Body + MessageId + CorrelationId + PreviousErrors + Headers) |
| `IMessageId` | Transport-specific message identifier (wraps `ISetting` with `.Id.Value`) |
| `ICorrelationId` | Correlation tracking across systems |
| `IAdditionalMessageData` | Metadata attached at send time (expiration, delay, route, headers, custom columns) |
| `IQueueOutputMessage` | Send result (HasError, SendingException, MessageId, CorrelationId) |

### Connection & Configuration

| Class/Interface | Purpose |
|---|---|
| `QueueConnection` | Encapsulates queue name + connection string |
| `IConnectionInformation` | Transport's view of connection details |
| `QueueProducerConfiguration` | Producer-side settings |
| `QueueConsumerConfiguration` | Consumer-side settings (WorkerCount, HeartBeat, Expiration, RetryDelay) |
| `ITransportInit` | Transport registration entry point (RegisterImplementations, SetDefaultsIfNeeded, SuppressWarningsIfNeeded, IsRelationalTransport) |

### Transport Init Hierarchy

```
TransportInitDuplex (abstract, IsRelationalTransport => false)
  -> TransportMessageQueueSharedInit (concrete)
       -> SqlServerMessageQueueInit (IsRelationalTransport => true)
       -> PostgreSQLMessageQueueInit (IsRelationalTransport => true)
       -> SqLiteMessageQueueSharedInit (IsRelationalTransport => true)
            -> SqLiteMessageQueueInit
       -> RedisQueueInit (inherits false)
       -> LiteDbMessageQueueInit (inherits false)
  -> MemoryMessageQueueInit (standalone)
       -> MemoryDashboardInit (adds dashboard handlers)
```

`IsRelationalTransport` is used by `DashboardService.GetJobsAsync()` to decide whether to query the shared DNWQJobs table directly (relational) or skip it.

---

## 3. Message Lifecycle & Statuses

### QueueStatuses Enum

```
NotQueued = -1   (Item not found in transport)
Waiting = 0      (Ready to process)
Processing = 1   (Currently being processed by a worker)
Error = 2        (Failed during processing — moved to MetaDataErrors)
Processed = 3    (Successfully completed, will not be requeued)
```

### Message Flow

```
Producer.Send() ──► Queue table (Body, Headers)
                ──► MetaData table (Status=Waiting, QueuedDateTime, CorrelationID, etc.)
                         │
                         ▼
Consumer polls MetaData ──► Status = Processing ──► Worker processes message
                                                        │
                                    ┌───────────────────┼───────────────────┐
                                    ▼                   ▼                   ▼
                               Success              Retryable           Fatal Error
                            Status=Processed      Error (retry)     Move to ErrorQueue
                                                  Status=Waiting    MetaDataErrors table
                                                  (after delay)     ErrorTracking table
```

### Table Relationships (Relational Transports)

For a queue named "MyQueue":

| Table | Purpose | Always Created |
|---|---|---|
| `MyQueue` | Message body + headers | Yes |
| `MyQueueMetaData` | Status, timestamps, routing, heartbeat | Yes |
| `MyQueueStatus` | Separate status tracking | Only if EnableStatusTable |
| `MyQueueMetaDataConfiguration` | Serialized queue configuration | Yes |
| `MyQueueErrorTracking` | Exception type + retry count per message | Yes |
| `MyQueueMetaDataErrors` | Copy of metadata for failed messages + exception details | Yes |
| `DNWQJobs` | Job scheduler metadata (shared across ALL queues) | If job scheduler used |

---

## 4. Transport Layer — Schema Reference

### 4.1 SQL Server

**Queue Table:** `QueueID` (BigInt PK Identity), `Body` (VarBinary(max)), `Headers` (VarBinary(max))

**MetaData Table:** `QueueID` (BigInt PK FK), `CorrelationID` (UniqueIdentifier), `QueuedDateTime` (DateTime), plus conditional: `Status` (Int), `Priority` (TinyInt), `QueueProcessTime` (DateTime), `HeartBeat` (DateTime nullable), `ExpirationTime` (DateTime), `Route` (Varchar 255)

**ErrorTracking:** `ErrorTrackingID` (BigInt PK), `QueueID` (BigInt), `ExceptionType` (Varchar 500), `RetryCount` (Int)

**MetaDataErrors:** All MetaData columns plus `LastException` (Varchar(max)), `LastExceptionDate` (DateTime)

**Dashboard SQL specifics:** Uses `WITH (NOLOCK)`, `OFFSET/FETCH` pagination, `DATEDIFF(SECOND, HeartBeat, GETUTCDATE())` for stale detection, `count_big(*)`, `CAST(SUM(...) AS BIGINT)`.

### 4.2 PostgreSQL

Same logical schema. Differences: `Bytea` for Body/Headers, `Uuid` for CorrelationID, `BigInt` (Unix ms) for HeartBeat/ExpirationTime/QueueProcessTime. Dashboard SQL uses `LIMIT/OFFSET` pagination, `HeartBeat < @ThresholdTicks` for stale detection, `count(*)`.

### 4.3 SQLite

Same logical schema. Differences: `Integer` for QueueID, `Blob` for Body/Headers, `Text` for CorrelationID, `Integer` (Unix ms) for timestamps. Dashboard SQL identical to PostgreSQL. DateTimeOffset in DNWQJobs stored as "o" (round-trip) format string.

### 4.4 Redis

No relational schema. Key Redis data structures:

| Key Pattern | Type | Purpose |
|---|---|---|
| `{QueueName}_}Pending` | Sorted Set | Pending message IDs |
| `{QueueName}_}Working` | Sorted Set | Processing messages (score = heartbeat timestamp) |
| `{QueueName}_}Values` | Hash | MessageID → serialized body |
| `{QueueName}_}Headers` | Hash | MessageID → serialized headers |
| `{QueueName}_}MetaData` | Hash | Message metadata |
| `{QueueName}_}Status` | Hash | Message status |
| `{QueueName}_}Error` | List | Messages in error state |
| `{QueueName}_}ErrorTime` | Hash | MessageID → error timestamp |
| `{QueueName}_}Delayed` | Sorted Set | Delayed messages |
| `{QueueName}_}Expiration` | Sorted Set | Expiring messages |
| `{QueueName}_}Route` | Hash | Route mappings |
| `{QueueName}_}JobEvent` | Hash | Job event times |

**Dashboard-specific:** 4 Lua scripts for atomic write operations. Listing is O(n) via HGETALL (known limitation — no server-side paging for Hash). Detail endpoint does per-message lookups across sorted sets for HeartBeat, ExpirationTime, QueueProcessTime, Route, CorrelationId.

### 4.5 LiteDB

Document database with collections. Key collections: QueueTable (Id int, Body byte[], Headers byte[]), MetaDataTable (QueueId int, Status enum, CorrelationId Guid, QueuedDateTime DateTime, optional fields), StatusTable, ErrorTrackingTable, MetaDataErrorsTable, ConfigurationTable, JobsTable.

Dashboard handlers use `int.Parse(messageId)` for ID conversion. Requeue operation creates a NEW MetaData record (moves between collections) rather than updating in-place.

### 4.6 Memory (In-Process)

Static `ConcurrentDictionary` collections keyed by `IConnectionInformation`:

| Collection | Purpose |
|---|---|
| `Queues` | `BlockingCollection<Guid>` — FIFO queue of message IDs |
| `QueueData` | `ConcurrentDictionary<Guid, QueueItem>` — waiting messages |
| `QueueWorking` | Same — messages being processed |
| `Jobs` | `ConcurrentDictionary<string, Guid>` — job→message mapping |
| `ErrorCounts` / `DequeueCounts` | `IncrementWrapper` — count-only tracking |

**QueueItem:** `Guid Id`, `dynamic Body`, `IDictionary<string,object> Headers`, `DateTime QueuedDateTime`, `Guid CorrelationId`, `DateTimeOffset JobEventTime`, `DateTimeOffset JobScheduledTime`, `string JobName`

**Limitations:** No persistence, no heartbeat, no message expiration, no routing, no priority, no rollback, no error detail storage (count only), stores live objects (not serialized bytes for body).

---

## 5. Legacy Admin API

### IAdminFunctions Interface

```csharp
public interface IAdminFunctions
{
    long? Count(QueueStatusAdmin? status);  // Waiting=0, Processing=1
}
```

Each transport implements this. The Dashboard API does NOT use `IAdminFunctions` — it uses its own `IQueryHandlerAsync`-based handlers instead.

### IAdminApi Interface

```csharp
public interface IAdminApi : IDisposable, IIsDisposed
{
    AdminApiConfiguration Configuration { get; }
    IReadOnlyDictionary<Guid, Tuple<IQueueContainer, QueueConnection>> Connections { get; }
    Guid AddQueueConnection(IQueueContainer container, QueueConnection connection);
    long? Count(Guid id, QueueStatusAdmin? status);
}
```

**Limited to count-only operations.** The Dashboard API supersedes this with full read/write capability.

---

## 6. Dashboard API Project

**Project:** `DotNetWorkQueue.Dashboard.Api` — ASP.NET Core class library (net8.0 + net10.0)
**References:** `DotNetWorkQueue`, `DotNetWorkQueue.Transport.RelationalDatabase`
**Does NOT reference** specific transports (SqlServer, Redis, etc.) — resolved at runtime via generic `QueueContainer<TTransportInit>`.

### 6.1 Entry Point & Configuration

Host applications integrate via two extension methods in `DashboardExtensions.cs`:

```csharp
// Register services
services.AddDotNetWorkQueueDashboard(opts =>
{
    opts.AddConnection<SqlServerMessageQueueInit>("Server=...", conn =>
    {
        conn.DisplayName = "Production SQL Server";
        conn.AddQueue("MyQueue1");
        conn.AddQueue("MyQueue2", container => { /* register interceptors */ });
    });
    opts.AddConnection<RedisQueueInit>("redis:6379", conn =>
    {
        conn.DisplayName = "Redis Cluster";
        conn.AddQueue("RedisQueue1");
    });
});

// Add middleware
app.UseDotNetWorkQueueDashboard();
```

**DashboardOptions:** `EnableSwagger` (default true), `AuthorizationPolicy` (string, null = open), `AddConnection<TTransportInit>(connectionString, configureQueues)` with optional `containerConfig` delegate.

**DashboardConnectionOptions:** `DisplayName`, `AddQueue(queueName)`, `AddQueue(queueName, interceptorConfig)`.

### 6.2 Controllers & Endpoints (17 total)

#### ConnectionsController (`api/v1/dashboard/connections`)

| Method | Route | Returns |
|---|---|---|
| `GET` | `/` | `IReadOnlyList<ConnectionResponse>` |
| `GET` | `/{connectionId}/queues` | `IReadOnlyList<QueueInfoResponse>` |
| `GET` | `/{connectionId}/jobs` | `IReadOnlyList<JobResponse>` |

#### QueuesController (`api/v1/dashboard/queues`)

| Method | Route | Returns |
|---|---|---|
| `GET` | `/{queueId}/status` | `QueueStatusResponse` |
| `GET` | `/{queueId}/features` | `QueueFeaturesResponse` |
| `GET` | `/{queueId}/messages` | `PagedResponse<MessageResponse>` |
| `GET` | `/{queueId}/messages/count` | `long` |
| `GET` | `/{queueId}/messages/{messageId}` | `MessageResponse` |
| `GET` | `/{queueId}/messages/stale` | `PagedResponse<MessageResponse>` |
| `GET` | `/{queueId}/errors` | `PagedResponse<ErrorMessageResponse>` |
| `GET` | `/{queueId}/messages/{messageId}/retries` | `IReadOnlyList<ErrorRetryResponse>` |
| `GET` | `/{queueId}/configuration` | `ConfigurationResponse` |
| `GET` | `/{queueId}/messages/{messageId}/body` | `MessageBodyResponse` |
| `GET` | `/{queueId}/messages/{messageId}/headers` | `MessageHeadersResponse` |
| `DELETE` | `/{queueId}/messages/{messageId}` | 204/404 |
| `DELETE` | `/{queueId}/errors` | `DeleteAllResponse` |
| `POST` | `/{queueId}/messages/{messageId}/requeue` | 204/404 |
| `POST` | `/{queueId}/messages/{messageId}/reset` | 204/404 |
| `PUT` | `/{queueId}/messages/{messageId}/body` | 204/400/404/409 |

### 6.3 Service Layer (DashboardService)

`DashboardService` bridges controllers to transport handlers. For every operation it:
1. Gets `IContainer` via `_dashboardApi.GetQueueContainer(queueId)`
2. Resolves appropriate `IQueryHandlerAsync<TQuery, TResult>` or `ICommandHandlerWithOutput<TCommand, long>`
3. Executes handler, maps result to API response model

**Special methods:**
- **GetFeatures**: Resolves `IQueueCreation`, reads `BaseTransportOptions` for feature flags. No query handler needed.
- **GetJobsByConnectionAsync**: Uses first queue on connection. For relational transports checks `IsRelationalTransport` and verifies DNWQJobs table exists before querying.
- **GetMessageBodyAsync**: Complex 5-step decode: load raw bytes → deserialize headers → extract `MessageInterceptorGraph` → decode body through interceptor pipeline → resolve `Queue-MessageBodyType` header for typed re-serialization.
- **EditMessageBodyAsync**: 8-step process: load raw → decode headers → resolve type (header first, fallback to runtime type) → reject if Processing → deserialize caller's JSON to resolved type → re-encode through pipeline → update interceptor graph → persist via `DashboardUpdateMessageBodyCommand`.
- **SanitizeHeaders**: Converts header values to JSON-safe types; handles `Guid`, `MessageInterceptorsGraph`, `ValueTypeWrapper<T>`, transport-specific correlation ID types (reflects `Guid Id` property).

### 6.4 Container Lifecycle (DashboardApi)

`DashboardApi` (internal, singleton) manages container creation:

**Initialization:** For each `DashboardConnectionRegistration`:
- Creates `QueueContainer<TTransportInit>` via reflection (`Activator.CreateInstance(typeof(QueueContainer<>).MakeGenericType(transportInitType))`)
- Determines `IsRelationalTransport` by instantiating the transport init
- For each queue: generates GUID queue ID, stores mapping to container + connection

**Container creation (lazy, cached per queue):**
```csharp
var container = queueContainer.CreateAdminContainer(queueConnection, interceptorConfig);
_adminContainers.TryAdd(queueId, container);
```

One `QueueContainer<TTransportInit>` per connection, one `IContainer` per queue (lazy on first access).

### 6.5 Exception Handling

`DashboardExceptionFilter` maps exceptions to HTTP status codes:
- `ObjectDisposedException` → 503 Service Unavailable
- `InvalidOperationException` → 404 Not Found
- `NotSupportedException` → 501 Not Implemented

### 6.6 File Inventory (28 source files)

```
DotNetWorkQueue.Dashboard.Api/
  DotNetWorkQueue.Dashboard.Api.csproj
  IDashboardApi.cs, DashboardApi.cs (internal)
  DashboardExtensions.cs
  DashboardConnectionInfo.cs, DashboardQueueInfo.cs
  InternalsVisibleForTests.cs
  Configuration/
    DashboardOptions.cs, DashboardConnectionOptions.cs
    DashboardQueueOptions.cs, DashboardConnectionRegistration.cs (internal)
  Controllers/
    ConnectionsController.cs, QueuesController.cs
  Middleware/
    DashboardExceptionFilter.cs (internal)
  Models/ (15 files)
    ConnectionResponse, QueueInfoResponse, QueueStatusResponse,
    QueueFeaturesResponse, MessageResponse, PagedResponse<T>,
    ErrorMessageResponse, ErrorRetryResponse, ConfigurationResponse,
    JobResponse, MessageBodyResponse, MessageHeadersResponse,
    DeleteAllResponse, EditMessageBodyRequest, EditMessageBodyResult (enum)
  Services/
    IDashboardService.cs, DashboardService.cs (internal)
```

---

## 7. Dashboard Handler Implementations

### 7.1 Shared Types (Transport.Shared)

All dashboard types live in `DotNetWorkQueue.Transport.Shared.Basic`.

**Query types (12)** in `Basic/Query/`:

| Class | Parameters | Returns |
|---|---|---|
| `GetDashboardStatusCountsQuery` | (none) | `DashboardStatusCounts` |
| `GetDashboardMessagesQuery` | `pageIndex, pageSize, statusFilter?` | `IReadOnlyList<DashboardMessage>` |
| `GetDashboardMessageCountQuery` | `statusFilter?` | `long` |
| `GetDashboardMessageDetailQuery` | `messageId` (string) | `DashboardMessage` |
| `GetDashboardStaleMessagesQuery` | `thresholdSeconds, pageIndex, pageSize` | `IReadOnlyList<DashboardMessage>` |
| `GetDashboardErrorMessagesQuery` | `pageIndex, pageSize` | `IReadOnlyList<DashboardErrorMessage>` |
| `GetDashboardErrorMessageCountQuery` | (none) | `long` |
| `GetDashboardErrorRetriesQuery` | `messageId` (string) | `IReadOnlyList<DashboardErrorRetry>` |
| `GetDashboardConfigurationQuery` | (none) | `byte[]` |
| `GetDashboardJobsQuery` | (none) | `IReadOnlyList<DashboardJob>` |
| `GetDashboardMessageBodyQuery` | `messageId` (string) | `DashboardMessageBody` |
| `GetDashboardMessageHeadersQuery` | `messageId` (string) | `DashboardMessageHeaders` |

**Command types (5)** in `Basic/Command/`:

| Class | Parameters |
|---|---|
| `DashboardDeleteMessageCommand` | `messageId` (string) |
| `DashboardDeleteAllErrorMessagesCommand` | (none) |
| `DashboardRequeueErrorMessageCommand` | `messageId` (string) |
| `DashboardResetStaleMessageCommand` | `messageId` (string) |
| `DashboardUpdateMessageBodyCommand` | `messageId, body (byte[]), headers (byte[])` |

**Result types (7):**

| Class | Key Properties |
|---|---|
| `DashboardStatusCounts` | `long Waiting, Processing, Error, Total` |
| `DashboardMessage` | `string QueueId, DateTimeOffset? QueuedDateTime, string CorrelationId, int? Status, int? Priority, DateTimeOffset? QueueProcessTime, DateTimeOffset? HeartBeat, DateTimeOffset? ExpirationTime, string Route` |
| `DashboardErrorMessage` | DashboardMessage fields + `long Id, string LastException, DateTimeOffset? LastExceptionDate` |
| `DashboardErrorRetry` | `long ErrorTrackingId, string QueueId, string ExceptionType, int RetryCount` |
| `DashboardJob` | `string JobName, DateTimeOffset? JobEventTime, DateTimeOffset? JobScheduledTime` |
| `DashboardMessageBody` | `byte[] Body, byte[] Headers` |
| `DashboardMessageHeaders` | `byte[] Headers` |

**Handler interfaces:**
- `IQueryHandlerAsync<TQuery, TResult>` — `Task<TResult> HandleAsync(TQuery query)`
- `ICommandHandlerWithOutput<TCommand, TOutput>` — `TOutput Handle(TCommand command)`

### 7.2 Relational Database Handlers (SqlServer, PostgreSQL, SQLite)

All 12 read handlers and 5 write handlers live in `Transport.RelationalDatabase/Basic/QueryHandler/` and `CommandHandler/`. Each has a matching prepare handler in `QueryPrepareHandler/`.

**Pattern:**
1. Constructor: `IDbConnectionFactory`, `IPrepareQueryHandler<TQuery, TResult>`, `IReadColumn`, optionally `ITransportOptionsFactory`
2. `HandleAsync()`: open connection → create command → `_prepareQuery.Handle(query, command, CommandStringTypes.GetDashboardXxx)` → execute reader → read columns
3. Both sync `IQueryHandler` and async `IQueryHandlerAsync` versions exist

**Shared helpers:**
- `DashboardMessageReader` (static): reads `DashboardMessage` from `IDataReader` with positional column indices; handles conditional columns based on `ITransportOptions`
- `DashboardDynamicColumnHelper` (static): `BuildDynamicColumns(ITransportOptions)` returns comma-prefixed string of enabled columns (e.g., `", Status, Priority, HeartBeat"`) for SQL template `{0}` placeholder

**Write command handlers:**
- `DashboardDeleteMessageCommand`: within transaction, deletes from MetaData, Queue, ErrorTracking, MetaDataErrors, optionally Status
- `DashboardDeleteAllErrorMessagesCommand`: 5 DELETE statements targeting all related tables WHERE Status=Error
- `DashboardRequeueErrorMessageCommand`: deletes MetaDataErrors + ErrorTracking, then UPDATEs MetaData SET Status=Waiting, HeartBeat=NULL WHERE Status=Error
- `DashboardResetStaleMessageCommand`: UPDATEs MetaData SET Status=Waiting, HeartBeat=NULL WHERE Status=Processing
- `DashboardUpdateMessageBodyCommand`: UPDATE Queue SET Body=@Body, Headers=@Headers

**SQL differences by transport:**

| Feature | SQL Server | PostgreSQL | SQLite |
|---|---|---|---|
| Read hints | `WITH (NOLOCK)` | none | none |
| Pagination | `OFFSET/FETCH` | `LIMIT/OFFSET` | `LIMIT/OFFSET` |
| Stale detection | `DATEDIFF(SECOND, HeartBeat, GETUTCDATE()) > @Threshold` | `HeartBeat < @ThresholdTicks` | `HeartBeat < @ThresholdTicks` |
| Count function | `count_big(*)` | `count(*)` | `count(*)` |
| Status sum cast | `CAST(SUM(...) AS BIGINT)` | plain SUM | plain SUM |

**DI registration:** `RelationalDatabaseMessageQueueInit.RegisterCommandsExplicit()` registers all handlers as singletons. SqlServer/PostgreSQL/SQLite init classes call this through their base classes. No separate DashboardInit class needed.

### 7.3 LiteDB Handlers

12 async query handlers + 5 command handlers in `Transport.LiteDB/Basic/QueryHandler/` and `CommandHandler/`.

**Pattern:** Constructor takes `LiteDbConnectionManager`, `TableNameHelper`, optionally `ILiteDbMessageQueueTransportOptionsFactory`. Uses LiteDB LINQ: `.Query().Where(...).Skip(...).Limit(...)`.

**Key behaviors:**
- Message IDs: `int.Parse(messageId)` conversion
- StatusCounts: counts from MetaData (Waiting, Processing) + MetaDataErrors (Error)
- Messages with statusFilter=2 (Error): queries MetaDataErrors table instead of MetaData
- MessageDetail: checks MetaData first, then MetaDataErrors for error messages
- **Requeue is a move operation**: creates NEW MetaData record from error record, deletes from MetaDataErrors + ErrorTracking (different from relational UPDATE-in-place)
- Timestamps: `new DateTimeOffset(record.DateTime, TimeSpan.Zero)` conversion

**DI registration:** Direct registrations in `LiteDbMessageQueueInit` (lines 172-208).

### 7.4 Redis Handlers

12 async query handlers + 5 command handlers in `Transport.Redis/Basic/QueryHandler/` and `CommandHandler/`.

**Pattern:** Constructor takes `IRedisConnection`, `RedisNames`, optionally `RedisHeaders`, `IInternalSerializer`, `ICompositeSerialization`.

**Key behaviors:**
- Message IDs used as-is (string)
- StatusCounts: `HashLength(MetaData)` for total, `SortedSetLength(Working)` for processing, `ListLength(Error)` for error. Waiting = total - processing - error (clamped to 0).
- **Messages listing is O(n):** `HashGetAll(MetaData)` gets ALL entries, then filters/pages client-side. Known limitation.
- **Messages listing only returns:** QueueId, Status, QueuedDateTime. Other fields require per-message lookups (deferred to detail endpoint).
- **MessageDetail does full lookups:** HeartBeat from Working sorted set score, ExpirationTime from Expiration score, QueueProcessTime from Delayed score, Route from Route hash, CorrelationId from Headers hash (deserialization)
- Configuration: always returns `null` (Redis has no config table)
- ErrorMessages: `LastException` always null (Redis stores retry counts only, no exception text)
- Jobs: only returns `JobEventTime` (from JobEvent hash), `JobScheduledTime` always null

**4 Lua scripts** for atomic write operations:
- `DashboardDeleteAllErrorMessagesLua`: LRANGE all error IDs, HDEL from metadata/values/headers/status, ZREM from errortime, DEL error list
- `DashboardRequeueErrorMessageLua`: LREM from error list, ZREM from errortime, HSET status=0, RPUSH to pending (route-aware)
- `DashboardResetStaleMessageLua`: ZSCORE working to verify, ZREM from working, HSET status=0, RPUSH to pending (route-aware)
- `DashboardUpdateMessageBodyLua`: HEXISTS check, HSET values + headers

**DI registration:** `RedisQueueInit` registers 4 Lua script classes as singletons (required for injection), then 12 query handlers + 5 command handlers.

### 7.5 Memory Handlers

12 async query handlers + 5 command handlers in `Transport.Memory/Basic/QueryHandler/` and `CommandHandler/`.

**Functional handlers** (return real data from `IDataStorage`):
- StatusCounts: `RecordCount` (waiting), `WorkingRecordCount` (processing), Error=0 always
- Messages: `GetWaitingMessages(skip, pageSize)` and `GetProcessingMessages()`
- MessageDetail: `FindMessage(Guid.Parse(messageId))`
- MessageBody: serializes live object to bytes via `ICompositeSerialization`
- MessageHeaders: serializes headers dict via `IInternalSerializer`
- DeleteMessage: `DeleteMessage(Guid.Parse(id))`, returns 1 or 0

**No-op handlers** (features not supported by memory transport):
- StaleMessages, ErrorMessages, ErrorMessageCount, ErrorRetries, Configuration → empty/null/0
- DeleteAllErrors, RequeueError, ResetStale, UpdateBody → returns 0

**DI registration:** `MemoryDashboardInit` extends `MemoryMessageQueueInit`, calls `base.RegisterImplementations()` then registers all 17 handlers. This is the ONLY transport with a separate DashboardInit class (because Memory transport is in the core library, not a separate project).

### 7.6 Handler Registration Summary

| Transport | Init Class | Dashboard Registration Location |
|---|---|---|
| SqlServer | `SqlServerMessageQueueInit` | `RelationalDatabaseMessageQueueInit.RegisterCommandsExplicit()` |
| PostgreSQL | `PostgreSQLMessageQueueInit` | Same (shared relational base) |
| SQLite | `SqLiteMessageQueueInit` | Same (shared relational base) |
| Redis | `RedisQueueInit` | Direct in `RedisQueueInit` (lines 162-204) |
| LiteDB | `LiteDbMessageQueueInit` | Direct in `LiteDbMessageQueueInit` (lines 172-208) |
| Memory | `MemoryDashboardInit` | Separate subclass of `MemoryMessageQueueInit` |

**Key implication for integration tests:** For relational/Redis/LiteDB transports, the standard init class already registers dashboard handlers. For Memory transport, you must use `MemoryDashboardInit` specifically.

---

## 8. Container & Factory Pattern

### Container Hierarchy

```
QueueCreationContainer<TTransportInit>
  └── GetQueueCreation<TQueue>(connection) → IQueueCreation
        ├── .CreateQueue() → QueueCreationResult
        ├── .RemoveQueue() → QueueRemoveResult
        ├── .Scope → ICreationScope
        └── .BaseTransportOptions → IBaseTransportOptions

QueueContainer<TTransportInit>
  ├── CreateProducer<T>(connection) → IProducerQueue<T>
  ├── CreateConsumer(connection) → IConsumerQueue
  ├── CreateAdminContainer(connection) → IContainer  ← Dashboard uses this
  ├── CreateAdminContainer(connection, Action<IContainer>) → IContainer
  ├── CreateAdminFunctions(connection) → IAdminFunctions
  └── CreateAdminApi() → IAdminApi
```

### IContainer Interface

The raw container returned by `CreateAdminContainer`. Key methods:
- `GetInstance<TService>()` — resolve registered service
- `Register<TService, TImpl>(LifeStyles)` — register implementation
- `RegisterNonScopedSingleton<T>(instance)` — register pre-existing instance (NOT disposed with container)
- `RegisterDecorator(...)` — decorator pattern

### CreateAdminContainer Flow

```csharp
public IContainer CreateAdminContainer(QueueConnection queueConnection,
    Action<IContainer> registerServiceInternal)
{
    var container = _createContainerInternal().Create(
        QueueContexts.Admin,           // context type
        _registerService,              // user overrides
        queueConnection,               // queue + connection
        _transportInit,                // e.g., SqlServerMessageQueueInit
        ConnectionTypes.Status,        // connection type
        registerServiceInternal,       // interceptor config
        _setOptions);
    Containers.Add(container);
    return container;
}
```

### Full DI Registration Order

1. **Core defaults** (`ComponentRegistration.RegisterDefaults`) — serializers, message factories, consumer/producer infrastructure
2. `AllowOverridingRegistrations = true`
3. **Transport registrations** (`register.RegisterImplementations()`) — transport services + dashboard handlers
4. **Internal overrides** (`registerServiceInternal`) — interceptor configuration
5. **User overrides** (`_registerService`) — shared CreationScope, custom serializers
6. `AllowOverridingRegistrations = false`
7. **Conditional fallbacks** (`RegisterFallbacks`)
8. Warning suppression + verification (DEBUG only)
9. **Default policies + transport defaults** (`SetDefaultsIfNeeded`)
10. User options (`setOptions`)

Transport registrations override core defaults; internal overrides override transport; user overrides override everything.

### CreationScope Pattern (Memory Transport)

`CreationScope` holds `ConcurrentBag<IClear>` (ContainedClears) and `ConcurrentBag<IDisposable>` (ContainedDisposables). `DataStorage` implements `IClear` and is added via `scope.AddScopedObject(holder)`.

To share data between containers (e.g., producer + admin):
```csharp
var scope = oCreation.Scope;  // from QueueCreationContainer
using var creator = new QueueContainer<MemoryDashboardInit>(
    serviceRegister => serviceRegister.RegisterNonScopedSingleton(scope));
```

### Transport Init Types

| Transport | Init Class | Queue Creation Class |
|---|---|---|
| SQL Server | `SqlServerMessageQueueInit` | `SqlServerMessageQueueCreation` |
| PostgreSQL | `PostgreSqlMessageQueueInit` | `PostgreSqlMessageQueueCreation` |
| SQLite | `SqLiteMessageQueueInit` | `SqLiteMessageQueueCreation` |
| Redis | `RedisQueueInit` | `RedisQueueCreation` |
| LiteDB | `LiteDbMessageQueueInit` | `LiteDbMessageQueueCreation` |
| Memory | `MemoryMessageQueueInit` / `MemoryDashboardInit` | `MessageQueueCreation` |

### Connection String Formats

| Transport | Format |
|---|---|
| SQL Server | `"Server=host;Database=db;user=user;password=pwd;"` |
| PostgreSQL | `"Server=host;Port=5432;Database=db;userid=user;"` |
| SQLite | `"Data Source=C:\path\file.db;Version=3;"` or `"FullUri=file:...?mode=memory&cache=shared"` |
| Redis | `"host:port,defaultDatabase=0,syncTimeout=15000"` |
| LiteDB | `"Filename=C:\path\file.db;Connection=shared;"` |
| Memory | Any string (used as key for static collections) |

---

## 9. Job Scheduler System

### Architecture

```
JobSchedulerContainer
  └── IJobScheduler
        ├── AddUpdateJob<TTransportInit, TJobQueueCreation>(name, connection, schedule, action)
        ├── GetAllJobs() → IEnumerable<IScheduledJob>
        ├── Start() / Stop()
        └── Uses Schyntax for schedule parsing (e.g., "sec(*)", "min(*/5)")
```

**DNWQJobs table is shared** across ALL queues in a database. Job names must be globally unique per database. Dashboard queries jobs at the connection level (not per-queue) using the first queue on the connection.

**Timestamp format varies by transport:** SQL Server uses DateTimeOffset; PostgreSQL/SQLite use BigInt (Unix ms) / "o" format string; Redis uses Hash with Unix ms values; LiteDB uses DateTime.

---

## 10. Configuration & Feature Flags

### Transport Options

Set at queue creation time, stored in Configuration table:

| Option | Effect | Dashboard Implication |
|---|---|---|
| `EnablePriority` | Priority column in MetaData | `DashboardMessage.Priority` populated |
| `EnableStatus` | Status column | Status-based filtering works |
| `EnableStatusTable` | Separate Status table | Delete/Requeue also update Status table |
| `EnableHeartBeat` | HeartBeat column | Stale message detection works |
| `EnableDelayedProcessing` | QueueProcessTime column | `DashboardMessage.QueueProcessTime` populated |
| `EnableMessageExpiration` | ExpirationTime column | `DashboardMessage.ExpirationTime` populated |
| `EnableRoute` | Route column | `DashboardMessage.Route` populated |

**Dashboard must read** feature flags via `IQueueCreation.BaseTransportOptions` to know which columns exist (relational schemas are dynamic).

**Configuration table serialization:** Uses `IInternalSerializer` (plain Newtonsoft.Json), NOT the interceptor pipeline. Dashboard can deserialize without encryption keys.

---

## 11. Heartbeat System

Workers send periodic heartbeat updates while processing. If a worker dies, the heartbeat monitor detects stale heartbeats and resets messages to Waiting.

- `HeartBeat.UpdateTime` — update frequency (Schyntax, e.g., `"sec(*%10)"`)
- `HeartBeat.MonitorTime` — monitor check frequency
- `HeartBeat.Time` — stale threshold

**Dashboard relevance:** The stale messages endpoint (`GetDashboardStaleMessages`) finds messages in Processing with heartbeats older than `thresholdSeconds`. The reset endpoint (`DashboardResetStaleMessage`) sets them back to Waiting.

**Fixed bug (v0.8.1):** `HeartBeatScheduler` used a fixed static queue name causing cross-consumer interference. Fixed to use per-instance `"HeartBeatWorkers-{Guid}"`.

---

## 12. Error Handling & Retry

### Retry Configuration

Per exception type with progressive delays:
```csharp
queue.Configuration.TransportConfiguration.RetryDelayBehavior.Add(
    typeof(InvalidDataException),
    new List<TimeSpan> { 3s, 6s, 9s });  // 3 retries
```

### Error Tables

- **ErrorTracking:** `(ErrorTrackingID, QueueID, ExceptionType, RetryCount)` — per message per exception type
- **MetaDataErrors:** Copy of MetaData + `LastException`, `LastExceptionDate` — snapshot on permanent failure

### Dashboard Write Operations for Errors

- `DashboardDeleteAllErrorMessages`: removes error records and associated queue/metadata entries
- `DashboardRequeueErrorMessage`: moves error message back to Waiting, clears error tracking
- **Redis limitation:** `LastException` text not stored (only retry counts)
- **Memory limitation:** Error count only, no error detail storage

---

## 13. Observability (Tracing & Metrics)

### OpenTelemetry Tracing

Activity source `"DotNetWorkQueue"` with spans for Send, Receive, Process. Context propagated via headers.

### App.Metrics Integration

`DotNetWorkQueue.AppMetrics` package. Counters for messages sent/received/processed/errors/retries. Registered via `container.RegisterMetrics()`.

### Polly Policies

`IPolicies` interface for resilience. `EnableChaos` for fault injection testing.

---

## 14. Message Interceptors

### How They Work

**Send:** `ASerializer.MessageToBytes()` serializes body, passes through `IMessageInterceptor` chain. Each interceptor's `BaseType` recorded in `MessageInterceptorsGraph` stored in headers.

**Receive:** `ASerializer.BytesToMessage()` reads graph from headers, applies interceptors in reverse order.

**Built-in interceptors:** `GZipMessageInterceptor`, `TripleDesMessageInterceptor`

### Dashboard Implications

- Graph in headers tells dashboard exactly which interceptors to use
- Built-in interceptors must be registered in dashboard's DI container
- Encryption keys needed per-queue if TripleDES used
- Per-queue `InterceptorConfiguration` delegate in dashboard config handles this
- **Configuration data is NOT intercepted** — uses plain `IInternalSerializer`

---

## 15. Transport Feature Matrix

| Feature | SQL Server | PostgreSQL | SQLite | Redis | LiteDB | Memory |
|---|---|---|---|---|---|---|
| Heartbeat | Yes | Yes | Yes | Yes | Yes | No |
| Priority | Yes | Yes | Yes | Yes (ordering) | Yes | No |
| Status Tracking | Yes | Yes | Yes | Yes | Yes | No |
| Delayed Processing | Yes | Yes | Yes | Yes | Yes | No |
| Message Routing | Yes | Yes | Yes | Yes | Yes | No |
| Message Expiration | Yes | Yes | Yes | Yes | Yes | No |
| Error Tracking | Full | Full | Full | Count only | Full | Count only |
| Error Exception Text | Yes | Yes | Yes | No | Yes | No |
| Configuration Storage | Yes | Yes | Yes | No | Yes | No |
| Job Scheduler | Yes | Yes | Yes | Yes | Yes | Yes |
| Persistence | Yes | Yes | Yes | Yes | Yes | No |
| Message Rollback | Yes | Yes | Yes | Yes | Yes | No |
| Dashboard Read Handlers | 12 (relational) | 12 (relational) | 12 (relational) | 12 (Redis) | 12 (LiteDB) | 12 (Memory) |
| Dashboard Write Handlers | 5 (relational) | 5 (relational) | 5 (relational) | 5 (Lua) | 5 (LiteDB) | 1 functional + 4 no-op |
| IsRelationalTransport | true | true | true | false | false | false |
| Message ID Type | long | long | long | string | int | Guid |
| Timestamp Storage | DateTime | BigInt (Unix ms) | Integer (Unix ms) | BigInt (Unix ms) | DateTime | DateTime |
| Body Storage | VarBinary(max) | Bytea | Blob | Hash value | byte[] | dynamic (live object) |

---

## 16. Test Infrastructure & Coverage

### Unit Test Projects

| Project | Dashboard Tests | Test Count | Test Depth |
|---|---|---|---|
| DotNetWorkQueue.Dashboard.Api.Tests | 6 files | ~84 | Full behavioral (mocked service/API) |
| DotNetWorkQueue.Transport.Memory.Tests | 17 files | ~34 | Constructor guard-clause only |
| DotNetWorkQueue.Transport.RelationalDatabase.Tests | 34 files | ~131 | Constructor + behavioral (DB mocked) |
| DotNetWorkQueue.Transport.LiteDb.Tests | 17 files | ~60 | Constructor guard-clause only |
| DotNetWorkQueue.Transport.Redis.Tests | 17 files | ~47 | Constructor guard-clause only |
| SqlServer/PostgreSQL/SQLite.Tests | 0 files | 0 | No dashboard tests |

### Dashboard.Api.Tests Patterns

**Controller tests:** Mock `IDashboardService`, create real controller, assert on `IActionResult` types (`OkObjectResult`, `NotFoundResult`, `NoContentResult`, `BadRequestObjectResult`, `ConflictObjectResult`). Verify service calls with `Received(1)`.

**Service tests:** Mock `IDashboardApi` + `IContainer` + handlers. Complex tests for body decode/edit use `SetupBodyDecodeContainer()` / `SetupEditBodyContainer()` helpers that wire up full serialization pipelines (`ICompositeSerialization`, `IInternalSerializer`, `IHeaders`, `IMessageFactory`).

### RelationalDatabase.Tests Patterns

**QueryHandler tests:** Mock `IDbConnectionFactory`, `DbConnection`, `DbCommand`, `DbDataReader`. Test both "has rows" and "no rows" paths. Verify column read indices.

**PrepareHandler tests:** Use `FakeCommandStringCache` (extends `CommandStringCache` with all dashboard `CommandStringTypes` populated). Verify `CommandText` is set and parameters are added with correct names/values.

**Test helpers:** `FakeCommandStringCache`, `DataParameterCollection` (wraps `List<IDbDataParameter>`).

### Integration Test Projects

| Project | Dashboard Tests | External Service Required |
|---|---|---|
| Memory.Integration.Tests | 27 tests | No |
| Memory.Linq.Integration.Tests | 0 | No |
| SqlServer.IntegrationTests | 0 | SQL Server |
| PostgreSQL.Integration.Tests | 0 | PostgreSQL |
| SQLite.Integration.Tests | 0 | SQLite (file-based) |
| Redis.IntegrationTests | 0 | Redis |
| LiteDb (no integration project) | N/A | N/A |

### Assertion Libraries

- Dashboard.Api.Tests, Memory.Integration.Tests: **FluentAssertions** (`.Should().Be()`)
- All other test projects: **xUnit native** (`Assert.Equal`, `Assert.Throws<>`)

---

## 17. Integration Test Patterns

### Overall Architecture

```
Transport-Specific Test (xUnit Theory/Fact)
    → Shared Implementation class (IntegrationTests.Shared)
        → Uses QueueCreationContainer + QueueContainer
```

Each transport test project provides: connection info, queue name generation, shared helpers (Verify, GenerateData, SetOptions, VerifyQueueCount).

### Memory Dashboard Integration Test Pattern (Established)

File: `DotNetWorkQueue.Transport.Memory.Integration.Tests/Dashboard/DashboardQueries.cs`

```csharp
private static void RunDashboardTest(int messageCount, int consumeCount,
    Action<IContainer> testAction)
{
    using (var connectionInfo = new IntegrationConnectionInfo())
    {
        var queueName = GenerateQueueName.Create();
        var connection = new QueueConnection(queueName, connectionInfo.ConnectionString);

        // 1. Create queue via QueueCreationContainer<MemoryDashboardInit>
        using (var queueCreator = new QueueCreationContainer<MemoryDashboardInit>())
        using (var oCreation = queueCreator.GetQueueCreation<MessageQueueCreation>(connection))
        {
            oCreation.CreateQueue();
            var scope = oCreation.Scope;

            // 2. Create QueueContainer with shared scope
            using (var creator = new QueueContainer<MemoryDashboardInit>(
                serviceRegister => serviceRegister.RegisterNonScopedSingleton(scope)))
            {
                // 3. Produce messages
                using (var producer = creator.CreateProducer<FakeMessage>(connection))
                    for (var i = 0; i < messageCount; i++)
                        producer.Send(new FakeMessage());

                // 4. Simulate consuming (waiting → processing)
                if (consumeCount > 0)
                {
                    var realScope = (CreationScope)scope;
                    realScope.ContainedClears.TryPeek(out var obj);
                    var dataStorage = (IDataStorage)obj;
                    for (var i = 0; i < consumeCount; i++)
                        dataStorage.GetNextMessage(null, TimeSpan.FromSeconds(1));
                }

                // 5. Create admin container and test
                using (var adminContainer = creator.CreateAdminContainer(connection))
                    testAction(adminContainer);
            }
            oCreation.RemoveQueue();
            scope.Dispose();
        }
    }
}
```

**Handler resolution from admin container:**
```csharp
// Read query (async):
var handler = container.GetInstance<IQueryHandlerAsync<
    GetDashboardStatusCountsQuery, DashboardStatusCounts>>();
var result = handler.HandleAsync(new GetDashboardStatusCountsQuery())
    .GetAwaiter().GetResult();

// Write command (sync):
var handler = container.GetInstance<ICommandHandlerWithOutput<
    DashboardDeleteMessageCommand, long>>();
var result = handler.Handle(new DashboardDeleteMessageCommand(messageId));
```

**27 tests covering:** status counts (6), message listing (4), detail/body/headers (4), jobs (1), delete commands (3), no-op handlers (9).

### Transport-Specific Integration Test Infrastructure

**SQL Server:**
- Connection from `connectionstring.txt` file
- `Helpers.SetOptions()` configures 10 boolean flags
- Verification via direct SQL queries (`SqlConnection`/`SqlCommand`)
- Admin test uses `CreateAdminFunctions()`

**PostgreSQL:**
- Connection hardcoded: `"Server=192.168.0.2;Port=5432;..."`
- Same pattern as SQL Server with `NpgsqlConnection`
- Resource-constrained server: batch sizes reduced, timeouts increased

**SQLite:**
- `IntegrationConnectionInfo` creates temp `.db3` file or in-memory DB
- Cleanup deletes temp file on `Dispose()`

**Redis:**
- `ConnectionInfo` class with server addresses
- Options via `QueueConfigurationSend` extension methods (not `SetOptions`)
- Verification via `RedisConnection`, `RedisNames`, `db.HashLength()`

**LiteDB:**
- **No integration test project exists.** Would need to follow SQLite pattern (temp file).

### Shared Test Utilities (IntegrationTests.Shared)

- `FakeMessage`: POCO with Name, BornOn, HomePage, Amount, Allowed, MoreInfo, computed Id
- `GenerateMessage.Create<T>()`: auto-fills via `Tynamix.ObjectFiller`
- `SharedSetup.CreateCreator<TTransportInit>(...)`: factory for `QueueContainer<T>` with optional metrics, interceptors, scope, chaos
- `ProducerShared.RunTest<TTransportInit, TMessage>(...)`: complete produce lifecycle
- `AdminSharedConsumer<T>`: create consumer + admin count checks mid-processing
- `MessageHandlingShared`: handler helpers (sleep/count/error/rollback scenarios)

---

## 18. Dashboard Integration Test Strategy

### Current State

Only the Memory transport has dashboard integration tests (27 tests in `Dashboard/DashboardQueries.cs`). These test handlers at the DI container level — resolving `IQueryHandlerAsync<>` and `ICommandHandlerWithOutput<>` from an admin container with real `DataStorage`.

### Next Phase: Full Dashboard API Integration Tests

The goal is to spin up the `DotNetWorkQueue.Dashboard.Api` using the test framework and exercise ALL transport functions end-to-end through the API layer.

**Key architectural decisions needed:**

1. **Test host approach**: Use `WebApplicationFactory<T>` or `TestServer` to host the Dashboard API in-process, configure `DashboardOptions` with test transport connections.

2. **Per-transport test state setup:**

| Transport | Produce Messages | Create Error State | Create Processing State | Create Stale State |
|---|---|---|---|---|
| Memory | `IProducerQueue<T>.Send()` | Not supported (count only) | `IDataStorage.GetNextMessage()` | Not supported (no heartbeat) |
| SQL Server | `IProducerQueue<T>.Send()` | Raw SQL: `UPDATE MetaData SET Status=2` | Real consumer or raw SQL | Set old HeartBeat via SQL |
| PostgreSQL | Same | Same | Same | Same |
| SQLite | Same | Same | Same | Same |
| Redis | `IProducerQueue<T>.Send()` | `db.ListRightPush(Error, id)` | Consumer or sorted set manipulation | Set old score on Working sorted set |
| LiteDB | `IProducerQueue<T>.Send()` | Insert into MetaDataErrors collection | Consumer or direct update | Set old HeartBeat on MetaData record |

3. **Service dependencies per transport:**

| Transport | External Service | Connection Source | Can Run in CI |
|---|---|---|---|
| Memory | None | `"none"` | Yes |
| SQL Server | SQL Server instance | `connectionstring.txt` | If server available |
| PostgreSQL | PostgreSQL instance | Hardcoded / `connectionstring.txt` | If server available |
| SQLite | None (file-based) | Temp file or in-memory | Yes |
| Redis | Redis instance | Config class | If server available |
| LiteDB | None (file-based) | Temp file | Yes |

4. **Scope sharing for Memory transport**: `RegisterNonScopedSingleton(scope)` on the `QueueContainer` is needed so that the producer and admin container share the same static `DataStorage`.

5. **Dashboard handler availability**: Relational/Redis/LiteDB transports register dashboard handlers in their standard init classes. Only Memory needs `MemoryDashboardInit` specifically.

### Expected Test Coverage

For each transport, test all 17 handler types through the Dashboard API endpoints:

**Read operations (12):**
- Status counts (all states)
- Message listing (filtered by Waiting, Processing, Error)
- Message count (with filters)
- Message detail (exists, not found)
- Stale messages (if heartbeat supported)
- Error messages (if error tracking supported)
- Error message count
- Error retries
- Configuration (if stored)
- Jobs (if job scheduler used)
- Message body (raw bytes or decoded)
- Message headers

**Write operations (5):**
- Delete single message
- Delete all error messages
- Requeue error message
- Reset stale message
- Update message body

### Known Transport Limitations for Testing

| Limitation | Affected Transports | Test Impact |
|---|---|---|
| No error detail storage | Memory, Redis | Error message/retry tests return empty/zero |
| No heartbeat support | Memory | Stale message tests return empty |
| No configuration storage | Memory, Redis | Configuration tests return null |
| No body update (live objects) | Memory | UpdateBody returns 0 |
| O(n) message listing | Redis | Large dataset tests would be slow |
| LastException always null | Redis | Error message LastException assertion differs |
| JobScheduledTime always null | Redis | Job response assertion differs |
