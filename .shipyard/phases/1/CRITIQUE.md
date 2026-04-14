# Plan Critique — Phase 1 (Feasibility Stress Test)

**Phase:** 1 — TaskScheduler Lock Contention Fix + Unit Tests
**Date:** 2026-04-14
**Type:** plan-review (Mode B — feasibility stress test)
**Target repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Companion:** see `VERIFICATION.md` for Mode A coverage results.

## Sibling-Repo Ground Truth (verified by grep/glob)

| Claim | Reality | Status |
|---|---|---|
| `Source/TaskSchedulerJobCountSync.cs` exists | `Source/TaskSchedulerJobCountSync.cs` present | CONFIRMED |
| `Source/TaskSchedulerMultiple.cs` exists | Present | CONFIRMED |
| `Source/ITaskSchedulerBus.cs` exists | Present | CONFIRMED |
| `ITaskSchedulerBus.Start()` returns `NetMQActor` | Line 31: `NetMQActor Start();` | CONFIRMED |
| `_lockSocket` field + 9 occurrences | `private readonly object _lockSocket = new object();` at line 39. `lock (_lockSocket)` at lines 72, 91, 110, 126, 134, 145, 177, 251. **8** `lock`-statement usages + **1** field decl = 9 total occurrences. | CONFIRMED (matches PLAN-1.2 Task 2 `<done>` note) |
| `_stopRequested`, `_running` fields | Lines 36–37: `private volatile bool _stopRequested; private volatile bool _running;` | CONFIRMED |
| `_actor` field + type | Line 44: `private NetMQActor _actor;` | CONFIRMED |
| `ProcessMessages` method | Line 175, body lines 175–233 (approx — grep shows body through ~233) | CONFIRMED |
| `TryReceiveFrameString(10ms)` in `ProcessMessages` | Line 182: `_actor.TryReceiveFrameString(TimeSpan.FromMilliseconds(10), Encoding.ASCII, out message)` | CONFIRMED |
| `while (!_stopRequested)` polling loop | Line 153 | CONFIRMED |
| `while (_running) Sleep(100)` hot-wait in Dispose | Line 264: `while (_running)` | CONFIRMED |
| `TaskSchedulerMultiple.Start()` wraps `_jobCount.Start()` in `Task.Run` | Lines 55–62: `public override void Start()` → `Task.Run(() => { _jobCount.Start(); });` | CONFIRMED (PLAN-1.3 Task 2 step 2 target is real) |
| `NextPort()` in existing test | `TaskSchedulerJobCountSyncTests.cs:15`: `_nextPort = 40000 + Random.Shared.Next(0, 10000);` — range **40000–49999** | CONFIRMED |
| `XunitLogger` test helper | `TaskSchedulerJobCountSyncTests.cs:154`: `private class XunitLogger : ILogger` — **non-generic**, private nested | CONFIRMED |
| `InternalsVisibleTo` to Tests assembly | Tests.csproj line 32: `InternalsVisibleTo Include="...Tests"` (confirmed via grep of main csproj) | CONFIRMED |
| `TreatWarningsAsErrors=true` | Main csproj line 8 | CONFIRMED |
| `NoWarn CS1591` | Main csproj line 21 | CONFIRMED |
| `NetMQ 4.0.2.2` package ref | Main csproj line 37: `PackageReference Include="NetMQ" Version="4.0.2.2"` | CONFIRMED |
| Tests project target framework | `<TargetFramework>net8.0</TargetFramework>` (single, not multi) | CONFIRMED (matches PLAN-2.1 preamble "net8.0 ONLY") |
| Main library target frameworks | `<TargetFrameworks>net10.0;net8.0</TargetFrameworks>` | CONFIRMED |

## Per-Plan Feasibility

### PLAN-1.1 — NetMQ API probe + SetCountMsg scaffolding

| Check | Verdict | Notes |
|---|---|---|
| File paths resolve (creates only) | GREEN | Both new files land under existing Tests project directory. |
| API probe approach sound | GREEN | `new NetMQQueue<int>()` + `ReceiveReady` + `NetMQPoller.RunAsync()` is the documented NetMQ 4.x pattern. If it compiles, downstream plans are unblocked. |
| TDD order viable | GREEN | Writing `SetCountMsgTests` first, observing CS0246, then adding the struct is textbook TDD and will work because `InternalsVisibleTo` is already wired. |
| Struct placement note | GREEN | Plan explicitly says "inside the same namespace block, outside the class body" — compiles cleanly. |
| License header guidance | GREEN | Plan matches RESEARCH 6.7 — existing test files have none. |
| Commit messages specified | GREEN | Both tasks have explicit commit message strings. |
| **Complexity** | GREEN | 2 files, 1 directory, 2 tasks. |

### PLAN-1.2 — Poller infrastructure refactor

| Check | Verdict | Notes |
|---|---|---|
| Target file exists | GREEN | `TaskSchedulerJobCountSync.cs` confirmed. |
| Line numbers cited in plan match reality | YELLOW | Plan references "line 44 next to `_actor`", "line 128 `_actor = _bus.Start()`", "line 140 `GetHostAddress` round-trip", "lines 145–150 initial broadcast", "lines 153–170 while loop", "lines 175–233 ProcessMessages", "lines 251–254 Dispose guard". Actual: `_actor` at line 44 ✓; `_actor = _bus.Start()` at line 128 ✓ (inside `lock(_lockSocket)` at 126); `GetHostAddress` at 136, `ReceiveFrameString` at 138, `_hostPort` parse at 139 (plan says ~140 ≈ correct); initial broadcast at 145–149 ✓; `while(!_stopRequested)` at 153 ✓; `ProcessMessages` starts at 175 ✓; Dispose `lock(_lockSocket) { _actor?.Dispose(); }` at lines 251–253 ✓. **Line numbers are accurate.** |
| All 3 tasks edit same file sequentially | GREEN | Same plan → sequential execution by design. |
| Intermediate-state thread-safety caveat documented | GREEN | Task 2 `CRITICAL NOTE` explicitly flags the risk and gives the escape hatch: "COLLAPSE Tasks 2 and 3 into a single commit". |
| `TryDequeue(out, TimeSpan)` fallback provided | GREEN | Task 3 step 3 has `while (e.Queue.Count > 0) { var m = e.Queue.Dequeue(); }` fallback. |
| Final `_lockSocket` removal verifiable | GREEN | Task 3 `<done>` requires `grep -n _lockSocket` → zero hits. |
| **Complexity** | YELLOW | 3 tasks all mutating the same 280-line file. High surgical density but all within one plan so there's no parallel-edit collision. Builder will feel the weight. |

### PLAN-1.3 — Async-friendly Start + Dispose cleanup

| Check | Verdict | Notes |
|---|---|---|
| File paths resolve | GREEN | Both `TaskSchedulerJobCountSync.cs` and `TaskSchedulerMultiple.cs` confirmed. |
| `TaskSchedulerMultiple.cs:55–63` wraps `Start()` in `Task.Run` | GREEN | Confirmed — grep returned exactly that shape at lines 55–62. |
| `_poller.Run()` on dedicated `Thread` approach | GREEN | Standard NetMQ pattern. `Thread.Join(TimeSpan.FromSeconds(5))` is the bounded hang-breaker. |
| Preserved beacon sleep | GREEN | Explicitly kept per CONTEXT-1.md decision #3. |
| `_log.LogError` API shape | YELLOW | Plan says "use whatever shape the file uses" — grep shows actual calls like `_log.LogError($"Failed to handle NetMCQ commands{...}{error}")` (string formatting, NOT `LogError(exception, message)`). Builder must match the **string-format** shape, not the structured-logging `(ex, msg)` shape shown in the plan sketch. **Minor — flag for builder awareness.** |
| Dispose ordering | GREEN | `Stop → Join → outbound.Dispose → actor.Dispose → poller.Dispose` is correct per RESEARCH.md 3.2 + 7.4. |
| **Complexity** | GREEN | 2 tasks, 2 files, same directory. |

### PLAN-2.1 — Concurrency + state + lifecycle tests

| Check | Verdict | Notes |
|---|---|---|
| All new test file paths land in existing Tests project | GREEN | 3 new .cs files under `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/`. |
| `NextPort()` pattern replicated | YELLOW → Risk (d) below | PLAN-2.1 Task 1 uses seed `41000 + Random.Shared.Next(0, 5000)` (range 41000–45999). Existing test uses `40000 + Random.Shared.Next(0, 10000)` (range 40000–49999). **Overlap: 41000–45999.** Collision possible under parallel test collections — but `[Collection("NetMQ")]` serializes NetMQ tests, so the collision window is between sequential `NextPort()` calls from *different static counters in different classes* — both static, both seeded randomly at class load. Realistically low-probability but non-zero. |
| `XunitLogger` generic vs non-generic | YELLOW | Plan sketches use `XunitLogger<TaskSchedulerBus>(_output)` and `XunitLogger<TaskSchedulerJobCountSync>(_output)` — generic. Existing class is **non-generic** `XunitLogger : ILogger`. Builder must either promote to generic (touching existing test file), duplicate as non-generic (one per new file), or create a new generic sibling helper. Plan says "builder's call" which is fair, but **flag explicitly**. |
| `[Collection("NetMQ")]` serialization | GREEN | Consistent with existing suite. |
| `Task.Run(() => sync.Start()) + await Task.Delay(...)` convention | GREEN | Matches RESEARCH 5.2. |
| Plain `Assert.*`, no FluentAssertions | GREEN | Matches RESEARCH 6.2. |
| Deadlock-detector timeout generosity | GREEN | 30 s detector on concurrency test, 10 s on lifecycle — plenty. |
| 5× loop in verify for concurrency test | GREEN | `for i in 1..5; do dotnet test ... || break; done` — syntactically valid shell. |
| Target framework net8.0 only | GREEN | Matches actual Tests.csproj. |
| **Complexity** | GREEN | 3 tasks, 3 new files, single directory. |

## Architect's Four Flagged Risks — Verdicts

### Risk (a): `NetMQQueue<T>.TryDequeue(out, TimeSpan)` on NetMQ 4.0.2.2

**Verdict: YELLOW (CAUTION)**

**Evidence:** I cannot execute `dotnet build` to verify, and I cannot grep into the NetMQ package sources from this session. The plan already acknowledges the risk and provides a working fallback (`while (e.Queue.Count > 0) { var m = e.Queue.Dequeue(); }`). PLAN-1.1 Task 1 is specifically the compile-time probe that will surface this before any production code is touched.

**Mitigation:** PLAN-1.1 Task 1 is the designed gate. If the probe fails, builder updates CONTEXT-1.md and pivots to the PairSocket fallback. The `TryDequeue` overload question is secondary — PLAN-1.2 Task 3 step 3 already has the `while (Count > 0)` fallback inline. **No action required from architect.**

### Risk (b): `XunitLogger` nested class — shared helper vs duplication

**Verdict: YELLOW (CAUTION)**

**Evidence:** Confirmed — `XunitLogger` is a `private class` nested inside `TaskSchedulerJobCountSyncTests` at line 154, and it is **non-generic**. PLAN-2.1 sketches code that instantiates `new XunitLogger<TaskSchedulerBus>(_output)` (generic) — this **will not compile** as-written because the type is non-generic.

**Three viable paths for builder:**
1. Promote the existing private nested class to an internal top-level generic `XunitLogger<T> : ILogger<T>` in a new helper file (e.g., `TestHelpers/XunitLogger.cs`) — touches existing test file imports but gives cleanest surface.
2. Duplicate the non-generic `XunitLogger` as a private nested class in each of the 3 new test files.
3. Create an internal non-generic `XunitLogger` at top-level and have new tests pass `new XunitLogger(_output)` instead of `new XunitLogger<T>(_output)`.

**Recommendation to architect:** Update PLAN-2.1 to explicitly say "use `new XunitLogger(_output)` (non-generic) — the existing class is private nested non-generic at `TaskSchedulerJobCountSyncTests.cs:154`. Promote to an internal top-level class in a new helper file before wave 4 starts, so all 3 new test classes can reference it." This is a **one-sentence edit** and it unblocks ambiguity for the builder.

### Risk (c): `-p:CI=true TreatWarningsAsErrors` cleanliness at PLAN-1.2 Task 3 and PLAN-2.1 Task 3

**Verdict: GREEN**

**Evidence:** Main csproj confirms `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (line 8) and `<NoWarn>CS1591</NoWarn>` (line 21). The plans correctly note that XML doc comments on `SetCountMsg` won't trip warnings because CS1591 is suppressed. All `<done>` gates require "0 errors, 0 warnings" and the plan explicitly says `dotnet build -c Release -p:CI=true` runs at PLAN-1.2 Task 3, PLAN-1.3 Task 2, and PLAN-2.1 Task 3. Each builder commit is gated on clean Release CI build. **No action required.**

### Risk (d): `NextPort()` base seed collision risk (40000/41000/42000 across tests)

**Verdict: YELLOW (CAUTION)**

**Evidence:**
- Existing: `40000 + Random.Shared.Next(0, 10000)` → **range 40000–49999**
- PLAN-2.1 Task 1 Concurrency: `41000 + Random.Shared.Next(0, 5000)` → **range 41000–45999**
- PLAN-2.1 Task 3 Lifecycle: `42000 + Random.Shared.Next(0, 5000)` → **range 42000–46999**

All three ranges overlap. Because `NextPort()` is a **per-class static `Interlocked.Increment`**, each class's counter is independent, so two classes seeded inside the overlap can legitimately produce the same port number on different calls. Mitigated partially by `[Collection("NetMQ")]` serializing NetMQ tests — **but** the mitigation only works if `[Collection("NetMQ")]` is applied identically across all four test classes and xUnit honors it across the whole assembly. Confirmed: existing test and all three PLAN-2.1 sketches use `[Collection("NetMQ")]`.

**Residual risk:** Collection serialization serializes *test execution*, not *counter state*. Two sequential tests from different classes can legitimately get overlapping ports if one ends and the next starts in the same millisecond — but since each test disposes its bus before the next starts (within `[Collection("NetMQ")]`), the NetMQ port will be released before reuse. **Probability of live-collision: very low. Probability of TIME_WAIT socket residue on Linux / Docker: moderate.**

**Recommendation to architect:** Change the three PLAN-2.1 seeds to **disjoint ranges**:
- Existing `TaskSchedulerJobCountSyncTests`: keep `40000 + Random.Shared.Next(0, 3000)` → 40000–42999
- PLAN-2.1 Concurrency: `43000 + Random.Shared.Next(0, 3000)` → 43000–45999
- PLAN-2.1 State: `46000 + Random.Shared.Next(0, 2000)` → 46000–47999
- PLAN-2.1 Lifecycle: `48000 + Random.Shared.Next(0, 2000)` → 48000–49999

This change is mechanical and eliminates the overlap entirely. Touching the existing `TaskSchedulerJobCountSyncTests.cs` from 40000–49999 down to 40000–42999 is a trivial one-line edit the builder can make as a preliminary step in PLAN-2.1 Task 1. Alternatively, keep it simpler by just moving PLAN-2.1 seeds **above** the existing range: `50000 + ...`, `55000 + ...`, `60000 + ...` — zero change to existing test, zero risk of overlap.

## Complexity Flags

- **No plan touches >10 files.** Max is PLAN-1.3 with 2 files.
- **No plan crosses >3 directories.** All work lives under `Source/` and `Source/...Tests/`.
- **PLAN-1.2 has 3 tasks all editing the same production file** — not a complexity flag (it's a single plan, sequential execution), but worth noting for the builder: this is the highest-density surgery in the phase.

## Hidden Dependencies / File Collisions

- Wave 1 (PLAN-1.1) creates `TaskSchedulerJobCountSync.cs` additions (struct only) + new test files.
- Wave 2 (PLAN-1.2) edits `TaskSchedulerJobCountSync.cs` heavily.
- Wave 3 (PLAN-1.3) edits `TaskSchedulerJobCountSync.cs` again + `TaskSchedulerMultiple.cs`.
- Wave 4 (PLAN-2.1) creates new test files only.

**No parallel waves → no cross-plan file collision.** Sequential editing of `TaskSchedulerJobCountSync.cs` across waves 1→2→3 is safe because each wave commits before the next begins.

## Verify Command Syntax Check

Every `<verify>` block reviewed:

| Plan.Task | Command | Syntax | Project path valid |
|---|---|---|---|
| 1.1.1 | `dotnet test "...Tests.csproj" --filter "FullyQualifiedName~NetMqQueueApiProbeTests.NetMqQueue_WithPoller_ReceivesEnqueuedItem"` | Valid | Yes |
| 1.1.2 | `dotnet test ... --filter "FullyQualifiedName~SetCountMsgTests"` | Valid | Yes |
| 1.2.1–3 | `dotnet build "...sln" -c Debug && dotnet test "...Tests.csproj"` | Valid | **See caveat below** |
| 1.3.1 | Same as 1.2 | Valid | Same caveat |
| 1.3.2 | Adds `-c Release -p:CI=true` branch | Valid | Same caveat |
| 2.1.1 | `for i in 1 2 3 4 5; do dotnet test ... --filter "FullyQualifiedName~TaskSchedulerJobCountSyncConcurrencyTests" || break; done` | Valid bash | Yes |
| 2.1.2 | `dotnet test ... --filter "FullyQualifiedName~TaskSchedulerJobCountSyncStateTests"` | Valid | Yes |
| 2.1.3 | Lifecycle filter + full suite + Release CI build | Valid | Yes |

**Caveat:** The plans reference `"Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln"`. I did NOT find a `.sln` file in `Source/` during my glob (the directory-listing command earlier was truncated before scanning for `.sln`). Builder should verify existence at the start of wave 2. If the solution file is at a different path (e.g., repo root, or `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln` at a different case), the `dotnet build -c Debug` and `dotnet build -c Release -p:CI=true` commands will fail. **Fallback:** `dotnet build Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj` (the main .csproj, which IS confirmed present) works the same.

## Top Issues

1. **[MEDIUM] Initial broadcast does not go through queue path.** PROJECT.md Workstream 1 bullet explicitly requires "`Start` publishes the initial broadcast through the queue path as well." PLAN-1.2/1.3 send it directly via `_actor.SendMoreFrame(...).SendFrame(_hostAddress)` on the caller thread. See VERIFICATION.md Gap A/B. Decide: widen the struct to an `OutboundMsg` union, or amend CONTEXT-1.md to document the deviation.

2. **[LOW-MEDIUM] `XunitLogger` generic/non-generic mismatch.** PLAN-2.1 code sketches use `XunitLogger<T>` but the existing nested class is non-generic. Builder will hit a CS0305 compile error if they copy-paste the sketch. One-line architect amendment recommended.

3. **[LOW] `NextPort()` seed overlap.** Three new test classes seed into the same 41000–46999 range that overlaps with the existing class's 40000–49999 range. Serialization via `[Collection("NetMQ")]` mitigates live collision but TIME_WAIT residue on Linux/Docker is possible. One-line architect amendment recommended: disjoint the seeds.

4. **[LOW] PLAN-1.3 `_log.LogError` shape.** Plan sketch uses `_log.LogError(ex, "msg")` (structured); actual code uses `_log.LogError($"...{error}")` (interpolated string, no exception argument). Builder should match existing shape.

5. **[LOW] `.sln` path in verify commands is unverified.** Use `.csproj` fallback if the solution file path is wrong.

## Verdict

**CAUTION** — all plans are feasible as written, file paths and API surface check out against the actual sibling repo, the four-wave dependency chain is clean, and no complexity or file-collision flags are raised. Issues #1 (initial broadcast semantics) and #2 (XunitLogger generic) are the only items that could cause a builder to stop-and-ask; both are low-cost architect amendments. Issue #3 (port seed overlap) is a latent flake risk worth fixing preemptively. Builder may proceed with awareness of these four items and the ability to route back to the architect for #1 if the scope widening is preferred over the deviation note.

**READY / CAUTION / REVISE → CAUTION**
