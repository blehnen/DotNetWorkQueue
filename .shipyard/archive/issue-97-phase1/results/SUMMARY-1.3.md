# SUMMARY-1.3: Regression Tests for Bug A and Bug B

## Status: COMPLETE

## Tasks Completed

### Task 1: Bug A regression test (decorator messageId capture)
Added `MessageFailedProcessing_When_Inner_Handler_Clears_MessageId_Still_Records_Error` to
`Source/DotNetWorkQueue.Tests/History/Decorator/ReceiveMessagesErrorHistoryDecoratorTests.cs`.

The test configures the inner handler to clear `context.MessageId` (set it to null) as a side
effect of `MessageFailedProcessing`, simulating how `ReceiveErrorMessage` calls
`SetMessageAndHeaders(null, ...)`. It then asserts that `history.RecordError("42", ...)` was
still called with the original string value — proving the decorator captured the id before
delegating to the inner handler.

### Task 2: Bug B Redis regression tests (RecordProcessingStart guard)
Added two tests to `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs`:

- `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite` — uses `CreateEnabledWithDb`
  to inject a mock IDatabase returning `MessageHistoryStatus.Error` from `HashGet`, then asserts
  `HashSet` with `Status=Processing` is never called.
- `RecordProcessingStart_When_Status_Is_Enqueued_Sets_Processing` — same setup but HashGet
  returns `MessageHistoryStatus.Enqueued`, asserts `HashSet` with `Status=Processing` is called.

Required adding `using DotNetWorkQueue.Configuration;` (where `MessageHistoryStatus` lives) to
the Redis test file — it was not previously imported.

### Task 3: Bug B Memory regression tests (RecordProcessingStart guard)
Added two tests to `Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs`:

- `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite` — drives through the full
  Enqueue→ProcessingStart→Error lifecycle, then calls `RecordProcessingStart` again and asserts
  status remains `Error`.
- `RecordProcessingStart_When_Status_Is_Complete_Does_Not_Overwrite` — same pattern ending in
  `Complete`.

## Files Modified

- `Source/DotNetWorkQueue.Tests/History/Decorator/ReceiveMessagesErrorHistoryDecoratorTests.cs` — 1 test added
- `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs` — 2 tests added, 1 using directive added
- `Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs` — 2 tests added

## Decisions Made

- Added `using DotNetWorkQueue.Configuration;` to the Redis test file inline as a required
  compilation fix. The plan referenced `MessageHistoryStatus` without noting the namespace was
  not already imported in that file. This is a trivial using addition, not an architectural change.
- The Memory Bug B tests exercise the real in-memory store (not mocks), so they serve as
  integration-style regression tests against the actual `WriteMessageHistoryHandler` implementation.

## Issues Encountered

- Initial Redis build failed: `MessageHistoryStatus` not in scope. Fixed by adding the missing
  `using DotNetWorkQueue.Configuration;` directive.
- Commit failed with `cannot lock ref 'HEAD'` — a parallel PLAN-1.1/1.2 agent had committed
  ahead on the same branch. Resolved by updating the HEAD ref to the current tip and recommitting.

## Verification Results

All tests passed on net10.0 (net48 skipped — no mono in WSL environment, consistent with project norms):

```
ReceiveMessagesErrorHistoryDecoratorTests: Passed 6/6
WriteMessageHistoryHandlerTests (Redis):   Passed 21/21
WriteMessageHistoryHandlerTests (Memory):  Passed 31/31
```

The new tests pass because PLAN-1.1 and PLAN-1.2 fixes were already applied to this branch
before PLAN-1.3 ran. This is the expected outcome: parallel execution meant fixes landed first.

## Commit

`3ddc78a2 shipyard(phase-1): add regression tests for Bug A and Bug B`
