---
phase: fix-history-error-status
plan: "1.3"
wave: 1
dependencies: []
must_haves:
  - Test that decorator captures messageId before inner handler clears it (Bug A regression test)
  - Test that Redis RecordProcessingStart does not overwrite Error status (Bug B regression test)
  - Test that Memory RecordProcessingStart does not overwrite Error status (Bug B regression test)
files_touched:
  - Source/DotNetWorkQueue.Tests/History/Decorator/ReceiveMessagesErrorHistoryDecoratorTests.cs
  - Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs
  - Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs
tdd: true
---

# Plan 1.3: Unit tests for Bug A and Bug B regression scenarios

## Context

Plans 1.1 and 1.2 fix the two bugs. This plan adds targeted regression tests that would **fail on the old code** and **pass on the fixed code**. All three plans are in Wave 1 because the test files are disjoint from the implementation files (tests can be written first in TDD style, then the fixes applied to make them pass).

## Dependencies

None for writing the tests. Tests will only **pass** after Plans 1.1 and 1.2 are applied. TDD workflow: write tests first (red), apply fixes (green).

## Tasks

### Task 1: Add regression test for Bug A -- decorator captures messageId before inner handler clears it

**Files:** `Source/DotNetWorkQueue.Tests/History/Decorator/ReceiveMessagesErrorHistoryDecoratorTests.cs`
**Action:** modify

**Description:**

Add a new test method to the existing `ReceiveMessagesErrorHistoryDecoratorTests` class. This test simulates the bug scenario: the inner handler's `MessageFailedProcessing` clears `context.MessageId` (sets it to null), and verifies that `RecordError` is still called with the original messageId.

Add this test method after the existing `MessageFailedProcessing_When_History_Throws_Exception_Is_Swallowed` test (after line 87):

```csharp
[TestMethod]
public void MessageFailedProcessing_When_Inner_Handler_Clears_MessageId_Still_Records_Error()
{
    var (decorator, inner, history, _, _) = CreateDecorator(enabled: true, trackError: true);
    var context = CreateContext();
    var message = Substitute.For<IReceivedMessageInternal>();
    var exception = new InvalidOperationException("test error");

    // Simulate inner handler clearing context.MessageId (as ReceiveErrorMessage does via SetMessageAndHeaders(null, ...))
    inner.MessageFailedProcessing(message, context, exception).Returns(callInfo =>
    {
        context.MessageId.Returns((IMessageId)null);
        return ReceiveMessagesErrorResult.Error;
    });

    decorator.MessageFailedProcessing(message, context, exception);

    // RecordError must still be called with the ORIGINAL messageId value "42"
    history.Received(1).RecordError("42", Arg.Is<string>(s => s.Contains("test error")));
}
```

Key details:
- The `CreateContext` helper (line 89) sets up `context.MessageId.Id.Value` to return `42L`, so `ToString()` yields `"42"`.
- The inner handler's `.Returns()` callback changes `context.MessageId` to return null, simulating what `SetMessageAndHeaders(null, ...)` does.
- The assertion checks that `RecordError` was called with the string `"42"` (the pre-captured value), proving the decorator captured it before delegation.
- **On the old code**: this test **fails** because the decorator reads `context.MessageId` after the inner handler nullifies it, so the guard `context.MessageId != null` is false and `RecordError` is never called.
- **On the fixed code**: this test **passes** because the decorator captures the messageId before delegation.

**Acceptance Criteria:**
- Test is named `MessageFailedProcessing_When_Inner_Handler_Clears_MessageId_Still_Records_Error`
- Test verifies `history.Received(1).RecordError("42", ...)` is called
- Test fails on unfixed code, passes on fixed code

### Task 2: Add regression test for Bug B -- Redis RecordProcessingStart does not overwrite Error status

**Files:** `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs`
**Action:** modify

**Description:**

Add a new test method to the existing `WriteMessageHistoryHandlerTests` class. This test uses the existing `TestableWriteMessageHistoryHandler` and `CreateEnabledWithDb` patterns already in the file (lines 16-27, 233-249).

Add this test method after the existing `RecordError_WithoutStartedUtc_WritesDurationZero` test (after line 281):

```csharp
[TestMethod]
public void RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite()
{
    var (handler, db) = CreateEnabledWithDb();

    // Simulate a record already in Error status
    db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
        .Returns((RedisValue)(int)MessageHistoryStatus.Error);

    handler.RecordProcessingStart("q1");

    // HashSet should NOT have been called (no status overwrite)
    db.DidNotReceive().HashSet(
        Arg.Any<RedisKey>(),
        Arg.Is<HashEntry[]>(entries => ContainsEntry(entries, "Status", (int)MessageHistoryStatus.Processing)),
        Arg.Any<CommandFlags>());
}
```

Note: The existing `CreateEnabledWithDb` helper (line 233) already sets up `db.HashGet` to return `0L` by default. The test overrides the specific `"Status"` field call to return `Error`. The `ContainsEntry` helper already exists at line 283.

Also add a complementary positive test to confirm the guard allows the transition from Enqueued:

```csharp
[TestMethod]
public void RecordProcessingStart_When_Status_Is_Enqueued_Sets_Processing()
{
    var (handler, db) = CreateEnabledWithDb();

    // Simulate a record in Enqueued status
    db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
        .Returns((RedisValue)(int)MessageHistoryStatus.Enqueued);

    handler.RecordProcessingStart("q1");

    // HashSet SHOULD have been called with Status=Processing
    db.Received().HashSet(
        Arg.Any<RedisKey>(),
        Arg.Is<HashEntry[]>(entries => ContainsEntry(entries, "Status", (int)MessageHistoryStatus.Processing)),
        Arg.Any<CommandFlags>());
}
```

**Acceptance Criteria:**
- `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite` verifies `db.DidNotReceive().HashSet(...)` for Processing status
- `RecordProcessingStart_When_Status_Is_Enqueued_Sets_Processing` verifies `db.Received().HashSet(...)` for Processing status
- Both tests use the existing `CreateEnabledWithDb` and `ContainsEntry` helpers

### Task 3: Add regression test for Bug B -- Memory RecordProcessingStart does not overwrite Error status

**Files:** `Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs`
**Action:** modify

**Description:**

Add a new test method to the existing `WriteMessageHistoryHandlerTests` class. This test uses the existing `CreateHandlerWithKey` helper and the real in-memory data store.

Add this test method after the existing `RecordProcessingStart_RecordNotFound_DoesNotThrow` test (after line 111):

```csharp
[TestMethod]
public void RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite()
{
    var (handler, key) = CreateHandlerWithKey(enableHistory: true);

    // Set up a record that has already been marked as Error
    handler.RecordEnqueue("q1", "c1", null, null, null, null);
    handler.RecordProcessingStart("q1"); // Enqueued -> Processing
    handler.RecordError("q1", "fatal error"); // Processing -> Error

    // Now attempt to start processing again (simulates re-dequeue for error queue)
    handler.RecordProcessingStart("q1");

    // Status must remain Error, not be overwritten to Processing
    var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
    var record = records["q1"];
    Assert.AreEqual(MessageHistoryStatus.Error, record.Status);
}
```

Also add a complementary test for the Complete status guard:

```csharp
[TestMethod]
public void RecordProcessingStart_When_Status_Is_Complete_Does_Not_Overwrite()
{
    var (handler, key) = CreateHandlerWithKey(enableHistory: true);

    handler.RecordEnqueue("q1", "c1", null, null, null, null);
    handler.RecordProcessingStart("q1");
    handler.RecordComplete("q1"); // Processing -> Complete

    // Attempt to start processing again
    handler.RecordProcessingStart("q1");

    // Status must remain Complete
    var records = WriteMessageHistoryHandler.GetRecordsForQueue(key);
    var record = records["q1"];
    Assert.AreEqual(MessageHistoryStatus.Complete, record.Status);
}
```

Key details:
- These tests exercise the full lifecycle through the real in-memory data structures, making them true integration-style unit tests.
- **On the old code**: the first test **fails** because `RecordProcessingStart` unconditionally sets Status=Processing, overwriting the Error status.
- **On the fixed code**: the test **passes** because the guard `r.Status == MessageHistoryStatus.Enqueued` prevents the overwrite.

**Acceptance Criteria:**
- `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite` asserts `MessageHistoryStatus.Error` is preserved
- `RecordProcessingStart_When_Status_Is_Complete_Does_Not_Overwrite` asserts `MessageHistoryStatus.Complete` is preserved
- Both tests use the existing `CreateHandlerWithKey` helper pattern

## Verification

After Plans 1.1 and 1.2 are applied:

```bash
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --no-restore --filter "FullyQualifiedName~ReceiveMessagesErrorHistoryDecoratorTests"
dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --no-restore --filter "FullyQualifiedName~WriteMessageHistoryHandlerTests"
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --no-restore --filter "FullyQualifiedName~WriteMessageHistoryHandlerTests"
```

All tests (existing + new) must pass. The new tests must fail if you revert Plans 1.1 or 1.2.
