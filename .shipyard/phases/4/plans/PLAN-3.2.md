---
phase: phase-4-litedb-redis-job-handlers
plan: "3.2"
wave: 3
dependencies: ["1.1"]
must_haves:
  - New test file for DoesJobExistLua
  - Uses virtual TryExecute seam from Plan 1.1
  - Covers null-result -> NotQueued, valid-result -> mapped QueueStatuses, IsDisposed -> NotQueued
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/Lua/DoesJobExistLuaTests.cs
tdd: true
risk: low
---

# Plan 3.2 — DoesJobExistLua Tests

## Context

`Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/DoesJobExistLua.cs` inherits `BaseLua`. It calls `TryExecute(object)` (made virtual in Plan 1.1) and maps the `RedisResult` to a `QueueStatuses` enum. Constructor: `(IRedisConnection connection, RedisNames redisNames)`.

Test pattern — subclass to override the seam:

```csharp
private sealed class TestableDoesJobExistLua : DoesJobExistLua
{
    public TestableDoesJobExistLua(IRedisConnection conn, RedisNames names) : base(conn, names) { }
    public RedisResult NextResult { get; set; } = RedisResult.Create(RedisValue.Null);
    public override RedisResult TryExecute(object parameters) => NextResult;
}
```

`IRedisConnection` and `RedisNames` — check whether `RedisNames` is an interface or concrete. If concrete and non-trivial, substitute or construct minimally.

## Dependencies

- **Plan 1.1** — requires `BaseLua.TryExecute(object)` to be `virtual`.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/Lua/DoesJobExistLuaTests.cs" tdd="true">
  <action>
  1. READ `Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/DoesJobExistLua.cs` to confirm the exact result-to-QueueStatuses mapping and the constructor signature.
  2. READ `Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/BaseLua.cs` to confirm Plan 1.1 has landed (`public virtual RedisResult TryExecute`).
  3. READ `Source/DotNetWorkQueue.Transport.Redis/IRedisConnection.cs` and `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisNames.cs` to determine how to construct the constructor args (substitute vs. minimal real instance).
  4. Create the folder `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/Lua/` if it does not exist.
  5. CREATE `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/Lua/DoesJobExistLuaTests.cs` with LGPL-2.1 header.
  6. Add a `private sealed class TestableDoesJobExistLua : DoesJobExistLua` nested in the test class that overrides `TryExecute(object)` to return a configured `RedisResult`.
  7. Add `[TestMethod]`s:
     - `Execute_ReturnsNotQueued_WhenResultIsNullRedisValue` — configure `NextResult = RedisResult.Create(RedisValue.Null)`, assert the mapped return equals `QueueStatuses.NotQueued`.
     - `Execute_ReturnsMappedStatus_WhenResultIsValidInteger` — for each distinct mapping in the source (e.g., `0 -> NotQueued`, `1 -> Processing`, etc.), add a case (either one test per mapping or a `[DataRow]`-driven parameterized test).
     - `Execute_ReturnsNotQueued_WhenConnectionIsDisposed` — substitute `IRedisConnection` so `IsDisposed` returns true; invoke; assert `QueueStatuses.NotQueued`. (Note: if `IsDisposed` is only checked inside `BaseLua.TryExecute` and your test overrides that method, this scenario must be exercised through the base method rather than the testable override. In that case, add a second testable subclass that does NOT override `TryExecute` and verify via a disposed-connection substitute — but only if `IRedisConnection.IsDisposed` is the sole gate on that path.)
     - `Constructor_NullConnection_Throws` and `Constructor_NullRedisNames_Throws` using `Assert.ThrowsExactly<ArgumentNullException>` if the production constructor has null guards.
  8. Use NSubstitute for `IRedisConnection`; construct `RedisNames` with whatever minimal seed matches existing Redis tests.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~DoesJobExistLuaTests"</verify>
  <done>Test file exists. All enumerated scenarios pass on net10.0 and net8.0. Every distinct result-to-QueueStatuses mapping from the source has a corresponding assertion.</done>
</task>
