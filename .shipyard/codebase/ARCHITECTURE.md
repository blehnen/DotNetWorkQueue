# Architecture

## Overview

DotNetWorkQueue is a modular, transport-agnostic message queue library for .NET built on a layered architecture with dependency injection at its core. The system uses SimpleInjector as its IoC container, a Command/Query Separation (CQS) pattern for all data access, and a Decorator pattern for cross-cutting concerns (metrics, tracing, logging, policies). Each transport (SQL Server, PostgreSQL, SQLite, Redis, LiteDB, Memory) plugs into the core via the `ITransportInit` interface, which registers its specific implementations into the container.

## Layer Diagram

```
+---------------------------------------------------------------+
|                     Consumer / Producer API                     |
|  QueueContainer<T>  QueueCreationContainer<T>  SchedulerContainer  |
+---------------------------------------------------------------+
        |                      |                      |
        v                      v                      v
+---------------------------------------------------------------+
|                        Core Library                            |
|  DotNetWorkQueue (Queue, Configuration, Messages, IoC,        |
|   JobScheduler, Policies, Metrics, Trace, Interceptors,       |
|   Serialization, Factory, Notifications)                       |
+---------------------------------------------------------------+
        |                      |                      |
        v                      v                      v
+------------------+  +---------------------+  +---------------+
| Transport.Shared |  | Transport.Relational |  | (Redis/LiteDB |
|  (CQS pattern,   |  | Database (SQL-      |  |  skip this    |
|   base init)     |  |  specific CQS,       |  |  layer)       |
+------------------+  |  CommandHandlers,     |  +---------------+
        |              |  QueryHandlers)       |         |
        v              +---------------------+         |
+---------------------------------------------------------------+
|              Transport Implementations                          |
|  SqlServer | PostgreSQL | SQLite | Redis | LiteDB | Memory     |
+---------------------------------------------------------------+
        |                      |                      |
        v                      v                      v
+---------------------------------------------------------------+
|         External Storage (databases, Redis, filesystem)        |
+---------------------------------------------------------------+

+---------------------------------------------------------------+
|                     Dashboard Stack                             |
|  Dashboard.Api (ASP.NET Core REST) --> Dashboard.Ui (Blazor)  |
|  Dashboard.Client (HTTP client + consumer tracking)            |
+---------------------------------------------------------------+
```

## Key Architectural Patterns

### 1. Transport Abstraction via IoC Registration

The central architectural decision is that **every queue operation is abstracted behind an interface, and each transport provides its own implementations by registering them into a SimpleInjector container**. The `ITransportInit` interface (defined in `Source/DotNetWorkQueue/ITransportInit.cs`) is the entry point for every transport:

```csharp
public interface ITransportInit
{
    void RegisterImplementations(IContainer container, RegistrationTypes registrationType, QueueConnection queueConnection);
    void SuppressWarningsIfNeeded(IContainer container, RegistrationTypes registrationType);
    void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType);
    bool IsRelationalTransport { get; }
}
```

- Evidence: `Source/DotNetWorkQueue/ITransportInit.cs`
- Sub-interfaces: `ITransportInitSend`, `ITransportInitReceive`, `ITransportInitDuplex` control whether a transport supports send, receive, or both.
- Evidence: `Source/DotNetWorkQueue/Configuration/TransportInitDuplex.cs`, `TransportInitSend.cs`, `TransportInitReceive.cs`

The `CreateContainer<T>.Create()` method orchestrates the full registration pipeline:
1. Register core defaults (`ComponentRegistration.RegisterDefaults`)
2. Allow override mode on the container
3. Call `register.RegisterImplementations()` -- transport-specific registrations
4. Call user-provided `registerServiceInternal` -- internal overrides
5. Call user-provided `registerService` -- caller overrides
6. Disable overrides, register conditional fallbacks (`RegisterFallbacks`)
7. Register decorators for metrics, logging, tracing, policies
8. Verify container (DEBUG only)
9. Call `SetDefaultsIfNeeded` -- transport defaults for configuration

- Evidence: `Source/DotNetWorkQueue/IoC/CreateContainer.cs` (lines 57-147)

### 2. Transport Init Hierarchy

All concrete transports inherit through a layered init hierarchy:

```
ITransportInit
  +-- ITransportInitDuplex (abstract: TransportInitDuplex)
        +-- TransportMessageQueueSharedInit (registers ITransportCommitMessage, ITransportHandleMessage)
              +-- SqlServerMessageQueueInit
              +-- RedisQueueInit
              +-- PostgreSqlMessageQueueInit
              +-- SqLiteMessageQueueInit
              +-- LiteDbMessageQueueInit
              +-- MemoryMessageQueueInit
```

- Evidence: `Source/DotNetWorkQueue.Transport.Shared/TransportMessageQueueSharedIinit.cs` (line 29: `public class TransportMessageQueueSharedInit : TransportInitDuplex`)
- Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs` (line 47: `public class SqlServerMessageQueueInit : TransportMessageQueueSharedInit`)
- Evidence: `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs` (line 48: `public class RedisQueueInit : TransportMessageQueueSharedInit`)

Relational transports (SqlServer, PostgreSQL, SQLite) additionally use `RelationalDatabaseMessageQueueInit<TQueueId, TCorrelationId>` for shared SQL-based command/query registration via assembly scanning.

- Evidence: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalDatabaseMessageQueueInit.cs` (lines 42-458)
- The generic parameters (`<long, Guid>` for SQL Server) determine the queue ID and correlation ID types.

### 3. Command/Query Separation (CQS)

All data access operations are modeled as either commands (mutations) or queries (reads), defined in `Transport.Shared`:

- **`ICommandHandler<TCommand>`** -- executes a mutation with no return value
  - Evidence: `Source/DotNetWorkQueue.Transport.Shared/ICommandHandler.cs`
- **`ICommandHandlerWithOutput<TCommand, TOutput>`** -- executes a mutation returning a result
  - Evidence: `Source/DotNetWorkQueue.Transport.Shared/ICommandHandlerWithOutput.cs`
- **`IQueryHandler<TQuery, TResult>`** -- executes a synchronous query
  - Evidence: `Source/DotNetWorkQueue.Transport.Shared/IQueryHandler.cs`
- **`IQueryHandlerAsync<TQuery, TResult>`** -- executes an async query
  - Evidence: `Source/DotNetWorkQueue.Transport.Shared/IQueryHandlerAsync.cs`

For relational transports, there is a **Prepare Handler** layer (`IPrepareCommandHandler<T>`, `IPrepareQueryHandler<T,R>`) that separates SQL parameter preparation from execution, allowing transport-specific SQL dialects while sharing execution logic.

- Evidence: `Source/DotNetWorkQueue.Transport.RelationalDatabase/IPrepareCommandHandler.cs`, `IPrepareQueryHandler.cs`

Command/query handlers are registered by **assembly scanning** in `RelationalDatabaseMessageQueueInit.RegisterCommands()`:

```csharp
container.Register(typeof(ICommandHandler<>), LifeStyles.Singleton, target);
container.Register(typeof(IQueryHandler<,>), LifeStyles.Singleton, target);
```

- Evidence: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalDatabaseMessageQueueInit.cs` (lines 415-458)

### 4. Decorator Pattern for Cross-Cutting Concerns

The system makes extensive use of the Decorator pattern (via SimpleInjector's `RegisterDecorator`) to layer cross-cutting behavior onto core interfaces. Decorators are registered in a specific order in `ComponentRegistration.RegisterSharedDefaults()`:

1. **Message cancellation** -- wraps handler with per-message cancel token
2. **Message ID scope** -- adds message ID to logging scope
3. **History** -- records message processing history
4. **Metrics** -- instruments operations with `System.Diagnostics.Metrics`
5. **Policies** -- wraps with Polly resilience pipelines
6. **Logging** -- adds structured logging
7. **Tracing** -- adds OpenTelemetry distributed tracing spans

- Evidence: `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs` (lines 326-437)

Transport-specific decorators add retry behavior around command/query handlers:

```csharp
container.RegisterDecorator(typeof(ICommandHandlerWithOutput<,>),
    typeof(RetryCommandHandlerOutputDecorator<,>), LifeStyles.Singleton);
container.RegisterDecorator(typeof(IQueryHandler<,>),
    typeof(RetryQueryHandlerDecorator<,>), LifeStyles.Singleton);
```

- Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs` (lines 154-164)

The Metrics decorator classes are in `Source/DotNetWorkQueue/Metrics/Decorator/` (19 decorator files covering all major interfaces).

### 5. Producer/Consumer Pattern

The public API is exposed through container wrapper classes parameterized by transport type:

- **`QueueContainer<TTransportInit>`** -- creates producer and consumer queues
  - Evidence: `Source/DotNetWorkQueue/QueueContainer.cs` (line 34)
- **`QueueCreationContainer<TTransportInit>`** -- creates/manages queue infrastructure
  - Evidence: `Source/DotNetWorkQueue/QueueCreationContainer.cs` (line 31)
- **`SchedulerContainer`** -- creates task schedulers
  - Evidence: `Source/DotNetWorkQueue/SchedulerContainer.cs`
- **`JobSchedulerContainer`** -- creates recurring job schedulers
  - Evidence: `Source/DotNetWorkQueue/JobSchedulerContainer.cs`

Each `Create*` method on `QueueContainer` creates a **new IoC container** scoped to that queue instance, with appropriate `QueueContexts` and `ConnectionTypes`:

```csharp
var container = _createContainerInternal().Create(
    QueueContexts.ConsumerQueue, _registerService, queueConnection,
    _transportInit, ConnectionTypes.Receive, x => { }, _setOptions);
return container.GetInstance<IConsumerQueue>();
```

- Evidence: `Source/DotNetWorkQueue/QueueContainer.cs` (lines 81-89)

## Message Lifecycle / Data Flow

### Producer Side (Send)

```
User code
  --> ProducerQueue<T>.Send(message, data)
        --> CheckMessageType() -- enforces public class requirement
        --> GenerateMessageHeaders.HeaderSetup(data) -- builds header dictionary
        --> MessageFactory.Create(message, headers) -- wraps in IMessage
        --> AddStandardMessageHeaders.AddHeaders(message, data) -- adds correlation ID, timestamp, etc.
        --> ISendMessages.Send(message, data)
              --> [PolicyDecorator] wraps with Polly pipeline
              --> [TraceDecorator] creates OpenTelemetry span
              --> [MetricsDecorator] records send metrics
              --> Transport-specific SendMessages implementation
                    --> ICommandHandlerWithOutput<SendMessageCommand, long>.Handle()
                          --> [RetryDecorator] wraps with SQL retry policy
                          --> [TraceDecorator] adds transport-specific tags
                          --> Actual SQL INSERT / Redis EVAL / etc.
```

- Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 270-283)

### Consumer Side (Receive + Process)

```
ConsumerQueue.Start<T>(workerAction)
  --> RegisterMessages.Register(workerAction) -- stores the user delegate
  --> QueueMonitor.Start() -- starts heartbeat, expiration, error clearing monitors
  --> PrimaryWorker.Start() -- creates worker thread(s)
        --> WorkerCollection.Start() -- starts additional worker threads

PrimaryWorker.MainLoop() [on dedicated thread]:
  while (!ShouldExit):
    --> Check for idle state / single-worker optimization
    --> MessageProcessing.Handle()
          --> IMessageContextFactory.Create() -- creates per-message context
          --> IReceiveMessages.ReceiveMessage(context)
                --> [TraceDecorator] creates receive span
                --> [PolicyDecorator] wraps with Polly pipeline
                --> [MetricsDecorator] records receive metrics
                --> Transport-specific dequeue (SQL SELECT + UPDATE / Redis EVAL)
          --> If message received:
                --> ProcessMessage.Handle(context, transportMessage)
                      --> IHeartBeatWorkerFactory.Create(context) -- starts heartbeat for this message
                      --> IHandleMessage.Handle(message, workerNotification)
                            --> [TraceDecorator] creates handle span
                            --> [MetricsDecorator] records handle metrics
                            --> [CancellationDecorator] registers cancel token
                            --> User workerAction delegate invoked
                      --> ICommitMessage.Commit(context) -- mark message as completed
                      --> HeartBeat.Stop() -- stop heartbeat for this message
                --> On exception: MessageExceptionHandler handles error
                      --> Move to error queue / increment retry count
          --> If no message: IQueueWait backoff delay
```

- Evidence: `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs` (lines 103-127)
- Evidence: `Source/DotNetWorkQueue/Queue/PrimaryWorker.cs` (lines 121-148)
- Evidence: `Source/DotNetWorkQueue/Queue/MessageProcessing.cs` (lines 66-99, 114-120)
- Evidence: `Source/DotNetWorkQueue/Queue/ProcessMessage.cs` (lines 69-98)

### Message States

Messages transition through these states during their lifecycle:

```
[Waiting] --> [Processing] --> [Completed]
                |                  |
                v                  v
           [Error Queue]     [Deleted]
                |
                v
           [Retry] --> [Waiting]
```

- Evidence: `Source/DotNetWorkQueue/QueueStatuses.cs` [Inferred from usage in command handlers]

## DI Container Architecture

### Container Creation Flow

```
QueueContainer<T>.CreateConsumer(queueConnection)
  --> CreateContainer<T>.Create(queueType, registerService, queueConnection, transportInit, connectionType, ...)
        1. new SimpleInjector.Container()
        2. Wrap in ContainerWrapper (implements IContainer)
        3. Register QueueContext singleton (tracks what type of queue this is)
        4. ComponentRegistration.RegisterDefaults(container, registrationType, connection)
             --> RegisterSharedDefaults() -- logging, tracing, policies, serialization, headers
             --> If Send: register producer config, message factories, producer queue
             --> If Receive: register consumer config, workers, monitors, heartbeat, schedulers
        5. container.Options.AllowOverridingRegistrations = true
        6. transportInit.RegisterImplementations() -- transport overrides core registrations
        7. registerServiceInternal() -- internal overrides (e.g., task factory injection)
        8. registerService() -- user overrides
        9. container.Options.AllowOverridingRegistrations = false
       10. ComponentRegistration.RegisterFallbacks() -- conditional/generic registrations
       11. transportInit.SuppressWarningsIfNeeded()
       12. ComponentRegistration.SuppressWarningsIfNeeded()
       13. container.Verify() -- DEBUG ONLY
       14. ComponentRegistration.SetupDefaultPolicies()
       15. transportInit.SetDefaultsIfNeeded() -- transport-specific config defaults
       16. setOptions?.Invoke() -- user option overrides
       17. Return container
```

- Evidence: `Source/DotNetWorkQueue/IoC/CreateContainer.cs` (lines 57-147)

### Container Isolation

Each queue instance gets its own dedicated SimpleInjector container. The `BaseContainer` class manages disposal of all child containers:

```csharp
protected readonly ConcurrentBag<IDisposable> Containers;
```

- Evidence: `Source/DotNetWorkQueue/BaseContainer.cs` (lines 31-101)

Container creation is **thread-safe** via a static lock:

```csharp
lock (ContainerLocker.Locker) { ... }
```

- Evidence: `Source/DotNetWorkQueue/IoC/CreateContainer.cs` (line 66)

### Registration Types

The `RegistrationTypes` flags enum controls which registrations are activated:

- `Send` -- producer-side registrations (message factories, send config)
- `Receive` -- consumer-side registrations (workers, monitors, heartbeat)
- `Send | Receive` -- full duplex (most transports)

- Evidence: `Source/DotNetWorkQueue/IoC/CreateContainer.cs` (lines 174-191, `GetRegistrationType` method)

## Threading and Concurrency Model

### Worker Thread Architecture

The consumer side uses a **dedicated thread per worker** model:

1. **PrimaryWorker** -- one per consumer queue, runs the main processing loop on its own `Thread`
   - Evidence: `Source/DotNetWorkQueue/Queue/PrimaryWorker.cs` (line 83: `WorkerThread = new Thread(MainLoop)`)
2. **Worker** (additional) -- zero or more additional workers based on `IWorkerConfiguration.WorkerCount`
   - Evidence: `Source/DotNetWorkQueue/Queue/Worker.cs` (line 77: `WorkerThread = new Thread(MainLoop)`)
3. **WorkerCollection** -- manages the set of additional workers
   - Evidence: `Source/DotNetWorkQueue/Queue/WorkerCollection.cs`

Workers support an **adaptive idle mode**: when all workers are idle and `SingleWorkerWhenNoWorkFound` is enabled, additional workers are paused and only the primary worker polls. When work is found, all workers are resumed.

- Evidence: `Source/DotNetWorkQueue/Queue/PrimaryWorker.cs` (lines 130-133)

### Thread-Safe Disposal

All disposable objects use `Interlocked.Increment` for thread-safe single-disposal:

```csharp
if (Interlocked.Increment(ref _disposeCount) != 1) return;
```

- Evidence: `Source/DotNetWorkQueue/Queue/BaseQueue.cs` (line 185), `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (line 231)

### Async Task Tracking

The producer tracks in-flight async operations via `Interlocked.Increment`/`Decrement` on `_asyncTaskCount`, and disposal waits for all async tasks to complete:

```csharp
if (_asyncTaskCount > 0)
{
    WaitOnAsyncTask.Wait(() => _asyncTaskCount > 0, ...);
}
```

- Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 144-156, 233-238)

### HeartBeat System

Long-running messages use a heartbeat to signal liveness:

- **HeartBeatScheduler** -- singleton that manages a thread pool for sending heartbeats
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatScheduler.cs`
- **HeartBeatWorker** -- per-message heartbeat worker, created and disposed per message processing
  - Evidence: `Source/DotNetWorkQueue/Queue/ProcessMessage.cs` (line 71: `using (var heartBeat = _heartBeatWorkerFactory.Create(context))`)
- **HeartBeatMonitor** -- background monitor that resets messages whose heartbeats have expired (dead consumer detection)
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatMonitor.cs`

### Queue Monitor System

The `QueueMonitor` orchestrates multiple background monitors that run on their own threads:

1. **HeartBeatMonitor** -- resets stale heartbeats (dead consumer detection)
2. **ClearExpiredMessagesMonitor** -- removes messages past their expiration
3. **ClearErrorMessagesMonitor** -- cleans up error records after configured time
4. **ClearHistoryMonitor** -- purges old history records

Monitors are started conditionally based on configuration and transport options:

```csharp
if (_heartBeatConfiguration.Enabled) _monitors.Add(_heartBeatFactory);
if (_expirationConfiguration.Enabled) _monitors.Add(_clearMessagesFactory);
if (_clearMessageErrorConfiguration.Enabled) _monitors.Add(_clearErrorMessages);
if (_baseTransportOptions.EnableHistory) _monitors.Add(_clearHistoryMonitor);
```

- Evidence: `Source/DotNetWorkQueue/Queue/QueueMonitor.cs` (lines 86-116)

### Maintenance Mode

Monitors can run in either consumer mode or standalone maintenance mode, controlled by `MaintenanceMode` enum:

- Evidence: `Source/DotNetWorkQueue/Configuration/MaintenanceMode.cs`
- Evidence: `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs` (line 117: `if (_configuration.MaintenanceMode == MaintenanceMode.Consumer)`)

## Configuration Hierarchy

Configuration is structured in a composable hierarchy:

```
QueueConsumerConfiguration (consumer-side config)
  +-- TransportConfigurationReceive (transport-level receive settings)
  +-- IWorkerConfiguration (thread count, idle behavior)
  +-- IHeartBeatConfiguration (enabled, interval, monitor time)
  +-- IMessageExpirationConfiguration (enabled, monitor time)
  +-- IMessageErrorConfiguration (enabled, age threshold)
  +-- IHeaders (standard + custom message headers)
  +-- IConfiguration (AdditionalConfiguration -- bag for transport-specific options)
  +-- BaseTimeConfiguration (SNTP time sync settings)

QueueProducerConfiguration (producer-side config)
  +-- TransportConfigurationSend (transport-level send settings)
  +-- IHeaders
  +-- IConfiguration (AdditionalConfiguration)
  +-- BaseTimeConfiguration
```

- Evidence: `Source/DotNetWorkQueue/Configuration/QueueConsumerConfiguration.cs` (lines 29-64)

Transport-specific options (e.g., `SqlServerMessageQueueTransportOptions`) implement `ITransportOptions` and are stored in the `AdditionalConfiguration` dictionary:

```csharp
configurationSend.AdditionalConfiguration.SetSetting(configurationSendName, options);
```

- Evidence: `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalDatabaseMessageQueueInit.cs` (lines 106-110)

Configuration objects become read-only once the queue starts:

- Evidence: `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs` (line 123: `_configuration.SetReadOnly()`)
- Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 275-276)

## Extension Points

### Adding a New Transport

To add a new transport:

1. **Create a project** referencing `DotNetWorkQueue` (and optionally `Transport.Shared` or `Transport.RelationalDatabase`)
2. **Implement an init class** extending `TransportMessageQueueSharedInit` (which extends `TransportInitDuplex`)
3. **Override `RegisterImplementations`** to register:
   - `IConnectionInformation` -- connection info parsing
   - `ISendMessages` -- message sending
   - `IReceiveMessages` -- message receiving
   - `IQueueCreation` -- schema/table creation
   - `IRemoveMessage` -- message deletion
   - Transport-specific command/query handlers
4. **Override `SetDefaultsIfNeeded`** to configure heartbeat, expiration, retry policies
5. **Optionally register decorators** for transport-specific retry behavior

For relational databases, use `RelationalDatabaseMessageQueueInit<TQueueId, TCorrelationId>` which provides assembly-scanned registration of all CQS handlers and shared SQL implementations.

- Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs` (full file)
- Evidence: `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs` (full file)

### Interceptors

Message interceptors (`IMessageInterceptor`) can transform messages during serialization/deserialization. Built-in interceptors:

- `GZipMessageInterceptor` -- compression
- `TripleDesMessageInterceptor` -- encryption

- Evidence: `Source/DotNetWorkQueue/Interceptors/GZipMessageInterceptor.cs`, `TripleDesMessageInterceptor.cs`

### Policies

Polly v8 resilience pipelines are registered per-transport via `IPolicies.Registry`:

- Evidence: `Source/DotNetWorkQueue/Policies/Policies.cs` (lines 24-51)
- Transports define their own retry policies (e.g., SQL transient error retry)
- Evidence: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/RetrySqlPolicyCreation.cs`

## Summary Table

| Aspect | Detail | Confidence |
|--------|--------|------------|
| Architecture style | Modular library with transport plugin system via IoC | Observed |
| IoC container | SimpleInjector 5.5.0, one container per queue instance | Observed |
| Transport plugin mechanism | `ITransportInit.RegisterImplementations()` overrides core registrations | Observed |
| Data access pattern | Command/Query Separation (CQS) with handler interfaces | Observed |
| Cross-cutting concerns | Decorator pattern (metrics, logging, tracing, policies, cancellation, history) | Observed |
| Threading model | Dedicated `Thread` per worker, adaptive idle optimization | Observed |
| Heartbeat mechanism | Per-message heartbeat via `HeartBeatWorker`, background monitor for dead detection | Observed |
| Configuration | Composable hierarchy, becomes read-only at queue start | Observed |
| Serialization | Newtonsoft.Json default, pluggable via `ISerializer` | Observed |
| Resilience | Polly v8 `ResiliencePipelineRegistry`, per-transport policies | Observed |
| Tracing | OpenTelemetry via `System.Diagnostics.ActivitySource` | Observed |
| Metrics | `System.Diagnostics.Metrics` (built-in .NET metrics) | Observed |
| Multi-targeting | net10.0, net8.0, net48, netstandard2.0 with conditional compilation | Observed |
| Dashboard | ASP.NET Core REST API + Blazor Server UI + HTTP client | Observed |

## Open Questions

- The `ContainerLocker.Locker` static lock on container creation (line 66 of `CreateContainer.cs`) suggests a threading issue with SimpleInjector decorator registration. The comment says "should not be needed, but have found no other solution." This could be a performance bottleneck if many queues are created concurrently.
- [Inferred] The Memory transport includes a full in-process implementation inside the core `DotNetWorkQueue` project (`Source/DotNetWorkQueue/Transport/Memory/Basic/`), which appears to be the default/fallback transport. The actual `DotNetWorkQueue.Transport.Memory` project may extend or wrap this.
- The `QueueContexts` enum (referenced in `CreateContainer.cs`) controls which context the container is created for (ConsumerQueue, ProducerQueue, Admin, etc.), but its full set of values was not examined directly.
