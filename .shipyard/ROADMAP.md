# Roadmap: Dashboard API History Tests — Redis & LiteDb

## Overview

Add integration tests for the two transports missing history test coverage. Both test files follow the established `MemoryHistoryTests.cs` pattern. No production code changes.

## Dependency Graph

```
Phase 1: LiteDbHistoryTests | RedisHistoryTests  ──> Done
```

Single phase, two parallel plans. No inter-plan dependencies — each transport's tests are self-contained.

---

## Phase 1: Add History Integration Tests

- **Scope:** Two new test files in `DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/`
- **Dependencies:** None (test-only changes)
- **Risk:** Low — no production changes. Redis tests may need connection string tuning for Jenkins.

### Plan 1.1: LiteDbHistoryTests.cs

**File:** `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/LiteDbHistoryTests.cs` (new)

Two test classes:
- `LiteDbHistoryDisabledTests` (~4 tests): history endpoints return empty when disabled
- `LiteDbHistoryEnabledTests` (~11 tests): full history lifecycle with send + consume

Uses `LiteDbDashboardInit`, file-based connection string. Runs everywhere.

### Plan 1.2: RedisHistoryTests.cs

**File:** `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/RedisHistoryTests.cs` (new)

Two test classes:
- `RedisHistoryDisabledTests` (~4 tests): history endpoints return empty when disabled
- `RedisHistoryEnabledTests` (~11 tests): full history lifecycle with send + consume

Uses `RedisDashboardInit`, connection string from `connectionstring.txt`. Runs in Jenkins CI only.

### Success Criteria

1. `dotnet build "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj"`
2. `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~LiteDb"`
3. `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~RedisHistory"` (when Redis available)
4. All existing Dashboard integration tests pass
