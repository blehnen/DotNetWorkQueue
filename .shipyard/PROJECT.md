# Integration Test Cleanup

## Description

Clean up dead code in Redis integration tests and add retry resilience for remote transport integration tests. The Redis `ConnectionInfoTypes` enum is vestigial from when Redis for Windows was supported — it has a single `Linux` value and the parameter is accepted but completely ignored in every test. Remote transport tests (SqlServer, PostgreSQL, Redis) hitting network services should retry once on failure to match previous TeamCity CI behavior.

## Goals

1. Remove the dead `ConnectionInfoTypes` enum and simplify Redis `ConnectionInfo` to a static class matching the SqlServer/PostgreSQL pattern
2. Add assembly-level `[RetryOnFailure(MaxRetries = 1)]` to all 6 remote transport integration test projects

## Non-Goals

- No changes to LiteDB, SQLite, or Memory transports
- No `#if NETFULL` cleanup (deferred to net48 removal)
- No callback signature refactoring in shared test infrastructure
- No test logic or coverage changes
- No changes to test execution speed or parallelism

## Requirements

### Redis ConnectionInfoTypes Removal
- Delete `ConnectionInfoTypes` enum from `ConnectionString.cs`
- Convert `ConnectionInfo` from instance class to static class (reads from `connectionstring.txt`)
- Remove `ConnectionInfoTypes.Linux` from all DataRow attributes across 34 test files
- Remove `type` parameter from test method signatures
- Replace `new ConnectionInfo(type).ConnectionString` with `ConnectionInfo.ConnectionString`

### Remote Transport Test Retry
- Add `[assembly: RetryOnFailure(MaxRetries = 1)]` via `AssemblyInfo.cs` in each of these 6 projects:
  - `DotNetWorkQueue.Transport.Redis.IntegrationTests`
  - `DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests`
  - `DotNetWorkQueue.Transport.SqlServer.IntegrationTests`
  - `DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests`
  - `DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests`
  - `DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests`

## Non-Functional Requirements

- Zero change in test coverage
- All existing tests must continue to pass

## Success Criteria

1. `ConnectionInfoTypes` enum no longer exists anywhere in the codebase
2. Redis `ConnectionInfo` is a static class matching SqlServer/PostgreSQL pattern
3. All 34 Redis test files compile without `ConnectionInfoTypes` references
4. All 6 remote transport test projects have assembly-level `[RetryOnFailure(MaxRetries = 1)]`
5. All integration tests pass (Redis, SqlServer, PostgreSQL)
6. No test coverage regression

## Constraints

- .NET 4.8 support must be maintained (net48 + net10.0 multi-targeting)
- MSTest 3.x `[RetryOnFailure]` attribute must be available on both target frameworks
