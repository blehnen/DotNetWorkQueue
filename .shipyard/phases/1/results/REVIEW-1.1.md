# Review: Plan 1.1

## Verdict: PASS

---

## Stage 1: Spec Compliance

**Verdict: PASS**

### Task 1: Capture messageId before delegating to inner handler

- Status: PASS
- Evidence: `Source/DotNetWorkQueue/History/Decorator/ReceiveMessagesErrorHistoryDecorator.cs` lines 45-47 capture three locals (`messageId`, `hasMessageId`, `messageIdValue`) before the call to `_handler.MessageFailedProcessing()` on line 49. All three downstream uses of `context.MessageId` in the original code (the guard condition, the `RecordError` call, and the `LogWarning` call) have been replaced with the pre-captured locals.
- Notes: The fix exactly matches the plan's stated action. The done criterion — "RecordError is called with the correct messageId even when inner handler clears context" — is directly exercised by test `MessageFailedProcessing_When_Inner_Handler_Clears_MessageId_Still_Records_Error`, which simulates the inner handler returning `null` for `context.MessageId` and then asserts `RecordError("42", ...)` is still received.

---

## Stage 2: Code Quality

### Critical

None.

### Minor

- **`messageIdValue` is `null`-safe but the `LogWarning` call passes it as a structured-log parameter without a null guard.**
  File: `Source/DotNetWorkQueue/History/Decorator/ReceiveMessagesErrorHistoryDecorator.cs`, line 62.
  If `hasMessageId` is true but `messageId.Id.Value.ToString()` somehow returns `null` (implementation-defined), the structured log message emits a null literal for `{MessageId}`. This is not reachable under normal conditions because `hasMessageId` gates `ToString()`, but the string was computed independently. The risk is negligible in practice; logging a null parameter is non-fatal. No immediate action required.

- **Test `MessageFailedProcessing_When_Enabled_Records_Error` does not assert the exact messageId string passed to `RecordError`.**
  File: `Source/DotNetWorkQueue.Tests/History/Decorator/ReceiveMessagesErrorHistoryDecoratorTests.cs`, line 39.
  It uses `Arg.Any<string>()` for the first parameter. The regression test at line 107 covers the exact value (`"42"`), so the bug scenario is tested, but the "happy path" test does not pin the messageId. This is a minor coverage gap — a refactor that accidentally passed the wrong id string could still make the happy-path test pass.
  Remediation: Change line 39 to `history.Received(1).RecordError("42", Arg.Is<string>(s => s.Contains("test error")));`.

### Positive

- The comment on line 44 precisely documents the non-obvious reason for the early capture, which is exactly what future maintainers need to understand why `context.MessageId` is not read in-place.
- A dedicated regression test (`MessageFailedProcessing_When_Inner_Handler_Clears_MessageId_Still_Records_Error`) tests the exact bug scenario described in the plan, including the simulated inner-handler side-effect and an exact assertion on the captured value.
- The fix is minimal and surgical: three added lines, three substituted references. No other behavior was altered.
- LGPL header is present and unchanged.
- No extra features were added beyond the spec.
