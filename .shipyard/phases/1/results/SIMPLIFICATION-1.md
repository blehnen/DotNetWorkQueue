# Phase 1 Simplification Review — TaskScheduler Lock Contention Fix

**Date:** 2026-04-14
**Scope:** Cumulative diff `master..phase-1-lock-fix` (12 commits)
**Repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Files analyzed:**
- Production: `Source/TaskSchedulerJobCountSync.cs`, `Source/TaskSchedulerMultiple.cs`
- Tests: `TaskSchedulerJobCountSyncTests.cs` (existing, lightly touched), `TaskSchedulerJobCountSyncConcurrencyTests.cs`, `TaskSchedulerJobCountSyncStateTests.cs`, `TaskSchedulerJobCountSyncLifecycleTests.cs`, `SetCountMsgTests.cs`, `NetMqQueueApiProbeTests.cs`

## Summary

The cumulative production diff is in genuinely good shape — the refactor of `TaskSchedulerJobCountSync` is a clean, well-commented Phase A/B/C startup split with no dead code, no defensive bloat, and no over-abstraction. The `SetCountMsg` record struct is the right minimum (two fields, value semantics). The Dispose path correctly stops the poller, joins with a timeout, and preserves the existing Win32 socket-error swallow. The only material findings are in the **test layer**, where four files independently re-implement the same three helpers (`XunitLogger`, `NextPort()` + seed counter, `BeaconInterface`). That's a textbook 4-occurrence Rule-of-Three trigger and worth one small extraction. There's also one piece of probe scaffolding (`NetMqQueueApiProbeTests`) that has served its diagnostic purpose and should arguably be deleted now that the real handler tests cover the same code path. Nothing here blocks shipping Phase 1 or starting Phase 2.

## Findings

### High priority (recommend action before Phase 2)

None. No production-code issues; no correctness or readability regressions vs. the pre-refactor file.

### Medium priority (defer to a later cleanup phase or Phase 4/5 CI wiring)

**M1. Quadruple-duplicated test helpers (`XunitLogger`, port seed, `BeaconInterface`).**
The same three helpers are copy-pasted across four files:

| Helper | Files |
|---|---|
| `private class XunitLogger : ILogger` (~15 LoC) | `TaskSchedulerJobCountSyncTests.cs:154`, `...ConcurrencyTests.cs:90`, `...StateTests.cs:75`, `...LifecycleTests.cs:66` |
| `_nextPort` + `NextPort()` (different seed bases per file: 40000/50000/55000/60000) | `...Tests.cs:15-16`, `...ConcurrencyTests.cs:16-17`, `...StateTests.cs:16-17`, `...LifecycleTests.cs:15-16` |
| `BeaconInterface` ternary on `RuntimeInformation.IsOSPlatform(OSPlatform.Linux)` (+ 4-line explanatory comment in the original file only) | `...Tests.cs:18-23`, `...ConcurrencyTests.cs:19-20`, `...StateTests.cs:19-20`, `...LifecycleTests.cs:18-19` |

That's ~80 lines of pure duplication and — more importantly — three separate maintenance points if the beacon-interface logic, the port-allocation strategy, or the logger output ever needs to change. The seed-base trick (each file picks its own decade so ports don't collide across parallel test files) is fragile: a fifth test file added by Phase 2/3 will need yet another magic number and the author has to remember the existing ones.

**Suggested fix:** add a single `TestHelpers.cs` (or `NetMqTestSupport.cs`) in `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/` containing:
- `internal sealed class XunitLogger : ILogger` (one copy)
- `internal static class TestPorts { public static int Next() => Interlocked.Increment(ref _next); private static int _next = 40000 + Random.Shared.Next(0, 20000); }` (one shared monotonic counter — the per-file decade strategy is unnecessary because `Interlocked.Increment` is atomic across all callers)
- `internal static class BeaconInterfaces { public static readonly string Default = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "" : "loopback"; }` with the explanatory comment kept once

Then each test file replaces ~20 lines with three `using`/reference sites. **Net delete: ~60 lines.** **Effort: trivial (~20 minutes).**

**Why medium not high:** the duplication is xUnit-idiomatic and the existing tests pass. It's mechanical cleanup that makes Phase 2 easier (Phase 2 will likely add more sync/lifecycle tests), so doing it before Phase 2 is *nice* but not required. Defer is fine; just track it.

**M2. `NetMqQueueApiProbeTests.cs` is throwaway probe code.**
This 35-line file does exactly one thing: assert that `NetMQQueue<int>` + `NetMQPoller` fire `ReceiveReady` when an item is enqueued. That's a verification of the third-party NetMQ library API contract that the Phase 1 refactor depends on. Useful as a one-shot "does this API actually work the way I think it does" probe during the design phase — but now that `TaskSchedulerJobCountSync.RunPoller` exercises the exact same code path through real handler tests in `TaskSchedulerJobCountSyncTests`/`...ConcurrencyTests`/`...StateTests`, the probe is redundant. If NetMQ ever breaks this contract, ten other tests will fail before this one does.

**Suggested fix:** delete the file. **Effort: trivial.** **Risk:** essentially zero — no other test references it.

**Why medium not high:** harmless to leave, but it's exactly the kind of "scaffolding left behind after the real implementation landed" pattern that accumulates over many phases.

### Low priority / informational

**L1. Beacon-interface comment lives in only one of four files.**
`TaskSchedulerJobCountSyncTests.cs:18-21` has a 4-line comment explaining *why* Linux uses `""` instead of `"loopback"` (kernel won't deliver 255.255.255.255 broadcasts back to a 127.0.0.1 socket). The other three files have the same `BeaconInterface` constant but no comment. Anyone reading just the new files will see a magic ternary with no rationale. M1's helper extraction fixes this for free by collapsing all four sites to a single commented constant.

**L2. `OnActorReady` `else if`/`TryParse`/`ContainsKey`/`TryAdd` ladder unchanged from pre-refactor.**
`TaskSchedulerJobCountSync.cs:162-222` is structurally identical to the master version: a long `else if` ladder, a `ContainsKey` + `TryAdd` + indexer pattern that could be a single `_otherProcessorCounts[key] = value;`, and an outer try/catch that logs and swallows everything. None of this was in scope for the lock-contention fix — flagging only so the orchestrator knows it's pre-existing tech debt, not Phase 1 regression. **Defer indefinitely; not Phase-1 work.**

**L3. `OnOutboundReady` calls `TryDequeue` with `TimeSpan.Zero` in a `while` loop.**
`TaskSchedulerJobCountSync.cs:224-233`. This is the correct NetMQ idiom — drain on signal, never block. Calling out only because it can *look* like a busy loop on a quick read; it isn't, and the inline pattern is the right one. **No action.**

**L4. `Thread.Sleep(1100)` magic number in `Start()`.**
`TaskSchedulerJobCountSync.cs:118` — "second beacon time, so we wait to ensure beacon has fired." Pre-existing from before Phase 1; the refactor preserved it intentionally. A named `BeaconGracePeriod` const would document intent, but this is style polish and not a Phase 1 deliverable. **Defer.**

**L5. `RunPoller` wires events then constructs `_poller`.**
`TaskSchedulerJobCountSync.cs:147-160`. The handler subscriptions happen before the `NetMQPoller` is built. This is correct (NetMQPoller picks up the existing handlers at `Run` time) and matches the comment block in `Start()` that explicitly hands ownership to the poller thread. **No action; called out as deliberately correct so a future reviewer doesn't "fix" it.**

## Specific Recommendations

### M1 (recommended, deferrable)

**File to add:** `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/NetMqTestSupport.cs`

```csharp
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests
{
    internal sealed class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        public XunitLogger(ITestOutputHelper output) => _output = output;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            try { _output.WriteLine($"[{logLevel}] {formatter(state, exception)}"); }
            catch { /* test may have ended */ }
        }
    }

    internal static class TestPorts
    {
        // Single monotonic counter shared across all test files; Interlocked guarantees
        // uniqueness without per-file seed-decade hacks.
        private static int _next = 40000 + Random.Shared.Next(0, 20000);
        public static int Next() => Interlocked.Increment(ref _next);
    }

    internal static class BeaconInterfaces
    {
        // On Linux, NetMQBeacon's "loopback" mode binds to 127.0.0.1 but sends to
        // 255.255.255.255, and the kernel will not deliver those broadcasts back to a
        // 127.0.0.1 socket. Use the first available interface ("") instead, which binds
        // to the subnet broadcast address and works for same-host peer discovery on
        // both platforms.
        public static readonly string Default =
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "" : "loopback";
    }
}
```

Then in each of the 4 test files:
- Delete the nested `XunitLogger` class.
- Delete `_nextPort` + `NextPort()`.
- Delete `BeaconInterface` constant + comment.
- Replace call sites: `NextPort()` → `TestPorts.Next()`, `BeaconInterface` → `BeaconInterfaces.Default`, `new XunitLogger(output)` keeps working unchanged (same class name, now top-level internal).

**Effort:** trivial (~20 min). **Net LoC:** roughly -60.

### M2 (recommended, deferrable)

Delete `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/NetMqQueueApiProbeTests.cs`. **Effort:** trivial (~1 min). **Net LoC:** -35.

## Accept-as-is items

- **`SetCountMsg` as a `readonly record struct`** at `TaskSchedulerJobCountSync.cs:286`. Two fields, value equality, allocation-free. This is the minimum viable model for the outbound queue — there is no simpler alternative that wouldn't reintroduce primitive-pair ambiguity. Keep.
- **`SocketException` swallow with hardcoded `10035`/`10054` codes** at `TaskSchedulerJobCountSync.cs:258-265`. Pre-existing pattern preserved by the refactor; matches the documented Win32 behaviour for sockets being torn down during dispose. Not a Phase 1 finding.
- **`Thread.Sleep(1100)` in `Start()`**. Documented inline ("second beacon time"). Pre-existing.
- **The Phase A/B/C comment block in `Start()`** at `TaskSchedulerJobCountSync.cs:112-133`. Verbose, but earns its keep — it documents the *non-obvious thread-ownership transfer* that is the entire point of the refactor. Without it the next maintainer would be tempted to "simplify" the initial broadcast through `_outbound` and reintroduce the race. **Keep all of it.**
- **Per-test-file `_nextPort` seed bases (40000/50000/55000/60000).** Looks like duplication-with-variation but is actually a deliberate cross-file collision-avoidance trick. M1's shared `TestPorts.Next()` makes this unnecessary, but if M1 is deferred, the seed-base strategy is the right workaround.
- **The 4 test classes themselves being separated** (`...Tests`/`...ConcurrencyTests`/`...StateTests`/`...LifecycleTests`). Splitting by concern keeps each file focused and lets `[Collection("NetMQ")]` serialize NetMQ-touching tests without serializing pure state tests. Don't merge them.

## Recommendation for the orchestrator

**Defer all findings to ISSUES.md for a later cleanup phase.**

Phase 1 production code is clean and ready to ship. The only findings are test-layer cleanups that are mechanical, low-risk, and have zero impact on the lock-contention fix itself. Doing M1 + M2 now would take ~25 minutes total and net ~95 LoC removed, but they don't block Phase 2 and don't affect correctness. If Phase 2 is going to add another sync/lifecycle test file, *then* M1 becomes worth doing first — otherwise track both as simplifier issues and pick them up during the Phase 4/5 cleanup window.

**Priority counts:** High 0 / Medium 2 / Low 5
