# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DotNetWorkQueue is a producer/distributed consumer library for .NET. It supports queueing POCOs, compiled LINQ expressions, and re-occurring job scheduling. Targets .NET 10.0 and .NET 8.0.

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

Tests use MSTest 3.x, NSubstitute for mocking, AutoFixture for test data, and FluentAssertions.

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

# Additional unit test projects:
dotnet test "Source\DotNetWorkQueue.Dashboard.Api.Tests\DotNetWorkQueue.Dashboard.Api.Tests.csproj"
dotnet test "Source\DotNetWorkQueue.Transport.Memory.Tests\DotNetWorkQueue.Transport.Memory.Tests.csproj"

# In-memory integration tests (no external services needed):
dotnet test "Source\DotNetWorkQueue.Transport.Memory.Integration.Tests\DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj"
dotnet test "Source\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj"

# Dashboard API integration tests (Memory/SQLite/LiteDb only, no external services):
dotnet test "Source\DotNetWorkQueue.Dashboard.Api.Integration.Tests\DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory|FullyQualifiedName~Sqlite|FullyQualifiedName~LiteDb"

# Dashboard API integration tests (all transports, requires running services):
dotnet test "Source\DotNetWorkQueue.Dashboard.Api.Integration.Tests\DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj"
```

Integration tests for SQL Server, PostgreSQL, Redis, SQLite, and LiteDb require running instances of those services and connection strings configured in `connectionstring.txt` files within each integration test project.

## Architecture

### Core Layer: `DotNetWorkQueue`
The main library containing all abstractions, interfaces, and default implementations. Key namespaces:
- `Configuration` - Queue configuration objects (`QueueProducerConfiguration`, `QueueConsumerConfiguration`, `QueueConnection`)
- `IoC` - DI abstractions (`IContainer`, `IContainerFactory`) using SimpleInjector
- `Queue` - Queue implementations for producers and consumers
- `Messages` - `IMessage<T>`, `IReceivedMessage<T>`, `ISentMessage`
- `JobScheduler` - Recurring job scheduling using cron format
- `Policies` - Polly-based resilience/retry policies
- `Trace` - OpenTelemetry distributed tracing integration

### Transport Abstraction (layered)
1. **`Transport.Shared`** - Base interfaces and Command/Query pattern (`ICommandHandler<T>`, `IQueryHandler<T,TR>`, `IQueryHandlerAsync<T,TR>`) for transport-independent data access
2. **`Transport.RelationalDatabase`** - SQL-specific abstractions built on Transport.Shared
3. **Transport implementations** - Each transport (SqlServer, PostgreSQL, SQLite, Redis, LiteDb) implements `ITransportInit` (with `Send`/`Receive`/`Duplex` variants) and registers its DI bindings via `RegisterImplementations()`

### Producer/Consumer Pattern
- `IProducerQueue<T>` / `IConsumerQueue` - POCO message queues
- `IProducerMethodQueue` / `IConsumerMethodQueue` - Delegate-based queues
- LINQ expression variants for compiled expressions
- `IJobScheduler` for recurring scheduled jobs

### Dependency Injection
SimpleInjector is the IoC container. Each transport has an init class implementing `ITransportInit` that registers its services. `IContainerFactory` provides root-level container access to avoid circular dependencies.

### Multi-targeting
Projects target net10.0 and net8.0. Legacy conditional compilation symbols (NETFULL, NETSTANDARD2_0) have been removed.

## Key Dependencies

- **SimpleInjector 5.5.0** - DI container
- **Polly 8.6.5** - Resilience/retry
- **Newtonsoft.Json 13.0.4** - Serialization
- **Microsoft.Data.SqlClient 6.1.3** - SQL Server (replaced System.Data.SqlClient)
- **OpenTelemetry 1.14.0** - Distributed tracing
- **System.Diagnostics.Metrics** - Built-in metrics via `System.Diagnostics.DiagnosticSource` (users add OpenTelemetry.Metrics exporters to collect)
- **Cronos** - Cron expression parsing (5-field and 6-field with seconds)
- **CronExpressionDescriptor** - Human-readable cron schedule descriptions

## Conventions

- All source files include LGPL-2.1 license headers (see `DotNetWorkQueue.licenseheader`)
- Interface prefix: `I` (e.g., `IQueue`); Factory suffix: `Factory`; Config suffix: `Configuration`
- Abstract base classes use prefix `A` or suffix `Base`
- Thread-safe disposal via `Interlocked` operations throughout
- **CI**: Jenkins is the local CI server (setup guide at `docs/jenkins-setup.md`). It runs 13 parallel integration test stages on Docker agents (net10.0 only) with Coverlet code coverage uploaded to Codecov.io. GitHub Actions (`.github/workflows/ci.yml`) runs net10.0 unit tests on ubuntu-latest for CI validation.

## Lessons Learned

- When multi-targeting to net10.0 for Linux, check for: case-sensitive file paths in .csproj references, native library dependencies (libsqlite3, libdl), `#if NETFULL` guards on .NET Framework-only APIs (SoapFormatter, GetObjectData), and timer/clock resolution differences in tests.
- Connection strings for `dotnet test --no-build` must be written to the bin output directory, not just the source directory.
- Jenkins agent JRE version must exactly match the master's Java version — class file version mismatch causes silent agent launch failures.
- When a change marked "out of scope" keeps causing friction in CI (e.g., hardcoded connection strings), just do it — the cost of workarounds exceeds the cost of the fix.
- Label-based Jenkins agents are simpler than Docker Pipeline plugin for pre-built images.
- Integration test metrics assertions can race: the handler callback signals completion before `CommitMessage.Commit()` increments the counter. Poll the live `IMetrics` object instead of taking a single snapshot.
- Enabling `--retry-failed-tests` requires migrating ALL test projects to Microsoft.Testing.Platform (`EnableMSTestRunner` + `TestingPlatformDotnetTestSupport` in Directory.Build.props) — partial migration breaks coverage collection.
- Dockerfile COPY paths must match exact Linux filesystem casing: `LiteDb.csproj` (not `LiteDB.csproj`), `Directory.Build.props` is in `Source/` not the repo root.
- `--no-restore` on `dotnet publish` in Docker fails when a later `COPY Source/` invalidates the restore cache layer.
- 13 parallel Jenkins stages need staggered startup (5s intervals) to avoid GitHub clone rate-limiting.
- SQL UPDATE tests that only assert parameter values can pass while the UPDATE is a silent no-op: a WHERE clause guard may exclude the very rows you're trying to fix. Capture and assert the actual `CommandText` to catch this — the parameter assertion alone is a false positive.
- StackExchange.Redis `ConnectionMultiplexer` cannot be mocked with NSubstitute (sealed types + extension methods). Expose a `protected virtual GetDb()` seam on Redis handlers for testability; keep classes internal to contain the scope.
- `RedisValue.Null` cast to `(int)` yields `0`, not an exception. When comparing against enums where `0` is a valid member (e.g., `MessageHistoryStatus.Enqueued`), always check `.HasValue` before casting to avoid null-value collisions.
- NuGet version ordering: `0.9.3` < `0.9.19`, so you can't go back to a lower version number after incrementing past it.

## Code Quality
- Prefer correct, complete implementations over minimal ones.
- Use appropriate data structures and algorithms — don't brute-force what has a known better solution.
- When fixing a bug, fix the root cause, not the symptom.
- If something I asked for requires error handling or validation to work reliably, include it without asking.
