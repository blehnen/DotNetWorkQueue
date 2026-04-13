# Phase 4 Research: LiteDb + Redis Transport Job Handler Tests

## Existing Test Inventory

### LiteDb Tests (existing) -- in `DotNetWorkQueue.Transport.LiteDb.Tests/`

| Handler | Existing Test | Note |
|---------|--------------|------|
| `DashboardUpdateMessageBodyCommandHandler` | EXISTS at `Basic/CommandHandler/DashboardUpdateMessageBodyCommandHandlerTests.cs` | Coverage 40.9% -- needs EXPANSION |
| `GetDashboardErrorRetriesQueryHandlerAsync` | EXISTS | Already covered |
| `GetDashboardJobsQueryHandlerAsync` | EXISTS | Already covered |
| `DoesJobExistQueryHandler` | EXISTS at `Basic/QueryHandler/DoesJobExistQueryHandlerTests.cs` | Reference pattern |
| `LiteDbJobQueueCreation`, `LiteDbJobSchedulerLastKnownEvent`, `LiteDbJobSchema` | EXIST | Already covered |

### LiteDb Missing Tests (Phase 4 NEW work)

| Handler | Source File | Approach |
|---------|------------|----------|
| `SetJobLastKnownEventCommandHandler` | `Source/DotNetWorkQueue.Transport.LiteDB/Basic/CommandHandler/SetJobLastKnownEventCommandHandler.cs` | In-memory LiteDB |
| `LiteDbSendJobToQueue` | `Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbSendJobToQueue.cs` | In-memory LiteDB + mocked deps |
| `GetJobIdQueryHandler` (LiteDb-specific) | `Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryHandler/GetJobIdQueryHandler.cs` | In-memory LiteDB |
| `RollbackMessageCommandHandler` | `Source/DotNetWorkQueue.Transport.LiteDB/Basic/CommandHandler/RollbackMessageCommandHandler.cs` | In-memory LiteDB |

### Redis Tests (existing) -- in `DotNetWorkQueue.Transport.Redis.Tests/`

| Handler | Existing Test | Note |
|---------|--------------|------|
| `DashboardUpdateMessageBodyCommandHandler` | EXISTS at `Basic/CommandHandler/DashboardUpdateMessageBodyCommandHandlerTests.cs` | The COMMAND HANDLER is tested. The underlying `DashboardUpdateMessageBodyLua` (the Lua script class) is what's at low coverage. |
| Several other dashboard handlers | EXIST | Already covered |

### Redis Missing Tests (Phase 4 NEW work)

| Handler | Source File | Approach |
|---------|------------|----------|
| `RedisJobQueueCreation` | `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisJobQueueCreation.cs` | Mock `RedisQueueCreation` -- thin wrapper, no Lua, no seam needed |
| `DoesJobExistLua` | `Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/DoesJobExistLua.cs` | Subclass + override seam |
| `DashboardUpdateMessageBodyLua` | `Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/DashboardUpdateMessageBodyLua.cs` | Subclass + override seam |

## Architectural Findings

### LiteDb -- Concrete `LiteDatabase` is the challenge

`SetJobLastKnownEventCommandHandler` (already inspected):
```csharp
public void Handle(SetJobLastKnownEventCommand command)
{
    var col = command.Database.GetCollection<Schema.JobsTable>(_tableNameHelper.JobTableName);
    // ...col.Update / col.Insert
}
```

`command.Database` is `LiteDB.LiteDatabase` (sealed). Cannot mock with NSubstitute. **Solution:** Use real in-memory LiteDB instances:
```csharp
using var db = new LiteDatabase(":memory:");
var command = new SetJobLastKnownEventCommand(db, "JobName", DateTimeOffset.Now, DateTimeOffset.Now);
handler.Handle(command);
// Assert on the resulting collection state via db.GetCollection<JobsTable>(...).FindAll()
```

`LiteDbSendJobToQueue` (already inspected):
- Internal class
- Constructor: `(LiteDbConnectionManager connectionInformation, IProducerMethodQueue queue, IQueryHandler<DoesJobExistQuery, QueueStatuses> doesJobExist, IRemoveMessage removeMessage, IQueryHandler<GetJobIdQuery<int>, int> getJobId, CreateJobMetaData createJobMetaData, IGetTimeFactory getTimeFactory)`
- Inherits `ASendJobToQueue` -- overrides delegate to injected query handlers
- Most overrides are testable via NSubstitute mocks (similar to SqlServer pattern)
- Only `DoesJobExist()` uses `_connectionInformation.GetDatabase()` -- needs in-memory LiteDB or `LiteDbConnectionManager` mock

### Redis -- BaseLua Seam Refactor

Current `BaseLua.TryExecute()` (already inspected):
```csharp
public RedisResult TryExecute(object parameters)
{
    if (Connection.IsDisposed)
        return RedisResult.Create(RedisValue.Null);
    var db = Connection.Connection.GetDatabase();  // <-- sealed chain
    try {
        return parameters != null ? db.ScriptEvaluate(LoadedLuaScript, parameters) : db.ScriptEvaluate(LoadedLuaScript);
    }
    catch (RedisException e) { /* NOSCRIPT retry */ }
}
```

**Refactor:** Make `TryExecute` virtual:
```csharp
public virtual RedisResult TryExecute(object parameters)  // change to virtual
{
    // unchanged body
}

public virtual Task<RedisResult> TryExecuteAsync(object parameters)  // change to virtual
{
    // unchanged body
}
```

Test pattern:
```csharp
private class TestableDoesJobExistLua : DoesJobExistLua
{
    public TestableDoesJobExistLua(IRedisConnection conn, RedisNames names) : base(conn, names) { }
    public RedisResult NextResult { get; set; } = RedisResult.Create(RedisValue.Null);
    public override RedisResult TryExecute(object parameters) => NextResult;
}
```

This is a minimal additive change. Existing Lua handlers (subclasses) work unchanged because they don't currently override `TryExecute`.

### RedisJobQueueCreation -- Thin wrapper

Already inspected:
```csharp
public class RedisJobQueueCreation : IJobQueueCreation
{
    private readonly RedisQueueCreation _creation;
    public RedisJobQueueCreation(RedisQueueCreation creation) { _creation = creation; }
    public bool IsDisposed => _creation.IsDisposed;
    public ICreationScope Scope => _creation.Scope;
    public QueueCreationResult CreateJobSchedulerQueue(...) => _creation.CreateQueue();
    public QueueRemoveResult RemoveQueue() => _creation.RemoveQueue();
}
```

`RedisQueueCreation` is a concrete type. Need to check if it can be mocked or constructed cheaply for tests. May need to use NSubstitute on the interface methods if `RedisQueueCreation` has virtuals, or wrap in a fake.

## Recommended Plan Structure

Per CONTEXT-4.md decisions: per-handler plans, max 1 task each.

### Wave 1 -- Production Refactors
- **Plan 1.1:** Add `protected virtual` to `BaseLua.TryExecute` and `BaseLua.TryExecuteAsync` (1 task, single file change)

### Wave 2 -- LiteDb Tests (parallel-safe, all in LiteDb.Tests project but different files)
- **Plan 2.1:** `SetJobLastKnownEventCommandHandlerTests` (NEW)
- **Plan 2.2:** `LiteDbSendJobToQueueTests` (NEW)
- **Plan 2.3:** `GetJobIdQueryHandlerTests` (NEW, LiteDb-specific)
- **Plan 2.4:** `RollbackMessageCommandHandlerTests` (NEW)
- **Plan 2.5:** `DashboardUpdateMessageBodyCommandHandlerTests` -- EXPAND existing

### Wave 3 -- Redis Tests (parallel-safe, all in Redis.Tests project but different files; depends on Wave 1)
- **Plan 3.1:** `RedisJobQueueCreationTests` (NEW, no seam dependency)
- **Plan 3.2:** `DoesJobExistLuaTests` (NEW, depends on Plan 1.1 seam)
- **Plan 3.3:** `DashboardUpdateMessageBodyLuaTests` (NEW, depends on Plan 1.1 seam)

**Total: 9 plans across 3 waves.** Plans 3.1 has no Wave 1 dependency, but for orchestration simplicity all Redis test plans are in Wave 3.

## Notes for Builders

1. **LiteDb tests:** Use `using var db = new LiteDatabase(new MemoryStream())` or `new LiteDatabase(":memory:")` -- check which works in net10.0
2. **LiteDb command construction:** Many commands take a `LiteDatabase` parameter. Pass the in-memory instance directly.
3. **Redis tests:** Don't try to mock `IConnectionMultiplexer` or `IDatabase` -- use the seam subclass pattern.
4. **`DashboardUpdateMessageBodyCommandHandlerTests` (LiteDb)** needs INSPECTION first to see what's already covered before expanding.
5. **`RedisJobQueueCreation`** -- `RedisQueueCreation` is concrete; check if it has a constructor that allows minimal setup or needs an alternative test approach (e.g., test only the wrapper delegation, accepting that the underlying RedisQueueCreation isn't mocked).
