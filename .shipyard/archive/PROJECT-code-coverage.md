# Project: Code Coverage Improvement

## Description

Close the most significant code coverage gaps in DotNetWorkQueue by adding targeted unit tests for job scheduler handlers, enabling trace instrumentation in CI integration tests, and cleaning up dead code. The approach favors integration-test-friendly patterns but uses unit tests where integration tests are impractical (CI timing issues, DI wiring).

Current overall coverage: 88.9% line / 73.4% branch. The goal is not a specific number but to close obvious gaps in under-tested areas.

## Goals

1. Unit test the shared job scheduler command/query handlers in `Transport.RelationalDatabase` and transport-specific variants in LiteDb and Redis
2. Enable in-memory trace exporting in CI integration tests so existing test runs cover `TraceExtensions` code paths across all transports
3. Investigate `ObjectPool` in core -- if it's dead code from dynamic LINQ removal, delete it
4. Improve `Dashboard.Api` `DashboardExtensions` branch coverage where practical

## Non-Goals

- Dashboard.Ui Blazor component testing (needs bUnit/Playwright infrastructure -- separate effort)
- Fixing job scheduler integration test CI timing issues (separate effort)
- Achieving a specific coverage percentage target
- Writing tests for DI/startup wiring overloads that aren't worth chasing

## Requirements

### Workstream 1: Job Scheduler Handler Unit Tests

- Test shared handlers in `Transport.RelationalDatabase`: `SetJobLastKnownEventCommandHandler`, `CreateJobTablesCommandHandler`, `GetJobIdQueryHandler`, `SendJobToQueue` logic
- Test shared handlers in `Transport.Shared` if applicable
- Test transport-specific handlers for LiteDb (custom job handlers) and Redis (Lua-based job handlers)
- Use mocked data access -- no real databases, no timing sensitivity
- Follow existing test conventions: MSTest 3.x, NSubstitute, AutoFixture, FluentAssertions 6.12.2
- Cover `GetDashboardErrorRetriesQueryHandlerAsync` and `GetDashboardJobsQueryHandlerAsync` where below 40%

### Workstream 2: In-Memory Trace Exporter for CI Integration Tests

- Add `System.Diagnostics.ActivityListener` or OpenTelemetry `InMemoryExporter` to the integration test harness
- Existing integration tests already exercise traced code paths -- enabling a listener covers `TraceExtensions` automatically
- Target assemblies: SqlServer, PostgreSQL, SQLite, Redis, LiteDb (all have `TraceExtensions` at 0%)
- No network calls to Jaeger/external collectors -- in-memory only
- Must not slow down CI pipeline meaningfully

### Workstream 3: ObjectPool Dead Code Investigation

- Determine whether `ObjectPool` in core DotNetWorkQueue is still referenced
- If dead code from dynamic LINQ removal: delete it entirely (prefer compile errors over test coverage of unused code)
- If still used: add unit tests

### Workstream 4: Dashboard.Api DashboardExtensions (Lower Priority)

- Identify which untested branches in `DashboardExtensions` represent real configuration scenarios
- Add integration tests with different configuration combinations where practical
- Accept that some DI registration overloads may not be worth testing

## Non-Functional Requirements

- All new tests must pass in CI (GitHub Actions + Jenkins)
- No new external service dependencies for CI
- No changes to existing test behavior or existing coverage
- New unit test projects (if needed) must be added to the solution and CI pipeline

## Success Criteria

1. Job scheduler handlers in `Transport.RelationalDatabase` have unit test coverage above 80%
2. LiteDb and Redis custom job handlers have targeted unit tests
3. `TraceExtensions` across all transports show non-zero coverage after CI integration test run
4. `ObjectPool` is either deleted (if dead) or tested (if live)
5. All existing tests continue to pass
6. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` -- 0 errors
7. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` -- 0 errors, 0 warnings

## Constraints

- Job scheduler integration tests remain disabled in CI due to timing issues -- do not attempt to fix that here
- FluentAssertions pinned to 6.12.2 (last MIT version)
- Must work on net10.0 and net8.0
- Dashboard.Ui coverage improvement is out of scope
