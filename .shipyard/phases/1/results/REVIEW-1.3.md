# REVIEW-1.3: Regression Tests for Bug A and Bug B

Reviewer: Claude Sonnet 4.6
Commit: 3ddc78a2
Branch: fix_history_for_error_messages
Date: 2026-04-06

---

## Stage 1: Spec Compliance

**Verdict:** PASS

### Task 1: Bug A regression test -- decorator captures messageId before inner handler clears it

- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Tests/History/Decorator/ReceiveMessagesErrorHistoryDecoratorTests.cs`, lines 89-108. Test method `MessageFailedProcessing_When_Inner_Handler_Clears_MessageId_Still_Records_Error` is present under `[TestMethod]`.
- Notes: The test correctly simulates the race by using an NSubstitute `Returns` callback that sets `context.MessageId.Returns((IMessageId)null)` as a side effect of `inner.MessageFailedProcessing`. It then asserts `history.Received(1).RecordError("42", ...)`, pinning the expected value to the string "42" which matches the `setting.Value.Returns(42L)` in `CreateContext()`. This will fail on unfixed code (where messageId is read after delegation) and pass on code where capture happens before delegation -- which is exactly what `ReceiveMessagesErrorHistoryDecorator.cs` line 45 (`var messageId = context.MessageId;`) implements.

### Task 2: Bug B Redis regression tests -- RecordProcessingStart guard

- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs`, lines 284-314. Both `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite` and `RecordProcessingStart_When_Status_Is_Enqueued_Sets_Processing` are present under `[TestMethod]`.
- Notes: Tests use `CreateEnabledWithDb()`, which returns a `TestableWriteMessageHistoryHandler` with the `GetDb()` seam injecting a fully-controllable `IDatabase` mock. Each test overrides the default `HashGet` return with the specific status being tested. The negative test uses `db.DidNotReceive().HashSet(...)` filtered by a `ContainsEntry` predicate checking `Status == Processing`. The positive test uses `db.Received().HashSet(...)` with the same predicate. The `using DotNetWorkQueue.Configuration;` directive was correctly added at line 2.

### Task 3: Bug B Memory regression tests -- RecordProcessingStart guard

- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs`, lines 388-419. Both `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite` and `RecordProcessingStart_When_Status_Is_Complete_Does_Not_Overwrite` are present under `[TestMethod]`.
- Notes: Tests drive the real in-memory store through the full lifecycle (Enqueue -> ProcessingStart -> Error/Complete) then call `RecordProcessingStart` a second time and assert the terminal status is preserved. `CreateHandlerWithKey` uses unique GUIDs for queue name and connection string, preventing static-dictionary collisions between parallel test runs. The plan required Error and Complete statuses both be covered; both are present.

---

## Stage 2: Code Quality

### Critical

None.

### Important

- **Bug A test: `context.MessageId` re-stub may not take effect if NSubstitute caches the return** -- `Source/DotNetWorkQueue.Tests/History/Decorator/ReceiveMessagesErrorHistoryDecoratorTests.cs`, lines 98-102.

  Inside the `Returns` callback the test does `context.MessageId.Returns((IMessageId)null)`. NSubstitute property stubs are recorded on the substitute itself and re-evaluated on every access, so this works correctly at runtime. However, there is a subtlety: the `context.MessageId` expression inside the callback accesses the substitute's property to set up the new return -- but because `context` is a substitute and `MessageId` was already stubbed in `CreateContext()`, the `.Returns(null)` call inside the callback re-targets the same property stub. This is valid NSubstitute usage and the builder confirmed the test passes. The risk is that a future refactor of `CreateContext()` to return a concrete `IMessageContext` wrapper could silently break this interaction. Consider adding a brief comment noting this relies on `IMessageContext` being a substitute, not a concrete type.
  - Remediation: Add a one-line comment: `// context must remain an NSubstitute proxy for this re-stub to work`.

- **Redis test: guard condition on `HashGet` field name is implicit** -- `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs`, lines 289 and 305.

  The test stubs `db.HashGet(..., Arg.Is<RedisValue>("Status"), ...)`. If the production code changes the Redis field name from `"Status"` to anything else (e.g., `"status"`, `"MessageStatus"`), the stub silently falls through to the default `HashGet` return of `0L` set by `CreateEnabledWithDb()`. This means `RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite` would still pass (since `0L` != `Error`) even if the guard condition is broken. The test is not fully hermetic against field name drift.
  - Remediation: Extract the Redis field name into a named constant (e.g., `RedisHistoryFields.Status`) in the production code and reference it in the test stub. Alternatively, add an assertion that `HashGet` was called with the expected field name to confirm the production code is reading the right field.

### Suggestions

- **Memory Bug B tests do not assert `RetryCount` is unmodified** -- `Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs`, lines 388-419.

  The tests only assert `record.Status`. Adding `Assert.AreEqual(0, record.RetryCount)` (or the expected count from the preceding rollback-free path) would confirm that `RecordProcessingStart` does not side-effect other fields when it short-circuits on a terminal status.

- **Redis "Enqueued sets Processing" test does not assert `StartedUtc` or timestamp fields** -- `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs`, lines 300-313.

  The positive-path test only asserts that `Status=Processing` appears in the `HashSet` call. Asserting that `StartedUtc` is also present in the entries would tighten the contract against partial writes.

- **No test for `RecordProcessingStart` when status is `Processing` (already in-flight)** -- both Redis and Memory transports.

  The plan covers Error and Enqueued cases. There is no regression test for what happens when `RecordProcessingStart` is called while status is already `Processing` (concurrent dequeue). This is a less common path but documenting the expected behavior via a test would improve confidence.

---

## Summary

**Verdict:** APPROVE

All three plan tasks are correctly implemented. The tests are structured appropriately, follow existing NSubstitute and MSTest conventions, use proper isolation (mocked `IDatabase` seam for Redis, real in-memory store for Memory), and exercise the exact scenario each bug describes. The two Important findings are not blockers -- one is a documentation gap in a valid pattern, the other is a test-hermetics concern about field name drift that is low-risk given the field name is stable. No regressions introduced.

Critical: 0 | Important: 2 | Suggestions: 3
