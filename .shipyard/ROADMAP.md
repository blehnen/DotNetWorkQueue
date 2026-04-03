# Roadmap: Fix History Duration for Fast-Completing Messages

## Overview

GitHub issue #94: messages that complete in under 1 millisecond show no duration (blank or inconsistent values) in the Dashboard history view. This is caused by a race condition where `RecordComplete` reads `StartedUtc` from the database before `RecordProcessingStart` has persisted it, resulting in `DurationMs` being NULL instead of 0. The fix normalizes all transports to store `DurationMs = 0` when the duration is unmeasurable, and updates the Dashboard UI to display "< 1 ms" for that case.

This is a single-phase cosmetic fix. No pipeline, metrics, or architectural changes are involved.

## Dependency Graph

```
Phase 1 (Backend normalization + UI display fix) ──> Done
```

Single phase, no inter-phase dependencies.

---

## Phase 1: Normalize DurationMs Across Transports and Fix Dashboard Display

- **Scope:** 100% of project. Two vertical slices that share no file dependencies and can execute in parallel: (A) backend transport normalization and (B) Dashboard UI display fix. Both are small and self-contained.
- **Dependencies:** None
- **Risk:** Low -- cosmetic fix only. `DurationMs` is used exclusively for history display. No changes to message processing pipelines, metrics, OpenTelemetry, or API response shapes. The `DurationMs` property remains `long?` on all models; we only change what value gets stored and how `0` is rendered.

### Bug Analysis

| Transport | Write side | Read side | Fix needed |
|---|---|---|---|
| RelationalDatabase (SqlServer, PostgreSQL, SQLite) | `DurationMs` stays NULL -- second UPDATE has `WHERE StartedUtc IS NOT NULL` guard | Reads raw DB value (null if NULL) | Fix write: set `DurationMs = 0` when StartedUtc is NULL |
| Redis | Already stores `0L` when `startedTicks == 0` (correct) | **`DurationMs = durationMs > 0 ? durationMs : (long?)null`** converts `0` back to `null` | Fix read: stop converting `0` to `null` in `QueryMessageHistoryHandler.cs` |
| LiteDb | `DurationMs` defaults to `0` (long field) but only because it is not set | **`DurationMs = h.DurationMs > 0 ? h.DurationMs : (long?)null`** converts `0` back to `null` | Fix read: stop converting `0` to `null` in `QueryMessageHistoryHandler.cs`; make write explicit for clarity |
| Memory | `DurationMs` stays `null` (only set inside `if (r.StartedUtc.HasValue)`) | No conversion (in-memory) | Fix write: add `else r.DurationMs = 0` |

### Files Touched

**Backend -- write side (transport history writers):**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/WriteMessageHistoryHandler.cs` -- fix `RecordComplete` to set `DurationMs = 0` when StartedUtc is NULL
- `Source/DotNetWorkQueue/Transport/Memory/Basic/WriteMessageHistoryHandler.cs` -- fix `RecordComplete` to set `DurationMs = 0` when StartedUtc is null
- `Source/DotNetWorkQueue.Transport.LiteDB/Basic/WriteMessageHistoryHandler.cs` -- make `RecordComplete` explicitly set `DurationMs = 0` when StartedUtc is 0
- `Source/DotNetWorkQueue.Transport.Redis/Basic/WriteMessageHistoryHandler.cs` -- no write-side change needed (already stores `0L`)

**Backend -- read side (transport history query handlers):**
- `Source/DotNetWorkQueue.Transport.Redis/Basic/QueryMessageHistoryHandler.cs` (line 124) -- change `durationMs > 0 ? durationMs : (long?)null` to preserve `0` as a valid value (e.g., `durationMs > 0 ? durationMs : 0L`)
- `Source/DotNetWorkQueue.Transport.LiteDB/Basic/QueryMessageHistoryHandler.cs` (line 100) -- same fix: stop converting `0` to `null`

**Backend unit tests:**
- `Source/DotNetWorkQueue.Tests/Transport/Memory/Basic/WriteMessageHistoryHandlerTests.cs` -- update `RecordComplete_WithoutStarted_DurationIsNull` to assert `DurationMs == 0` instead of `null`; update `RecordError_WithoutStarted_DurationIsNull` similarly
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/WriteMessageHistoryHandlerTests.cs` -- no structural changes needed (mocked DB, does not verify DurationMs value)
- `Source/DotNetWorkQueue.Transport.LiteDb.Tests/Basic/WriteMessageHistoryHandlerTests.cs` -- verify existing tests still pass; may need updates if tests assert `null` for `DurationMs`
- `Source/DotNetWorkQueue.Transport.Redis.Tests/Basic/WriteMessageHistoryHandlerTests.cs` -- verify existing tests still pass; may need updates if tests assert `null` for `DurationMs`

**Dashboard UI:**
- `Source/DotNetWorkQueue.Dashboard.Ui/Components/Shared/HistoryTab.razor` -- update `FormatDuration` method: when `ms == 0`, return `"< 1 ms"` instead of `"0ms"`

### Success Criteria

1. **Build succeeds:**
   ```bash
   dotnet build "Source/DotNetWorkQueue.sln" -c Debug
   ```

2. **All affected unit tests pass:**
   ```bash
   dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"
   dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"
   dotnet test "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"
   dotnet test "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" --filter "FullyQualifiedName~WriteMessageHistoryHandler"
   ```

3. **Dashboard API tests pass (memory transport -- no external services):**
   ```bash
   dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory"
   ```

4. **Dashboard UI builds:**
   ```bash
   dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"
   ```

5. **Behavioral verification:** A fast-completing message on any transport stores `DurationMs = 0` (not null) and the Dashboard history table displays "< 1 ms" for that entry instead of blank or "0ms".
