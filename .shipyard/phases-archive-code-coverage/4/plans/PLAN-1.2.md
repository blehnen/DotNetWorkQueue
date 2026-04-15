---
phase: phase-4-litedb-redis-job-handlers
plan: "1.2"
wave: 1
dependencies: []
must_haves:
  - RedisJobQueueCreation constructor takes IQueueCreation (interface) instead of concrete RedisQueueCreation
  - Constructor adds Guard.NotNull on the creation parameter
  - Existing DI registration continues to work (RedisQueueCreation already implements IQueueCreation)
  - Redis transport still builds cleanly
files_touched:
  - Source/DotNetWorkQueue.Transport.Redis/Basic/RedisJobQueueCreation.cs
tdd: false
risk: low
---

# Plan 1.2 -- RedisJobQueueCreation: Depend on IQueueCreation Interface

## Context

`RedisJobQueueCreation` currently takes a concrete `RedisQueueCreation` (which is `public sealed`). This makes the wrapper untestable -- NSubstitute cannot mock sealed types.

`RedisQueueCreation` already implements `IQueueCreation` (defined in `Source/DotNetWorkQueue/IQueueCreation.cs`). The `IQueueCreation` interface exposes all the members `RedisJobQueueCreation` consumes:
- `IsDisposed` (via `IIsDisposed`)
- `Scope`
- `CreateQueue()`
- `RemoveQueue()`
- `Dispose()` (via `IDisposable`)

**Minimal refactor:** Change the `RedisJobQueueCreation` constructor parameter type from `RedisQueueCreation` to `IQueueCreation`. SimpleInjector's auto-resolve will continue to work since `RedisQueueCreation` is the only `IQueueCreation` implementation registered for this transport. Add a `Guard.NotNull` for the creation parameter (it currently has none).

This is a minimal additive change. No new types, no API surface added, no removal of `sealed`.

## Dependencies

None.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.Redis/Basic/RedisJobQueueCreation.cs" tdd="false">
  <action>
1. Read `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisJobQueueCreation.cs`
2. Change the field type:
   - `private readonly RedisQueueCreation _creation;` -> `private readonly IQueueCreation _creation;`
3. Change the constructor parameter type:
   - `public RedisJobQueueCreation(RedisQueueCreation creation)` -> `public RedisJobQueueCreation(IQueueCreation creation)`
4. Add `Guard.NotNull(() => creation, creation);` at the top of the constructor (matches the project convention -- check existing handlers for the exact `Guard.NotNull` usage pattern)
5. Add `using DotNetWorkQueue.Validation;` if not already present
6. Verify the existing methods (`CreateJobSchedulerQueue`, `RemoveQueue`, `IsDisposed`, `Scope`, `Dispose`) still compile -- they all use members defined on `IQueueCreation`
7. The `RedisQueueCreation` class itself does NOT change. It still implements `IQueueCreation`.
8. Check the DI registration in `Source/DotNetWorkQueue.Transport.Redis/RedisMessageQueueInit.cs` (or similar). `IQueueCreation` should already be registered with `RedisQueueCreation` as the implementation. Do not change registration unless build fails.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" -c Debug 2>&1 | tail -5 && dotnet build "Source/DotNetWorkQueue.sln" -c Debug 2>&1 | tail -5</verify>
  <done>RedisJobQueueCreation constructor accepts IQueueCreation. Guard.NotNull is applied. Redis project builds. Full solution builds. No DI registration changes needed (or, if needed, they were minimal and documented).</done>
</task>

## Notes

- This refactor is binary-breaking for any external code that constructs `RedisJobQueueCreation` directly with a concrete `RedisQueueCreation` -- but in practice this class is only resolved via SimpleInjector DI, so no external consumers are affected.
- After this plan lands, Plan 3.1 (RedisJobQueueCreationTests) can mock `IQueueCreation` directly and verify all delegation paths cleanly.
