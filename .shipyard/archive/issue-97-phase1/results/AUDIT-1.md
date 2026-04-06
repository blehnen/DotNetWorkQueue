# Security Audit Report — Phase 1

## Executive Summary

**Verdict:** PASS
**Risk Level:** Low

This phase is a display-only bug fix with a narrow scope: three production files and three test files changed. No new dependencies were introduced, no external inputs are added to trust boundaries, and no secrets were found anywhere in the diff. The exception text written to history is already bounded by `MaxExceptionLength` before storage, which is the only user-influenced data flowing through the changed code. There are no exploitable vulnerabilities in this changeset.

### What to Do

| Priority | Finding | Location | Effort | Action |
|----------|---------|----------|--------|--------|
| 1 | Memory transport `Data` dict is a static field — connection string leaks into process-wide state | `WriteMessageHistoryHandler.cs:30` | Small | Document this as intentional in-process behavior or add a clearing API for test isolation |
| 2 | Redis `RecordComplete`/`RecordError` read `StartedUtc` with an unchecked cast | `WriteMessageHistoryHandler.cs:79,90` | Trivial | Add `.HasValue` guard before the explicit cast to `(long)` |
| 3 | Test-seam class `TestableWriteMessageHistoryHandler` is not `sealed` | `WriteMessageHistoryHandlerTests.cs:17` | Trivial | Mark as `sealed` to prevent accidental subclassing in test code |

### Themes
- The code is internally consistent: all three transport implementations apply the same `Enqueued`-only guard on `RecordProcessingStart`, and the decorator correctly pre-captures `messageId` before delegation.
- Exception text is the only externally-sourced data in the pipeline; it is already truncated before storage. No injection vectors exist.

---

## Detailed Findings

### Critical

None.

### Important

**[I1] Unchecked `(long)` cast on `RedisValue` for `StartedUtc` in `RecordComplete` and `RecordError`**
- **Location:** `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs:79` and `:90`
- **Description:** Both methods call `(long)db.HashGet(HistoryHashKey(queueId), "StartedUtc")` without first checking `HasValue`. If the key does not exist in Redis (e.g., history was pruned, or history was disabled when the message was enqueued), `RedisValue.Null` is returned. Casting `RedisValue.Null` explicitly to `(long)` throws a `RedisException` / `InvalidCastException` at runtime. The `RecordProcessingStart` method (fixed in this phase) now correctly checks `.HasValue` first — `RecordComplete` and `RecordError` do not apply the same pattern.
- **Impact:** A message that reaches `RecordComplete` or `RecordError` when its history hash no longer exists in Redis will throw an unhandled exception inside the history write path. Depending on where this is called in the consumer pipeline, it may surface as an unhandled exception or be silently swallowed, but in either case the history write fails and the status remains `Processing` — the exact bug this phase was meant to fix. (CWE-20, defensive coding gap)
- **Remediation:** Apply the same `HasValue` guard used in `RecordProcessingStart`:
  ```csharp
  var rawStarted = db.HashGet(HistoryHashKey(queueId), "StartedUtc");
  var startedTicks = rawStarted.HasValue ? (long)rawStarted : 0L;
  ```
- **Evidence:**
  ```csharp
  // Line 79
  var startedTicks = (long)db.HashGet(HistoryHashKey(queueId), "StartedUtc");
  // Line 90
  var startedTicks = (long)db.HashGet(HistoryHashKey(queueId), "StartedUtc");
  ```

---

### Advisory

- **Static `Data` dictionary in Memory `WriteMessageHistoryHandler` accumulates across the process lifetime** (`WriteMessageHistoryHandler.cs:30-31`) — The `ConcurrentDictionary<string, ConcurrentDictionary<...>> Data` is a `private static readonly` field, meaning all queues sharing a process share one unbounded dictionary with no eviction. This is consistent with the in-memory transport's general design (no persistence), but over very long-running processes or repeated integration test runs it will grow without bound. Consider documenting this or providing a `Reset()` / `Clear()` method for test isolation. Note: tests already mitigate this with unique GUIDs per test (`Guid.NewGuid():N`), so there is no test interference risk here.

- **`RecordProcessingStart` comment in Redis test is slightly misleading** (`WriteMessageHistoryHandlerTests.cs:306-308`) — The comment states "RedisValue.Null casts to (int)0, which is the same as MessageHistoryStatus.Enqueued — the bug." This is accurate for explicit `(int)` cast but not universally true. Minor documentation clarity issue; no security impact.

- **Redis `WriteMessageHistoryHandler` is `public`** (`WriteMessageHistoryHandler.cs:27`) — The class carries a `protected virtual GetDb()` seam intended only for test injection. Exposing it `public` widens the attack surface slightly and means any downstream code could subclass it and override `GetDb()` to redirect Redis writes. The class should be `internal` to match the project's convention for transport handler classes. This is consistent with the note in CLAUDE.md: "keep classes internal to contain the scope."

---

## Cross-Component Analysis

**Decorator-to-transport data flow is coherent.** The decorator (`ReceiveMessagesErrorHistoryDecorator`) captures `messageId` before calling the inner handler, then passes it as a plain string to `IWriteMessageHistory.RecordError`. All three transport implementations (`Redis`, `Memory`, and implicitly the relational database transports via the existing decorator contract) receive only a string queue ID and a bounded exception string. No raw `IMessageContext`, `IReceivedMessageInternal`, or live database handles cross the boundary. This is the correct trust-boundary design.

**The `MaxExceptionLength` truncation guard is in the decorator, not the transport.** This means it applies uniformly to all transports — any transport receiving `RecordError` gets at most `MaxExceptionLength` bytes of exception text regardless of whether the transport itself enforces a column limit. This is the correct layering. No transport-specific bypass exists in the changed code.

**`RecordProcessingStart` guard consistency.** The fix correctly applies a `Status == Enqueued` guard in both Memory (`WriteMessageHistoryHandler.cs:60`) and Redis (`WriteMessageHistoryHandler.cs:68-69`). The relational database transports were not changed in this phase, implying they either already had this guard or are not affected — this was not audited as it is out of scope for the diff. The builder should confirm the relational transport's `RecordProcessingStart` applies the same guard if it exists.

---

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | No injection, no auth changes, exception text is bounded before storage |
| Secrets & Credentials | Yes | No secrets, keys, tokens, or connection strings in any changed file |
| Dependencies | Yes | No new dependencies added in this phase |
| Infrastructure as Code | N/A | No IaC files changed |
| Docker/Container | N/A | No Dockerfile changes |
| Configuration | Yes | No configuration files changed |

## Dependency Status

No dependencies were added or changed in this phase.

| Package | Version | Known CVEs | Status |
|---------|---------|-----------|--------|
| StackExchange.Redis | (existing, unchanged) | None known at audit date | OK |

## IaC Findings

Not applicable — no infrastructure files changed in this phase.
