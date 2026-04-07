---
phase: dashboard-history-tests
plan: "1.2"
reviewer: Claude Sonnet 4.6
date: 2026-04-06
commit: f2b432c6
---

## Stage 1: Spec Compliance
**Verdict:** FAIL

### Task 1: Create RedisHistoryTests.cs with Disabled (4) and Enabled (14) test classes

#### RedisHistoryDisabledTests
- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/RedisHistoryTests.cs` lines 40–115 declare `RedisHistoryDisabledTests` with exactly 4 `[TestMethod]` methods: `History_Returns_Empty_When_Not_Enabled` (L79), `HistoryCount_Returns_Zero_When_Not_Enabled` (L89), `HistoryByMessageId_Returns_NotFound_When_Not_Enabled` (L99), `PurgeHistory_Returns_Zero_When_Not_Enabled` (L107).
- Notes: Uses `RedisQueueInit`/`RedisQueueCreation`, `ConnectionStrings.Redis`, 2-arg `AddConnection`, string not-found ID — all correct.

#### RedisHistoryEnabledTests — Test Count Mismatch
- Status: FAIL
- Evidence: The implementation at lines 122–382 contains **15** `[TestMethod]` methods. The plan's `must_haves`, `<done>` criteria (task 1), and embedded reference code all specify **14**. The extra test is `History_Filtered_By_Processing_Status_Returns_Empty` (line 267), which does not appear in the plan's 14-test enumeration.
- Notes: The commit message itself acknowledges 15 tests ("15 enabled"), which confirms the deviation is intentional but it was not reflected as an approved plan change. The done criteria for task 2 reads "All 18 Redis history tests pass (4 disabled + 14 enabled)" — with 19 tests in the file the verification command count is also wrong.

#### RedisHistoryEnabledTests — Missing ICreationScope Capture and Disposal
- Status: FAIL
- Evidence: The plan's embedded reference code (PLAN-1.2.md lines 181, 199, 252) declares `private ICreationScope _scope`, assigns `_scope = _creation.Scope` after `CreateQueue()`, and calls `_scope?.Dispose()` in cleanup. The actual implementation omits the `_scope` field entirely (class fields at lines 126–129 contain no `ICreationScope`), never captures `_creation.Scope`, and `CleanupAsync` at lines 190–196 does not dispose the creation scope.
- Notes: For Redis this may be a no-op at runtime (Redis creation scopes may be empty), but it is a spec deviation. If the scope carries any registered resources, they would leak across test runs.

#### Transport-Specific Adaptations
- Status: PASS
- Evidence:
  - `RedisQueueInit` / `RedisQueueCreation` used throughout (L43, L97, L139–140).
  - `ConnectionStrings.Redis` at lines 50 and 135.
  - `AddConnection<RedisQueueInit>(connStr, conn => conn.AddQueue(...))` — 2-arg overload, no `RegisterNonScopedSingleton` (L60, L177).
  - `_creation.Options.EnableHistory = true` via `RedisBaseTransportOptions` (L141).
  - String not-found ID `"nonexistent-id-12345"` used in both disabled (L102) and enabled (L330) classes.
  - `using DotNetWorkQueue.Transport.Redis.Basic` at L29.

#### LGPL-2.1 License Header
- Status: PASS
- Evidence: Lines 1–18 contain the full LGPL-2.1 header matching the project template.

#### Consumer Timeout Deviation
- Status: PASS (minor deviation, not a failure criterion)
- Evidence: Implementation uses `TimeSpan.FromSeconds(60)` (L168); plan's reference code specifies `TimeSpan.FromSeconds(30)`. The done criteria do not specify a timeout value, so this does not constitute a spec failure — but see Important finding below.

### Task 2: Run Redis history tests
- Status: NOT EVALUATED
- Notes: Task 2 is a runtime verification requiring a live Redis instance. The done criteria ("All 18 Redis history tests pass") cannot be confirmed from static analysis and is rendered incorrect by the 15-vs-14 test count discrepancy regardless.

### Conflict with PLAN-1.1
- Status: PASS (no conflict)
- Evidence: PLAN-1.1 touches only `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/LiteDbHistoryTests.cs`. PLAN-1.2 touches only `RedisHistoryTests.cs`. No shared files.

---

## Stage 2: Code Quality
Not performed — Stage 1 failed.

---

## Issues to Resolve Before Re-Review

### Critical

1. **Test count mismatch: 15 enabled tests vs. spec's 14** — `RedisHistoryTests.cs` line 267
   - `History_Filtered_By_Processing_Status_Returns_Empty` is present in the implementation but absent from the plan. Either the plan must be updated to list 15 enabled tests (and update `must_haves`, task 1 `<done>`, and task 2 `<done>` accordingly) or this test must be removed.
   - Remediation: If the test is intentional and correct (which it appears to be — it provides useful coverage), update PLAN-1.2.md `must_haves` to "15 tests", task 1 `<done>` to "15 test methods", and task 2 `<done>` to "All 19 Redis history tests pass (4 disabled + 15 enabled)". Do not remove the test.

2. **Missing `ICreationScope` capture and disposal** — `RedisHistoryTests.cs` lines 126–129 (class fields) and lines 190–196 (`CleanupAsync`)
   - The plan specifies capturing `_creation.Scope` after `CreateQueue()` and disposing it in cleanup. The implementation skips both. Even if the scope is empty for Redis, the omission is a spec deviation and a latent resource-leak risk.
   - Remediation: Add `private ICreationScope _scope;` field, assign `_scope = _creation.Scope;` immediately after `_creation.CreateQueue()` (line 143), and add `_scope?.Dispose();` in `CleanupAsync` after `_creationContainer?.Dispose()`.

### Important

1. **Consumer wait timeout doubled without documentation** — `RedisHistoryTests.cs` line 168
   - The implementation uses `TimeSpan.FromSeconds(60)` where the plan specifies 30 seconds. The change is defensible for a network-dependent transport under CI load, but it is undocumented and could mask slow-consumer bugs in CI.
   - Remediation: Add an inline comment explaining the longer timeout (e.g., `// 60s: Redis network round-trips under CI load are slower than in-process transports`) so future readers understand the intent.

---

## Summary
**Verdict:** BLOCK

The implementation delivers solid test coverage with correct Redis-specific adaptations, but two spec deviations prevent a clean pass: the enabled test class contains 15 tests where the plan specifies 14 (the extra test `History_Filtered_By_Processing_Status_Returns_Empty` is good coverage but unplanned), and the `ICreationScope` field is omitted from both fields and cleanup contrary to the plan's reference code. The simplest resolution is to amend the plan to match the implementation for the test count, and add the two-line scope handling to the implementation. No logic bugs were found in the test assertions themselves.

Critical: 2 | Important: 1 | Suggestions: 0
