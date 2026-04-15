---
phase: phase-4-litedb-redis-job-handlers
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - BaseLua.TryExecute(object) is virtual so test subclasses can override the Redis interaction
  - BaseLua.TryExecuteAsync(object) is virtual so test subclasses can override the Redis interaction
  - Redis transport still builds cleanly with no behavior changes
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/BaseLua.cs
tdd: false
risk: low
---

# Plan 1.1 — BaseLua Test Seam: Virtualize TryExecute Overloads

## Context

Phase 4 adds unit tests for Redis Lua script classes (`DoesJobExistLua`, `DashboardUpdateMessageBodyLua`) that inherit from `BaseLua`. `BaseLua.TryExecute(object)` internally calls `Connection.Connection.GetDatabase().ScriptEvaluate(...)`. `StackExchange.Redis.IDatabase` / `IConnectionMultiplexer` are sealed and cannot be mocked with NSubstitute (see CLAUDE.md lessons learned).

Minimal refactor: change the two `TryExecute` overloads from `public` to `public virtual` so tests can subclass a Lua handler and override the Redis interaction point. No body changes. All existing Lua subclasses continue to work — none currently override `TryExecute`.

## Dependencies

None.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/BaseLua.cs" tdd="false">
  <action>Open `Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/BaseLua.cs`. Locate the method declared as `public RedisResult TryExecute(object parameters)` and change its signature to `public virtual RedisResult TryExecute(object parameters)`. Locate the method declared as `public Task<RedisResult> TryExecuteAsync(object parameters)` and change its signature to `public virtual Task<RedisResult> TryExecuteAsync(object parameters)`. Do not modify method bodies, XML doc comments, license header, or any other code. Preserve file encoding and line endings.</action>
  <verify>dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" -c Debug</verify>
  <done>Build succeeds with zero errors and zero new warnings. The file contains exactly one line matching `public virtual RedisResult TryExecute(object` and one line matching `public virtual Task<RedisResult> TryExecuteAsync(object`. No other files are modified.</done>
</task>

## Verification

```bash
dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" -c Debug
dotnet build "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" -c Debug
```

Both builds must succeed with zero errors. Existing Redis tests should continue to pass unchanged.
