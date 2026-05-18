# Research: Phase 6 — Negative-Path Coverage on Memory/Redis/LiteDb

## §1 Transport Init class locations (anchors for assembly scans)

| Transport | Init class | File path |
|---|---|---|
| Memory | `MemoryMessageQueueInit` | `Source/DotNetWorkQueue/Transport/Memory/Basic/MemoryMessageQueueInit.cs` (note: lives in CORE assembly, not its own — same as the existing convention) |
| Redis | `RedisQueueInit` | `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs` |
| LiteDb | `LiteDbMessageQueueInit` | `Source/DotNetWorkQueue.Transport.LiteDb/Basic/LiteDbMessageQueueInit.cs` |

## §2 Invariant verification (already true at the source level)

`grep -rln "IRelationalWorkerNotification\|IRelationalProducerQueue" Source/DotNetWorkQueue.Transport.Memory Source/DotNetWorkQueue.Transport.Redis Source/DotNetWorkQueue.Transport.LiteDb --include="*.cs"` returns **zero matches**. The 3 non-relational transport assemblies have no source-level reference to either relational interface. Phase 6's job is to lock this invariant with runtime tests + assembly-scan invariants.

Note: Memory transport's `MemoryMessageQueueInit` lives in the **core** `DotNetWorkQueue` assembly (not its own assembly). The grep + assembly scan must anchor on the appropriate type for each transport, not on assembly boundaries — Memory's "transport assembly" is shared with core.

## §3 Test project structure

| Project | Has `Basic/` subdir? | Existing related tests |
|---|---|---|
| `Source/DotNetWorkQueue.Transport.Memory.Tests/` | yes | Memory's own tests live in `DotNetWorkQueue.Transport.Memory.Tests` (not core) |
| `Source/DotNetWorkQueue.Transport.Redis.Tests/` | yes | RedisConnectionInfoTests, etc. |
| `Source/DotNetWorkQueue.Transport.LiteDb.Tests/` | yes | LiteDbConnectionInformationTests, etc. |

## §4 Test seam pattern (mirrors Phase 5's deleted `SqliteProducerDoesNotImplementRelationalTests.cs`)

Two assertions per test class:

**(a) Reflection-based type-system check** — assert `typeof(IRelationalWorkerNotification).IsAssignableFrom(typeof({TransportSpecificType}))` is `false`. For non-relational transports the cleanest target is the `WorkerNotification` registered in DI (which would resolve to the core `WorkerNotification` since none of these transports override the binding).

**(b) Assembly-scan invariant** — `typeof({InitType}).Assembly.GetTypes().Any(t => t.GetInterfaces().Any(i => i == typeof(IRelationalWorkerNotification)))` is `false`.

Phase 5's deleted test used the open-generic `IRelationalProducerQueue<>` shape. For Phase 6, the target is the closed `IRelationalWorkerNotification` (non-generic), so the assembly scan is simpler.

## §5 Architect handoff summary

- **3 test files, 1 plan, 1 wave**, S-sized phase.
- Each test class: 2 `[TestMethod]`s (reflection check + assembly scan).
- Plus a single Verification gate `grep -rln "IRelationalWorkerNotification" Source/DotNetWorkQueue.Transport.{Memory,Redis,LiteDb} --include="*.cs"` confirming zero matches.
- For Memory: the assembly anchor is the **core** assembly (via `MemoryMessageQueueInit`'s declaring assembly), so the assembly-scan check effectively covers the core assembly too — `IRelationalWorkerNotification` lives in `Transport.RelationalDatabase` so the core assembly should also not reference it. Confirm grep returns zero for Memory's scan target.
- No `Tx` token. No new production code. MSTest 3.x assertions.
