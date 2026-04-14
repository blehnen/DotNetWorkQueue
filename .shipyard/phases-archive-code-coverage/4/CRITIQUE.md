# Phase 4 Plan Critique

**Mode:** Plan Critique (feasibility stress test)
**Date:** 2026-04-12
**Phase:** phase-4-litedb-redis-job-handlers

## Source File Existence Check

All 10 source/reference files confirmed present:

- LiteDb: `SetJobLastKnownEventCommandHandler.cs`, `LiteDbSendJobToQueue.cs`, `GetJobIdQueryHandler.cs`, `RollbackMessageCommandHandler.cs`, `DashboardUpdateMessageBodyCommandHandler.cs` — all exist.
- Redis: `BaseLua.cs`, `RedisJobQueueCreation.cs`, `RedisQueueCreation.cs`, `DoesJobExistLua.cs`, `DashboardUpdateMessageBodyLua.cs` — all exist.
- Existing test file for Plan 2.5: `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/CommandHandler/DashboardUpdateMessageBodyCommandHandlerTests.cs` — exists (Plan 2.5 will augment, not create fresh).

## Per-Plan Findings

### PLAN-1.1 — BaseLua virtual seam — READY
- Confirmed `BaseLua.cs:87` currently reads `public RedisResult TryExecute(object parameters)`. Change to `public virtual` is a one-line, zero-risk edit.
- Independent; unblocks 3.2 and 3.3.

### PLAN-2.1 — SetJobLastKnownEventCommandHandler — READY
- Source file present. No cross-plan concerns.

### PLAN-2.2 — LiteDbSendJobToQueue — READY (InternalsVisibleTo already configured)
- **Critical concern RESOLVED:** `Source/DotNetWorkQueue.Transport.LiteDB/InternalsVisibleForTests.cs:21` already contains `[assembly: InternalsVisibleTo("DotNetWorkQueue.Transport.LiteDb.Tests")]`.
- `LiteDbSendJobToQueue` at line 32 is `internal class` but directly reachable from the test project — no AssemblyInfo.cs mutation needed.
- Plan's conditional "add InternalsVisibleTo if absent" branch will be skipped; builder should simply confirm and proceed.
- Recommend pruning `files_touched` entry for `Properties/AssemblyInfo.cs` since it is now known to be unnecessary.

### PLAN-2.3 — GetJobIdQueryHandler — READY
- Source file present. Standard query handler test pattern.

### PLAN-2.4 — RollbackMessageCommandHandler — READY
- Source file present.

### PLAN-2.5 — DashboardUpdateMessageBodyCommandHandler augmentation — READY
- Existing test file confirmed; plan correctly targets augmentation rather than creation.

### PLAN-3.1 — RedisJobQueueCreation wrapper — CAUTION (sealed blocker)
- **Critical concern CONFIRMED:** `RedisQueueCreation` is declared `public sealed class RedisQueueCreation : IQueueCreation` at `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueCreation.cs:30`. It cannot be subclassed, and NSubstitute cannot mock sealed types. Plan 3.1 strategy (a) "`Substitute.ForPartsOf<RedisQueueCreation>`" is therefore **impossible**.
- Constructor signature (5 concrete/abstract args): `(IConnectionInformation, IRedisConnection, RedisNames, ICreationScope, RedisBaseTransportOptions)`. Some are interfaces (mockable) but `RedisNames` and `RedisBaseTransportOptions` are concrete types that must be cheaply constructable for strategy (b) to work.
- `RedisJobQueueCreation` constructor (line 33) takes a single `RedisQueueCreation creation` parameter with **no null guard** (`_creation = creation;` directly). Therefore the plan's "`Constructor_NullCreation_Throws`" test will FAIL — there is nothing to assert.
- **Recommendation:** Plan 3.1 should be REVISED to either:
  1. Explicitly acknowledge the sealed blocker and scope the plan to only the delegation paths exercised via a real `RedisQueueCreation` built with substituted `IConnectionInformation` + `IRedisConnection` + real `RedisNames`/`ICreationScope`/`RedisBaseTransportOptions` — but only the property reads (`IsDisposed`, `Scope`) and `RemoveQueue` (which no-ops if no script was loaded) may be safe without a live Redis. `CreateJobSchedulerQueue` almost certainly hits the network.
  2. Drop the null-guard test or first add a null guard to the production wrapper (requires a new task in the plan).
  3. Accept full partial completion — constructor sanity test only, remaining members `[Ignore]`'d — and treat Redis job queue creation as integration-test-only coverage.
- Plan 3.1 should be marked partial-allowed and the task must be updated to state that strategy (a) is explicitly ruled out because `RedisQueueCreation` is `sealed`.

### PLAN-3.2 — DoesJobExistLua — READY (depends on 1.1)
- Dependency on Plan 1.1 is correctly declared in frontmatter (`dependencies: ["1.1"]`).
- Testable subclass pattern is sound because Plan 1.1 makes `TryExecute` virtual.

### PLAN-3.3 — DashboardUpdateMessageBodyLua — READY (assumed; confirm dependency)
- Not directly inspected but referenced as depending on Plan 1.1 seam; plan family is consistent with 3.2.

## Wave / Dependency / File-Conflict Review

- Wave 1 (1.1) → Wave 2 (2.1–2.5) / Wave 3 (3.1–3.3) ordering is correct.
- 3.2 and 3.3 depend on 1.1. 3.1 declares no dependency (correct).
- Each plan touches a distinct test file under a distinct test project path. No file conflicts detected.
- Only Wave 1 touches production code (`BaseLua.cs`), and only by adding `virtual` — no conflict with Wave 3 tests.

## Critical Call-Outs

1. **Plan 2.2 InternalsVisibleTo:** Already configured via `Source/DotNetWorkQueue.Transport.LiteDB/InternalsVisibleForTests.cs`. No action needed; plan can proceed on the primary path.
2. **Plan 3.1 RedisQueueCreation sealed:** Strategy (a) from the plan is impossible. Null-guard test as written will fail because the wrapper has no null guard. Plan should be revised to remove strategy (a), remove the null-guard test (or add the guard to production), and explicitly permit partial completion.

## Overall Verdict

**CAUTION** — 8 of 9 plans are READY. Plan 3.1 needs a small revision to remove the impossible strategy (a) and either drop the null-guard test or add a production null guard. Everything else, including the Plan 2.2 internal-access concern, is resolved by existing repo state.
