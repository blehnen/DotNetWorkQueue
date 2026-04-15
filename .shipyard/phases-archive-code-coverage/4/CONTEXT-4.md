# Phase 4 Context: LiteDb + Redis Transport Job Handler Tests

## Decisions

### Scope
Tackle the testability issues deferred from Phase 3:
- **LiteDb handlers** that use concrete `LiteDatabase` (sealed)
- **Redis Lua handlers** that use the sealed `IConnectionMultiplexer` chain
- **Redis `RedisJobQueueCreation`** thin wrapper

### LiteDb Mock Strategy
- **Use real in-memory LiteDatabase instances** -- `new LiteDatabase(":memory:")` in tests
- No production code changes for LiteDb (no interface seam refactor)
- Tests are essentially unit tests with real LiteDB instead of mocks
- Slightly slower than pure mocks but exercises real LiteDB behavior
- Recommendation: create a small `LiteDbInMemoryFixture` test helper if duplication appears

### Redis Seam Design
- **Add `protected virtual RedisResult TryExecute(object parameters)` to `BaseLua`** -- single seam covers sync calls
- **For async Lua handlers**, add `protected virtual Task<RedisResult> TryExecuteAsync(object parameters)` if any async Lua handler exists
- The seam allows test subclasses to return a mocked `RedisResult` without touching the sealed StackExchange.Redis chain
- Existing Lua handlers (subclasses of `BaseLua`) need NO changes; the base implementation remains the default behavior
- This is a minimal, additive refactor

### Plan Split (smaller plans for reliability)
**Per-handler plans** -- one plan per handler, max 1 task each. Higher orchestration cost but lowest builder lockup risk and max parallelism.

### Wave Structure
- **Wave 1: Production refactors** -- Add `BaseLua.TryExecute` virtual seam (1 plan)
- **Wave 2: LiteDb tests** -- 5 plans (one per LiteDb handler)
- **Wave 3: Redis tests** -- 4 plans (one per Redis handler; Lua handlers depend on Wave 1)

Plans within waves can run in parallel. Each test project is a different transport so no file conflicts within waves.

### Coverage Targets
- LiteDb handlers: at least 70% line coverage
- Redis handlers: at least 70% line coverage

### Test Conventions
- MSTest 3.x, NSubstitute, FluentAssertions where existing tests use it
- `Assert.ThrowsExactly<ArgumentNullException>` for null guards
- For LiteDb tests, prefer real in-memory `LiteDatabase` over NSubstitute mocks
- For Redis tests, subclass each Lua handler in tests and override the new `TryExecute` seam

### Out of Scope
- LiteDb dashboard handlers other than the explicitly listed ones
- Any production refactor beyond the minimal `BaseLua` seam
- Adding test projects (use existing `DotNetWorkQueue.Transport.LiteDb.Tests` and `DotNetWorkQueue.Transport.Redis.Tests`)
