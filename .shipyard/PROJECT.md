# Project: Dashboard API History Tests — Redis & LiteDb

## Description

Add end-to-end Dashboard API integration tests for Redis and LiteDb transports covering history endpoints. These transports are missing from the existing history test coverage (Memory, SQLite, SqlServer, PostgreSQL already have tests). The Redis history purge bug (#103) went undetected for 7 days after release because no integration test exercised the Redis purge code path.

Both test files follow the established `MemoryHistoryTests.cs` pattern with Disabled + Enabled test classes.

## Goals

1. Add `RedisHistoryTests.cs` with `RedisHistoryDisabledTests` + `RedisHistoryEnabledTests` (~15 tests)
2. Add `LiteDbHistoryTests.cs` with `LiteDbHistoryDisabledTests` + `LiteDbHistoryEnabledTests` (~15 tests)
3. Both follow the exact `MemoryHistoryTests.cs` pattern: send + consume messages, then test listing, pagination, status filtering, count, individual record lookup, purge
4. Tests run in Jenkins CI (Redis available) and locally for LiteDb (no server dependency)

## Non-Goals

- Improving DashboardExtensions.cs coverage (separate effort)
- Adding SqlServer/PostgreSQL/SQLite history tests (already exist)
- Adding Docker Compose or testcontainers for local Redis
- Changing the Dashboard API itself
- Adding new test infrastructure beyond what already exists

## Requirements

### RedisHistoryTests.cs
- `RedisHistoryDisabledTests`: history endpoints return empty/zero/NotFound when `EnableHistory = false`
- `RedisHistoryEnabledTests`: send + consume messages, verify all history endpoints work correctly
- Connection string from `connectionstring.txt` (matching existing Redis integration test pattern)
- Uses `RedisDashboardInit` and appropriate queue creation type
- Skipped when Redis is not available (no connection string)

### LiteDbHistoryTests.cs
- `LiteDbHistoryDisabledTests`: history endpoints return empty/zero/NotFound when `EnableHistory = false`
- `LiteDbHistoryEnabledTests`: send + consume messages, verify all history endpoints work correctly
- File-based connection string (no server dependency)
- Uses `LiteDbDashboardInit` and appropriate queue creation type

### Test coverage per Enabled class
- History listing with pagination (page 0, page 1, beyond last)
- Status filtering (Complete, Error, Processing)
- History count (with and without status filter)
- History by queue ID (found and not found)
- Record field validation (QueueId, Status, EnqueuedUtc)
- Purge with date filter (removes completed records)
- Purge with future days (removes nothing)

## Non-Functional Requirements

- Tests must be independent (no shared state between test methods)
- Cleanup must remove queues/files after each test class
- Redis tests must not fail when Redis is unavailable (skip gracefully)

## Success Criteria

1. `RedisHistoryTests.cs` passes when Redis is available (Jenkins CI)
2. `LiteDbHistoryTests.cs` passes everywhere (no external dependencies)
3. All existing Dashboard integration tests continue to pass
4. `dotnet build "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj"` succeeds

## Constraints

- Must follow existing `MemoryHistoryTests.cs` pattern exactly
- Must use existing test infrastructure (`DashboardTestServer`, `TransportFixture`, `ConnectionStrings`, etc.)
- Redis tests run in Jenkins CI only (connection string gated)
- No changes to production code
