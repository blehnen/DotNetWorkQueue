# Project: Fix History Duration for Fast-Completing Messages

## Description

Fix GitHub issue #94: some history entries show no duration (or inconsistent values) for messages that complete in under 1 millisecond. This is a race condition where `RecordComplete` reads `StartedUtc` from the database before `RecordProcessingStart` has persisted it. The fix is cosmetic — normalize the backend to store `0` when duration is unmeasurable, and display "< 1 ms" in the Dashboard UI.

## Goals

1. Normalize all transports to store `DurationMs = 0` when a message completes but `StartedUtc` was not yet persisted (race condition on fast messages)
2. Display "< 1 ms" in the Dashboard UI when `DurationMs == 0` and status is Complete
3. Fix the `0L` vs `null` inconsistency across transports for this case

## Non-Goals

- Structural fix (passing start timestamp in-memory through the processing pipeline) — overkill for a display-only issue
- Changing metrics, OpenTelemetry, or any non-history code — DurationMs is purely for history display
- Redesigning the history recording pipeline
- Fixing any other history-related issues

## Requirements

### Backend (Transport History Writers)

- RelationalDatabase transports (SqlServer, PostgreSQL, SQLite): ensure `DurationMs = 0` (not null) when `StartedUtc` is missing but message status is Complete
- Redis transport: same normalization to `0`
- LiteDb transport: verify behavior (works in-memory, may not race, but should be consistent)
- Memory transport: normalize from `(long?)null` to `0` for consistency

### Dashboard UI

- History table: display "< 1 ms" when `DurationMs == 0` and status indicates completion
- No changes to other history columns or the history API response shape

## Non-Functional Requirements

- All existing unit tests must continue to pass
- All existing Dashboard API integration tests must continue to pass
- No changes to the `MessageHistoryRecord` class shape or the Dashboard API response contract

## Success Criteria

1. A fast-completing message on any transport shows "< 1 ms" in the Dashboard history view instead of blank/0
2. All transports consistently store `DurationMs = 0` for sub-millisecond completions
3. `dotnet build "Source/DotNetWorkQueueNoTests.sln"` succeeds
4. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"` passes
5. `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj"` passes
6. `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory"` passes

## Constraints

- DurationMs is purely for history display — does not feed into metrics, OpenTelemetry, or aggregation queries
- The race condition only occurs for sub-millisecond messages, so `0` is a correct approximation
- Must not change the `MessageHistoryRecord` property types or Dashboard API response shape
