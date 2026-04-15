---
phase: phase-4-litedb-redis-job-handlers
plan: "3.3"
wave: 3
dependencies: ["1.1"]
must_haves:
  - New test file for DashboardUpdateMessageBodyLua
  - Uses virtual TryExecute seam from Plan 1.1
  - Covers every branch enumerated from the source
  - Tests pass on net10.0 and net8.0
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/Lua/DashboardUpdateMessageBodyLuaTests.cs
tdd: true
risk: low
---

# Plan 3.3 — DashboardUpdateMessageBodyLua Tests

## Context

`Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/DashboardUpdateMessageBodyLua.cs` inherits `BaseLua` and executes a Lua script to update a dashboard message body. The builder must READ the source to enumerate: parameter packaging, result interpretation, and any IsDisposed short-circuit.

Uses the same seam-subclass test pattern as Plan 3.2 — depends on the `public virtual TryExecute` added in Plan 1.1.

## Dependencies

- **Plan 1.1** — requires `BaseLua.TryExecute(object)` to be `virtual`.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/Lua/DashboardUpdateMessageBodyLuaTests.cs" tdd="true">
  <action>
  1. READ `Source/DotNetWorkQueue.Transport.Redis/Basic/Lua/DashboardUpdateMessageBodyLua.cs`. Record the public Execute method signature, parameter packaging, result interpretation (what does a null result mean? what does a numeric/boolean result map to?), and whether it uses any `RedisNames` keys.
  2. Confirm `BaseLua.TryExecute(object)` is `public virtual` (Plan 1.1).
  3. READ the companion test file created by Plan 3.2 (`DoesJobExistLuaTests.cs`) once it exists, or mirror its structure from the Plan 3.2 spec, to keep style consistent.
  4. Ensure the folder `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/Lua/` exists.
  5. CREATE `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/Lua/DashboardUpdateMessageBodyLuaTests.cs` with LGPL-2.1 header.
  6. Add a nested `private sealed class TestableDashboardUpdateMessageBodyLua : DashboardUpdateMessageBodyLua` that overrides `TryExecute(object)` to return a configured `RedisResult`, and optionally capture the `parameters` argument for assertion.
  7. Add `[TestMethod]`s:
     - For each result branch enumerated from the source (e.g., success, no-op / not found, null), add one test.
     - `Execute_PassesMessageIdAndBody_ToLuaParameters` — use the captured `parameters` object to verify the Lua script is called with the expected fields (message id, body bytes/string, any key inputs from `RedisNames`).
     - `Constructor_NullConnection_Throws` and `Constructor_NullRedisNames_Throws` via `Assert.ThrowsExactly<ArgumentNullException>` if the production code has those guards.
  8. Use NSubstitute for `IRedisConnection`. Construct `RedisNames` minimally, matching the approach already used in other Redis tests.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~DashboardUpdateMessageBodyLuaTests"</verify>
  <done>Test file exists. All enumerated branches are covered. Tests pass on net10.0 and net8.0. Parameter-packaging assertion confirms the correct fields are forwarded to the Lua script.</done>
</task>
