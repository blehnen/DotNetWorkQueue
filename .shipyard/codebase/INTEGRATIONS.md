# INTEGRATIONS.md -- External System Integrations

## Overview

DotNetWorkQueue integrates with five storage backends (SQL Server, PostgreSQL, SQLite, Redis, LiteDB) plus an in-memory transport, each accessed through transport-specific client libraries. It also integrates with OpenTelemetry for distributed tracing, `System.Diagnostics.Metrics` for built-in metrics, Polly for resilience, GuerrillaNtp for NTP time synchronization, and exposes a REST/Swagger Dashboard API built on ASP.NET Core. A separate Dashboard Client library communicates with the API over HTTP.

## Findings

### Database Integrations

#### SQL Server

- **Client library**: `Microsoft.Data.SqlClient` 6.1.3 (the modern, cross-platform Microsoft ADO.NET provider; replaced the older `System.Data.SqlClient`)
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj` (line 64)
- **Connection pattern**: `IDbConnectionFactory` creates new `SqlConnection` instances per operation using the connection string from `IConnectionInformation`
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/DbConnectionFactory.cs` (lines 31-49)
  ```csharp
  public IDbConnection Create()
  {
      return new SqlConnection(_connectionInformation.ConnectionString);
  }
  ```
- **Connection string format**: Standard SQL Server connection string with named parameters
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/connectionstring.txt`
  - Example: `Server=192.168.0.2;Application Name=IntegrationTesting;Database=IntegrationTests;user=brian;password=123abc;max pool size=500;TrustServerCertificate=True;`
- **Connection string storage**: Plain text `connectionstring.txt` files in integration test projects, copied to output directory
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj` (lines 40-42)
- **Layer**: Built on `DotNetWorkQueue.Transport.RelationalDatabase` shared abstraction layer
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj` (line 68)

#### PostgreSQL

- **Client library**: `Npgsql` 8.0.8
  - Evidence: `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj` (line 77)
- **Connection pattern**: Same `IDbConnectionFactory` pattern, creates `NpgsqlConnection` instances
  - Evidence: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/DbConnectionFactory.cs` (lines 28-43)
  ```csharp
  public IDbConnection Create()
  {
      return new NpgsqlConnection(_connectionInformation.ConnectionString);
  }
  ```
- **Connection string format**: Standard Npgsql connection string with keepalive settings
  - Evidence: `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/connectionstring.txt`
  - Example: `Server=192.168.0.2;Port=5432;Database=integrationtesting;Maximum Pool Size=250;userid=brian;password=123abc;Trust Server Certificate=true;Keepalive=15;Tcp Keepalive=true;`
- **Resilience**: PostgreSQL transport includes its own `Polly.Core` 8.6.5 dependency for transport-specific retry policies
  - Evidence: `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj` (line 78)
- **Layer**: Built on `DotNetWorkQueue.Transport.RelationalDatabase` shared abstraction layer
  - Evidence: `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj` (line 82)

#### SQLite

- **Client library**: `System.Data.SQLite.Core` 1.0.119 (the official SQLite ADO.NET provider)
  - Evidence: `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` (line 76)
- **Connection pattern**: `IDbConnectionFactory` delegates to `IDbFactory.CreateConnection()` which creates `SQLiteConnection` instances
  - Evidence: `Source/DotNetWorkQueue.Transport.SQLite/Basic/DbConnectionFactory.cs` (lines 29-54)
  - Evidence: `Source/DotNetWorkQueue.Transport.SQLite/Basic/DbFactory.cs` (lines 42-45)
  ```csharp
  public IDbConnection CreateConnection(string connectionString, bool forMemoryHold)
  {
      return new SQLiteConnection(connectionString);
  }
  ```
- **Features**: WAL mode support (`EnableWalMode` option, default true, skipped for in-memory databases)
  - [Inferred] Based on CLAUDE.md documentation
- **Layer**: Built on `DotNetWorkQueue.Transport.RelationalDatabase` shared abstraction layer
  - Evidence: `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` (line 80)

#### Redis

- **Client library**: `StackExchange.Redis` 2.10.1
  - Evidence: `Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj` (line 77)
- **Serialization**: `MsgPack.Cli` 1.0.1 for MessagePack binary serialization
  - Evidence: `Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj` (line 76)
- **Connection pattern**: Singleton `ConnectionMultiplexer` created lazily with double-checked locking via `IRedisConnection`
  - Evidence: `Source/DotNetWorkQueue.Transport.Redis/RedisConnection.cs` (lines 30-121)
  ```csharp
  private void EnsureCreated()
  {
      if (_connection != null || string.IsNullOrEmpty(_connectionInformation.ConnectionString)) return;
      lock (_connectionLock)
      {
          if (_connection != null) return;
          _connection = ConnectionMultiplexer.Connect(_connectionInformation.ConnectionString);
      }
  }
  ```
- **Disposal**: Thread-safe disposal via `Interlocked.Increment` pattern
  - Evidence: `Source/DotNetWorkQueue.Transport.Redis/RedisConnection.cs` (lines 89-96)
- **Layer**: Built on `DotNetWorkQueue.Transport.Shared` (not RelationalDatabase, since Redis is not relational)
  - Evidence: `Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj` (line 81)

#### LiteDB

- **Client library**: `LiteDB` 5.0.21 (embedded NoSQL document database)
  - Evidence: `Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDb.csproj` (line 76)
- **Connection pattern**: `LiteDbConnectionManager` manages `LiteDatabase` instances with two modes:
  - **Direct mode**: Single `LiteDatabase` instance created lazily and reused (non-disposable wrapper)
  - **Shared mode**: New `LiteDatabase` instance per operation (disposable wrapper)
  - Supports connection sharing across creation scopes via `ICreationScope`
  - Evidence: `Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbConnectionManager.cs` (lines 29-141)
  ```csharp
  public LiteDbConnection GetDatabase()
  {
      if (_shared || _disposedValue)
          return new LiteDbConnection(new LiteDatabase(_connectionInformation.ConnectionString), true);
      // ... lazy singleton creation for direct mode
  }
  ```
- **Layer**: Built on `DotNetWorkQueue.Transport.Shared` (not RelationalDatabase)
  - Evidence: `Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDb.csproj` (line 80)

#### Memory (In-Process)

- **No external dependency**: Pure in-memory transport with no external client libraries
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory/DotNetWorkQueue.Transport.Memory.csproj` (lines 72-75) -- only project references, no PackageReferences
- **Layer**: Built on `DotNetWorkQueue.Transport.Shared`
  - Evidence: `Source/DotNetWorkQueue.Transport.Memory/DotNetWorkQueue.Transport.Memory.csproj` (line 73)

### Connection Abstraction Layer

- **`IConnectionInformation`**: Core interface providing `ConnectionString`, `QueueName`, and `Server` properties
  - Evidence: `Source/DotNetWorkQueue/IConnectionInformation.cs`
- **`IDbConnectionFactory`**: Relational database abstraction for creating `IDbConnection` instances
  - Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/DbConnectionFactory.cs`, `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/DbConnectionFactory.cs`, `Source/DotNetWorkQueue.Transport.SQLite/Basic/DbConnectionFactory.cs`
- **Transport initialization**: Each transport implements `ITransportInit` (with `Send`/`Receive`/`Duplex` variants) and registers all DI bindings via `RegisterImplementations()`
  - Evidence: `Source/DotNetWorkQueue/ITransportInit.cs`, `Source/DotNetWorkQueue/ITransportInitSend.cs`, `Source/DotNetWorkQueue/ITransportInitReceive.cs`

### Distributed Tracing (OpenTelemetry)

- **Package**: `OpenTelemetry` 1.14.0
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (line 74)
- **Implementation**: Uses `System.Diagnostics.ActivitySource` for span creation and `OpenTelemetry.Context.Propagation` for context propagation across message boundaries
  - Evidence: `Source/DotNetWorkQueue/Trace/InjectHeaders.cs` (lines 26-27, 43-47)
  ```csharp
  using OpenTelemetry;
  using OpenTelemetry.Context.Propagation;
  // ...
  var mapping = Propagators.DefaultTextMapPropagator;
  mapping.Inject(new PropagationContext(context, Baggage.Current), message.Headers, InjectTraceContextIntoBasicProperties);
  ```
- **Trace context extraction**: Uses `TraceContextPropagator` to extract parent context from message headers on the consumer side
  - Evidence: `Source/DotNetWorkQueue/Trace/InjectHeaders.cs` (lines 79-83)
- **Decorator pattern**: Tracing is applied via decorators on key operations -- 14+ trace decorators covering message handling, sending, receiving, heartbeats, rollbacks, and more
  - Evidence: `Source/DotNetWorkQueue/Trace/Decorator/` directory contains files like `MessageHandlerDecorator.cs`, `SendHeartBeatDecorator.cs`, `ReceiveMessagesDecorator.cs`, etc.
- **Tags**: Spans are enriched with `Server`, `Queue`, `CorrelationId`, `Route`, `MessageId`, and custom user tags
  - Evidence: `Source/DotNetWorkQueue/Trace/InjectHeaders.cs` (lines 150-162)
- **Integration test support**: `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.14.0 is available in the integration test metrics project
  - Evidence: `Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj` (line 28)

### Metrics (System.Diagnostics.Metrics)

- **Implementation**: Built on `System.Diagnostics.Metrics` (via `System.Diagnostics.DiagnosticSource` 10.0.1)
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (line 78)
- **Meter name**: `"DotNetWorkQueue"` is the root meter; child contexts use `DotNetWorkQueue.{contextName}`
  - Evidence: `Source/DotNetWorkQueue/Metrics/Net/MetricsNet.cs` (lines 39, 46)
  ```csharp
  _meter = new Meter("DotNetWorkQueue");
  // child:
  new MetricsContextNet(new Meter($"{_meter.Name}.{name}"))
  ```
- **Instrument types**: Counters (UpDownCounter), Meters (Counter), Histograms, Timers (Histogram<double>), and Observable Gauges
  - Evidence: `Source/DotNetWorkQueue/Metrics/Net/MetricsNet.cs` (lines 65-122)
- **Metric decorators**: 18+ metric decorators covering serialization, message handling, queue creation, heartbeats, error handling, and more
  - Evidence: `Source/DotNetWorkQueue/Metrics/Decorator/` directory
- **NoOp fallback**: `MetricsNoOp` class for when metrics are not needed
  - Evidence: `Source/DotNetWorkQueue/Metrics/NoOp/MetricsNoOp.cs`
- **Snapshot API**: `GetCollectedMetrics()` returns a `MetricsSnapshot` with counter and meter values
  - Evidence: `Source/DotNetWorkQueue/Metrics/Net/MetricsNet.cs` (lines 125-136)

### Resilience (Polly)

- **Package**: `Polly.Core` 8.6.5 (V8 API with `ResiliencePipeline`)
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (line 76)
- **Registry**: Uses `Polly.Registry.ResiliencePipelineRegistry<string>` for named pipeline lookup
  - Evidence: `Source/DotNetWorkQueue/Policies/Policies.cs` (lines 32, 41)
- **Policy definitions**: `PolicyDefinitions` class defines named policy slots; transports add their own via `TransportDefinition` dictionary
  - Evidence: `Source/DotNetWorkQueue/Policies/Policies.cs` (lines 37, 47)
- **Policy decorators**: Applied via decorator pattern for send, receive, and heartbeat operations
  - Evidence: `Source/DotNetWorkQueue/Policies/Decorator/ISendMessagesPolicyDecorator.cs`, `ISendHeartBeatPolicyDecorator.cs`, `IReceiveMessagesPolicyDecorator.cs`
- **Chaos engineering**: `EnableChaos` flag available for resilience testing
  - Evidence: `Source/DotNetWorkQueue/Policies/Policies.cs` (line 50)

### NTP Time Synchronization

- **Package**: `GuerrillaNtp` 3.1.0
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (line 70)
- **Implementation**: `SntpTime` class provides UTC time by querying an NTP server and caching the offset. Opt-in -- requires explicit registration in the DI container.
  - Evidence: `Source/DotNetWorkQueue/Time/SntpTime.cs` (lines 35-48)
  ```csharp
  public class SntpTime : BaseTime
  {
      private readonly NtpClient _ntpClient;
      public SntpTime(ILogger log, SntpTimeConfiguration configuration)
          : base(log, configuration)
      {
          _ntpClient = new NtpClient(configuration.Server);
      }
  }
  ```

### Dashboard API (ASP.NET Core REST)

- **Framework**: ASP.NET Core with `Microsoft.AspNetCore.App` framework reference
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj` (line 40)
- **API style**: Controller-based REST API with attribute routing under `api/v1/dashboard/`
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Api/Controllers/QueuesController.cs` (line 32)
  ```csharp
  [Route("api/v1/dashboard/queues")]
  ```
- **Controllers**:
  - `ConnectionsController` -- connection management (`api/v1/dashboard/connections`)
  - `ConsumersController` -- consumer tracking (`api/v1/dashboard/consumers`)
  - `QueuesController` -- queue operations (`api/v1/dashboard/queues/{queueId}/...`) including status, messages, errors, history, configuration, cancellation
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Api/Controllers/` directory
- **Endpoint inventory** (QueuesController alone):
  - `GET .../status` -- queue status counts
  - `GET .../features` -- transport features
  - `GET .../maintenance` -- maintenance service status
  - `GET .../messages` -- paged message list
  - `GET .../messages/count` -- message count
  - `GET .../messages/{messageId}` -- message detail
  - `GET .../messages/{messageId}/body` -- decoded message body
  - `GET .../messages/{messageId}/headers` -- message headers
  - `GET .../messages/{messageId}/retries` -- error retry tracking
  - `GET .../messages/stale` -- stale heartbeat messages
  - `DELETE .../messages/{messageId}` -- delete message
  - `POST .../messages/{messageId}/requeue` -- requeue error message
  - `POST .../messages/{messageId}/reset` -- reset stale message
  - `POST .../messages/{messageId}/cancel` -- cancel running message
  - `PUT .../messages/{messageId}/body` -- edit message body
  - `DELETE .../errors` -- delete all error messages
  - `POST .../errors/requeue-all` -- requeue all errors
  - `POST .../messages/reset-all` -- reset all stale
  - `GET .../history` -- paged history
  - `GET .../history/{messageId}` -- history by message
  - `GET .../history/count` -- history count
  - `DELETE .../history` -- purge old history
  - `GET .../configuration` -- queue configuration
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Api/Controllers/QueuesController.cs` (lines 49-348)
- **Swagger/OpenAPI**: `Swashbuckle.AspNetCore` 7.2.0 with optional API key security scheme
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs` (lines 82-118)
- **Authentication**: Optional API key via `X-Api-Key` header, optional ASP.NET Core authorization policy
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs` (lines 71-77, 94-100)
- **Setup pattern**: Extension methods `AddDotNetWorkQueueDashboard()` and `UseDotNetWorkQueueDashboard()` for ASP.NET Core DI and middleware pipeline
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs` (lines 46-143)

### Dashboard Client (HTTP)

- **Pattern**: Typed `HttpClient` wrapper (`DashboardApiClient`) that calls the Dashboard API REST endpoints
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Client/DashboardApiClient.cs` (lines 33-56)
- **Consumer tracking**: `DashboardConsumerClient` auto-registers with the Dashboard API, sends periodic heartbeats via `System.Threading.Timer`, and unregisters on disposal
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Client/DashboardConsumerClient.cs` (lines 32-55)
- **Serialization**: Uses `System.Text.Json` (not Newtonsoft.Json) for HTTP payload serialization
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Client/DashboardApiClient.cs` (lines 37-40)
- **Configuration**: `DashboardClientOptions` with `DashboardApiUrl` and optional `ApiKey`
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Client/DashboardApiClient.cs` (lines 46-54)

### Dashboard UI (Blazor Server)

- **Framework**: Blazor Server with `Microsoft.NET.Sdk.Web`
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj` (line 1)
- **Component library**: MudBlazor 9.1.0
  - Evidence: `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj` (line 20)

### LINQ Expression Serialization

- **Library**: Aq.ExpressionJsonSerializer (vendored, per-TFM)
  - Evidence: `Lib/Aq.ExpressionJsonSerializer/` directory, referenced from `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 85-86, 94-95, 104-105, 113-114)
- **Purpose**: Serializes and deserializes LINQ expression trees to/from JSON, enabling remote LINQ expression execution across producer/consumer boundaries

### Job Scheduling

- **Library**: Schyntax (vendored, per-TFM)
  - Evidence: `Lib/Schyntax/` directory, referenced from `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 82-83, 91-92, 101-102, 110-111)
- **Purpose**: Cron-like schedule syntax for recurring job scheduling via `IJobScheduler`

### Dynamic LINQ (net48 only)

- **Library**: JpLabs.DynamicCode (vendored, single net48 DLL)
  - Evidence: `Lib/JpLabs.DynamicCode/JpLabs.DynamicCode.dll`, referenced from `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 123-126)
- **Purpose**: Dynamic lambda expression compilation at runtime
- [Inferred] Only functional on net48 target based on the single DLL and NuGet pack target (line 138)

## Summary Table

| Integration | Client Library | Version | Connection Pattern | Confidence |
|-------------|---------------|---------|-------------------|------------|
| SQL Server | Microsoft.Data.SqlClient | 6.1.3 | New `SqlConnection` per operation via `IDbConnectionFactory` | Observed |
| PostgreSQL | Npgsql | 8.0.8 | New `NpgsqlConnection` per operation via `IDbConnectionFactory` | Observed |
| SQLite | System.Data.SQLite.Core | 1.0.119 | New `SQLiteConnection` per operation via `IDbFactory` | Observed |
| Redis | StackExchange.Redis | 2.10.1 | Singleton `ConnectionMultiplexer` with lazy init | Observed |
| LiteDB | LiteDB | 5.0.21 | `LiteDbConnectionManager` with shared/direct modes | Observed |
| Memory | (none) | N/A | Pure in-process data structures | Observed |
| Distributed tracing | OpenTelemetry | 1.14.0 | `ActivitySource` + context propagation via headers | Observed |
| Metrics | System.Diagnostics.DiagnosticSource | 10.0.1 | `Meter`/`Counter`/`Histogram` instruments | Observed |
| Resilience | Polly.Core | 8.6.5 | `ResiliencePipelineRegistry<string>` | Observed |
| NTP | GuerrillaNtp | 3.1.0 | `NtpClient` with cached offset (opt-in) | Observed |
| Dashboard API | ASP.NET Core | Framework ref | REST controllers under `api/v1/dashboard/` | Observed |
| Dashboard Client | HttpClient | N/A | Typed HTTP wrapper with heartbeat timer | Observed |
| Dashboard UI | Blazor Server + MudBlazor | 9.1.0 | Server-side Blazor rendering | Observed |
| Swagger | Swashbuckle.AspNetCore | 7.2.0 | Auto-generated OpenAPI docs | Observed |
| LINQ serialization | Aq.ExpressionJsonSerializer | vendored | JSON round-trip of expression trees | Observed |
| Job scheduling | Schyntax | vendored | Cron-like schedule parsing | Observed |
| MessagePack (Redis) | MsgPack.Cli | 1.0.1 | Binary serialization for Redis values | Observed |

## Open Questions

- Are there any Redis Lua scripts used for atomic queue operations? The `StackExchange.Redis` connection pattern suggests possible script usage, but this was not investigated in depth.
- What specific Polly resilience strategies are configured by the transports? The `TransportDefinition` dictionary is populated at runtime by transport init classes.
- Does the Dashboard API have rate limiting or request throttling beyond the API key authentication?
- The connection string files in integration test projects contain credentials. Are these test-only credentials for a local dev environment, or do they need to be rotated?
