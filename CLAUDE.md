# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DotNetWorkQueue is a producer/distributed consumer library for .NET. It supports queueing POCOs, LINQ statements (compiled or dynamic), and re-occurring job scheduling. Targets .NET 10.0, .NET 8.0, .NET Framework 4.8, and .NET Standard 2.0.

## Build Commands

```bash
# Build entire solution
dotnet build "Source\DotNetWorkQueue.sln" -c Debug

# Build without test projects
dotnet build "Source\DotNetWorkQueueNoTests.sln" -c Debug

# Build a specific project
dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj"
```

Release builds enable `TreatWarningsAsErrors` and XML documentation generation.

## Running Tests

Tests use xUnit 2.9.3, NSubstitute for mocking, AutoFixture for test data, and FluentAssertions.

```bash
# Run all unit tests for a specific project
dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"

# Run a single test by fully qualified name
dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~MyTestClassName.MyTestMethod"

# Unit test projects (no external dependencies needed):
dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"
dotnet test "Source\DotNetWorkQueue.Transport.SqlServer.Tests\DotNetWorkQueue.Transport.SqlServer.Tests.csproj"
dotnet test "Source\DotNetWorkQueue.Transport.PostgreSQL.Tests\DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj"
dotnet test "Source\DotNetWorkQueue.Transport.Redis.Tests\DotNetWorkQueue.Transport.Redis.Tests.csproj"
dotnet test "Source\DotNetWorkQueue.Transport.SQLite.Tests\DotNetWorkQueue.Transport.SQLite.Tests.csproj"
dotnet test "Source\DotNetWorkQueue.Transport.LiteDb.Tests\DotNetWorkQueue.Transport.LiteDb.Tests.csproj"
dotnet test "Source\DotNetWorkQueue.Transport.RelationalDatabase.Tests\DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj"
dotnet test "Source\DotNetWorkQueue.AppMetrics.Tests\DotNetWorkQueue.AppMetrics.Tests.csproj"

# In-memory integration tests (no external services needed):
dotnet test "Source\DotNetWorkQueue.Transport.Memory.Integration.Tests\DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj"
dotnet test "Source\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj"
```

Integration tests for SQL Server, PostgreSQL, Redis, SQLite, and LiteDb require running instances of those services and connection strings configured in `connectionstring.txt` files within each integration test project.

## Architecture

### Core Layer: `DotNetWorkQueue`
The main library containing all abstractions, interfaces, and default implementations. Key namespaces:
- `Configuration` - Queue configuration objects (`QueueProducerConfiguration`, `QueueConsumerConfiguration`, `QueueConnection`)
- `IoC` - DI abstractions (`IContainer`, `IContainerFactory`) using SimpleInjector
- `Queue` - Queue implementations for producers and consumers
- `Messages` - `IMessage<T>`, `IReceivedMessage<T>`, `ISentMessage`
- `JobScheduler` - Recurring job scheduling using Schyntax format
- `Policies` - Polly-based resilience/retry policies
- `Trace` - OpenTelemetry distributed tracing integration

### Transport Abstraction (layered)
1. **`Transport.Shared`** - Base interfaces and Command/Query pattern (`ICommandHandler<T>`, `IQueryHandler<T,TR>`) for transport-independent data access
2. **`Transport.RelationalDatabase`** - SQL-specific abstractions built on Transport.Shared
3. **Transport implementations** - Each transport (SqlServer, PostgreSQL, SQLite, Redis, LiteDb) implements `ITransportInit` (with `Send`/`Receive`/`Duplex` variants) and registers its DI bindings via `RegisterImplementations()`

### Producer/Consumer Pattern
- `IProducerQueue<T>` / `IConsumerQueue` - POCO message queues
- `IProducerMethodQueue` / `IConsumerMethodQueue` - Delegate-based queues
- LINQ expression variants for dynamic/compiled expressions
- `IJobScheduler` for recurring scheduled jobs

### Dependency Injection
SimpleInjector is the IoC container. Each transport has an init class implementing `ITransportInit` that registers its services. `IContainerFactory` provides root-level container access to avoid circular dependencies.

### Multi-targeting
Projects use conditional compilation: `NETFULL` for .NET 4.8-specific code (thread abort support, dynamic LINQ), `NETSTANDARD2_0` for .NET Standard paths.

## Key Dependencies

- **SimpleInjector 5.5.0** - DI container
- **Polly 8.6.5** - Resilience/retry
- **Newtonsoft.Json 13.0.4** - Serialization
- **Microsoft.Data.SqlClient 6.1.3** - SQL Server (replaced System.Data.SqlClient)
- **OpenTelemetry 1.14.0** - Distributed tracing
- **App.Metrics 4.3.0** - Metrics integration
- Custom libraries in `/Lib`: Schyntax (scheduling), Aq.ExpressionJsonSerializer (LINQ serialization), JpLabs.DynamicCode (dynamic lambdas)

## Conventions

- All source files include LGPL-2.1 license headers (see `DotNetWorkQueue.licenseheader`)
- Interface prefix: `I` (e.g., `IQueue`); Factory suffix: `Factory`; Config suffix: `Configuration`
- Abstract base classes use prefix `A` or suffix `Base`
- Thread-safe disposal via `Interlocked` operations throughout
- CI runs on AppVeyor (master branch only)
