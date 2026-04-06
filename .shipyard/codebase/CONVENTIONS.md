# Conventions

## Overview
DotNetWorkQueue follows a consistent set of naming, structural, and behavioral conventions across its core library and transport implementations. The codebase prioritizes thread-safe disposal, null-guard validation on all constructor parameters, and a decorator-based approach to cross-cutting concerns (logging, tracing, metrics). Source files in the production library carry LGPL-2.1 license headers; test files are mixed, with older tests omitting headers and newer tests including them.

## Findings

### Naming Conventions

- **Interface prefix `I`**: All interfaces use the `I` prefix consistently throughout the codebase.
  - Evidence: `Source/DotNetWorkQueue/IQueue.cs`, `Source/DotNetWorkQueue/IConsumerQueue.cs`, `Source/DotNetWorkQueue/IProducerQueue.cs`, `Source/DotNetWorkQueue/IContainer.cs`

- **Abstract class prefix `A`**: Abstract base classes use the `A` prefix rather than the more common `Abstract` or `Base` prefix.
  - Evidence: `Source/DotNetWorkQueue/ASerializer.cs` (line 30: `public abstract class ASerializer`)
  - Evidence: `Source/DotNetWorkQueue/ASendJobToQueue.cs`
  - Evidence: `Source/DotNetWorkQueue/ATaskScheduler.cs`

- **Abstract class suffix `Base`**: Some abstract classes use the `Base` suffix instead of the `A` prefix.
  - Evidence: `Source/DotNetWorkQueue/BaseContainer.cs` (line 31: `public abstract class BaseContainer`)
  - Evidence: `Source/DotNetWorkQueue/Queue/BaseQueue.cs` (line 30: `public abstract class BaseQueue`)
  - Evidence: `Source/DotNetWorkQueue/Queue/BaseMonitor.cs`, `Source/DotNetWorkQueue/Queue/MultiWorkerBase.cs`, `Source/DotNetWorkQueue/Queue/WorkerBase.cs`
  - [Inferred] The `A` prefix appears on older, higher-level abstractions, while `Base` suffix appears on queue-related base classes. No strict rule separates the two conventions.

- **Factory suffix `Factory`**: Factory classes and interfaces consistently use the `Factory` suffix.
  - Evidence: `Source/DotNetWorkQueue/IContainerFactory.cs`, `Source/DotNetWorkQueue/Factory/HeartBeatWorkerFactory.cs`, `Source/DotNetWorkQueue/Factory/MessageContextFactory.cs`, `Source/DotNetWorkQueue/Factory/PrimaryWorkerFactory.cs`
  - The Factory directory contains 24 factory implementations.

- **Configuration suffix `Configuration`**: Configuration classes use the `Configuration` suffix.
  - Evidence: `Source/DotNetWorkQueue/Configuration/QueueConsumerConfiguration.cs`, `Source/DotNetWorkQueue/Configuration/HeartBeatConfiguration.cs`, `Source/DotNetWorkQueue/Configuration/WorkerConfiguration.cs`

- **NoOp suffix**: No-operation implementations use the `NoOp` suffix and implement `INoOperation` as a marker interface.
  - Evidence: `Source/DotNetWorkQueue/INoOperation.cs` (line 24: `public interface INoOperation`)
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorkerNoOp.cs` (line 27: `public class HeartBeatWorkerNoOp : IHeartBeatWorker, INoOperation`)
  - Evidence: `Source/DotNetWorkQueue/Queue/ClearExpiredMessagesMonitorNoOp.cs`, `Source/DotNetWorkQueue/Queue/QueueWaitNoOp.cs`, `Source/DotNetWorkQueue/Queue/HeartBeatMonitorNoOp.cs`, `Source/DotNetWorkQueue/Queue/ClearHistoryMonitorNoOp.cs`

- **Decorator suffix**: Decorator classes are named `{Interface}Decorator` and reside in `Decorator` subdirectories organized by concern (Logging, Trace, Metrics).
  - Evidence: `Source/DotNetWorkQueue/Logging/Decorator/RollbackMessageDecorator.cs`
  - Evidence: `Source/DotNetWorkQueue/Trace/Decorator/CommitMessageDecorator.cs`
  - Evidence: `Source/DotNetWorkQueue/Metrics/Decorator/ICommitMessageDecorator.cs`

- **Command/Query naming**: Transport data-access operations follow CQRS naming with `Command` and `Query` suffixes.
  - Evidence: `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs`, `Source/DotNetWorkQueue.Transport.Shared/Basic/Query/GetJobIDQuery.cs`
  - Handlers are named `ICommandHandler<TCommand>` and `IQueryHandler<TQuery, TResult>`.
  - Evidence: `Source/DotNetWorkQueue.Transport.Shared/ICommandHandler.cs`

- **Test class naming**: Test classes mirror source class names with a `Tests` suffix. Test methods use PascalCase with underscores describing the scenario.
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (line 17: `public class ConsumerQueueTests`)
  - Test method examples: `IsDisposed_False_By_Default`, `Disposed_Instance_Sets_IsDisposed`, `Call_Dispose_Multiple_Times_Ok`, `Set_HeartBeatMonitorTime_WhenReadOnly_Fails`

- **File naming**: One public type per file, file named after the type. Interfaces have their own files (e.g., `IQueue.cs` for `IQueue`).
  - Evidence: `Source/DotNetWorkQueue/IQueue.cs`, `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs`, `Source/DotNetWorkQueue/Queue/ProducerQueue.cs`

### License Header Convention

- **LGPL-2.1 header required on all production source files**: Every `.cs` file in the main library and transport projects begins with a standard 18-line LGPL-2.1 license block.
  - Evidence: `Source/DotNetWorkQueue/IQueue.cs` (lines 1-18), `Source/DotNetWorkQueue/Queue/BaseQueue.cs` (lines 1-18), `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 1-18)
  - The header format is:
    ```csharp
    // ---------------------------------------------------------------------
    //This file is part of DotNetWorkQueue
    //Copyright © 2015-2026 Brian Lehnen
    //
    //This library is free software; you can redistribute it and/or
    //modify it under the terms of the GNU Lesser General Public
    //License as published by the Free Software Foundation; either
    //version 2.1 of the License, or (at your option) any later version.
    // ... (full LGPL-2.1 notice)
    // ---------------------------------------------------------------------
    ```

- **Test files mostly omit license headers**: The majority of unit test files do not include the license header. Only a small number of recently-added test files include it.
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (starts with `using System;` at line 1, no header)
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/AddStandardMessageHeadersTests.cs` (starts with license header -- newer file)
  - Count: approximately 3 of 167 test files in the core test project include the license header.

- **Third-party vendored code uses its own license**: The Guard class carries a BSD license from NETFx.
  - Evidence: `Source/DotNetWorkQueue/netfx/System/Guard.cs` (lines 1-31, BSD license block)

### Validation / Guard Clauses

- **`Guard.NotNull` on every constructor parameter**: All constructors validate injected dependencies using `Guard.NotNull(() => param, param)`, a lambda-expression-based guard from the NETFx library.
  - Evidence: `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs` (lines 63-68: six Guard.NotNull calls)
  - Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 67-72: six Guard.NotNull calls)
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (lines 75-80: six Guard.NotNull calls)
  - The Guard class resides in `Source/DotNetWorkQueue/netfx/System/Guard.cs` under namespace `DotNetWorkQueue.Validation`.

- **`Guard.NotNull` on public method parameters**: Public methods that accept reference parameters also validate with `Guard.NotNull`.
  - Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 272-273 in `InternalSend`)
  - Evidence: `Source/DotNetWorkQueue/ASerializer.cs` (line 51 in `MessageToBytes`)

### Disposal Pattern

- **Thread-safe disposal via `Interlocked` counter**: The standard disposal pattern uses a private `int _disposeCount` field, with `Interlocked.Increment` to ensure disposal runs exactly once.
  - Evidence: `Source/DotNetWorkQueue/Queue/BaseQueue.cs` (lines 41, 182-186):
    ```csharp
    private int _disposeCount;
    // ...
    if (Interlocked.Increment(ref _disposeCount) != 1) return;
    ```
  - Evidence: `Source/DotNetWorkQueue/BaseContainer.cs` (lines 33, 87)
  - Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 47, 231)
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (lines 41, 156)

- **`IsDisposed` property via `Interlocked.CompareExchange`**: The `IsDisposed` property reads the dispose count atomically.
  - Evidence: `Source/DotNetWorkQueue/Queue/BaseQueue.cs` (line 194):
    ```csharp
    public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;
    ```

- **`ThrowIfDisposed` with `[CallerMemberName]`**: A protected method throws `ObjectDisposedException` using the caller's name.
  - Evidence: `Source/DotNetWorkQueue/Queue/BaseQueue.cs` (lines 162-168):
    ```csharp
    protected void ThrowIfDisposed([CallerMemberName] string name = "")
    {
        if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            throw new ObjectDisposedException(name);
    }
    ```
  - This pattern is duplicated in each class that implements `IDisposable` -- it is not inherited from a single base. `BaseQueue`, `BaseContainer`, `ProducerQueue<T>`, and `HeartBeatWorker` all define their own `_disposeCount` and `ThrowIfDisposed`.

- **`IIsDisposed` interface**: Classes expose disposal state via `IIsDisposed.IsDisposed`.
  - Evidence: `Source/DotNetWorkQueue/IIsDisposed.cs`

- **Standard `Dispose(bool)` pattern**: All disposable classes follow the standard .NET Dispose pattern with `Dispose()` calling `Dispose(true)` + `GC.SuppressFinalize(this)`.
  - Evidence: `Source/DotNetWorkQueue/Queue/BaseQueue.cs` (lines 172-176)
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorkerNoOp.cs` (lines 60-64)

### Thread Safety Patterns

- **`Monitor.Enter`/`Monitor.Exit` for property access**: Thread-safe boolean properties use explicit `Monitor.Enter`/`Monitor.Exit` with try/finally rather than `lock` statements.
  - Evidence: `Source/DotNetWorkQueue/Queue/BaseQueue.cs` (lines 80-88, `ShouldWork` getter):
    ```csharp
    Monitor.Enter(_shouldWorkLocker);
    try { return _shouldWork; }
    finally { Monitor.Exit(_shouldWorkLocker); }
    ```
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (lines 282-290, `Running` property; lines 316-326, `Stopped` property)

- **`Interlocked` for atomic operations**: Counters (dispose count, async task count, started flag) use `Interlocked` methods.
  - Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 144, 155 -- `_asyncTaskCount` tracking with `Interlocked.Increment`/`Decrement`)
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (line 99 -- `Interlocked.CompareExchange(ref _started, 1, 0)`)

- **`lock` for critical sections**: Traditional lock statements are used for larger critical sections.
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (lines 104, 158, 257-265 -- nested locks for cancel token)

- **`ConcurrentBag<T>` for thread-safe collections**: Used when parallel processing requires thread-safe addition.
  - Evidence: `Source/DotNetWorkQueue/BaseContainer.cs` (line 38: `ConcurrentBag<IDisposable>`)
  - Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (line 348: `ConcurrentBag<QueueMessage<IMessage, IAdditionalMessageData>>` in `InternalSendPrepare`)

### Error Handling Patterns

- **Custom exception hierarchy**: All domain exceptions inherit from `DotNetWorkQueueException`, which extends `System.Exception`.
  - Evidence: `Source/DotNetWorkQueue/Exceptions/DotNetWorkQueueException.cs` (line 28: `public class DotNetWorkQueueException : Exception`)
  - Specialized exceptions: `SerializationException`, `InterceptorException`, `CommitException`, `CompileException`, `JobSchedulerException`, `MessageException`, `PoisonMessageException`, `ReceiveMessageException`
  - Evidence: All files in `Source/DotNetWorkQueue/Exceptions/`

- **Wrapping exceptions with domain types**: Low-level exceptions are caught and re-thrown as domain-specific exceptions with descriptive messages.
  - Evidence: `Source/DotNetWorkQueue/ASerializer.cs` (lines 58-59):
    ```csharp
    catch (Exception error)
    {
        throw new SerializationException("An error has occurred when converting a message into byte array", error);
    }
    ```

- **Logging decorators for error swallowing**: The logging decorator pattern catches exceptions from inner handlers, logs them, and returns a default value instead of propagating.
  - Evidence: `Source/DotNetWorkQueue/Logging/Decorator/RollbackMessageDecorator.cs` (lines 47-55):
    ```csharp
    try { return _handler.Rollback(context); }
    catch (Exception e) {
        _log.LogError($"An error has occurred while trying to rollback a message{System.Environment.NewLine}{e}");
        return false;
    }
    ```

- **`ThreadAbortException` handling**: Specific handling for thread abort (relevant to .NET Framework).
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (lines 222-232)

### Logging Patterns

- **`Microsoft.Extensions.Logging.ILogger`**: The project uses `Microsoft.Extensions.Logging` for all logging.
  - Evidence: `Source/DotNetWorkQueue/Queue/BaseQueue.cs` (line 20: `using Microsoft.Extensions.Logging;`, line 65: `protected ILogger Log { get; }`)
  - Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (line 46: `private readonly ILogger _log;`)

- **String interpolation for log messages**: Log messages use C# string interpolation directly.
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (line 212):
    ```csharp
    _logger.LogTrace($"Set heartbeat for message {status.MessageId.Id.Value}");
    ```
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (line 237):
    ```csharp
    _logger.LogError($"An error has occurred while updating the heartbeat field...{System.Environment.NewLine}{error}");
    ```

- **Log levels used**: `LogTrace` for fine-grained operational events, `LogDebug` for queue start/expected conditions, `LogWarning` for non-fatal issues, `LogError` for caught exceptions.
  - Evidence: `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs` (line 125: `Log.LogDebug("Queue started")`)
  - Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (line 237: `_log.LogWarning(...)`)
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (lines 167, 212, 216, 224, 236)

### Configuration Immutability Pattern

- **`IReadonly` / `ISetReadonly` interfaces**: Configuration objects become immutable after the queue starts, enforced by `SetReadOnly()` and `FailIfReadOnly()`.
  - Evidence: `Source/DotNetWorkQueue/IReadonly.cs` (line 24: `public interface IReadonly`)
  - Evidence: `Source/DotNetWorkQueue/Configuration/QueueConsumerConfiguration.cs` (lines 122-127):
    ```csharp
    public MaintenanceMode MaintenanceMode
    {
        get => _maintenanceMode;
        set { FailIfReadOnly(); _maintenanceMode = value; }
    }
    ```
  - Evidence: `Source/DotNetWorkQueue/Configuration/QueueConsumerConfiguration.cs` (lines 161-167, `FailIfReadOnly` method)

- **Read-only propagation**: `SetReadOnly()` cascades to child configurations.
  - Evidence: `Source/DotNetWorkQueue/Configuration/QueueConsumerConfiguration.cs` (lines 140-145):
    ```csharp
    protected set {
        _isReadonly = value;
        HeartBeat.SetReadOnly();
        MessageExpiration.SetReadOnly();
        Worker.SetReadOnly();
        TransportConfiguration.SetReadOnly();
    }
    ```

### Async Patterns

- **`ConfigureAwait(false)` everywhere**: All `await` calls in the library use `ConfigureAwait(false)` to avoid synchronization context capture.
  - Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 149, 151, 173, 193, 305, 335)
  - Evidence: `Source/DotNetWorkQueue/Messages/HandleMessage.cs`, `Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs`

- **Async task count tracking**: `ProducerQueue<T>` tracks outstanding async operations with `Interlocked` and blocks disposal until they complete.
  - Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 144-157, 233-238)

### Conditional Compilation

- **`#if NETFULL`**: Used to gate .NET Framework 4.8-specific functionality, primarily thread abort and dynamic LINQ features.
  - Evidence: `Source/DotNetWorkQueue/Queue/AbortWorkerThread.cs` (lines 65-72):
    ```csharp
    #if NETFULL
        workerThread.Abort();
        return true;
    #else
        return false;
    #endif
    ```
  - Used in 11 files total: `ProducerMethodQueue.cs`, `ProducerMethodJobQueue.cs`, `ScheduledJob.cs`, `CompileException.cs`, `AbortWorkerThread.cs`, and several interface/scheduling files.
  - Evidence: Grep across `Source/DotNetWorkQueue` found 11 files with `#if NETFULL`

- **`NETSTANDARD2_0`**: Defined for .NET Standard 2.0 builds but currently only appears in project property groups, not in `#if` directives within source code.
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 24-26)

### Import/Using Ordering

- **System namespaces first, then project namespaces**: The predominant pattern places `System.*` using statements first, followed by `DotNetWorkQueue.*` namespaces, followed by third-party namespaces like `Microsoft.Extensions.Logging`.
  - Evidence: `Source/DotNetWorkQueue/Queue/ProducerQueue.cs` (lines 19-30):
    ```csharp
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetWorkQueue.Configuration;
    using DotNetWorkQueue.Logging;
    using DotNetWorkQueue.Messages;
    using DotNetWorkQueue.Validation;
    using Microsoft.Extensions.Logging;
    ```
  - [Inferred] This convention is not enforced by any visible linter or `.editorconfig` -- it appears to be maintained by convention and IDE sorting.

- **Test files are less consistent**: Test file imports follow a similar pattern but without strict ordering.
  - Evidence: `Source/DotNetWorkQueue.Tests/Queue/ConsumerQueueTests.cs` (lines 1-12): System first, then AutoFixture, then DotNetWorkQueue, then NSubstitute, then Microsoft.VisualStudio.TestTools.

### XML Documentation Comments

- **All public types and members have XML doc comments**: The production library consistently provides `<summary>`, `<param>`, `<returns>`, `<remarks>`, `<seealso>`, and `<exception>` tags.
  - Evidence: `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs` (lines 41-51: constructor parameters all documented)
  - Evidence: `Source/DotNetWorkQueue/ASerializer.cs` (lines 27-31: class-level summary, lines 44-49: method documentation)
  - This is enforced in Release builds via `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<DocumentationFile>` settings.
  - Evidence: `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (lines 42-44)

- **`<inheritdoc />` shorthand**: Used when implementations match their interface signatures exactly.
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (lines 94, 114, 141)

### Code Organization Within Files

- **`#region` directives**: Used to group logical sections within classes, particularly `Dispose` blocks and `Constructor` sections.
  - Evidence: `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (line 52: `#region Member level Variables`, line 56: `#region Private Methods`, line 57: `#region Constructor`, line 123: `#region IDisposable`)
  - Evidence: `Source/DotNetWorkQueue/Queue/ConsumerQueue.cs` (line 129: `#region Dispose`)
  - Evidence: `Source/DotNetWorkQueue/BaseContainer.cs` (line 48: `#region Dispose`)

### Decorator Architecture for Cross-Cutting Concerns

- **Three decorator layers**: The codebase applies cross-cutting concerns through three parallel sets of decorators wrapping the same core interfaces:
  1. **Logging decorators** in `Source/DotNetWorkQueue/Logging/Decorator/` -- 8 decorators
  2. **Tracing decorators** in `Source/DotNetWorkQueue/Trace/Decorator/` -- 15 decorators
  3. **Metrics decorators** in `Source/DotNetWorkQueue/Metrics/Decorator/` -- 18 decorators
  - Each decorator wraps an inner handler, adds its concern, and delegates to the original.
  - Evidence: `Source/DotNetWorkQueue/Logging/Decorator/RollbackMessageDecorator.cs` (wraps `IRollbackMessage`)
  - Evidence: `Source/DotNetWorkQueue/Trace/Decorator/CommitMessageDecorator.cs` (wraps `ICommitMessage`)

### Transport Registration Convention

- **`ITransportInit` / `TransportInitSend` / `TransportInitReceive` / `TransportInitDuplex`**: Each transport provides abstract init classes that register DI bindings, suppress warnings, and set defaults.
  - Evidence: `Source/DotNetWorkQueue/Configuration/TransportInitSend.cs` (lines 27-58): Three virtual methods: `RegisterImplementations`, `SuppressWarningsIfNeeded`, `SetDefaultsIfNeeded`

## Summary Table

| Convention | Detail | Confidence |
|---|---|---|
| Interface prefix `I` | All interfaces: `IQueue`, `IConsumerQueue`, etc. | Observed |
| Abstract prefix `A` | Older abstractions: `ASerializer`, `ASendJobToQueue`, `ATaskScheduler` | Observed |
| Abstract suffix `Base` | Queue-related bases: `BaseQueue`, `BaseContainer`, `MultiWorkerBase` | Observed |
| Factory suffix | All factories: `HeartBeatWorkerFactory`, `MessageFactory`, etc. | Observed |
| Configuration suffix | All config classes: `QueueConsumerConfiguration`, etc. | Observed |
| NoOp suffix + `INoOperation` | `HeartBeatWorkerNoOp`, `QueueWaitNoOp`, etc. | Observed |
| Decorator suffix + subdirectory | `RollbackMessageDecorator` in `Logging/Decorator/` | Observed |
| Command/Query CQRS naming | `SendMessageCommand`, `GetJobIDQuery` | Observed |
| LGPL-2.1 header | All production `.cs` files; most test files omit | Observed |
| `Guard.NotNull` on all ctor params | Every constructor validates injected deps | Observed |
| Thread-safe dispose via `Interlocked` | `_disposeCount` + `Interlocked.Increment` | Observed |
| `ThrowIfDisposed` with `[CallerMemberName]` | Protected method, duplicated per class | Observed |
| `Monitor.Enter`/`Exit` for bool properties | `ShouldWork`, `Running`, `Stopped`, `Started` | Observed |
| `ConfigureAwait(false)` everywhere | All `await` calls | Observed |
| Config immutability: `SetReadOnly`/`FailIfReadOnly` | Prevents mutation after queue starts | Observed |
| XML doc comments on all public APIs | Enforced by `TreatWarningsAsErrors` in Release | Observed |
| `#region` grouping | Dispose, Constructor, Member Variables sections | Observed |
| `#if NETFULL` conditional compilation | 11 files, primarily thread abort and dynamic LINQ | Observed |
| Import order: System, project, third-party | Convention, not enforced by tooling | Inferred |
| Test naming: PascalCase_with_underscores | `IsDisposed_False_By_Default`, etc. | Observed |

## Open Questions

- There is no `.editorconfig` or formatting configuration file visible in the repository. The formatting conventions (brace placement, indentation) appear consistent but are not explicitly enforced by tooling.
- The duplication of `_disposeCount`, `ThrowIfDisposed`, and `IsDisposed` across many classes (rather than inheriting from a single base) is an intentional choice but could be investigated for potential consolidation.
- Some test files include the LGPL header and some do not. It is unclear whether the intent is to eventually add headers to all test files.
