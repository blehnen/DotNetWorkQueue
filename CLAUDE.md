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
- Integration test metrics assertions can race in **any** of the `VerifyMetrics.Verify*` methods — the handler callback signals completion before the underlying counter/meter is incremented. As of 2026-05-19 the polling pattern is applied uniformly to `VerifyProcessedCount`, `VerifyPoisonMessageCount`, `VerifyExpiredMessageCount`, `VerifyRollBackCount`, `VerifyProducedCount`, and `VerifyProducedAsyncCount` via a shared private `PollUntil` helper. Default polling timeout is 15s (bumped from 5s after a `processed=99/100` chaos+hold-transaction flake on SqlServer). When adding a NEW `Verify*` helper, route it through `PollUntil` and accept `IMetrics` (not just `MetricsSnapshot`) — taking a one-shot snapshot is the wrong pattern.
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
- **Publish-workflow dry-run on a fresh master merge will fail verify-gate until Jenkins finishes.** A master merge does trigger Jenkins' Multibranch Pipeline (same as PRs), but the 14-stage matrix takes ~50 min to post its `continuous-integration/jenkins/branch` status. If you run `workflow_dispatch` with `dry_run=true` immediately after merging, verify-gate correctly fails with `Jenkins status on <sha> is 'missing'; required 'success'` — fail-loud as designed. Check readiness first: `gh api repos/blehnen/DotNetWorkQueue/commits/<master-sha>/status --jq '.statuses[] | select(.context=="continuous-integration/jenkins/branch") | .state'` — wait for `success` before triggering the dry-run. This is not a bug to fix; it's the gate working.
- **Microsoft.OpenApi 1→2 namespace/API restructure (hit via Swashbuckle 10).** `Microsoft.OpenApi.Models.*` types flattened to root `Microsoft.OpenApi` namespace. `AddSecurityRequirement(...)` signature changed to `Func<OpenApiDocument, OpenApiSecurityRequirement>` (lambda); use `OpenApiSecuritySchemeReference(..., hostDocument: doc)` for scheme refs. `OpenApiSecurityRequirement` value type is `List<string>`, not `string[]`. On this repo the migration was 5 edits across `Source/DotNetWorkQueue.Dashboard.Api/Extensions/DashboardExtensions.cs` + its swagger tests — expect the same shape anywhere Swashbuckle's legacy `Microsoft.OpenApi.Models` using statements appear.
- **The `IDbConnection` abstraction pays off for transport major bumps.** Npgsql 8→10 (2-major leap) and Microsoft.Data.SqlClient 6→7 both compiled clean with zero migration surface on this codebase, because the existing discipline of never casting to sealed transport types (`NpgsqlConnection`, `SqlConnection`, `SqliteConnection`) absorbed both jumps. Keep that discipline — any new handler that reintroduces a sealed-type cast is a future migration tax that will be paid the next time these transports jump majors.
- **CVE-fix plans must cite the advisory's explicit patched version, not "newer".** Phase 3 PLAN-5.1 initial draft specified `System.Security.Cryptography.Xml 8.0.2` — which IS the vulnerable version listed in GHSA-37gx-xxp4-5rgx. Caught in plan critique before build. Author CVE-fix plans against the advisory's "Patched versions" field verbatim, not a vague "bump to latest" — the vulnerable version often sits numerically close to the fix.
- **Aggressive one-pass dependency refresh is viable on this codebase — precedent, not default.** 8 majors + 1 CVE fix on a single branch, zero reverts, Jenkins green first try (2026-04). The posture worked because multi-targeting caps (net8 compat) were pre-identified, landmines were enumerated from prior lessons, and `IDbConnection` abstractions absorbed the transport bumps. Future refreshes can use this as precedent when the same preconditions hold; fall back to per-major PRs when they don't.
- **Uncommitted `.shipyard/STATE.json` silently blocks `git pull`.** When a session ends mid-build, Shipyard mutates STATE.json without committing. The next `git pull` fails with `Your local changes to the following files would be overwritten by merge: .shipyard/STATE.json` — easy to misread as a remote/tag issue. Fix: commit state transitions promptly at session close, or `git stash push .shipyard/STATE.json` then pull. A pre-pull hook that auto-stashes `.shipyard/STATE.json` would eliminate the foot-gun entirely.
- **CI filters inherited from prior CI servers are stale until proven otherwise.** The `--filter "FullyQualifiedName!~JobScheduler"` exclusion across all 13 Jenkins integration stages was carried over from the TeamCity era. PR #130 (issue #127, 2026-04-21) dropped all 13 exclusions in one commit and got two consecutive green runs (PR + post-merge master) with zero flakiness. Lesson: when migrating CI, treat inherited filters/exclusions as suspect — re-validate them against the new infrastructure rather than assuming the old reason still applies. One experimental PR is cheaper than indefinitely missing regression coverage.
- **Microsoft.Playwright.MSTest pins MSTest 2.x — incompatible with this repo's MSTest 4.x.** PR #133 (issue #126, 2026-04-21) attempted to use the wrapper for the standard `[TestClass]` + `PageTest` ergonomics; central package management resolved MSTest to 4.2.1 but Playwright.MSTest's `PageTest` was built against MSTest 2.x and its tests silently failed to discover via VSTest (`No test is available`). Fix: use raw `Microsoft.Playwright` and hand-roll the assembly/class fixtures (single `[AssemblyInitialize]` static class for `IPlaywright` + `IBrowser`; per-test `[TestInitialize]`/`[TestCleanup]` for `IBrowserContext` + `IPage`). Don't reach for `Microsoft.Playwright.MSTest` here.
- **WebApplicationFactory + Playwright is a dead end on Blazor Server.** PR #133 spent significant time fighting `WebApplicationFactory<Program>` to expose a Kestrel URL: WAF's `ConfigureWebHost` forces `UseTestServer()` after user setup runs, so the resolved `IServer` is always TestServer (no real socket → no Playwright). The dual-host trick (build TestServer host, then a parallel Kestrel host from the same builder) ran into address-binding races and `UseUrls(":0")` literal-port issues. Working approach: skip WAF entirely, launch Dashboard.Ui as a child process via `dotnet bin/.../DotNetWorkQueue.Dashboard.Ui.dll`, set config overrides through env vars (`Key:Subkey` → `Key__Subkey`), parse Kestrel's `Now listening on:` log line for the bound URL. See `Source/DotNetWorkQueue.Dashboard.Ui.E2E.Tests/Fixtures/DashboardSubprocess.cs`.
- **Blazor Server `OnAfterRender`-driven redirects race the SignalR circuit attach in E2E tests.** `MainLayout.razor`'s "redirect to /login if unauthenticated" path runs from `OnAfterRender(firstRender)` on the *interactive* render pass, which only happens after the browser opens the SignalR WebSocket. Playwright assertions can fire before that handshake completes, so the URL stays at `/` for 5+ seconds. PR #133 dropped `RootRedirectsToLogin_WhenAuthEnabledAndUnauthenticated` E2E test for this reason — the identical assertion is covered by the bUnit `MainLayoutTests` which doesn't have the circuit timing dependency. Pattern: keep redirect/state-transition assertions in bUnit; reserve E2E for plain HTTP flows (form POSTs, static page renders).
- **Jenkins agent `docker` label means the agent IS a Docker container — not that it has the docker CLI.** PR #133 first attempted `agent { docker { image 'mcr.microsoft.com/playwright/dotnet:...' } }` for the E2E stage and got `docker: not found` on line 1 of the Jenkins script. The `docker`-labeled agents in this repo can't launch nested containers. For stages needing extra tooling (Playwright browsers, etc.), install at stage time on the standard agent — for Playwright specifically: `dotnet exec --runtimeconfig <test>.runtimeconfig.json bin/.../Microsoft.Playwright.dll install --with-deps chromium`. No `pwsh` needed (the docs default to `pwsh playwright.ps1 install` but that's not what the repo's agents run).
- **String-comparator drift — normalize both sides or neither.** Outbox milestone (PR #138) hit a symmetry bug where `SqlServerExternalDbNameExtractor` applied `.ToUpperInvariant()` but the validator compared the result against `IConnectionInformation.Container` (verbatim from connection-string `InitialCatalog`) with `StringComparer.Ordinal`. False mismatches only surfaced during Phase 6 integration tests with a mixed-case catalog. Fix: pass-through on the extractor side (commit `994e1404`), matching the PostgreSQL extractor that was already pass-through. Pattern: any new "compare these two strings" code MUST verify both upstream sources apply identical normalization (or both apply none) — don't add `.ToUpperInvariant()` on one side without an explicit invariant test against the other side.
- **No `Tx` abbreviation for "transaction" — use the full word in identifiers and prose.** Outbox milestone (ISSUE-036) drifted to `Tx` across internal symbols and docs through Phases 2-5; user review caught the inconsistency during Phase 6. Resolved in commits `9858f04f` (production + tests) and `ef848165` (PG Wave 2 follow-up — plan code shapes had been authored pre-rename and the builder followed them literally). When a phase-wide rename lands mid-build, `grep -nP "\bOldToken\b" .shipyard/phases/*/plans/*.md` to refresh outstanding plan files before kicking off subsequent waves.
- **Plan code shapes can drift from the actual API surface.** Outbox PR #138 / REVIEW-1.2 caught a missing `using DotNetWorkQueue.Configuration;` in the `docs/outbox-pattern.md` tutorial code block — copy-paste would have failed CS0246 because `QueueConnection` lives in `DotNetWorkQueue.Configuration` not `DotNetWorkQueue` root. Pattern: reviewers for doc/code-example plans should TRACE the example against the current API surface (mentally compile it), not just verify it matches the plan's code shape.
- **Phase scope reframing — RESEARCH should validate the phase isn't already done.** ROADMAP Phase 7 framed as "add XML doc comments to phases 2-4 public types." RESEARCH §1 surfaced that the builders had already added docs as they went. Phase 7 reframed to a VERIFICATION pass + csproj gate fixes (net8 `<DocumentationFile>` gap + ISSUE-032 NU1902 closure on Transport.SQLite). Pattern: when a phase title implies authoring, researcher should explicitly confirm the work was not already incidentally done before architect plans new authoring work.
- **`<WarningsNotAsErrors>` pattern for accepted advisory carry-forward.** ISSUE-032 (OpenTelemetry NU1902 advisory) blocked the strictest `dotnet build -c Release -p:CI=true` build path on `Transport.SQLite`. Phase 7 PLAN-1.1 / commit `88ff8996` added `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to all 3 Release PropertyGroup blocks (`Release|net10.0`, `Release|net8.0`, `Release|AnyCPU`). The advisory still surfaces as a visible warning on every Release build (long-term remediation isn't forgotten) but the build is no longer blocked. Reusable pattern when a ship-blocking advisory is out of scope for the current milestone.

## Code Quality
- Prefer correct, complete implementations over minimal ones.
- Use appropriate data structures and algorithms — don't brute-force what has a known better solution.
- When fixing a bug, fix the root cause, not the symptom.
- If something I asked for requires error handling or validation to work reliably, include it without asking.
- New and changed features should be covered by either unit or integration Tests
- Features that might vary by the transport implementation should have integration Tests; This has caused issues before with Redis History for example
