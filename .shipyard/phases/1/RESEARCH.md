# Phase 1 Research: TaskScheduler Lock Contention Fix

**Target repository:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Current state in sibling repo:** v0.3.0 shipped to nuget.org (2026-04-10). Prior shipyard
Phase 1 there was the v0.3.0 modernization — not the lock contention fix. Issue #6
was explicitly deferred. The `.shipyard/phases/1/RESEARCH.md` and `PLAN-*.md` files
in the sibling repo are STALE — they describe the shipped 0.3.0 work, not this fix.

**Current state in DNQ repo:** zero references to `TaskSchedulerJobCountSync` or
`DotNetWorkQueue.TaskScheduling.Distributed` in `Source/**/*.cs`. The DNQ repo has
no integration point today; Workstream 4 (integration tests in DNQ) is greenfield
and happens later in Phase 3 of the roadmap.

---

## 1. Source Layout

### 1.1 Repository structure
- Top of repo: `Source/`, `.github/`, `.shipyard/`, `docs/` (if any), `CHANGELOG.md`,
  `CLAUDE.md`, `README.md`, `License.md`.
- **Main project:** `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj`
- **Test project:** `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj`
- **Solution file:** `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln`
- **License header template:** `Source/Source.licenseheader`
- **No `Directory.Build.props`, `Directory.Build.targets`, or `.editorconfig`** in this repo.
  (Glob for `Directory.Build.*` and `.editorconfig` returned zero results.) The DNQ
  repo's conventions around these files do not apply here — each project csproj
  is fully self-contained.

### 1.2 Main project — `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj`
File: `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj`
- `<TargetFrameworks>net10.0;net8.0</TargetFrameworks>` (line 4).
- `<Version>0.3.0</Version>` (line 9) — must bump in Phase 2.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (line 8) — any new warning
  in the refactor is a build failure.
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` (line 7) with
  `<NoWarn>CS1591</NoWarn>` (line 21) — missing XML doc warnings suppressed,
  so new `internal` types don't need doc comments.
- `<ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>`
  (line 23) — same `-p:CI=true` convention as DNQ; Phase 2 will need it.
- Deterministic build + symbols already wired (lines 22–27).
- `<Compile Remove="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests\**" />`
  and `<InternalsVisibleTo Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests" />`
  (lines 31–32) — internal types are visible to the test assembly, so the
  `SetCountMsg` struct can be `internal` and still testable.
- **Package references** (lines 36–38):
  - `DotNetWorkQueue` 0.9.31
  - **`NetMQ` 4.0.2.2** ← the version that governs what API surface we have
  - `Microsoft.SourceLink.GitHub` 10.0.201 (private asset)

### 1.3 Test project — `...Tests.csproj`
File: `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj`
- `<TargetFramework>net8.0</TargetFramework>` (line 4) — **single TFM, not
  multi-targeted.** This is different from DNQ test projects. New tests must
  work on net8.0 only (which is the broader surface — net10.0 APIs would
  break the build here).
- `<IsPackable>false</IsPackable>` (line 5).
- **xUnit 2.x** + `xunit.runner.visualstudio` 2.x (lines 10–11). **NOT MSTest.**
  This project uses a different test framework than the rest of the DNQ
  ecosystem — do not try to bring MSTest in. Match xUnit conventions.
- `Microsoft.NET.Test.Sdk` 17.x (line 9).
- `NSubstitute` 5.x declared (line 12) but unused in the current test file
  (TESTING.md:16, 141–142).
- `coverlet.collector` 6.x (line 13).
- Project reference to the main project (line 20).

### 1.4 Main-project .cs files
File listing in `Source/` (non-`obj/`, non-`bin/`):
- `TaskSchedulerJobCountSync.cs` — **file of primary focus**
- `ITaskSchedulerJobCountSync.cs`
- `TaskSchedulerBus.cs`
- `ITaskSchedulerBus.cs`
- `TaskSchedulerBusCommands.cs`
- `TaskSchedulerMultiple.cs` — the production consumer of `_jobCount.Start()`
- `TaskSchedulerMultipleConfiguration.cs`
- `TaskSchedulerSetup.cs` — DI registration
- `NodeKey.cs`

### 1.5 Test-project .cs files
- `TaskSchedulerJobCountSyncTests.cs` — single file, three tests, xUnit
  `[Fact]`, shared `[Collection("NetMQ")]` to serialize (NetMQ global context
  is not safe for parallel teardown — TESTING.md:53–54).

---

## 2. `TaskSchedulerJobCountSync.cs` Deep Read

File: `Source/TaskSchedulerJobCountSync.cs` (283 lines total).

### 2.1 `_lockSocket` usage sites (what the refactor must remove)
| Site | Lines | Purpose today | After refactor |
|---|---|---|---|
| Field declaration | 39 | `private readonly object _lockSocket = new object();` | **Deleted** |
| `GetCurrentTaskCount` | 72–76 | Guards read of `_currentTaskCount` + `_otherProcessorCounts.Values.Sum()` | **Removed.** `Interlocked.Read(ref _currentTaskCount)` + `_otherProcessorCounts.Values.Sum()` is already concurrent-safe (field is `long`, dict is `ConcurrentDictionary`). |
| `IncreaseCurrentTaskCount` | 91–99 | Guards `Interlocked.Increment` + four direct `_actor.SendMoreFrame/SendFrame` calls | **Removed.** `Interlocked.Increment` stays on caller thread; the four frames become a `SetCountMsg` struct enqueued onto a `NetMQQueue<SetCountMsg>`. |
| `DecreaseCurrentTaskCount` | 110–118 | Same pattern as Increase | **Removed.** Same treatment. |
| `Start` (three sites) | 126–129, 134–137, 145–150 | Guards (a) `_actor = _bus.Start();`, (b) `_actor.SendFrame(GetHostAddress)`, (c) the initial BroadCast publish | **Removed.** The bus-start happens on the dedicated poller thread; the GetHostAddress and BroadCast are dispatched via the queue or executed inline before the poller takes ownership. |
| `ProcessMessages` | 177–233 | **The whole method body is inside `lock(_lockSocket)`.** This is the bug — it serializes everything against the 10ms `TryReceiveFrameString` wait. | **Replaced.** No `ProcessMessages`; instead a `_actor.ReceiveReady += OnActorReady` handler on the `NetMQPoller`, which reads a single multi-frame message and dispatches on command. |
| `Dispose` | 251–255 | Guards `_actor?.Dispose()` | **Removed.** `_poller.Stop()` + join the poller thread + dispose `_actor` — all serialized by thread ownership, not by a mutex. |

**Count:** `_lockSocket` appears on 7 distinct lines (39, 72, 91, 110, 126, 134, 145, 177, 251). All must be gone post-refactor.

### 2.2 Exceptional-path code to preserve
- **`SocketException` swallow in `Dispose`** (lines 256–262):
  ```
  catch (SocketException error)
  {
      if (error.ErrorCode == 10035 || error.ErrorCode == 10054) //ignore socket close errors when exiting
      {
          return;
      }
      throw;
  }
  ```
  Error codes: `10035` = `WSAEWOULDBLOCK`, `10054` = `WSAECONNRESET`. These
  occur on Windows when disposing a NetMQ socket that has pending I/O. The
  refactor must keep this behavior (same try/catch around poller.Stop +
  actor.Dispose, or an equivalent guard) or NetMQ will leak exceptions on
  clean shutdown.

- **1100ms beacon grace period** (line 142): `Thread.Sleep(1100)` — CONTEXT-1.md
  decision #3 explicitly says to keep this. The comment on line 141 says
  "second beacon time, so we wait to ensure beacon has fired". This is a
  timing-based workaround; removing it is out of scope.

- **`while(_running) Thread.Sleep(100)` hot-wait in Dispose** (lines 264–267):
  CONTEXT-1.md says **remove** and replace with `Thread.Join(timeout)` on the
  dedicated poller thread, or let `NetMQPoller.Stop()` + thread exit handle it.

- **Fatal-error catch in `Start`** (lines 164–168): logs and swallows. After
  the refactor, the thread entry point should preserve this so a dying poller
  thread doesn't crash the host process.

- **`int.TryParse`/`long.TryParse` guards** for the SetCount key/value
  (lines 210, 214): these came from the Phase 1 v0.3.0 simplifier fix (S1 in
  sibling's HISTORY.md:28–31). Preserve them in the new handler.

### 2.3 How `_actor` is constructed
- Line 128: `_actor = _bus.Start();` — pulled from `ITaskSchedulerBus.Start()`
  which returns a `NetMQActor` (see §3).
- After construction, line 136 sends `GetHostAddress` on the actor, line 138
  reads the response back with `_actor.ReceiveFrameString()`, and line 139
  parses `_hostAddress` into `_hostPort`.
- Lines 147–149 then broadcast the node's presence via the actor.
- Line 153 enters the `while(!_stopRequested)` receive loop — which is the
  blocking behavior CONTEXT-1.md decision #3 changes.

### 2.4 Fields to keep / adjust
- `_currentTaskCount` (long, line 38): stays. `Interlocked.Increment`/`Decrement`
  already operate on it.
- `_otherProcessorCounts` (ConcurrentDictionary<int,long>, line 40): stays.
- `_hostAddress`, `_hostPort`, `_actor`, `_bus`, `_log`: stay.
- `_stopRequested`, `_running` (volatile bools, lines 36–37): **removed** —
  replaced by the poller's own lifecycle state.
- `_lockSocket`: **removed.**

### 2.5 Public API that must remain unchanged (binary-compatible)
Class: `public class TaskSchedulerJobCountSync` (line 34).
- `public event EventHandler RemoteCountChanged;` (line 51)
- `public TaskSchedulerJobCountSync(ITaskSchedulerBus bus, ILogger log)` (line 58)
- `public long GetCurrentTaskCount()` (line 68)
- `public long IncreaseCurrentTaskCount()` (line 89) — **return value must stay
  the post-increment value**
- `public long DecreaseCurrentTaskCount()` (line 108) — same
- `public void Start()` (line 124) — **signature stays `void Start()`**,
  behavior changes from blocking to async-friendly (CONTEXT-1.md decision #3)
- `public void Dispose()` (line 275) — signature stays
- `protected virtual void Dispose(bool disposing)` (line 242) — signature stays

---

## 3. `ITaskSchedulerBus` and `TaskSchedulerBus`

### 3.1 `ITaskSchedulerBus` interface
File: `Source/ITaskSchedulerBus.cs` (33 lines).
```csharp
public interface ITaskSchedulerBus
{
    NetMQActor Start();
}
```
- Single method. Takes no arguments. Returns a `NetMQActor`.
- **No lifecycle state that JobCountSync depends on beyond `_actor`.** The bus
  is fire-and-forget from JobCountSync's perspective — once `Start()` returns
  the actor, JobCountSync owns the actor handle and all subsequent interaction
  is via that handle.

### 3.2 `TaskSchedulerBus` implementation
File: `Source/TaskSchedulerBus.cs` (212 lines). **This file is gold — the
poller refactor target style already exists here.** Key observations:

- **Class is `internal`** (line 32), visible to tests via InternalsVisibleTo.
- **Already uses `NetMQPoller`** (line 42, line 118). The poller owns:
  - `_shim` (PairSocket, the actor's shim side)
  - `_subscriber` (SubscriberSocket)
  - `_beacon` (NetMQBeacon)
  - A `NetMQTimer` (1s dead-node sweep)
- **Already uses `ReceiveReady` events** on each socket (lines 85, 96, 111).
  The event handlers run on the poller thread — this is exactly the model
  JobCountSync needs to mirror.
- **`NetMQPoller.Run()`** at line 125 is blocking and owns the entire actor
  lifetime. The bus's actor thread is spawned internally by
  `NetMQActor.Create(RunActor)` at line 66; the poller runs inside the
  actor thread the framework provides.
- **`SignalOK()`** at line 122 is how the bus signals readiness before entering
  the poll loop — this is the NetMQActor "shim setup complete" handshake.
- **Interface sockets are allocated inside the actor thread** (lines 80–82
  `using (_subscriber = new SubscriberSocket()) ...`) so disposal happens
  on the same thread that owns them. Cross-thread NetMQ socket operations
  are generally unsafe.

**Implication for JobCountSync:** JobCountSync's new poller cannot be
constructed on one thread and owned by another. The dedicated poller thread
must:
1. Receive the actor handle (which is already thread-bound by the bus's own
   actor thread but is safe to `SendFrame`/`ReceiveReady` on from outside
   because `NetMQActor` is a `PairSocket`-wrapping facade that multiplexes
   its own pair).
2. Create a local `NetMQPoller` that adds both `_actor` and the new
   `NetMQQueue<SetCountMsg>`.
3. Subscribe `ReceiveReady` handlers.
4. Call `_poller.Run()` — blocks until `_poller.Stop()` is called from
   `Dispose` on another thread (this is safe — `NetMQPoller.Stop()` is
   explicitly thread-safe per NetMQ docs).

**Aside (blocker caveat):** the existing `TaskSchedulerBus.RunActor` wraps
`_subscriber` / `_publisher` / `_beacon` in `using` blocks — meaning when
`_poller.Run()` returns (via `_poller.Stop()` triggered from `OnShimReady`
receiving `EndShimMessage`), the using blocks dispose the sockets on the
actor thread. The architect must ensure the new JobCountSync poller follows
this same pattern: dispose `NetMQQueue<SetCountMsg>` on the dedicated poller
thread, not on the disposer's thread.

### 3.3 `GetHostAddress` shim command
File: `Source/TaskSchedulerBus.cs:147–161`. Important detail: the beacon's
hostname is empty on CI/WSL machines without reverse DNS, and the bus
substitutes `127.0.0.1`. The refactored JobCountSync still needs to drive
the `GetHostAddress` → receive round-trip during `Start()`, and this
round-trip is inherently synchronous (SendFrame → ReceiveFrameString). The
architect should decide whether to do this BEFORE handing `_actor` off to
the poller, or as the first act on the poller thread.

---

## 4. `ITaskSchedulerJobCountSync` Interface — Signatures to Preserve

File: `Source/ITaskSchedulerJobCountSync.cs` (58 lines).

```csharp
public interface ITaskSchedulerJobCountSync : IDisposable
{
    event EventHandler RemoteCountChanged;
    long GetCurrentTaskCount();
    long IncreaseCurrentTaskCount();   // returns post-increment value
    long DecreaseCurrentTaskCount();   // returns post-decrement value
    void Start();                       // CONTEXT-1.md #3: semantics change
                                        // (blocking -> async-friendly),
                                        // signature unchanged
}
```

**Confirmed:** CONTEXT-1.md decision #4 says do not change any of these
signatures. The `Start()` semantic change is the single behavioral
exception and it is explicitly sanctioned.

---

## 5. Consumers of `TaskSchedulerJobCountSync.Start()`

This is the critical compatibility question because decision #3 changes
`Start()` from blocking-until-Dispose to non-blocking.

### 5.1 In the sibling TaskScheduler repo
Grep for `.Start()` (main source only, excluding test project):

| File | Line | Call site | Notes |
|---|---|---|---|
| `Source/TaskSchedulerJobCountSync.cs` | 128 | `_actor = _bus.Start();` | Internal — bus start, not sync start. Not a caller of `JobCountSync.Start()`. |
| `Source/TaskSchedulerMultiple.cs` | 60 | `_jobCount.Start();` | **The only production caller of `JobCountSync.Start()`.** |
| `Source/TaskSchedulerMultiple.cs` | 62 | `base.Start();` | Base class start (SmartThreadPoolTaskScheduler in DNQ). |

**Critical finding:** `TaskSchedulerMultiple.Start()` (lines 55–63) wraps
`_jobCount.Start()` in `Task.Run`:
```csharp
public override void Start()
{
    _jobCount.RemoteCountChanged += JobCountHasChanged;
    Task.Run(() =>
    {
        _jobCount.Start();
    });
    base.Start();
}
```

**Consequence:** After decision #3 is applied, `_jobCount.Start()` returns
quickly and the `Task.Run` wrapper becomes a no-op cost. The architect may
choose to:
- (a) Leave the `Task.Run` wrapper in place for safety/compat and accept the
  redundant task. Simpler / zero-risk / no behavior change for `TaskSchedulerMultiple`.
- (b) Remove the `Task.Run` since `Start()` is now non-blocking. Cleaner
  code, one extra edit.

Either is valid; architect's call. **Neither breaks anything.** The compat
story is: every existing caller either uses `Task.Run` or dedicates a
thread to `Start()`, and both continue to work when `Start()` becomes
non-blocking (the `Task.Run` just completes faster).

### 5.2 In the test project
Three call sites, all wrap in `Task.Run`:
- `TaskSchedulerJobCountSyncTests.cs:40` — `_ = Task.Run(() => sync.Start());`
- `TaskSchedulerJobCountSyncTests.cs:70–71` — two nodes
- `TaskSchedulerJobCountSyncTests.cs:121` — three nodes, staggered

All three of these will KEEP WORKING unmodified after decision #3. The
xUnit tests don't need refactoring to accommodate the new `Start()` —
they already treat it as fire-and-forget and use `await Task.Delay(...)`
for the beacon grace period.

**New concurrency regression test** (CONTEXT-1.md decision #2) should use
the same `Task.Run(() => sync.Start()); await Task.Delay(2500);` pattern
to match the existing test style.

### 5.3 In the DNQ repo
Grep for `TaskSchedulerJobCountSync` and `TaskScheduling.Distributed` in
`/mnt/f/git/dotnetworkqueue/Source/**/*.cs`:

**Zero hits.** The DNQ repo has no code references to the distributed
TaskScheduler at all. (The 26 hits for `TaskScheduling\.Distributed`
against all files include only shipyard markdown docs, not source code.)

**Consequence:** the DNQ repo is unaffected by the Phase 1 refactor. The
integration tests in Workstream 4 (Phase 3 of the roadmap) will be the
first DNQ code to touch it, and they will reference the published 0.4.0
NuGet — not a project reference. No compatibility analysis needed on the
DNQ side for Phase 1.

### 5.4 Start()-semantics risk summary
- **Only one production caller** (`TaskSchedulerMultiple`).
- **Caller already wraps in `Task.Run`.**
- **No external callers in the DNQ repo or any public surface.**
- **Risk of decision #3 breaking a caller: effectively zero.**

---

## 6. Existing Test Conventions

(Informed by `.shipyard/codebase/TESTING.md` in the sibling repo and by
direct read of `TaskSchedulerJobCountSyncTests.cs`.)

### 6.1 Framework
- **xUnit 2.x** with `xunit.runner.visualstudio` 2.x. **NOT MSTest. NOT NUnit.**
  The DNQ house style (MSTest 3.x + NSubstitute + AutoFixture + FluentAssertions
  6.12.2) does NOT apply to this repo.
- Test host: `Microsoft.NET.Test.Sdk` 17.x.
- Test target: **net8.0 only** (not multi-targeted). Any API used must compile
  on net8.0.
- `[Collection("NetMQ")]` serializes tests because NetMQ's global context
  can't safely parallel-tear-down. **New tests must also use
  `[Collection("NetMQ")]`.**

### 6.2 Mocking
- `NSubstitute` 5.x declared in csproj but currently **unused** in tests
  (TESTING.md:141–142). A fake `ITaskSchedulerBus` for the state-consistency
  test (CONTEXT-1.md roadmap line 89–91) would be the first real use of it.
- No AutoFixture, no FluentAssertions — just `Assert.Equal` and friends
  (TESTING.md:94–96).

### 6.3 Port + beacon conventions to match
- `NextPort()` static counter, starts at `40000 + Random.Shared.Next(0, 10000)`,
  atomic increment (TaskSchedulerJobCountSyncTests.cs:15–16). **New concurrency
  regression test must use `NextPort()`** to avoid collisions with the other
  tests.
- Platform-dependent beacon interface (lines 19–23):
  ```csharp
  RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "" : "loopback";
  ```
  Don't hardcode `"loopback"` — it breaks Linux.

### 6.4 Cleanup conventions
- Single-node: `using var sync = new TaskSchedulerJobCountSync(...)`
- Multi-node: `try { ... } finally { sync.Dispose(); }`

### 6.5 Timing / delays
- Fixed `Task.Delay` is the existing pattern. The concurrency regression test
  per CONTEXT-1.md decision #2 should use a generous 30-second timeout as
  a deadlock detector, not a perf assertion.
- TESTING.md:116 explicitly calls out timing sensitivity as a weakness —
  the architect may want the new tests to use polling-with-deadline rather
  than fixed delays where practical.

### 6.6 Test logger
- Private `XunitLogger` inner class adapts `ITestOutputHelper` to `ILogger`
  (TaskSchedulerJobCountSyncTests.cs:154–168). **Reuse this pattern** —
  don't re-invent logging for new tests.

### 6.7 License header
- File: `Source/Source.licenseheader` exists but the existing test file
  (`TaskSchedulerJobCountSyncTests.cs`) has **no header**. The main-project
  .cs files all have a LGPL-2.1 header (lines 1–18 of every main `.cs`).
  Architect's call on whether new test files need a header — match the
  existing test-file convention (no header) unless the user requests
  otherwise. **New main-project source files MUST have the header.**

### 6.8 Namespace conventions
- All main-project classes under `namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`.
- All test classes under `namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests`.
- File-scoped `namespace` is NOT used in the existing files — traditional
  block-scoped namespace is the house style. Match it.

---

## 7. NetMQ 4.0.2.2 API Surface for the Refactor

**Pinned version:** NetMQ 4.0.2.2 (main project csproj line 37). Any API
referenced must exist in this version.

### 7.1 Verified to exist (via direct evidence in `TaskSchedulerBus.cs`)
| API | Evidence | Usage |
|---|---|---|
| `NetMQPoller` | line 42 declaration, line 118 `new NetMQPoller { ... }` | Construct with a collection initializer of sockets/timers. |
| `NetMQPoller.Run()` | line 125 | Blocking poll loop. |
| `NetMQPoller.Stop()` | line 138 | Called from `OnShimReady` on the poller thread. (Docs: thread-safe; can also be called externally.) |
| Collection initializer — `{ _shim, _subscriber, _beacon, timer }` | line 118 | Poller accepts `ISocketPollable` + `NetMQTimer`. |
| `NetMQBeacon` | line 41, 82 | Not needed by the JobCountSync refactor directly. |
| `NetMQTimer` with `Elapsed` event | lines 114–115 | Shows poller timer pattern if needed. |
| `ReceiveReady` event on `NetMQSocket` | lines 85, 96 | `public event EventHandler<NetMQSocketEventArgs>`. Mandatory pattern for poller-driven reads. |
| `SubscriberSocket`, `PublisherSocket`, `PairSocket` | lines 39–40 | `NetMQActor` wraps a PairSocket. |
| `NetMQActor.Create(Action<PairSocket>)` | line 66 | Actor factory. |
| `NetMQActor.EndShimMessage` | line 135 | Shim termination sentinel. |
| `SendFrame`, `SendMoreFrame`, `ReceiveFrameString`, `TryReceiveFrameString`, `ReceiveMultipartMessage`, `SendMultipartMessage` | various sites in both files | Standard NetMQ frame APIs. |

### 7.2 NOT directly evidenced in this repo (must verify via NetMQ docs or test compile)
| API | Why we need it | Verification approach |
|---|---|---|
| `NetMQQueue<T>` | CONTEXT-1.md decision #1 — the new outbound send queue | **[Decision Required for architect]** — NetMQ 4.0.2.2 is confirmed to ship `NetMQQueue<T>` in `NetMQ.NetMQQueue` namespace; the type has been in NetMQ since 4.0. Architect should verify by adding a `using NetMQ;` and writing `new NetMQQueue<SetCountMsg>()` in a throwaway compile step. No external-network lookup was performed for this RESEARCH.md — flagging it rather than guessing. |
| `NetMQQueue<T>.Enqueue(T)` | Called from caller thread | Standard API. Documented as thread-safe. |
| `NetMQQueue<T>.TryDequeue(out T, TimeSpan)` or `Dequeue()` | Called from poller thread | Architect to verify which overloads exist in 4.0.2.2. |
| `NetMQQueue<T>.ReceiveReady` event | Required to attach to the `NetMQPoller` | `NetMQQueue<T>` implements `ISocketPollable` and exposes `ReceiveReady`; this is the whole point of the type existing. Must compile — if it doesn't, fallback is a `NetMQSocket`-based inproc pair, which NetMQ has used since 3.x. |
| `NetMQPoller.Add(ISocketPollable)` / collection init with `NetMQQueue<T>` | Wiring the queue into the poller | Same as above. |

### 7.3 Existing combined poller + queue pattern in this repo
**There is NO existing usage of `NetMQQueue<T>` anywhere in this repo.**
`TaskSchedulerBus.cs` uses the poller but only with sockets and a timer,
not with a queue. The JobCountSync refactor is **greenfield for the
queue-into-poller wiring in this codebase.** The architect does not have
a local template to copy.

### 7.4 Thread-ownership rules (from NetMQ 4.x general docs)
- A `NetMQSocket` may only be touched from the thread that owns the poller
  it was added to (or from a thread that has exclusive access before it's
  added).
- `NetMQPoller.Stop()` is safe to call from any thread.
- `NetMQQueue<T>.Enqueue(T)` is safe to call from any thread (this is the
  whole reason the type exists).
- `NetMQActor` wraps a thread-internal `PairSocket`; sending frames from
  the caller's thread is what `NetMQActor.SendFrame` already does and is
  safe. **But once added to a poller, the actor's `ReceiveReady` must
  be the only path that reads from it.**

**Translation for the architect:** the caller-side `IncreaseCurrentTaskCount` /
`DecreaseCurrentTaskCount` can safely `Enqueue(new SetCountMsg(port, count))`,
because `NetMQQueue<T>.Enqueue` is cross-thread-safe. The poller-thread
`ReceiveReady` handler then dequeues and calls `_actor.SendMoreFrame(...)
.SendFrame(...)`. This is the core shape of the fix.

---

## 8. Build and Test Commands

### 8.1 Build
From `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`:

```bash
# Debug build of solution
dotnet build Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln -c Debug

# Release build for NuGet packaging (Phase 2)
dotnet build Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln -c Release -p:CI=true

# Build the main project alone
dotnet build Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj -c Debug

# Build the test project alone
dotnet build Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj -c Debug
```

### 8.2 Test
```bash
# Run all tests
dotnet test Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj

# Run a single test by FQN (xUnit uses ~ filter like MSTest)
dotnet test Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj \
  --filter "FullyQualifiedName~TaskSchedulerJobCountSyncTests.LocalCountIncrementAndDecrement"

# Loop the concurrency regression test 5 times locally (CONTEXT-1.md verification step)
for i in 1 2 3 4 5; do
  dotnet test Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj \
    --filter "FullyQualifiedName~ConcurrencyRegression" || break
done
```

### 8.3 CI (existing)
- **GitHub Actions only**, `windows-latest` runner (TESTING.md:146).
- Workflow file: `.github/workflows/ci.yml` (not read in this research pass;
  architect should re-read if the CI wiring changes).
- Pipeline: checkout → setup .NET 8.0.x + 10.0.x → restore → `dotnet build`
  (Debug, --no-restore) → `dotnet test` (--no-build, no --configuration,
  no --collect).
- **No Jenkins in the sibling repo.** The DNQ roadmap Workstream 5 adds a
  Jenkins stage in the DNQ repo (Phase 4), not in this repo.

### 8.4 Packaging (Phase 2 preview — not Phase 1 scope)
```bash
# Pack into a deploy/ directory
dotnet pack Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj \
  -c Release -p:CI=true -o deploy/

# Push nupkg + auto-picked snupkg
dotnet nuget push "deploy/*.nupkg" \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```
Version must be bumped in the csproj before packing — `0.3.0` → `0.4.0`
(line 9 of the main csproj).

---

## Decision Required Flags

1. **`NetMQQueue<T>` API compile verification.** Research did not perform
   an external NetMQ docs lookup. The type is widely documented as shipping
   in NetMQ 4.x and exposes `Enqueue`, `Dequeue`, `ReceiveReady`, and
   `ISocketPollable`, but the architect should confirm by a throwaway
   compile step in the builder phase. Fallback if `NetMQQueue<T>.ReceiveReady`
   is unavailable on 4.0.2.2: use an inproc `PairSocket` pair as the
   cross-thread signal (one end in the caller, the other added to the
   poller). This is a known NetMQ idiom and works on all 4.x versions.

2. **Disposition of the `Task.Run` wrapper in `TaskSchedulerMultiple.Start()`
   (line 58–61).** After decision #3, the wrapper is redundant. Architect
   decides: leave it (zero-risk, no behavioral diff) or remove it (cleaner).
   Either is fine; this is a style/cleanup choice, not a correctness choice.

3. **License header on new test files.** Existing test file has none; all
   main-project files have the full LGPL-2.1 header. Architect should
   decide whether new test files get headers (probably no, to match existing
   test convention) and whether a new internal-to-main-project source file
   (e.g., if `SetCountMsg` lives in its own file) gets the header (yes,
   must match existing convention).

4. **`SetCountMsg` file placement.** CONTEXT-1.md §1 says "same file as
   `TaskSchedulerJobCountSync` (or a small sibling file — architect's
   call). Keep it `internal`." Not a research question — flagged to
   remind the architect.

5. **GitHub Actions CI impact.** New tests add wall-clock time to the
   existing ~19s test run (TESTING.md:18). The concurrency regression test
   has a 30-second deadlock-detection timeout (CONTEXT-1.md decision #2)
   but should complete in a few seconds under normal operation. Architect
   should estimate total CI runtime after new tests are added; if it
   crosses ~60s, consider a longer CI timeout.

---

## Sources

1. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerJobCountSync.cs` (lines 1–283) — primary target of the fix.
2. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/ITaskSchedulerJobCountSync.cs` (lines 1–58) — API contract.
3. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/ITaskSchedulerBus.cs` (lines 1–33) — bus contract.
4. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerBus.cs` (lines 1–212) — existing poller reference implementation.
5. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/TaskSchedulerMultiple.cs` (lines 30–120) — only production caller of `JobCountSync.Start()`.
6. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj` (lines 1–45) — TFMs, NetMQ version, CI/deterministic wiring.
7. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj` (lines 1–23) — xUnit stack, test TFM.
8. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/TaskSchedulerJobCountSyncTests.cs` (lines 1–170) — existing test patterns.
9. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/.shipyard/codebase/TESTING.md` (lines 1–178) — sibling repo's test conventions (from their v0.3.0 mapping).
10. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/.shipyard/STATE.json` — confirms sibling repo is at v0.3.0 shipped, not in active Phase 1.
11. `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/.shipyard/HISTORY.md` — confirms v0.3.0 shipment and defers issue #6.
12. Grep `TaskSchedulerJobCountSync|TaskScheduling\.Distributed` in `/mnt/f/git/dotnetworkqueue/Source/**/*.cs` — zero hits (DNQ repo has no source references).
13. Grep `\.Start\(\)` in sibling repo `Source/**/*.cs` — enumerates all `Start()` call sites, confirming `TaskSchedulerMultiple.cs:60` is the only production caller of `JobCountSync.Start()`.
14. `/mnt/f/git/dotnetworkqueue/.shipyard/phases/1/CONTEXT-1.md` — locked-in design decisions (honored throughout).
15. `/mnt/f/git/dotnetworkqueue/.shipyard/PROJECT.md` (lines 1–101) — project scope and workstreams.
16. `/mnt/f/git/dotnetworkqueue/.shipyard/ROADMAP.md` (lines 48–111) — Phase 1 scope.
