---
phase: fix-history-error-status
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - Capture messageId before delegating to inner error handler
  - RecordError is called with the correct messageId even when inner handler clears context
files_touched:
  - Source/DotNetWorkQueue/History/Decorator/ReceiveMessagesErrorHistoryDecorator.cs
tdd: false
---

# Plan 1.1: Fix ReceiveMessagesErrorHistoryDecorator -- capture messageId before delegation (Bug A)

## Context

`ReceiveMessagesErrorHistoryDecorator.MessageFailedProcessing()` delegates to `_handler.MessageFailedProcessing()` on line 44 **before** reading `context.MessageId` on line 45. The inner handler (e.g., `ReceiveErrorMessage`, `RedisQueueReceiveMessagesError`) calls `context.SetMessageAndHeaders(null, ...)` which sets `context.MessageId` to null. The subsequent guard `context.MessageId != null && context.MessageId.HasValue` evaluates to false, so `RecordError` is never called for terminal errors.

The fix: capture the messageId and its string value **before** calling the inner handler, then use the captured values in the history-recording block.

## Dependencies

None. This plan touches only the decorator file; no overlap with Plan 1.2 or 1.3.

## Tasks

### Task 1: Capture messageId before delegating to inner handler

**Files:** `Source/DotNetWorkQueue/History/Decorator/ReceiveMessagesErrorHistoryDecorator.cs`
**Action:** modify

**Description:**

In the `MessageFailedProcessing` method (lines 42-61), make these changes:

1. **Before line 44** (the `_handler.MessageFailedProcessing` call), add two local variables that capture the messageId while it is still valid:

```csharp
// Capture messageId before delegation -- inner handler may clear context.MessageId via SetMessageAndHeaders(null, ...)
var messageId = context.MessageId;
var hasMessageId = messageId != null && messageId.HasValue;
var messageIdValue = hasMessageId ? messageId.Id.Value.ToString() : null;
```

2. **Replace the existing guard on line 45** from:
```csharp
if (_options.EnableHistory && _options.HistoryOptions.TrackError && context.MessageId != null && context.MessageId.HasValue)
```
to:
```csharp
if (_options.EnableHistory && _options.HistoryOptions.TrackError && hasMessageId)
```

3. **Replace the RecordError call on line 53** from:
```csharp
_history.RecordError(context.MessageId.Id.Value.ToString(), exceptionText);
```
to:
```csharp
_history.RecordError(messageIdValue, exceptionText);
```

4. **Replace the log warning on line 57** from:
```csharp
_log.LogWarning(ex, "Failed to record history for error of message {MessageId}", context.MessageId.Id.Value);
```
to:
```csharp
_log.LogWarning(ex, "Failed to record history for error of message {MessageId}", messageIdValue);
```

The final method body should be:

```csharp
public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
{
    // Capture messageId before delegation -- inner handler may clear context.MessageId via SetMessageAndHeaders(null, ...)
    var messageId = context.MessageId;
    var hasMessageId = messageId != null && messageId.HasValue;
    var messageIdValue = hasMessageId ? messageId.Id.Value.ToString() : null;

    var result = _handler.MessageFailedProcessing(message, context, exception);
    if (_options.EnableHistory && _options.HistoryOptions.TrackError && hasMessageId)
    {
        try
        {
            var exceptionText = exception?.ToString();
            if (exceptionText != null && exceptionText.Length > _options.HistoryOptions.MaxExceptionLength)
                exceptionText = exceptionText.Substring(0, _options.HistoryOptions.MaxExceptionLength);

            _history.RecordError(messageIdValue, exceptionText);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to record history for error of message {MessageId}", messageIdValue);
        }
    }
    return result;
}
```

**Acceptance Criteria:**
- `messageIdValue` is captured on a line **before** `_handler.MessageFailedProcessing()` is called
- The guard condition uses `hasMessageId` (the pre-captured boolean), not `context.MessageId`
- `RecordError` is called with `messageIdValue` (the pre-captured string), not `context.MessageId.Id.Value.ToString()`
- The log warning uses `messageIdValue`, not `context.MessageId.Id.Value`
- No other behavioral changes to the method

## Verification

```bash
dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" --no-restore
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --no-restore --filter "FullyQualifiedName~ReceiveMessagesErrorHistoryDecoratorTests"
```

All existing tests must pass. The new test in Plan 1.3 will verify the bug-fix scenario (inner handler clears messageId).
