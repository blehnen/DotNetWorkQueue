# CONTEXT-6: Phase 6 Decisions (Negative-Path Coverage)

Captured 2026-05-18 — minimal CONTEXT for a mechanical phase.

## Scope (per ROADMAP.md lines 138-154)

3 negative-path unit tests confirming non-relational transports do NOT implement `IRelationalWorkerNotification`:
- `Transport.Memory.Tests`
- `Transport.Redis.Tests`
- `Transport.LiteDb.Tests`

Plus grep guards confirming the source of those 3 transport assemblies doesn't reference `IRelationalWorkerNotification`.

## Decisions

### 1. Test file location + naming

One test file per transport, following the Phase 3/4 sibling pattern:
- `Source/DotNetWorkQueue.Transport.Memory.Tests/Basic/MemoryDoesNotImplementIRelationalWorkerNotificationTests.cs`
- `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/RedisDoesNotImplementIRelationalWorkerNotificationTests.cs`
- `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/LiteDbDoesNotImplementIRelationalWorkerNotificationTests.cs`

If a `/Basic/` subdirectory doesn't exist for a given transport, place at the project root.

### 2. Test pattern

Two assertions per test file:

**(a) Container-resolution smoke test:**
Build a `QueueContainer<{Transport}MessageQueueInit>`, resolve `IWorkerNotification`, assert `is IRelationalWorkerNotification` is `false`. Pattern mirrors Phase 3/4 PLAN-2.2 smoke tests but inverted (asserts FALSE, not TRUE).

**(b) Assembly-scan invariant:**
Reflect over the transport assembly (anchored on its `MessageQueueInit` class); assert no type implements `IRelationalProducerQueue<>` (NOTE — looking at the deleted Phase 5 `SqliteProducerDoesNotImplementRelationalTests.cs` test, that scan checked `IRelationalProducerQueue<>` not `IRelationalWorkerNotification`). For Phase 6, we want BOTH:
- No type implements `IRelationalWorkerNotification`
- (Optionally) no type implements `IRelationalProducerQueue<>` either (would close the negative-path coverage for outbox too — confirm with assembly scan)

The Phase-6-specific focus per ROADMAP is `IRelationalWorkerNotification`. The outbox `IRelationalProducerQueue<>` was already covered by the outbox milestone's negative-path coverage for Memory/Redis/LiteDb — confirm and reuse those tests rather than duplicating.

### 3. Grep guards in test files' Verification sections

Each test plan includes a grep guard:
```bash
grep -rln "IRelationalWorkerNotification" Source/DotNetWorkQueue.Transport.{Memory,Redis,LiteDb} --include="*.cs"
```
Expected: zero matches in production source (test files don't count since they reference the type to assert NEGATIVE).

## Non-decisions
- All three tests fit in a single plan (PLAN-1.1 with 3 tasks — one per transport).
- No researcher needed; the deleted `SqliteProducerDoesNotImplementRelationalTests.cs` from Phase 5 is the reference template.
- No build risk; pure additive test code.

## Scope reminders for plan authors
- Tests do NOT need real Memory/Redis/LiteDb infrastructure — `QueueContainer<...>` build + DI resolution is sufficient (matches Phase 3/4/5 smoke pattern).
- Use MSTest 3.x assertions, NSubstitute if any mocking needed (probably none).
- LGPL header byte-identical to existing test files in each project.
- No `Tx` abbreviation.
