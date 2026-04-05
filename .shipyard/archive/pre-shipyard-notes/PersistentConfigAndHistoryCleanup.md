# Persistent Config + History Cleanup — Implementation Plan

## Goal

Make all transports persist queue configuration (transport options), eliminate the `IHistoryConfiguration` bridge, and re-enable history for Redis and Memory transports. After this, history works the same way on all 6 transports: set `EnableHistory = true` at queue creation, everything else is automatic.

## Current State

- **SQLite, SQL Server, PostgreSQL**: persist transport options to a Configuration table. History works via `SetDefaultsIfNeeded` bridge that copies `EnableHistory` from saved options to `IHistoryConfiguration.Enabled`. Working correctly.
- **LiteDB**: persists options to a Configuration collection, but newly added properties (like `EnableHistory`) don't deserialize correctly in in-memory mode. History table gets created but the bridge reads `false`.
- **Redis**: no persistent config. All options hardcoded in `RedisBaseTransportOptions`. History handlers removed.
- **Memory**: no persistent config. All options hardcoded in `TransportOptions`. History handlers removed.

## End State

- All 6 transports persist transport options
- `IBaseTransportOptions` is registered in every container's DI
- History decorators inject `IBaseTransportOptions` directly — no bridge, no `IHistoryConfiguration`
- `IHistoryConfiguration` and `HistoryConfiguration` deleted
- History handlers re-added for Redis and Memory
- All history integration tests pass

## Phase 1: Memory Transport — Persistent Config

Memory transport stores queue data in static `DataStorage`. Add options storage there.

**Changes:**
- `DataStorage`: add `ConcurrentDictionary<string, IBaseTransportOptions>` for saved options
- `MessageQueueCreation`: save options to `DataStorage` during `CreateQueue()`
- Create `MemoryMessageQueueTransportOptions` (new class, replaces hardcoded `TransportOptions`) with mutable properties including `EnableHistory`, `HistoryOptions`
- Create `MemoryMessageQueueTransportOptionsFactory` that loads from `DataStorage` (or returns defaults if not saved)
- `MemoryMessageQueueInit.SetDefaultsIfNeeded`: add same bridge pattern as relational transports
- Re-add history handlers (`WriteMessageHistoryHandler`, `QueryMessageHistoryHandler`, `PurgeMessageHistoryHandler`)
- Register handlers in `MemoryMessageQueueInit`
- Re-add Memory history integration test

## Phase 2: Redis Transport — Persistent Config

Redis stores queue metadata in hashes. Add a config hash per queue.

**Changes:**
- `RedisNames`: add `Configuration` property (e.g. `{YADNQ_queueName}_Configuration`)
- Create `RedisMessageQueueTransportOptions` (new class, replaces hardcoded `RedisBaseTransportOptions`) with mutable `EnableHistory`, `HistoryOptions`
- Save options: serialize to JSON, store in Redis hash during queue creation
- Create `RedisMessageQueueTransportOptionsFactory` that loads from Redis hash (or returns defaults)
- `RedisQueueInit.SetDefaultsIfNeeded`: add bridge pattern
- Re-add history handlers (`WriteMessageHistoryHandler`, `QueryMessageHistoryHandler`, `PurgeMessageHistoryHandler`)
- Register handlers in `RedisQueueInit`
- Re-add Redis history integration test
- Clean up on queue removal (delete the config hash)

## Phase 3: LiteDB — Fix Config Persistence

LiteDB already has config persistence but `EnableHistory` doesn't deserialize. Debug and fix.

**Investigation:**
- Check how `GetQueueOptionsQuery` deserializes `LiteDbMessageQueueTransportOptions`
- Check if the serializer (likely `IInternalSerializer` / Newtonsoft) handles new properties added after initial save
- Likely fix: the saved config blob was serialized before `EnableHistory` existed, so it deserializes with default `false`. New queues created with `EnableHistory = true` should work. This may only be an issue for queues created before the property was added — in which case it's expected behavior, not a bug.
- Test: create a NEW queue (not reuse old one) with `EnableHistory = true`, verify it loads correctly
- If the issue is in-memory mode specifically: LiteDB in-memory may share the DB but the Configuration collection may not be visible across containers. Check scope sharing.

**Changes (if it's a scope issue):**
- Ensure `LiteDbConnectionManager` is shared via `RegisterNonScopedSingleton(scope)` in tests
- The shared test already does this — may need to verify the options factory uses the shared connection

**Changes (if it's a serializer issue):**
- Ensure `EnableHistory` and `HistoryOptions` are serializable (they use `HistoryTransportOptions` which has only primitive types — should serialize fine)
- May need to add `[JsonProperty]` attributes or ensure default constructor

## Phase 4: Register IBaseTransportOptions in DI

Once all transports persist and load options, register `IBaseTransportOptions` in each transport's init so decorators can resolve it directly.

**Changes per transport:**

SQLite:
```csharp
container.Register<IBaseTransportOptions>(() =>
    (IBaseTransportOptions)container.GetInstance<ISqLiteMessageQueueTransportOptionsFactory>().Create(),
    LifeStyles.Singleton);
```

SQL Server, PostgreSQL: same pattern with their options factories.

LiteDB:
```csharp
container.Register<IBaseTransportOptions>(() =>
    (IBaseTransportOptions)container.GetInstance<LiteDbMessageQueueTransportOptionsFactory>().Create(),
    LifeStyles.Singleton);
```

Redis:
```csharp
container.Register<IBaseTransportOptions>(() =>
    (IBaseTransportOptions)container.GetInstance<RedisMessageQueueTransportOptionsFactory>().Create(),
    LifeStyles.Singleton);
```

Memory:
```csharp
container.Register<IBaseTransportOptions>(() =>
    (IBaseTransportOptions)container.GetInstance<MemoryMessageQueueTransportOptionsFactory>().Create(),
    LifeStyles.Singleton);
```

## Phase 5: Decorators Use IBaseTransportOptions Directly

Change all history decorators and handlers to inject `IBaseTransportOptions` instead of `IHistoryConfiguration`.

**Files to update:**

Core decorators (5 files in `DotNetWorkQueue/History/Decorator/`):
- `CommitMessageHistoryDecorator` — `_config.Enabled && _config.TrackComplete` → `_options.EnableHistory && _options.HistoryOptions.TrackComplete`
- `ReceiveMessagesHistoryDecorator` — same pattern for `TrackProcessing`
- `ReceiveMessagesErrorHistoryDecorator` — same for `TrackError`, use `_options.HistoryOptions.MaxExceptionLength`
- `RollbackMessageHistoryDecorator` — check `_options.EnableHistory`
- `SendMessagesHistoryDecorator` — same for `TrackEnqueue`

Core monitor:
- `ClearHistoryMonitor` — inject `IBaseTransportOptions` instead of `IHistoryConfiguration` for `Enabled` check and `MonitorTime`
- `QueueMonitor` — check `_options.EnableHistory` instead of `_historyConfiguration.Enabled`

Transport handlers (relational, LiteDB):
- `WriteMessageHistoryHandler` — `_config.Enabled` → `_options.EnableHistory`
- `QueryMessageHistoryHandler` — same
- `PurgeMessageHistoryHandler` — same

## Phase 6: Delete IHistoryConfiguration

Remove:
- `IHistoryConfiguration.cs`
- `Configuration/HistoryConfiguration.cs`
- All `IHistoryConfiguration` registrations from `ComponentRegistration.cs`
- `ApplyTransportOptions` method and bridge code from `SetDefaultsIfNeeded` (no longer needed)
- `IHistoryConfiguration` from `QueueMonitor` constructor
- `IClearHistoryMonitor` may need adjustment if it used `IHistoryConfiguration` for `MonitorTime`

## Phase 7: Fix History Query Pagination (Issue #5)

Move pagination to transport-specific SQL.

**Changes:**
- Create `GetDashboardHistoryMessagesQuery` in `Transport.Shared`
- SQLite: `LIMIT @PageSize OFFSET @Offset`
- SQL Server: `OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY`
- PostgreSQL: `LIMIT @PageSize OFFSET @Offset`
- LiteDB: LINQ `.Skip().Take()` (already does this)
- Remove in-memory pagination from shared `QueryMessageHistoryHandler`

## Phase 8: Fix LiteDB Test Cleanup (Issue #6)

**Changes:**
- Ensure admin container `using` block completes before `oCreation.RemoveQueue()`
- The shared `SimpleHistoryTest` already has the admin container in a `using` — verify it disposes before `finally`

## Phase 9: Tests

- Re-enable Memory history integration test
- Re-enable Redis history integration test
- Re-enable LiteDB history integration test (both in-memory and file-based once fixed)
- Re-enable Memory + Redis + LiteDB dashboard API history integration tests
- Update all unit tests that reference `IHistoryConfiguration`
- Verify all 6 transports pass CI

## Estimated Effort

| Phase | Effort | Files |
|-------|--------|-------|
| 1. Memory config | Small | ~5 new, ~3 modified |
| 2. Redis config | Medium | ~5 new, ~3 modified |
| 3. LiteDB fix | Small-Medium | ~2 modified (debugging) |
| 4. Register IBaseTransportOptions | Small | ~6 modified (one per transport init) |
| 5. Decorators refactor | Medium | ~10 modified |
| 6. Delete IHistoryConfiguration | Small | ~5 deleted/modified |
| 7. Pagination | Medium | ~4 new, ~1 modified |
| 8. Test cleanup | Small | ~1 modified |
| 9. Tests | Medium | ~8 re-enabled/modified |

Total: ~2-3 sessions
