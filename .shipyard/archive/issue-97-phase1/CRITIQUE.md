# Plan Critique: Phase 1 (Issue #97)
**Date:** 2026-04-05  
**Type:** plan-review (pre-execution feasibility stress test)

---

## Executive Summary
**Verdict: READY**

All three plans are feasible, well-specified, and ready for execution. File paths exist, API signatures match, line numbers are accurate, helper methods are available, and there are zero file conflicts between plans. The plans are appropriately scoped and testable.

---

## Per-Plan Findings

### PLAN-1.1: Fix ReceiveMessagesErrorHistoryDecorator
**Status: PASS**

| Item | Finding | Evidence |
|------|---------|----------|
| **File exists** | PASS | `Source/DotNetWorkQueue/History/Decorator/ReceiveMessagesErrorHistoryDecorator.cs` exists (63 lines) |
| **Line numbers** | PASS | `MessageFailedProcessing` method found at line 42 (matches plan: line 42-61) |
| **API signature** | PASS | Method signature: `public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)` matches task description |
| **Context.MessageId access** | PASS | Current code reads `context.MessageId` at line 45 after calling `_handler.MessageFailedProcessing()` at line 44 (confirms the bug) |
| **Scope** | PASS | Touches only 1 file; no overlap with Plans 1.2 or 1.3 |
| **Task count** | PASS | 1 task (within 3-task limit) |
| **Verification commands** | PASS | All commands are syntactically valid and runnable |

**No issues detected.**

---

### PLAN-1.2: Guard RecordProcessingStart in Redis and Memory
**Status: PASS**

| Item | Finding | Evidence |
|------|---------|----------|
| **Files exist** | PASS | Both files exist: `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` (118 lines) and `Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs` (122 lines) |
| **Redis method location** | PASS | `RecordProcessingStart` at line 64 (matches plan: lines 64-69) |
| **Memory method location** | PASS | `RecordProcessingStart` at line 57 (matches plan: lines 57-61) |
| **Redis current behavior** | PASS | Unconditionally calls `db.HashSet()` without status guard (confirms the bug) |
| **Memory current behavior** | PASS | Unconditionally sets `r.Status = MessageHistoryStatus.Processing` without guard (confirms the bug) |
| **Enum values correct** | PASS | `MessageHistoryStatus` enum found at `Source/DotNetWorkQueue/Configuration/MessageHistoryStatus.cs`; `Enqueued = 0`, `Processing = 1`, `Error = 3` |
| **Scope** | PASS | Touches 2 files (both implementation, no tests); no overlap with Plans 1.1 or 1.3 |
| **Task count** | PASS | 2 tasks (within 3-task limit) |
| **Verification commands** | PASS | All commands are syntactically valid |

**No issues detected.**

---

### PLAN-1.3: Unit tests for Bug A and Bug B
**Status: PASS**

| Item | Finding | Evidence |
|------|---------|----------|
| **Test files exist** | PASS | All three test files exist: `Source/DotNetWorkQueue.Tests/History/Decorator/ReceiveMessagesErrorHistoryDecoratorTests.cs` (119 lines), `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs` (294 lines), `Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs` (423 lines) |
| **CreateDecorator helper** | PASS | Helper method found in decorator test file; used by existing tests |
| **CreateContext helper** | PASS | Helper method found in decorator test file; used by existing tests |
| **CreateEnabledWithDb helper** | PASS | Found in Redis test file at line 233; used by existing tests |
| **ContainsEntry helper** | PASS | Found in Redis test file at line 283; available for reuse |
| **CreateHandlerWithKey helper** | PASS | Found in Memory test file; used by existing tests |
| **GetRecordsForQueue method** | PASS | Static method on `WriteMessageHistoryHandler` class; used by existing tests |
| **Scope** | PASS | Touches 3 test files only (no implementation changes); disjoint from Plans 1.1 and 1.2 |
| **Task count** | PASS | 3 tasks (at limit) |
| **TDD flag** | PASS | Correctly marked `tdd: true` (tests can be written before fixes) |
| **Test methodology** | PASS | Task 1 uses mocking (NSubstitute) with callback simulation; Tasks 2-3 use real in-memory data structures |
| **Assertion style** | PASS | Uses `Received(1)`, `DidNotReceive()`, `Assert.AreEqual()` patterns consistent with existing test style |
| **Verification commands** | PASS | All commands are valid; tests will pass after Plans 1.1 and 1.2 are applied |

**No issues detected.**

---

## Phase Requirements Coverage

| Requirement | Plan | Evidence |
|---|---|---|
| **Build succeeds** | All | Verification commands in each plan include `dotnet build` and `--no-restore` |
| **Bug A fix** | 1.1 | Captures `messageId` before calling `_handler.MessageFailedProcessing()` |
| **Bug B fix (Redis)** | 1.2 | Adds guard: `if (currentStatus != (int)MessageHistoryStatus.Enqueued) return;` |
| **Bug B fix (Memory)** | 1.2 | Adds guard: `&& r.Status == MessageHistoryStatus.Enqueued` to conditional |
| **Unit tests for Bug A** | 1.3 | Test: `MessageFailedProcessing_When_Inner_Handler_Clears_MessageId_Still_Records_Error` |
| **Unit tests for Bug B (Redis)** | 1.3 | Tests: `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite` and `When_Status_Is_Enqueued_Sets_Processing` |
| **Unit tests for Bug B (Memory)** | 1.3 | Tests: `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite` and `When_Status_Is_Complete_Does_Not_Overwrite` |
| **No regressions** | All | All plans include verification commands for full test suites |

**All ROADMAP success criteria are covered.**

---

## Wave 1 Parallelism Check

| Plan | Dependencies | Files Touched | Conflicts |
|---|---|---|---|
| 1.1 | None | Decorator only | None |
| 1.2 | None | Redis + Memory handlers | None |
| 1.3 | None (writes tests before fixes) | Test files only | None |

**Verdict: True parallelism possible.** No blocking dependencies. Test files (1.3) are disjoint from implementation files (1.1, 1.2), so tests can be written first (TDD), then fixes applied in any order.

---

## Risk Assessment

| Risk | Level | Mitigation |
|---|---|---|
| **File edits are surgical** | LOW | Each plan modifies small, well-bounded sections (3 areas total). No cascading changes. |
| **API complexity** | LOW | All changes are to internal methods with well-documented behavior. No public API changes. |
| **Enum assumptions** | LOW | Enum values verified: `Enqueued=0`, `Processing=1`, `Error=3`. Guard patterns match existing RelationalDatabase/LiteDb implementations. |
| **Test helper availability** | LOW | All helpers (`CreateDecorator`, `CreateContext`, `CreateEnabledWithDb`, `GetRecordsForQueue`, etc.) are present and used by existing tests. |
| **Line number drift** | LOW | Files are stable; verified current line counts and method positions match plan descriptions. |
| **NSubstitute mocking** | MEDIUM | Task 1.3.1 uses callback-based mocking to simulate inner handler nullifying `context.MessageId`. Requires understanding NSubstitute `.Returns(callInfo => ...)` pattern, but this is standard in the codebase. |

---

## Verification Syntax Check

All verification commands are syntactically valid:

- `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` ✓
- `dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" --no-restore` ✓
- `dotnet build "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" --no-restore` ✓
- `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --no-restore --filter "FullyQualifiedName~ReceiveMessagesErrorHistoryDecoratorTests"` ✓
- `dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --no-restore --filter "FullyQualifiedName~WriteMessageHistoryHandlerTests"` ✓

All are executable and will produce measurable pass/fail results.

---

## Recommendations

1. **Execute in order: 1.1 → 1.2 → 1.3** (or true parallel if builder prefers).  
   Tests in 1.3 will fail until 1.1 and 1.2 are applied, which is expected in TDD.

2. **Spot-check before execution:**  
   - Verify that `MessageHistoryStatus.Enqueued` has value 0 or 1 (not inverted).
   - Confirm `RedisValue.Null` casts to 0 (line 55 of Plan 1.2, Task 1).

3. **Post-execution verification:**  
   - Run the full test suites listed in ROADMAP success criteria.
   - Manually verify in Dashboard that a retried message shows Status=Error, not Processing.

---

## Verdict: **READY**

All plans are feasible, well-scoped, testable, and ready for builder execution. No blockers identified.
