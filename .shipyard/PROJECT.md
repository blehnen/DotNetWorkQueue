# Project: Redis History Bug Fixes

## Description

Fix two related bugs in the Redis transport's history handling. Issue #104: `RecordComplete` and `RecordError` in `WriteMessageHistoryHandler` perform unchecked `(long)` casts on `RedisValue` that throw when the hash is absent. Issue #103: `PurgeMessageHistoryHandler.Purge()` has broken logic that purges active (Enqueued/Processing) records and throws on missing hashes.

Both bugs are in the Redis transport only. No other transports are affected.

## Goals

1. Guard all Redis `HashGet` casts in `WriteMessageHistoryHandler.RecordComplete` and `RecordError` with `.HasValue` checks, defaulting to `0L` when absent
2. Fix `PurgeMessageHistoryHandler.Purge()` to only remove terminal-state records (Complete, Error, Deleted, Expired) whose `CompletedUtc` is older than the cutoff
3. Add `protected virtual GetDb()` test seam to `PurgeMessageHistoryHandler` (matching `WriteMessageHistoryHandler` pattern)
4. Add unit tests for all fixes

## Non-Goals

- Changing any other transport's history handlers
- Modifying the Dashboard UI or API
- Adding integration tests (unit tests with mocked IDatabase are sufficient)
- Changing the history data model or Redis key structure

## Requirements

### #104 — HasValue guard on StartedUtc
- `RecordComplete`: replace `(long)db.HashGet(...)` with `HasValue` check, default `0L`
- `RecordError`: same fix
- Unit tests: verify both methods handle missing hash gracefully (return `0L` duration)

### #103 — Purge logic fix
- Add `protected virtual GetDb()` seam to `PurgeMessageHistoryHandler`
- Add `.HasValue` guard on `CompletedUtc` HashGet — skip records where hash was pruned
- Read `Status` field; only purge when status is terminal (Complete, Error, Deleted, Expired)
- Replace broken condition with: Status is terminal AND CompletedUtc > 0 AND CompletedUtc < cutoff
- Unit tests: purge skips Processing records, purges old Complete records, handles missing hashes

## Non-Functional Requirements

- No performance regression in purge operations
- Consistent error handling pattern with the `RecordProcessingStart` fix from PR #105

## Success Criteria

1. `RecordComplete` and `RecordError` do not throw when Redis hash is absent
2. Purge only removes terminal-state records older than the cutoff
3. Purge does not throw when a hash is missing between index scan and field read
4. All existing Redis unit tests pass
5. New unit tests cover all fix scenarios
6. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds

## Constraints

- Fix must be in the Redis transport only
- Must follow existing `HasValue` guard pattern from PR #105
- Must follow `GetDb()` test seam pattern from `WriteMessageHistoryHandler`
