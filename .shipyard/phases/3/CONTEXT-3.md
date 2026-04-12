# Phase 3 Context: Transport-Specific Job Scheduler Handler Unit Tests

## Decisions

### Scope (expanded from original ROADMAP)
- **All 5 transports** for `SetJobLastKnownEvent` and `SendJobToQueue` variants (SqlServer, PostgreSQL, SQLite, LiteDb, Redis)
- **LiteDb-specific handlers** from original Phase 3 scope: `RollbackMessage`, `DashboardUpdateMessageBodyCommandHandler`, `GetJobIdQueryHandler`, plus the moved-from-Phase-2 items (`SetJobLastKnownEvent`, `LiteDbSendJobToQueue`)
- **Redis-specific handlers** from original Phase 3 scope: `RedisJobQueueCreation` (0% / 50 lines), `GetDashboardErrorRetriesQueryHandlerAsync`, Lua-based `DoesJobExistLua`, `DashboardUpdateMessageBodyLua`

### Redis GetDb() Seam
- Researcher should inspect the Redis job handlers to determine if the `protected virtual GetDb()` seam (per CLAUDE.md lesson) already exists
- If seams exist: tests can subclass and override
- If seams don't exist: plan will include adding the seam refactor as a prerequisite
- This is a planning gray area resolved during research

### Plan Split (smaller plans for lockup mitigation)
Per Phase 2 experience where builders failed on multi-file plans:
- **Max 1-2 tasks per plan**
- One plan per handler family per transport where possible
- Builders that need to create only 1 file are more reliable than ones that need to create 2+

### Mock Strategy
- Reuse `AdoNetMockFixture` / `AdoNetAsyncMockFixture` from Phase 2 for SqlServer/PostgreSQL/SQLite/LiteDb relational tests where applicable
- LiteDb-specific tests may need to mock the LiteDB API directly (not via ADO.NET interfaces) -- researcher to confirm
- Redis tests use the GetDb() seam pattern

### Test Project Locations
- `DotNetWorkQueue.Transport.SqlServer.Tests` for SqlServer handlers
- `DotNetWorkQueue.Transport.PostgreSQL.Tests` for PostgreSQL handlers
- `DotNetWorkQueue.Transport.SQLite.Tests` for SQLite handlers
- `DotNetWorkQueue.Transport.LiteDb.Tests` for LiteDb handlers
- `DotNetWorkQueue.Transport.Redis.Tests` for Redis handlers

### Coverage Target
- Each handler covered to at least 70% line coverage
- Higher (80%+) for the simpler handlers

### Testing Conventions
- MSTest 3.x, NSubstitute, FluentAssertions 6.12.2 (where existing tests use it)
- Use `Assert.ThrowsExactly<ArgumentNullException>` for null guards (MSTest 3.x)
- Reuse `AdoNetMockFixture` / `AdoNetAsyncMockFixture` from Phase 2 for relational transports if their handlers follow the same pattern
