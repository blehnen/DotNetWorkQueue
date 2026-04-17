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
- **CI**: Jenkins is the local CI server (setup guide at `docs/jenkins-setup.md`). It runs 14 parallel integration test stages on Docker agents (net10.0 only) with Coverlet code coverage uploaded to Codecov.io. The 14th stage (`TaskScheduler Distributed`) runs without Coverlet by design — it tests an external NuGet and the core DLLs it uses are already covered by the other 13 stages. GitHub Actions (`.github/workflows/ci.yml`) runs net10.0 unit tests + the TaskScheduler Distributed integration tests on ubuntu-latest for CI validation. **Jenkins is PR-triggered, not branch-triggered** — any feature-branch CI validation MUST open a (draft) PR to trigger a build.

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
- NuGet.org does not allow pushing `.snupkg` separately after the `.nupkg` is already published, and re-pushing the same version is blocked. The CLI's auto-match of `.snupkg` alongside `.nupkg` is unreliable on Windows (required 12 manual `.snupkg` pushes per release in the legacy flow). The `.github/workflows/publish.yml` GH Actions workflow splits the push into two explicit commands (`deploy/*.nupkg` then `deploy/*.snupkg`) on `ubuntu-latest`, which is portable. Do not run `dotnet nuget push` locally for real releases — push the `v<version>` tag and let the workflow do it.
- Release builds for NuGet must use `-p:CI=true` (e.g., `dotnet build -c Release -p:CI=true`) to enable `ContinuousIntegrationBuild` in Directory.Build.props. Without it, Source Link paths aren't deterministic and NuGet.org shows red validation indicators.
- `DotNetWorkQueue.IConfiguration` shadows `Microsoft.Extensions.Configuration.IConfiguration` in any code under the `DotNetWorkQueue.*` namespace hierarchy. C# resolves via namespace walk-up BEFORE `using` directives — even `using` aliases don't help. Use `global::Microsoft.Extensions.Configuration.IConfiguration` for all MS config type references in Dashboard.Ui code and tests.
- MudBlazor 9.x expansion panel property is `Expanded` (not `IsInitiallyExpanded`). Blazor silently ignores unknown attributes — no build error, just non-functional.
- NSubstitute indexer mocking fails on `IFeatureCollection` — use real `FeatureCollection` with `Set<T>()` in tests instead of mocking.
- `TraceExtensions` and trace decorator code paths show 0% coverage in tests unless an `ActivityListener` is registered for the matching `ActivitySource`. Without a listener, `ActivitySource.StartActivity()` returns `null` and the entire trace decorator chain short-circuits silently — no error, just silent skipping. To get trace coverage in integration tests, register a listener via `ActivitySource.AddActivityListener()` even if you don't need to assert on the activities.
- `Metrics.Metrics` namespace walk-up shadowing: inside `DotNetWorkQueue.IntegrationTests.*` projects, `new Metrics.Metrics(...)` resolves to `DotNetWorkQueue.IntegrationTests.Metrics.Metrics` via namespace walk-up. From a transport test project (e.g., `DotNetWorkQueue.Transport.Memory.Integration.Tests`), the same expression binds to the non-existent `DotNetWorkQueue.Metrics.Metrics`. Use the fully-qualified `DotNetWorkQueue.IntegrationTests.Metrics.Metrics` to disambiguate. Same root cause as the `IConfiguration` shadowing lesson above.
- Sync vs async handler mocking split: Sync `IQueryHandler<TQuery, TResult>` handlers can be tested by mocking `IDbConnection`/`IDbCommand`/`IDataReader` (the interfaces). Async `IQueryHandlerAsync<TQuery, TResult>` handlers MUST mock the abstract base classes `DbConnection`/`DbCommand`/`DbDataReader` because `OpenAsync`/`ExecuteReaderAsync`/`ReadAsync` are defined on the base classes, not the interfaces. Mocking the interface for an async handler compiles but the async methods silently no-op via NSubstitute defaults.
- MSTest 3.x uses `Assert.ThrowsExactly<T>` (not `Assert.ThrowsException<T>` from MSTest 2.x). When two concurrent edits mix old and new APIs, stale `obj/`+`bin/` cache can surface phantom compile errors against files that already use the correct API. After multi-file concurrent test edits, `rm -rf obj bin` and rebuild before chasing down "compile errors" that don't match the source.
- Async dashboard query handlers (`GetDashboardJobsQueryHandlerAsync`, `GetDashboardErrorRetriesQueryHandlerAsync`, etc.) do NOT take a `CancellationToken`. The `IQueryHandlerAsync<TQuery, TResult>` interface signature is `HandleAsync(TQuery query)` -- no token. Don't add cancellation tests for these handlers.
- Casting `IDbConnection` to a sealed transport-specific type (`NpgsqlConnection`, `SqliteConnection`, `Microsoft.Data.SqlClient.SqlConnection`) inside a handler breaks NSubstitute / Castle DynamicProxy mocking with `TypeLoadException: parent type is sealed`. Keep handlers operating on `IDbConnection` and use generic `DbType` enum values (`DbType.AnsiString`, `DbType.Int64`, `DbType.DateTimeOffset`) with `IDbCommand.CreateParameter()` + `Parameters.Add(param)`. The PostgreSQL `SetJobLastKnownEventCommandHandler` was re-refactored mid-Phase-3 (commit `9c77537d`) for exactly this reason.
- `IDbConnectionFactory` injection is the correct test seam for transport command handlers. The mock chain is `IDbConnectionFactory.Create() -> IDbConnection -> IDbCommand -> IDataParameterCollection`, with `Arg.Do<IDbDataParameter>(p => list.Add(p))` to capture parameters for assertion. Don't reach for `System.Reflection` or `Testable*` subclass workarounds to expose protected methods if the underlying problem is a hardcoded `new SqlConnection()` -- inject the factory instead.
- LiteDb handler unit tests use real in-memory `LiteDatabase` instances via `using var db = new LiteDatabase("Filename=:memory:");` rather than mocking `LiteDbConnectionManager`. `LiteDatabase` is cheap to construct in-memory, gives real collection/indexing behavior, and disposes cleanly. Handlers that accept a `LiteDatabase` directly (or can be invoked via a reflection-reached protected method) are testable this way. Handlers that reach through `LiteDbConnectionManager.GetDatabase()` inside `Handle()` are NOT -- see the companion lesson below.
- Redis Lua handler unit tests use a `Testable{X}Lua` private inner class that subclasses the concrete Lua class and overrides `TryExecute(object)` (and `TryExecuteAsync` if needed) to return a scripted `RedisResult` without a live Redis connection. The seam requires `TryExecute`/`TryExecuteAsync` to be `virtual` on `BaseLua` -- they are as of commit `c7a9dd80`. Pattern: subclass, expose a `NextResult` property, override `TryExecute` to set a `TryExecuteCalled` flag and return `NextResult`, then assert on the handler's output. `IRedisConnection` is mocked with NSubstitute (it is an interface); `ConnectionMultiplexer` is never touched.
- `LiteDbConnectionManager` has no injection seam: its constructor takes `IConnectionInformation` + `ICreationScope` and builds the `LiteDatabase` internally. Any LiteDb command/query handler that calls `GetDatabase()` inside `Handle()` cannot have that path unit-tested -- constructor-null-guard tests and reflection-reached protected helpers are the only viable unit-level coverage. Handle()-level coverage for such handlers lives in the LiteDb integration test suite (`Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/` for POCO handlers, `Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/JobScheduler/` for job-scheduler paths). Don't try to mock `LiteDbConnectionManager` -- it has no seams for that.
- ASP.NET Core `AddControllers(action)` in a bare `ServiceCollection` does NOT reliably surface user-added `MvcOptions.Conventions` via `IOptions<MvcOptions>.Value` -- filters added by the same action DO propagate, but conventions do not. Four debugging iterations in Phase 5 (PLAN-1.3) confirmed the contradiction: `mvcOptions.Filters` contained every Dashboard filter correctly, yet `mvcOptions.Conventions` showed only the framework-internal `ControllerApplicationModelConvention`. Root cause is ASP.NET Core's internal `ConfigureMvcOptions` / `AddApplicationPart` pipeline, which behaves differently without a real `IHostEnvironment`. For any test that must assert an `IControllerModelConvention` was registered, use an integration test with a real `WebApplication` pipeline (or `WebApplicationFactory`) -- not a bare `ServiceCollection`. The unit-test workaround is to test the convention's `Apply()` method directly, then cover end-to-end wiring with an integration test that exercises the full pipeline.
- `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.SchedulerContainer` does NOT expose `GetInstance<T>()` in the 0.4.0 NuGet. The only way to resolve `ITaskSchedulerJobCountSync` (or any other container-registered service) is the **IContainer closure pattern**: capture `IContainer` during the `SchedulerContainer(registerService)` callback, trigger build via `CreateTaskScheduler()`, then call `capturedContainer.GetInstance<T>()`. Used by `ConcurrencyRegressionTests` and `NodeDiscoveryTests` in the TaskScheduler Distributed integration test project. An earlier NodeDiscoveryTests draft used the nonexistent `SchedulerContainer.GetInstance<>` and produced 10 compile errors.
- `ITaskSchedulerJobCountSync.Start()` MUST be called before spawning threads that call `IncreaseCurrentTaskCount`/`DecreaseCurrentTaskCount`. Without `Start()`, `_outbound` is null and Phase 1's null-safe guard short-circuits every call, making any concurrency test a false positive that would pass even if the lock fix were reverted. `ConcurrencyRegressionTests` carries an inline comment documenting this invariant.
- DNQ queue names must be alphanumeric/underscore/dot — DNQ validation rejects hyphens. `Guid.NewGuid().ToString()` produces hyphenated strings that fail with `Queue name contains invalid characters`. Use `Guid.NewGuid().ToString("N")` (no hyphens) or a sanitized format. The TaskScheduler Distributed EndToEnd test uses `"q" + Guid.NewGuid().ToString("N")`.
- Memory transport storage is per-`QueueContainer<MemoryMessageQueueInit>` instance — two separate containers do NOT share the underlying `IDataStorage` via `RegisterNonScopedSingleton(scope)` alone. A naive hand-rolled producer/consumer split across two containers will see the producer's messages stay pending while the consumer's store is empty. For Memory-transport integration tests that need both roles, use the single-container shared runner (`DotNetWorkQueue.IntegrationTests.Shared.Consumer.Implementation.SimpleConsumer.Run<>`) which internally uses one container — but note that shared runner's `setOptions` parameter is `Action<TTransportCreate>` for transport options, NOT `Action<IContainer>` for container registration, so there's no seam to inject `InjectDistributedTaskScheduler` through. The Phase 3 EndToEndSchedulingTests was scope-reduced to a SimpleInjector `Verify()` smoke test because of this constraint.
- `-p:CI=true` is a NuGet packaging flag (it enables `ContinuousIntegrationBuild` in `Directory.Build.props` for deterministic Source Link paths during `dotnet build -c Release`) — it has NO effect on `dotnet test` and should not appear on test invocations. All 14 Jenkins integration test stages use `-c Debug` with no `-p:CI=true`. Only the pre-publish Release build uses `-c Release -p:CI=true`.
- Jenkins Multibranch Pipeline is **PR-triggered, not branch-triggered**. A `git push` of a feature branch alone will NOT cause Jenkins to build — you must open a (draft) PR via `gh pr create --draft --base master --head <branch>` to trigger the Jenkinsfile discovery and pipeline run. This is the correct pattern for CI-sensitive feature branch validation.
- Before starting a new Shipyard milestone on this repo, run `git fetch origin master && git log HEAD..origin/master --oneline` to confirm local master is current. Origin may have unpulled work from a concurrent milestone (e.g., a dashboard-coverage PR merged while you were offline). Resolving the divergence at ship time via merge-and-rebase is recoverable but expensive — resolving it up front is free.
- Jenkinsfile stagger formula is `(n-1) * 5` seconds per stage (0, 5, 10, ..., 60, 65) implemented as inline `sleep(time: N, unit: 'SECONDS')` calls at the start of each stage's `steps { }` block. With 14 stages, worst-case startup delay is 65s. Adding a 15th stage would push total startup delay past the current ceiling — either revisit the formula (shorter intervals) or batch subsequent stages into a nested parallel block.
- **Release flow (v<version> tag → publish.yml):** `Source/Directory.Build.props` line 4 carries `<Version>` (4-space indent, inside the existing `<PropertyGroup>` immediately after `<ManagePackageVersionsCentrally>`); the 12 packable csprojs inherit. Tag regex `^v\d+\.\d+\.\d+(-[A-Za-z0-9\.-]+)?$` is enforced by `verify-gate`. Tag version must equal `Directory.Build.props` `<Version>` exactly (stripped of `v`). Tag must land on a commit whose Jenkins status context `continuous-integration/jenkins/branch` is `success` (the B2 gate). Operator dry-run: Actions → Publish → Run workflow → `dry_run=true` exercises `verify-gate` + `build-pack` without publishing. Before the first real release, add the `NUGET_API_KEY` secret in GitHub repo Settings → Secrets and variables → Actions.
- **GitHub status API — rollup vs history endpoints.** `GET /repos/{owner}/{repo}/commits/{sha}/status` (singular) is the rollup endpoint: returns `.statuses[]` containing the latest state per unique context. `GET /commits/{sha}/statuses` (plural) returns EVERY status update ever posted — typically 15+ `pending` rows followed by one `success` row. A naive jq filter against the plural endpoint (`.[] | select(.context=="...") | .state`) emits multi-line output that silently breaks bash `[[ "$state" == "success" ]]` comparisons. The `publish.yml` B2 gate uses the singular rollup. If Jenkins ever changes its status context name, update the literal `continuous-integration/jenkins/branch` in `publish.yml` accordingly.

## Code Quality
- Prefer correct, complete implementations over minimal ones.
- Use appropriate data structures and algorithms — don't brute-force what has a known better solution.
- When fixing a bug, fix the root cause, not the symptom.
- If something I asked for requires error handling or validation to work reliably, include it without asking.
- New and changed features should be covered by either unit or integration Tests
- Features that might vary by the transport implementation should have integration Tests; This has caused issues before with Redis History for example
