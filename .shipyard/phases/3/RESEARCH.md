# Phase 3 Research: Transport-Specific Job Scheduler Handler Unit Tests

## Critical Feasibility Findings

Phase 3 has significant testability variations across transports. **Not all targeted handlers can be unit tested without refactoring.**

## Architectural Variants

### Variant A: Testable -- Uses connection from command
Pattern: `IDbConnection`/`IDbTransaction` passed via the command object. Unit testable with NSubstitute mocks.

| Handler | Testability |
|---------|-------------|
| `SQLite SetJobLastKnownEventCommandHandler` | TESTABLE -- already has tests at `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs` |

### Variant B: NOT Testable -- Hardcoded connection instantiation
Pattern: Handler does `new SqlConnection(connectionString)` / `new NpgsqlConnection(connectionString)` directly. **Cannot be unit tested without refactor**, would need integration test or `IDbConnectionFactory` injection.

| Handler | Testability |
|---------|-------------|
| `SqlServer SetJobLastKnownEventCommandHandler` | NOT TESTABLE -- `new SqlConnection(_connectionInformation.ConnectionString)` at line 56 |
| `PostgreSQL SetJobLastKnownEventCommandHandler` | NOT TESTABLE -- `new NpgsqlConnection(_connectionInformation.ConnectionString)` at line 51 |

### Variant C: LiteDB direct API
Pattern: Uses concrete `LiteDatabase` sealed type via `command.Database`. Not mockable; would need real in-memory LiteDB instance.

| Handler | Testability |
|---------|-------------|
| `LiteDb SetJobLastKnownEventCommandHandler` | DIFFICULT -- uses `LiteDatabase` directly, would need real in-memory LiteDB |
| `LiteDbSendJobToQueue` | DIFFICULT -- internal class, uses `LiteDbConnectionManager.GetDatabase()` returning real `LiteDatabase` |

### Variant D: Redis Lua scripts
Pattern: Inherits from `BaseLua`, calls `Connection.Connection.GetDatabase()` (sealed `IConnectionMultiplexer`/`IDatabase` from StackExchange.Redis). **No GetDb() seam exists.** Would need to add `protected virtual` seam to `BaseLua` or to each script.

| Handler | Testability |
|---------|-------------|
| `DoesJobExistLua` | NEEDS SEAM -- BaseLua.TryExecute calls Connection.Connection.GetDatabase() directly |
| `DashboardUpdateMessageBodyLua` | NEEDS SEAM -- same as above |

### Variant E: Thin wrappers
Pattern: Delegates to other testable types. Unit testable with mocked collaborators.

| Handler | Testability |
|---------|-------------|
| `RedisJobQueueCreation` | TESTABLE (mostly) -- thin wrapper around `RedisQueueCreation`. Note `RedisQueueCreation` itself uses a real `RedisConnection`, so even this requires careful mocking. |

## Existing Job-Related Test Files (already exist)

| Project | Existing test |
|---------|--------------|
| SqlServer.Tests | `Basic/SqlServerJobQueueCreationTests.cs` |
| PostgreSQL.Tests | `Basic/PostgreSqlJobQueueCreationTests.cs` |
| SQLite.Tests | `Basic/CommandHandler/SetJobLastKnownEventCommandHandlerTests.cs`, `Basic/SqliteJobQueueCreationTests.cs` |
| LiteDb.Tests | `Basic/LiteDbJobQueueCreationTests.cs`, `Basic/LiteDbJobSchedulerLastKnownEventTests.cs`, `Basic/LiteDbJobSchemaTests.cs` |
| Redis.Tests | (none for jobs) |

## Coverage Gaps -- Realistic Unit Test Scope

After eliminating non-testable handlers, here is what Phase 3 can realistically deliver as unit tests:

### Tier 1: Easy wins (no refactor needed)

1. **`SqlServer SqlServerJobSchema`** -- pure schema generation, mockable. (20% coverage, 50 lines)
2. **`PostgreSQL PostgreSqlJobSchema`** -- same. (20%, 40 lines)
3. **`SQLite SqliteJobSchema`** -- same. (20%, 40 lines)
4. **`SQLite SqliteSendToJobQueue`** -- if it follows the SQLite SetJobLastKnownEvent pattern (uses connection from command), it's testable. NEEDS INSPECTION.

### Tier 2: Need architectural refactor first

5. **`SqlServer SetJobLastKnownEventCommandHandler`** -- requires injecting `IDbConnectionFactory` instead of `new SqlConnection()`
6. **`PostgreSQL SetJobLastKnownEventCommandHandler`** -- same refactor
7. **`SqlServer SqlServerSendJobToQueue`** -- likely same pattern, NEEDS INSPECTION
8. **`PostgreSQL PostgreSQLSendJobToQueue`** -- likely same pattern, NEEDS INSPECTION
9. **`LiteDb SetJobLastKnownEventCommandHandler`** -- could test against real in-memory `LiteDatabase` instance
10. **`LiteDbSendJobToQueue`** -- internal class, complex mocking; integration test better choice

### Tier 3: Require seam refactor

11. **`Redis DoesJobExistLua`** -- needs `protected virtual TryExecute` seam in `BaseLua`
12. **`Redis DashboardUpdateMessageBodyLua`** -- same
13. **`Redis RedisJobQueueCreation`** -- thin wrapper, but needs careful mocking of `RedisQueueCreation`

## Recommendation: Rescope Phase 3

Given the architectural variations, Phase 3 has three viable scopes:

### Option A: Easy wins only (5-7 test files)
Just the JobSchema classes and any SQLite handlers that follow the testable pattern. This delivers real coverage gains without any production code changes. Estimated: 3-5 plans.

### Option B: Easy wins + refactor relational handlers (10+ test files)
Refactor SqlServer/PostgreSQL handlers to inject `IDbConnectionFactory`, then write tests. Larger scope, touches production code, but unifies the architecture. Estimated: 6-8 plans.

### Option C: All-in (refactor relational + add Redis seams + LiteDb in-memory)
Most ambitious. Requires production code changes across all 5 transports. Highest risk for builder lockups. Estimated: 10+ plans.

## Updated Findings After Further Inspection

### `SqlServerSendJobToQueue` is TESTABLE
Inherits from `ASendJobToQueue`. Constructor takes injected dependencies only:
- `IProducerMethodQueue queue`
- `IQueryHandler<DoesJobExistQuery<SqlConnection, SqlTransaction>, QueueStatuses> doesJobExist`
- `IRemoveMessage removeMessage`
- `IQueryHandler<GetJobIdQuery<long>, long> getJobId`
- `CreateJobMetaData createJobMetaData`
- `IGetTimeFactory getTimeFactory`

No hardcoded connections. `DoesJobExist()` and `DeleteJob()` overrides delegate to the injected query handlers. **All testable with NSubstitute.**

### `SqlServerJobSchema` is TESTABLE
Pure schema definition class. Constructor takes `TableNameHelper` (concrete, but trivially constructible) and `ISqlSchema` (interface, mockable). `GetSchema()` returns a `List<ITable>` describing the job table structure. No DB access.

### Likely also testable (same pattern, needs spot-check)
- `PostgreSQLSendJobToQueue.cs` -- same `ASendJobToQueue` pattern
- `SqliteSendToJobQueue.cs` -- same pattern
- `PostgreSqlJobSchema.cs`, `SqliteJobSchema.cs` -- same pure-schema pattern

### Still NOT testable without refactor
- `SqlServer SetJobLastKnownEventCommandHandler` -- hardcoded `new SqlConnection()`
- `PostgreSQL SetJobLastKnownEventCommandHandler` -- hardcoded `new NpgsqlConnection()`

### Revised Scope for Phase 3 (relational only, deferring LiteDb+Redis)

**Tier 1 (no refactor needed):**
1. `SqlServerJobSchemaTests.cs` -- NEW
2. `PostgreSqlJobSchemaTests.cs` -- NEW
3. `SqliteJobSchemaTests.cs` -- NEW
4. `SqlServerSendJobToQueueTests.cs` -- NEW
5. `PostgreSQLSendJobToQueueTests.cs` -- NEW
6. `SqliteSendToJobQueueTests.cs` -- NEW (assuming same pattern)

**Tier 2 (refactor + test):**
7. Refactor `SqlServer SetJobLastKnownEventCommandHandler` to inject `IDbConnectionFactory`, then add tests
8. Refactor `PostgreSQL SetJobLastKnownEventCommandHandler` to inject `IDbConnectionFactory`, then add tests

**Deferred to future phase (3B / 4):**
- LiteDb job handlers (concrete LiteDB types)
- Redis Lua handlers (need BaseLua seam)
- Redis RedisJobQueueCreation (thin wrapper but needs careful mocking)
- LiteDb dashboard handlers

## Note on agent dispatch

Phase 2 showed builders intermittently fail to commit/write summaries on multi-file plans. Phase 3 should use **smaller plans** (1-2 tasks max) and **explicit verification commands** in each plan's done criteria.
