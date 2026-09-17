# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DotNetWorkQueue is a producer/distributed consumer library for .NET. It supports queueing POCOs, compiled LINQ expressions, and re-occurring job scheduling. Targets .NET 10.0 and .NET 8.0.

## Project Instructions
Always use Context7 MCP when I need library/API documentation or setup steps. Automatically resolve library IDs and retrieve docs without being asked.

## Build Commands

```bash
# Build entire solution
dotnet build "Source\DotNetWorkQueue.sln" -c Debug

# Build without test projects
dotnet build "Source\DotNetWorkQueueNoTests.sln" -c Debug

# Build a specific project
dotnet build "Source\DotNetWorkQueue\DotNetWorkQueue.csproj"
```

Release builds enable `TreatWarningsAsErrors` and XML documentation generation. For NuGet release builds, always pass `-p:CI=true` to enable deterministic Source Link paths:

```bash
# Release build for NuGet publishing
dotnet build "Source\DotNetWorkQueueNoTests.sln" -c Release -p:CI=true

# Pack Dashboard.Ui (not auto-packed by build)
dotnet pack "Source\DotNetWorkQueue.Dashboard.Ui\DotNetWorkQueue.Dashboard.Ui.csproj" -c Release -p:CI=true
```

### Release publishing

Real releases are published by the tag-triggered `.github/workflows/publish.yml` workflow — do NOT invoke `dotnet nuget push` locally. Push a `v<version>` tag (matching `Source/Directory.Build.props` `<Version>`) to trigger the three-job pipeline (`verify-gate` → `build-pack` → `publish`). The existing local `dotnet build -c Release -p:CI=true` and `dotnet pack` commands shown above are for inspection / dry-run only. Operational dry-run: Actions → Publish → Run workflow → `dry_run=true` exercises the gate + pack jobs without publishing.

## Running Tests

Tests use **MSTest 4.4.0**, NSubstitute for mocking, and AutoFixture for test data. Assertions are MSTest's own (`Assert.AreEqual`, `Assert.IsTrue`, `Assert.Contains`, `Assert.Throws<T>` / `Assert.ThrowsExactly<T>`).

- **FluentAssertions is not used and must not be reintroduced** - it was deliberately removed in #159 on licensing grounds (version 8 changed to a paid licence for commercial use).
- **The MSTest version is not per-project.** It is pinned centrally in `Source/Directory.Packages.props`, so a single test project cannot opt in or out of a different version.
- MSTest 4.x removed `Assert.ThrowsException`; use `Assert.Throws<T>` or `Assert.ThrowsExactly<T>`.
- **Integration and E2E test classes carry `[Retry(1)]`** so a transient service fault costs one re-run rather than a whole red build (#271). Three things to know about it:
  - **The argument is retries, not attempts.** `[Retry(1)]` runs a failing test twice in total. `[Retry(2)]` would run it three times, which triples the cost of a test that is genuinely broken — measured, not assumed.
  - **A retry leaves no trace.** The junit report Jenkins reads records a retried test as an ordinary pass — no attempt count, nothing in the console either. Green therefore no longer means "nothing flaked". To measure flakiness, run a suite with the attribute removed.
  - **Unit tests deliberately do not have it.** A flaky unit test is a bug, and retrying one hides exactly the kind of race that `HeartBeatWorker` turned out to have.
- `[Retry]` is method-level only until MSTest 4.4.0, which is why the version is pinned at or above it. The `RetryBaseAttribute` extensibility point that would let a retry log itself is still experimental (`MSTESTEXP`, an error by default) and was left alone for that reason.

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

Integration tests for SQLite and LiteDb require running instances of those services and connection strings configured in `connectionstring.txt` files within each integration test project. The Dashboard API integration tests read their own `connectionstring-*.txt` files and still use external services too.

**The Redis, PostgreSQL and SQL Server transport suites do not.** With no `connectionstring.txt` in the output directory, each starts its own container and owns it for the run (issue #281) - both the plain and Linq suites of all three. SQL Server additionally creates its database and the non-default `test1`/`test2` schemas, so it needs nothing prepared by hand. That needs a reachable Docker daemon. Prefer a local one. `DOCKER_HOST=tcp://<host>:2375` reaches a remote daemon, but 2375 is Docker's plaintext, unauthenticated API - anything that can reach it is effectively root on that host - so use it only on a private network limited to trusted machines, and mutual TLS (2376) or an SSH transport otherwise. `docs/jenkins-setup.md` covers the CI side of this. Dropping a `connectionstring.txt` into the output directory still wins, which is how you point a suite at a server you already have.

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

- Library and production source files include LGPL-2.1 license headers (see `DotNetWorkQueue.licenseheader`). Test projects are exempt and mostly do not carry one; #206 scoped its header sweep to production files deliberately. Do not add headers to test files to satisfy this rule, and do not flag their absence
- Interface prefix: `I` (e.g., `IQueue`); Factory suffix: `Factory`; Config suffix: `Configuration`
- Abstract base classes use prefix `A` or suffix `Base`
- Thread-safe disposal via `Interlocked` operations throughout
- **CI**: Jenkins is the local CI server (setup guide at `docs/jenkins-setup.md`). It runs 14 parallel integration test stages on Docker agents (net10.0 only) with Coverlet code coverage uploaded to Codecov.io. The 14th stage (`TaskScheduler Distributed`) runs without Coverlet by design — it tests an external NuGet and the core DLLs it uses are already covered by the other 13 stages. GitHub Actions (`.github/workflows/ci.yml`) runs net10.0 unit tests + the TaskScheduler Distributed integration tests on ubuntu-latest for CI validation. **Jenkins is PR-triggered, not branch-triggered** — any feature-branch CI validation MUST open a regular (non-draft) PR to trigger a build. The project does NOT use draft PRs because CodeRabbit's free plan cannot review drafts; opening as a regular PR is what triggers both Jenkins and CodeRabbit.

## Lessons Learned

Historical engineering notes have been moved to [`docs/lessons-learned.md`](docs/lessons-learned.md) to keep this file small. Grep that file by topic (Jenkins, NuGet, NSubstitute, transport-specific, etc.) before re-discovering something the hard way.

## Code Quality
- Prefer correct, complete implementations over minimal ones.
- Use appropriate data structures and algorithms — don't brute-force what has a known better solution.
- When fixing a bug, fix the root cause, not the symptom.
- If something I asked for requires error handling or validation to work reliably, include it without asking.
- New and changed features should be covered by either unit or integration Tests
- Features that might vary by the transport implementation should have integration Tests; This has caused issues before with Redis History for example

## Pull requests

- A PR description explains the change to a reviewer: what it does, why, and what to look at. It is not a history of how the work got there.
- Leave out process narration — dead ends, wrong turns, numbers that were nearly reported, corrections to earlier readings, "what I got wrong". None of that helps someone reviewing the diff.
- Those things still get written down, just elsewhere: reasoning belongs in commit messages, and measurement dead ends belong in `Source/DotNetWorkQueue.Benchmarks/README.md`, which exists so they are not re-derived.

## Changelog.md

The changelog is for **consumers of the queue, not for us**. An entry answers "what changed, and does it affect me?" — if a reader wants the how or why, the commit history is a better record of it than a bullet will ever be.

Test every entry: **does a consumer act differently knowing this?** If not, it belongs in the commit message.

- One bullet per user-visible change, not one per aspect of it
- Target 200 characters; over 300 needs a reason
- Performance entries get one before/after pair — the practical effect, never the methodology that produced it
- ⚠️ only where a consumer must change configuration or expectations
- No implementation internals, no measurement narration, no "here is what we tried"

The detail has homes already: reasoning → the commit message; measurement methodology and dead ends → `Source/DotNetWorkQueue.Benchmarks/README.md`; implementation detail → code comments, next to the code.