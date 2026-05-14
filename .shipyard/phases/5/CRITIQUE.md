# Phase 5 Plan Critique (Feasibility Stress Test)

## Overall Verdict: READY

Plans align with the live codebase. All four test-project directories exist with their `Basic/` subfolders. Init class names, namespaces, ProjectReference paths, and Phase 2 type references all verified against source. One minor informational note on net8 wording in CONTEXT-5; no blocker.

## Per-Plan Findings

### PLAN-1.1 (Memory + LiteDb)

- **File paths exist.**
  - `Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj` — present.
  - `Source/DotNetWorkQueue.Transport.Memory.Tests/Basic/` — present (existing tests inside).
  - `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/` — present.
- **Memory.Tests.csproj current state matches plan assumption.** Project-ref `<ItemGroup>` contains exactly 2 entries (`Transport.Memory` + `DotNetWorkQueue`). Plan's "add a third entry" is accurate. ProjectReference syntax in plan matches existing style verbatim (`Include="..\DotNetWorkQueue.Transport.RelationalDatabase\DotNetWorkQueue.Transport.RelationalDatabase.csproj"`).
- **LiteDb.Tests.csproj already has the reference** (line 9 of csproj — `Transport.RelationalDatabase` present). No csproj edit required, as plan states.
- **Init class names verified.**
  - `MemoryDashboardInit` confirmed at `Source/DotNetWorkQueue.Transport.Memory/Basic/MemoryDashboardInit.cs:36` — inherits `MemoryMessageQueueInit`. Plan's scoping rationale (use the Memory NuGet binary, not the core DLL) is sound.
  - `LiteDbMessageQueueInit` confirmed at `Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbMessageQueueInit.cs:43`. Note directory uses `LiteDB` (caps), namespace + class use `LiteDb` (lower-b) — plan calls this out correctly.
- **Verbatim test source compiles** against the namespaces / types it imports: `DotNetWorkQueue.Queue.ProducerQueue<>`, `DotNetWorkQueue.Transport.RelationalDatabase.IRelationalProducerQueue<>`, `DotNetWorkQueue.Transport.Memory.Basic.MemoryDashboardInit`, `DotNetWorkQueue.Transport.LiteDb.Basic.LiteDbMessageQueueInit`. All four exist.
- **Verification commands are runnable** — syntactically valid `dotnet build`/`dotnet test --filter "FullyQualifiedName~..."` invocations.

### PLAN-1.2 (Redis + SQLite)

- **File paths exist.**
  - `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/` — present.
  - `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/` — present.
- **Redis.Tests.csproj current state matches plan assumption.** Two project refs (`Transport.Redis` + `DotNetWorkQueue`). Plan's third-entry addition is correct.
- **SQLite.Tests.csproj already references `Transport.RelationalDatabase`** (line 8 of csproj) — no csproj edit needed, as plan states.
- **Init class names verified.**
  - `RedisQueueInit` confirmed at `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs:48`.
  - `SqLiteMessageQueueInit` confirmed at `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueInit.cs:33` — exact camelCase `SqLite` (capital L) as plan documents. Plan's risk-callout (line 34) flags this correctly.
- **Decision-4 imports verified.** `RelationalProducerQueue<>` namespace is `DotNetWorkQueue.Transport.RelationalDatabase.Basic` (confirmed in source line 28). Plan imports both `...RelationalDatabase` and `...RelationalDatabase.Basic` — correct.
- **Verification commands runnable** — syntax valid.

## Init Class Name Verification

| Transport | Class (per plan) | Source file confirms |
|-----------|------------------|----------------------|
| Memory | `MemoryDashboardInit` | `Source/DotNetWorkQueue.Transport.Memory/Basic/MemoryDashboardInit.cs:36` |
| Redis | `RedisQueueInit` | `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs:48` |
| LiteDb | `LiteDbMessageQueueInit` | `Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbMessageQueueInit.cs:43` |
| SQLite | `SqLiteMessageQueueInit` | `Source/DotNetWorkQueue.Transport.SQLite/Basic/SqLiteMessageQueueInit.cs:33` |

All four match the live source.

## Project-Reference Syntax

Existing references in LiteDb.Tests.csproj and SQLite.Tests.csproj use the canonical `..\<ProjectDir>\<Project>.csproj` form with backslash separators — both PLAN-1.1 (Memory) and PLAN-1.2 (Redis) reproduce this style verbatim. No format drift.

## Forward References / Cross-Plan Conflicts

- Wave 1: PLAN-1.1 and PLAN-1.2 touch disjoint test projects (Memory + LiteDb vs. Redis + SQLite). Zero file overlap. Safe to run parallel.
- No cross-plan dependencies.

## Complexity

- PLAN-1.1: 3 files touched (1 csproj + 2 .cs), 2 directories.
- PLAN-1.2: 3 files touched (1 csproj + 2 .cs), 2 directories.
- Both well under Shipyard ≤10-files / ≤3-directories thresholds.

## Minor Observations (non-blocking)

1. **CONTEXT-5 line 97 mentions net8.0 build cleanliness**, but all 4 affected test csproj files target `<TargetFrameworks>net10.0</TargetFrameworks>` only. Plans' acceptance criteria correctly scope to net10.0. No action needed — context wording is stale relative to the test-project TFMs, but plans aren't impacted.
2. **Memory.Tests.csproj has a stray `<PackageReference Include="xunit.runner.visualstudio" />`** (line 21). Unrelated to Phase 5 — flagging as a pre-existing oddity for the docs/audit phase, not a Phase 5 issue.
3. **LiteDb.Tests directory has `Basic/`**, but its `LiteDbConnectionInformationTests.cs` lives at the project root rather than under `Basic/`. The plan's placement of the new file at `Basic/LiteDbProducerDoesNotImplementRelationalTests.cs` is consistent with `Basic/CommandHandler/...` patterns already inside the project. No conflict.

## Recommendation

Proceed to build. Both plans are concrete, file paths/types verified, ProjectReference syntax matches repo conventions, and Decision-4 namespace imports are correct. Builder can execute Wave 1 in parallel.
