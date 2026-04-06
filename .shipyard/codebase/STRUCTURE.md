# Structure

## Overview

The solution contains 39 projects organized into a layered structure: one core library, two shared transport abstraction layers, six transport implementations, three dashboard projects, a shared integration test library, and corresponding unit/integration test projects for each component. All source code lives under `Source/`, with vendored libraries in `Lib/`.

## Solution Organization

The solution file `Source/DotNetWorkQueue.sln` includes all projects. A secondary `Source/DotNetWorkQueueNoTests.sln` excludes test projects for faster builds.

### Core Libraries

| Project | Purpose | Target Frameworks |
|---------|---------|-------------------|
| `DotNetWorkQueue` | Core library -- all abstractions, interfaces, default implementations, IoC container, and the in-process Memory transport | net10.0, net8.0, net48, netstandard2.0 |
| `DotNetWorkQueue.Transport.Shared` | Transport abstraction layer -- CQS interfaces (`ICommandHandler`, `IQueryHandler`), shared init base class | net10.0, net8.0, net48, netstandard2.0 |
| `DotNetWorkQueue.Transport.RelationalDatabase` | SQL-specific abstractions -- prepared statement pattern, shared command/query handlers for relational databases | net10.0, net8.0, net48, netstandard2.0 |

- Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`, `Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj`, `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj`

### Transport Implementations

| Project | Storage Backend | Depends On | Key External Package |
|---------|----------------|------------|---------------------|
| `DotNetWorkQueue.Transport.SqlServer` | Microsoft SQL Server | Core + RelationalDatabase | Microsoft.Data.SqlClient 6.1.3 |
| `DotNetWorkQueue.Transport.PostgreSQL` | PostgreSQL | Core + RelationalDatabase | Npgsql 8.0.8 |
| `DotNetWorkQueue.Transport.SQLite` | SQLite (file/in-memory) | Core + RelationalDatabase | System.Data.SQLite.Core 1.0.119 |
| `DotNetWorkQueue.Transport.Redis` | Redis | Core + Shared | StackExchange.Redis 2.10.1 |
| `DotNetWorkQueue.Transport.LiteDB` | LiteDB (embedded NoSQL) | Core + Shared | LiteDB 5.0.21 |
| `DotNetWorkQueue.Transport.Memory` | In-process memory | Core | (none) |

- Evidence: Each transport's `.csproj` file lists its `ProjectReference` and `PackageReference` items.
- Note: Redis and LiteDB depend on `Transport.Shared` but **not** `Transport.RelationalDatabase`, since they are not SQL-based.
- Note: The Memory transport has an additional in-core implementation at `Source/DotNetWorkQueue/Transport/Memory/Basic/` that provides the base memory storage classes.

### Dashboard Projects

| Project | Purpose | Target Frameworks | Key External Package |
|---------|---------|-------------------|---------------------|
| `DotNetWorkQueue.Dashboard.Api` | ASP.NET Core REST API for queue monitoring/management | net10.0, net8.0 | Swashbuckle.AspNetCore 7.2.0 |
| `DotNetWorkQueue.Dashboard.Ui` | Blazor Server web UI for the dashboard | net10.0, net8.0 | MudBlazor 9.1.0 |
| `DotNetWorkQueue.Dashboard.Client` | HTTP client + consumer registration for dashboard | net10.0, net8.0 | Microsoft.Extensions.Http 9.0.3 |

- Evidence: `Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj`, `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj`, `Source/DotNetWorkQueue.Dashboard.Client/DotNetWorkQueue.Dashboard.Client.csproj`
- Note: Dashboard projects target only net10.0 and net8.0 (no net48/netstandard2.0).

### Test Projects

| Project | Type | Tests For |
|---------|------|-----------|
| `DotNetWorkQueue.Tests` | Unit | Core library |
| `DotNetWorkQueue.Transport.SqlServer.Tests` | Unit | SqlServer transport |
| `DotNetWorkQueue.Transport.PostgreSQL.Tests` | Unit | PostgreSQL transport |
| `DotNetWorkQueue.Transport.Redis.Tests` | Unit | Redis transport |
| `DotNetWorkQueue.Transport.SQLite.Tests` | Unit | SQLite transport |
| `DotNetWorkQueue.Transport.LiteDb.Tests` | Unit | LiteDB transport |
| `DotNetWorkQueue.Transport.RelationalDatabase.Tests` | Unit | Relational DB shared layer |
| `DotNetWorkQueue.Transport.Memory.Tests` | Unit | Memory transport |
| `DotNetWorkQueue.Dashboard.Api.Tests` | Unit | Dashboard API |
| `DotNetWorkQueue.Dashboard.Client.Tests` | Unit | Dashboard Client |
| `DotNetWorkQueue.IntegrationTests.Shared` | Shared library | Common integration test infrastructure |
| `DotNetWorkQueue.IntegrationTests.Metrics` | Shared library | Metrics integration test infrastructure |
| `DotNetWorkQueue.Transport.Memory.Integration.Tests` | Integration | Memory transport (no external deps) |
| `DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests` | Integration | Memory transport LINQ (no external deps) |
| `DotNetWorkQueue.Transport.SqlServer.IntegrationTests` | Integration | SqlServer (requires running instance) |
| `DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests` | Integration | SqlServer LINQ |
| `DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests` | Integration | PostgreSQL (requires running instance) |
| `DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests` | Integration | PostgreSQL LINQ |
| `DotNetWorkQueue.Transport.Redis.IntegrationTests` | Integration | Redis (requires running instance) |
| `DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests` | Integration | Redis LINQ |
| `DotNetWorkQueue.Transport.SQLite.Integration.Tests` | Integration | SQLite |
| `DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests` | Integration | SQLite LINQ |
| `DotNetWorkQueue.Transport.LiteDB.IntegrationTests` | Integration | LiteDB |
| `DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests` | Integration | LiteDB LINQ |
| `DotNetWorkQueue.Dashboard.Api.Integration.Tests` | Integration | Dashboard API (all transports) |

### Vendored Libraries

Located at `Lib/` in the repository root:

| Library | Purpose | TFM Variants |
|---------|---------|--------------|
| `Schyntax` | Cron-like schedule syntax for recurring jobs | net10.0, net8.0, net48, netstandard2.0 |
| `Aq.ExpressionJsonSerializer` | LINQ expression tree JSON serialization | net10.0, net8.0, net48, netstandard2.0 |
| `JpLabs.DynamicCode` | Dynamic lambda compilation | Single DLL (net48 only) |

- Evidence: `Lib/Schyntax/`, `Lib/Aq.ExpressionJsonSerializer/`, `Lib/JpLabs.DynamicCode/`
- Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 81-126, HintPath references)

## Project Dependency Graph

```
DotNetWorkQueue (Core)
  ^         ^         ^         ^
  |         |         |         |
  |   Transport.Shared  |    Dashboard.Api
  |      ^    ^         |       ^
  |      |    |         |       |
  | Transport.  |    Dashboard.Client (no project refs)
  | Relational  |    Dashboard.Ui    (no project refs)
  | Database    |
  |   ^  ^  ^  |
  |   |  |  |  |
  | SqlServer  |
  | PostgreSQL |
  | SQLite     |
  |            |
  +--- Redis --+
  +--- LiteDB -+
  +--- Memory
```

Detailed dependency chain:

- **`Transport.Shared`** depends on: `DotNetWorkQueue`
- **`Transport.RelationalDatabase`** depends on: `Transport.Shared`, `DotNetWorkQueue`
- **`Transport.SqlServer`** depends on: `Transport.RelationalDatabase`, `DotNetWorkQueue`
- **`Transport.PostgreSQL`** depends on: `Transport.RelationalDatabase`, `DotNetWorkQueue`
- **`Transport.SQLite`** depends on: `Transport.RelationalDatabase`, `DotNetWorkQueue`
- **`Transport.Redis`** depends on: `Transport.Shared`, `DotNetWorkQueue`
- **`Transport.LiteDB`** depends on: `Transport.Shared`, `DotNetWorkQueue`
- **`Transport.Memory`** depends on: `DotNetWorkQueue` (only)
- **`Dashboard.Api`** depends on: `DotNetWorkQueue`, `Transport.RelationalDatabase`
- **`Dashboard.Client`** has no project references (standalone HTTP client)
- **`Dashboard.Ui`** has no project references (standalone Blazor component library)

## Key Namespaces and Their Responsibilities

### Core Project (`Source/DotNetWorkQueue/`)

| Directory/Namespace | Files | Responsibility |
|---------------------|-------|----------------|
| `IoC/` | 3 files | `CreateContainer<T>`, `ContainerWrapper`, `ComponentRegistration` -- IoC container creation and default registrations |
| `Queue/` | 71 files | All queue implementations: `ProducerQueue`, `ConsumerQueue`, `ConsumerQueueAsync`, workers, monitors, heartbeat, message processing pipeline |
| `Configuration/` | 28 files | All configuration objects: `QueueConsumerConfiguration`, `QueueProducerConfiguration`, `QueueConnection`, transport init base classes |
| `Messages/` | 27 files | Message types: `Message`, `ReceivedMessage`, `AdditionalMessageData`, handler registration, method handling |
| `Factory/` | 24 files | Factory classes for workers, messages, schedulers, heartbeats, etc. |
| `JobScheduler/` | 9 files | Recurring job scheduling using Schyntax syntax |
| `Policies/` | 4 files | Polly resilience pipeline registry and policy definitions |
| `Metrics/` | 3 subdirs | `Net/` (System.Diagnostics.Metrics implementation), `NoOp/` (null metrics), `Decorator/` (19 metric decorators) |
| `Trace/` | 2 items | OpenTelemetry tracing decorators and header injection |
| `Interceptors/` | 4 files | Message interceptors (GZip compression, TripleDES encryption) |
| `Serialization/` | 7 files | JSON serialization (Newtonsoft), expression serialization, composite serializer |
| `Logging/` | 1 subdir | `Decorator/` -- logging decorators for queue operations |
| `LinqCompile/` | multiple | LINQ expression compilation and caching |
| `Transport/Memory/` | ~35 files | In-process memory transport (default/fallback implementation) |
| `History/` | 1 subdir | `Decorator/` -- message history tracking decorators |
| `Cache/` | multiple | Object caching and cache policies |
| `TaskScheduling/` | multiple | Custom task scheduler (`SmartThreadPoolTaskScheduler`) |
| `Time/` | multiple | Time providers (local, SNTP) |
| `Admin/` | multiple | Admin API implementation |
| `Notifications/` | multiple | Consumer queue notification system |
| `Exceptions/` | multiple | Custom exception types |
| `Validation/` | multiple | Guard clauses and validation utilities |
| Root | ~70 files | Interface definitions (`I*.cs`) that define the core contracts |

- Evidence: Directory listings of `Source/DotNetWorkQueue/`

### Transport.Shared (`Source/DotNetWorkQueue.Transport.Shared/`)

| Directory/Namespace | Responsibility |
|---------------------|----------------|
| Root | CQS interfaces: `ICommandHandler<T>`, `IQueryHandler<T,R>`, `IQueryHandlerAsync<T,R>`, `ICommandHandlerWithOutput<T,R>`, `ITransportCommitMessage`, `ITransportHandleMessage`, `ITransportRollbackMessage` |
| `Basic/` | [Inferred] Base implementations and shared command/query types |
| `Message/` | `TransportCommitMessage`, `TransportHandleMessage` |
| `Trace/` | Transport-level trace decorators |

- Evidence: `Source/DotNetWorkQueue.Transport.Shared/` directory listing

### Transport.RelationalDatabase (`Source/DotNetWorkQueue.Transport.RelationalDatabase/`)

| Directory/Namespace | Responsibility |
|---------------------|----------------|
| Root | Interfaces for relational concerns: `IDbConnectionFactory`, `IConnectionHolder`, `IPrepareCommandHandler`, `IPrepareQueryHandler`, `ITransportOptions`, `IReadColumn`, `IBuildMoveToErrorQueueSql`, `IOptionsSerialization` |
| `Basic/` | Shared implementations: `RelationalDatabaseMessageQueueInit`, `CommandStringCache`, `TableNameHelper`, `ConnectionHeader`, `DashboardDynamicColumnHelper` |
| `Basic/Command/` | Command objects (DTOs) for CQS pattern |
| `Basic/CommandHandler/` | 20 shared command handlers (create tables, delete messages, move to error queue, heartbeat, dashboard operations) |
| `Basic/Query/` | Query objects (DTOs) for CQS pattern |
| `Basic/QueryHandler/` | 41 shared query handlers (find expired, find heartbeat resets, dashboard queries, etc.) |
| `Basic/QueryPrepareHandler/` | SQL parameter preparation handlers |
| `Basic/CommandPrepareHandler/` | Command parameter preparation handlers |

- Evidence: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/CommandHandler/` (20 files), `Basic/QueryHandler/` (41 files)

### Transport Implementations (Internal Structure Pattern)

Each transport follows a consistent internal structure:

```
Transport.{Name}/
  +-- Basic/
  |     +-- {Name}MessageQueueInit.cs    -- ITransportInit implementation
  |     +-- {Name}MessageQueueCreation.cs -- IQueueCreation implementation
  |     +-- CommandHandler/              -- Transport-specific command handlers
  |     +-- QueryHandler/               -- Transport-specific query handlers
  |     +-- Message/                    -- Transport-specific message handling
  |     +-- Factory/                    -- Transport-specific factories
  |     +-- Time/                       -- Transport-specific time provider
  +-- Decorator/                        -- Transport-specific decorators (retry, etc.)
  +-- Trace/                            -- Transport-specific trace decorators
  +-- Schema/                           -- (Relational only) Table/index definitions
  +-- connectionstring.txt              -- Default connection string for integration tests
```

- Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/` directory structure
- Evidence: `Source/DotNetWorkQueue.Transport.Redis/Basic/` directory structure

### Dashboard (`Source/DotNetWorkQueue.Dashboard.Api/`)

| Directory/Namespace | Responsibility |
|---------------------|----------------|
| `Controllers/` | REST endpoints: `ConnectionsController`, `QueuesController`, `ConsumersController` |
| `Configuration/` | Dashboard configuration options |
| `Middleware/` | ASP.NET Core middleware |
| `Models/` | API request/response DTOs |
| `Services/` | Business logic services |
| `Examples/` | Example configurations |
| Root | `DashboardApi.cs`, `DashboardExtensions.cs` -- entry points and DI registration |

- Evidence: `Source/DotNetWorkQueue.Dashboard.Api/` directory listing

## Important Interfaces and Their Implementations

### Core Queue Interfaces

| Interface | Default Implementation | Purpose | Evidence |
|-----------|----------------------|---------|----------|
| `IConsumerQueue` | `ConsumerQueue` | Synchronous message consumer | `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs` |
| `IConsumerQueueAsync` | `ConsumerQueueAsync` | Asynchronous message consumer | `Source/DotNetWorkQueue/Queue/ConsumerQueueAsync.cs` |
| `IProducerQueue<T>` | `ProducerQueue<T>` | Typed message producer | `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` |
| `IProducerMethodQueue` | `ProducerMethodQueue` | LINQ/delegate producer | `Source/DotNetWorkQueue/Queue/ProducerMethodQueue.cs` |
| `IConsumerMethodQueue` | `ConsumerMethodQueue` | LINQ/delegate consumer | `Source/DotNetWorkQueue/Queue/ConsumerMethodQueue.cs` |
| `IJobScheduler` | `JobScheduler` | Recurring job scheduling | `Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs` |

### Transport Interfaces (per-transport implementations)

| Interface | Purpose | SqlServer Impl | Redis Impl |
|-----------|---------|---------------|------------|
| `ISendMessages` | Send messages to queue | `SendMessages<long>` (shared) | `RedisQueueSend` |
| `IReceiveMessages` | Receive messages from queue | `SqlServerMessageQueueReceive` | `RedisQueueReceiveMessages` |
| `IQueueCreation` | Create/drop queue infrastructure | `SqlServerMessageQueueCreation` | `RedisQueueCreation` |
| `IConnectionInformation` | Parse/hold connection info | `SqlConnectionInformation` | `RedisConnectionInfo` |
| `IResetHeartBeat` | Reset stale heartbeats | `ResetHeartBeat<long>` (shared) | `RedisQueueResetHeartBeat` |
| `ISendHeartBeat` | Send heartbeat for active msg | `SendHeartBeat<long>` (shared) | `RedisQueueSendHeartBeat` |
| `IRemoveMessage` | Delete completed messages | `RemoveMessage` | (via Redis Lua) |

- Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs` (registration lines)
- Evidence: `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs` (registration lines)

### Infrastructure Interfaces

| Interface | Default Implementation | Purpose |
|-----------|----------------------|---------|
| `ITransportInit` | (abstract, per-transport) | Transport IoC registration |
| `IContainer` | `ContainerWrapper` | IoC container abstraction over SimpleInjector |
| `IQueueMonitor` | `QueueMonitor` | Orchestrates background monitors |
| `IHeartBeatMonitor` | `HeartBeatMonitor` | Background heartbeat monitor |
| `IPrimaryWorker` | `PrimaryWorker` | Primary consumer worker thread |
| `IWorker` | `Worker` | Additional consumer worker thread |
| `IWorkerCollection` | `WorkerCollection` | Manages worker threads |
| `IMessageProcessing` | `MessageProcessing` | Synchronous message processing pipeline |
| `IPolicies` | `Policies` | Polly resilience pipeline registry |
| `IMetrics` | `MetricsNet` | System.Diagnostics.Metrics implementation |
| `ISerializer` | `JsonSerializer` | Newtonsoft.Json message serialization |

## File Organization Conventions

### Source File Naming

- **Interfaces**: Prefixed with `I` (e.g., `IConsumerQueue.cs`, `ITransportInit.cs`)
- **Abstract classes**: Prefixed with `A` (e.g., `ASerializer.cs`, `ATaskScheduler.cs`) or suffixed with `Base` (e.g., `BaseQueue.cs`, `MultiWorkerBase.cs`)
- **Implementations**: Named directly (e.g., `ConsumerQueue.cs`, `ProducerQueue.cs`)
- **Factories**: Suffixed with `Factory` (e.g., `WorkerFactory.cs`, `MessageProcessingFactory.cs`)
- **Decorators**: Suffixed with `Decorator` (e.g., `SendMessagesPolicyDecorator.cs`)
- **Configuration**: Suffixed with `Configuration` (e.g., `HeartBeatConfiguration.cs`)
- **NoOp implementations**: Suffixed with `NoOp` (e.g., `HeartBeatMonitorNoOp.cs`, `WriteMessageHistoryNoOp.cs`)
- **Commands/Queries**: Named by operation (e.g., `SendMessageCommand.cs`, `GetQueueCountQuery.cs`)
- **Command/Query Handlers**: Suffixed with `Handler` (e.g., `SendHeartBeatCommandHandler.cs`, `GetQueueCountQueryHandler.cs`)
- **Prepare Handlers**: Suffixed with `PrepareHandler` (e.g., `GetQueueCountQueryPrepareHandler.cs`)

- Evidence: All directory listings throughout the analysis

### Project Layout Convention

Each non-test project follows this structure:

```
{ProjectName}/
  +-- {ProjectName}.csproj
  +-- README.md                           -- Package readme
  +-- InternalsVisibleForTests.cs         -- Test assembly access (where applicable)
  +-- Basic/                              -- Primary implementation classes
  |     +-- CommandHandler/               -- CQS command handlers
  |     +-- QueryHandler/                 -- CQS query handlers
  |     +-- Factory/                      -- Factory classes
  |     +-- Message/                      -- Message-related classes
  +-- Decorator/                          -- Decorator classes
  +-- Trace/                              -- Tracing-related classes
  +-- Schema/                             -- Database schema (relational transports)
```

- Evidence: Consistent across `Transport.SqlServer`, `Transport.Redis`, `Transport.PostgreSQL`, `Transport.SQLite`, `Transport.LiteDB`

### Interface-to-Implementation Mapping Convention

Core interfaces are defined in the root of `Source/DotNetWorkQueue/` (as `I*.cs` files), while their default implementations are organized into subdirectories by domain concern (e.g., `Queue/`, `Factory/`, `Configuration/`, `Messages/`).

- Evidence: `Source/DotNetWorkQueue/IConsumerQueue.cs` (interface) vs `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs` (implementation)

### Configuration File Locations

| File | Location | Purpose |
|------|----------|---------|
| `DotNetWorkQueue.sln` | `Source/` | Full solution (all projects including tests) |
| `DotNetWorkQueueNoTests.sln` | `Source/` | Solution without test projects |
| `DotNetWorkQueue.licenseheader` | `Source/DotNetWorkQueue/` | LGPL-2.1 license header template |
| `connectionstring.txt` | Each integration test project | Default connection strings for tests |
| `xunit.runner.json` | `Source/` | [Legacy] xUnit runner config (project now uses MSTest) |
| `.github/workflows/ci.yml` | Root | GitHub Actions CI configuration |
| `CLAUDE.md` | Root | AI assistant instructions |

### Entry Points

| Entry Point | Usage |
|-------------|-------|
| `QueueContainer<TTransportInit>` | Main entry for creating producer/consumer queues |
| `QueueCreationContainer<TTransportInit>` | Entry for creating/managing queue infrastructure |
| `SchedulerContainer` | Entry for creating task schedulers |
| `JobSchedulerContainer` | Entry for creating recurring job schedulers |
| `DashboardApi.MapDashboardApi()` | [Inferred] Entry for mapping dashboard REST endpoints |

- Evidence: `Source/DotNetWorkQueue/QueueContainer.cs`, `Source/DotNetWorkQueue/QueueCreationContainer.cs`

## Summary Table

| Item | Detail | Confidence |
|------|--------|------------|
| Total projects in solution | 39 (14 library + 25 test) | Observed |
| Core library | `DotNetWorkQueue` with ~70 root interface files | Observed |
| Transport abstraction layers | 2 (`Transport.Shared`, `Transport.RelationalDatabase`) | Observed |
| Transport implementations | 6 (SqlServer, PostgreSQL, SQLite, Redis, LiteDB, Memory) | Observed |
| Dashboard projects | 3 (Api, Ui, Client) | Observed |
| Shared test libraries | 2 (`IntegrationTests.Shared`, `IntegrationTests.Metrics`) | Observed |
| CQS command handlers (relational shared) | 20 files | Observed |
| CQS query handlers (relational shared) | 41 files | Observed |
| Metrics decorator classes | 19 files | Observed |
| Queue operation classes | 71 files | Observed |
| Factory classes | 24 files | Observed |
| Vendored libraries | 3 (Schyntax, ExpressionJsonSerializer, DynamicCode) | Observed |
| Memory transport in core project | Yes, at `Transport/Memory/Basic/` (~35 files) | Observed |

## Open Questions

- The `DotNetWorkQueue.Transport.Memory` project's relationship to the in-core `Transport/Memory/Basic/` implementation is unclear. The external project contains only `Basic/CommandHandler/`, `Basic/QueryHandler/`, and `MemoryDashboardInit.cs`, suggesting it extends the core memory transport with dashboard support. [Inferred]
- The `xunit.runner.json` file in `Source/` appears to be a leftover from the xUnit-to-MSTest migration.
- The `DotNetWorkQueue.AppMetrics` projects referenced in CLAUDE.md memory notes do not appear to exist in the current solution, suggesting they were removed during the App.Metrics-to-System.Diagnostics.Metrics migration.
