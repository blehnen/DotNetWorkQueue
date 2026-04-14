# Phase 1 Security Audit — TaskScheduler Lock Contention Fix

**Date:** 2026-04-14
**Scope:** git diff master..phase-1-lock-fix (12 commits)
**Calibration:** Concurrency refactor; no auth/crypto/IO/IaC/dep changes expected.

## Verdict: CLEAN

## Findings

### Critical
none

### Important
none

### Minor / Informational

- **[INFO-1] Exception message in `LogError` uses string interpolation rather than structured logging template.**
  `TaskSchedulerJobCountSync.cs:143` (`$"A fatal error... {error}"`), `:158` (`$"TaskSchedulerJobCountSync poller thread terminated...{error}"`), `:220` (`$"Failed to handle NetMCQ commands...{error}"`).
  Not a security issue — the `error` object serialized via `ToString()` produces the standard .NET stack trace, which does not contain credentials, tokens, or remote addresses beyond what the wire protocol already carries. Matches the pre-refactor logging style. Recommendation: migrate to `_log.LogError(error, "...")` structured form in a future cleanup phase so sinks can redact/format consistently. Non-blocking.

- **[INFO-2] `LogWarning` timeout message at `TaskSchedulerJobCountSync.cs:252`** ("poller thread did not exit within 5s; forcing disposal") contains only the hardcoded literal — no variables, no state leak. Clean.

- **[INFO-3] `LogDebug` at `:172`, `:181`, `:189` emit host addresses / node URIs.** These are LAN peer endpoints already broadcast on the Bus by design (the whole purpose of the beacon protocol). They are not secrets in this threat model (per memory: "transport security is user's responsibility"). Pre-existing behavior, unchanged by this phase. No action.

## Scope Confirmation

- **Files changed (production):**
  - `Source/TaskSchedulerJobCountSync.cs` — lock + polling replaced by `NetMQPoller` + `NetMQQueue<SetCountMsg>` driven from a dedicated background `Thread`. `_lockSocket` removed; all socket I/O now routed through the poller thread after Start's Phase-A/B handshake.
  - `Source/TaskSchedulerMultiple.cs` — cosmetic cleanup (no logic delta relevant to security surface).
- **Files changed (tests, internal xUnit):**
  - `TaskSchedulerJobCountSyncTests.cs`
  - `TaskSchedulerJobCountSyncConcurrencyTests.cs`
  - `TaskSchedulerJobCountSyncStateTests.cs`
  - `TaskSchedulerJobCountSyncLifecycleTests.cs`
  - `NetMqQueueApiProbeTests.cs`
  - `SetCountMsgTests.cs`
- **New dependencies:** none. `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj` still pins `DotNetWorkQueue 0.9.31`, `NetMQ 4.0.2.2`, `Microsoft.SourceLink.GitHub 10.0.201`. Test project still pins `xunit`, `Microsoft.NET.Test.Sdk`, `NSubstitute`, `coverlet.collector`. No CVE rescan required.
- **Wire protocol changes:** none. `BroadCast` frame sent on the caller thread during Start (`SendMoreFrame(Publish) -> SendMoreFrame(BroadCast) -> SendFrame(_hostAddress)`) matches pre-refactor byte layout. Commit `fda0fd4` (the BroadCast revert) confirms pre-refactor parity. `SetCount` outbound frames are built from trusted local state (`_hostPort`, `newValue` via `Interlocked`), formatted with `CultureInfo.InvariantCulture`, unchanged from the prior code path.
- **Secrets scan:** clean. Grep for `password|api[_-]?key|secret|token|connectionstring|Bearer|private[_-]?key` across changed source files returned zero matches. The single base64-pattern regex hit in `TaskSchedulerJobCountSyncConcurrencyTests.cs` was the long class name itself — false positive.
- **Error-path information disclosure:** clean. `LogError`/`LogWarning` callsites in `RunPoller` and `Dispose` emit only (a) the hardcoded English message and (b) the .NET exception `ToString()`. No credentials, connection strings, tokens, or internal file paths beyond standard stack traces are surfaced. Consistent with the project's documented security model.

## Targeted concurrency-integrity review

Because Phase 1 removes the `_lockSocket` mutex, I checked the new poller-owned model for integrity regressions that could surface as exceptions leaking state:

1. **Double-dispose of `_actor` / `_outbound` / `_poller`.** `Dispose(bool)` uses `_disposedValue` gate and null-conditional disposal — safe. The `catch (SocketException)` silently swallows errors 10035 (WSAEWOULDBLOCK) / 10054 (WSAECONNRESET) on shutdown, which is benign and pre-existing behavior for NetMQ teardown. No new information disclosure.
2. **Cross-thread writes to `_actor`.** After Phase-C (`_pollerThread.Start()`), `_actor` is exclusively owned by the poller thread for all `Send*` / `Receive*` calls. `IncreaseCurrentTaskCount` / `DecreaseCurrentTaskCount` enqueue on `NetMQQueue<SetCountMsg>` (which is the thread-safe NetMQ-provided queue), not on `_actor` directly. The only caller-thread `_actor` writes happen in Phase-A/B of `Start()` BEFORE the poller thread is launched — safe by construction.
3. **Start-time race on `_outbound`.** `_outbound` is assigned before `_pollerThread.Start()` and read by `IncreaseCurrentTaskCount`/`DecreaseCurrentTaskCount` via null-conditional `?.Enqueue`. A caller that enqueues after `Dispose` nulls nothing (the field is not reset) may race on a disposed queue, but `NetMQQueue.Enqueue` on a disposed instance throws `ObjectDisposedException`, not a state leak. Pre-existing risk class. Advisory only.
4. **`RunPoller` catches all exceptions.** The `catch (Exception)` in `RunPoller` terminates the poller thread and logs, but does NOT re-raise or signal the owner, so a poller crash becomes a silent functional-correctness bug (counts stop syncing). This is a reliability concern, not a security concern, and is acceptable for the refactor's scope. A future enhancement could surface a health signal to the owner.

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Partial (targeted) | No injection / authn / authz / crypto / deserialization surface in diff. `int.TryParse`/`long.TryParse` wire-frame guards at `:194` / `:198` preserved exactly. |
| Secrets & Credentials | Yes | Grep-verified zero matches in prod + test diff. |
| Dependencies | Yes | No new `PackageReference` entries. |
| IaC / Container | N/A | None in scope. |
| Configuration | N/A | None in scope. |

## Recommendation

**Proceed — no action required.**

Phase 1 is a tightly scoped concurrency refactor. No security regressions, no new dependencies, no secrets, no wire-protocol or trust-boundary changes. The new poller-thread ownership model is sound, disposal is ordered and guarded, and error-path logging does not introduce information disclosure beyond what the project's documented security model already accepts. Ship it.
