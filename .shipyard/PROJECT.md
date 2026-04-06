# Project: Fix History Status for Errored Messages

## Description

Fix GitHub issue #97: Dashboard history shows `Status=Processing` for messages that exhausted retries and moved to the error queue. The root cause is that `RecordError` is never called on the retry-exhausted path — `ReceiveErrorMessage<T>.MessageFailedProcessing()` moves the record directly without going through the history decorator. A secondary issue is that `RecordProcessingStart` unconditionally resets Status to Processing on each retry, overwriting prior error info.

This affects all transports (Redis, SqlServer, PostgreSQL, SQLite, LiteDb, Memory), not just Redis where it was first observed.

## Goals

1. Ensure `RecordError` is called before a message moves to the error queue on retry exhaustion, so the history row shows `Status=Error` with the exception text
2. Prevent `RecordProcessingStart` from overwriting `Status=Error` back to `Processing` on retries — preserve the error state in history
3. Add unit tests covering the retry-exhausted history path

## Non-Goals

- Changing retry logic or poison message handling
- Modifying the Dashboard UI (it already renders Error status correctly)
- Changing the history API response shape or `MessageHistoryRecord` model
- Adding integration tests (unit tests on the decorator are sufficient)

## Requirements

### Decorator fix (terminal error)
- `ReceiveMessagesErrorHistoryDecorator` (or a new decorator) must call `RecordError` when `MessageFailedProcessing` results in a move-to-error-queue outcome
- The fix must work for all transports since they share `ReceiveErrorMessage<T>` from Transport.Shared

### Retry reset fix
- `RecordProcessingStart` should not overwrite `Status=Error` — either skip the status reset if status is already Error, or use a different approach
- This applies to all transport WriteMessageHistoryHandler implementations (Redis, Memory, RelationalDatabase)

### Tests
- Unit tests verifying that history records error status when retries are exhausted
- Unit tests verifying that RecordProcessingStart does not overwrite error status

## Non-Functional Requirements

- No changes to message processing performance
- No changes to the MessageHistoryRecord class shape

## Success Criteria

1. A message that exhausts retries shows `Status=Error` in Dashboard history (not `Processing`)
2. All existing unit tests pass
3. All existing Dashboard integration tests pass (Memory transport)
4. New unit tests cover both the terminal-error and retry-reset fixes
5. `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug` succeeds

## Constraints

- Fix must be in the decorator/shared layer, not per-transport
- Must not change public API surface
- Display-only bug — no changes to message processing pipelines
