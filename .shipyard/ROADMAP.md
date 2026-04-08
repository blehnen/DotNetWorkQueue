# Roadmap: Replace Schyntax with Cronos (issue #100)

## Overview

Replace the vendored Schyntax DLL (custom DSL, no NuGet package, no XML docs) with Cronos (MIT, zero dependencies, standard cron expressions) as the schedule parser. Add CronExpressionDescriptor for human-readable schedule descriptions in logging, dashboard, and API responses. This is a breaking change -- schedule strings change from Schyntax DSL to standard cron format, `IJobSchedule.Previous()` becomes nullable, and version bumps to 0.9.3.

**Total scope:** 2 interfaces, 3 implementations, 1 csproj + central package props, 3 transport init files, 2 test files, 3 doc files, 1 Lib directory deletion, optional dashboard enhancement.

## Dependency Graph

```
Phase 1  (Core: interface + implementation + csproj + NuGet)
   |
   +-----------+-----------+
   |           |           |
   v           v           v
Phase 2     Phase 3     Phase 4
(Transport  (Unit +     (Dashboard
 defaults)   integ       description
             tests)      enhancement)
   |           |           |
   +-----------+-----------+
               |
               v
          Phase 5
          (Cleanup + Docs + Version bump)
```

Phase 1 is the foundation -- the core library must compile with Cronos before anything else proceeds.
Phases 2, 3, and 4 depend on Phase 1 and can execute in parallel with each other.
Phase 5 depends on all prior phases (deletes Lib/, updates docs, bumps version to reflect final state).

---

## Phase 1: Core Library -- IJobSchedule, JobSchedule, csproj, and Configuration

**Risk:** HIGH -- every project in the solution depends on `IJobSchedule`. Changing the return type of `Previous()` to nullable is a breaking API change that affects `ScheduledJob.cs` catch-up logic. Getting the Cronos auto-detect wrong (5-field vs 6-field) breaks all scheduling.
**Scope:** ~30% of total work. Highest complexity -- new NuGet dependencies, interface change, implementation rewrite, null-safety fix in caller.
**Depends on:** Nothing.
**Strategy:** Fail fast. If `DotNetWorkQueueNoTests.sln` does not build after this phase, nothing else matters.

### What Changes

1. **`Source/Directory.Packages.props`** -- Add `<PackageVersion Include="Cronos" Version="..." />` and `<PackageVersion Include="CronExpressionDescriptor" Version="..." />` entries to the core dependencies section.
2. **`Source/DotNetWorkQueue/DotNetWorkQueue.csproj`** -- Remove both TFM-conditional `<Reference Include="Schyntax">` ItemGroups (net8.0 and net10.0). Remove the entire `<Target Name="IncludeVendoredDllsInPack">` block (which packs Schyntax DLLs into the nupkg). Add `<PackageReference Include="Cronos" />` and `<PackageReference Include="CronExpressionDescriptor" />` to the main `<ItemGroup>`.
3. **`Source/DotNetWorkQueue/IJobSchedule.cs`** -- Change return types of `Previous()` and `Previous(DateTimeOffset)` from `DateTimeOffset` to `DateTimeOffset?`. Add `string Description { get; }` property for human-readable cron description.
4. **`Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs`** -- Replace `using Schyntax;` with `using Cronos;` and `using CronExpressionDescriptor;`. Replace `Schedule _schedule` field with `CronExpression _expression` + `CronFormat _format` + stored `_originalText`. Constructor: count space-separated fields to auto-detect `CronFormat.Standard` (5) vs `CronFormat.IncludeSeconds` (6). `Next()` / `Next(DateTimeOffset)`: call `_expression.GetNextOccurrence(DateTimeOffset, TimeZoneInfo)`. `Previous()` / `Previous(DateTimeOffset)`: implement via `_expression.GetOccurrences(now - lookbackWindow, now).LastOrDefault()` returning `DateTimeOffset?`. `Description`: return `ExpressionDescriptor.GetDescription(originalText)`. Accept lookback window via constructor or configuration injection.
5. **`Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs`** (line 98) -- Null-check the result of `Schedule.Previous()`. If null, skip the catch-up logic and fall through to `Schedule.Next()`.
6. **`Source/DotNetWorkQueue/IHeartBeatConfiguration.cs`** (line 62) -- Update `<remarks>` doc comment from "schyntax format" to "standard cron format (5-field or 6-field with seconds)".
7. **Add `PreviousLookbackWindow` configuration** -- Add a `TimeSpan PreviousLookbackWindow` property (default 48h) to the job scheduler configuration. This could be on an existing config class or a new one injected into `JobSchedule`. Wire it through the DI registration in `JobSchedulerInit.cs` if needed.

### Files Touched

- `Source/Directory.Packages.props`
- `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`
- `Source/DotNetWorkQueue/IJobSchedule.cs`
- `Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs`
- `Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs`
- `Source/DotNetWorkQueue/IHeartBeatConfiguration.cs`
- `Source/DotNetWorkQueue/JobScheduler/JobSchedulerInit.cs` (if DI wiring changes needed for lookback config)
- `Source/DotNetWorkQueue/Configuration/` (if new config property added to existing class)

### Success Criteria

1. `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug` succeeds with 0 errors
2. `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release` succeeds with 0 errors, 0 warnings
3. `grep -r "Schyntax\|schyntax" Source/DotNetWorkQueue/ --include="*.cs" --include="*.csproj"` returns 0 matches
4. `IJobSchedule.Previous()` returns `DateTimeOffset?` (verified by interface inspection)
5. `IJobSchedule.Description` property exists
6. `ScheduledJob.cs` null-checks the `Previous()` result before using it

---

## Phase 2: Transport Heartbeat Default Strings

**Risk:** LOW -- mechanical string replacements in 3 transport init files. The values are equivalent (same intervals, different syntax). No logic changes.
**Scope:** ~10% of total work.
**Depends on:** Phase 1 (transport projects reference DotNetWorkQueue, which must compile with Cronos first).
**Parallel with:** Phases 3 and 4.

### What Changes

1. **`Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbMessageQueueInit.cs`** (line 330) -- Change `"sec(*%10)"` to `"*/10 * * * * *"` (6-field cron: every 10 seconds).
2. **`Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs`** (line 321) -- Change `"sec(*%10)"` to `"*/10 * * * * *"` (6-field cron: every 10 seconds).
3. **`Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalDatabaseMessageQueueInit.cs`** (line 144) -- Change `"min(*%2)"` to `"*/2 * * * *"` (5-field cron: every 2 minutes).

### Files Touched

- `Source/DotNetWorkQueue.Transport.LiteDB/Basic/LiteDbMessageQueueInit.cs`
- `Source/DotNetWorkQueue.Transport.Redis/Basic/RedisQueueInit.cs`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalDatabaseMessageQueueInit.cs`

### Success Criteria

1. `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug` succeeds with 0 errors
2. `grep -rn "sec(\*%\|min(\*%" Source/ --include="*.cs"` returns 0 matches (excluding bin/obj)
3. LiteDB default is `"*/10 * * * * *"`, Redis default is `"*/10 * * * * *"`, RelationalDatabase default is `"*/2 * * * *"`

---

## Phase 3: Unit Tests and Integration Test Strings

**Risk:** LOW -- string replacements only. No test logic changes. The cron equivalents produce identical scheduling behavior.
**Scope:** ~15% of total work.
**Depends on:** Phase 1 (test projects reference DotNetWorkQueue).
**Parallel with:** Phases 2 and 4.

### What Changes

1. **`Source/DotNetWorkQueue.Tests/Queue/HeartBeatWorkerTests.cs`** -- Replace `"sec(*%2)"` with `"*/2 * * * * *"` (lines 92, 110). Replace `"sec(*%59)"` with `"*/59 * * * * *"` (line 120).
2. **`Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/JobSchedulerTestsShared.cs`** -- Replace all 4 occurrences of `"min(*)"` with `"* * * * *"` (lines 40, 44, 100, 104).

### Files Touched

- `Source/DotNetWorkQueue.Tests/Queue/HeartBeatWorkerTests.cs`
- `Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/JobSchedulerTestsShared.cs`

### Success Criteria

1. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"` -- all tests pass
2. `grep -rn 'sec(\*%\|min(\*' Source/DotNetWorkQueue.Tests/ Source/DotNetWorkQueue.IntegrationTests.Shared/ --include="*.cs"` returns 0 matches
3. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds with 0 errors

---

## Phase 4: Dashboard CronExpressionDescriptor Integration (Optional Enhancement)

**Risk:** LOW -- additive change. No existing behavior is modified. Adds a human-readable description field to job API responses.
**Scope:** ~10% of total work.
**Depends on:** Phase 1 (CronExpressionDescriptor NuGet package added in Phase 1).
**Parallel with:** Phases 2 and 3.

### What Changes

1. **Determine integration surface** -- Identify where schedule descriptions should appear: API responses (`JobResponse`), logging output in `JobScheduler.cs`, dashboard UI if applicable.
2. **`Source/DotNetWorkQueue.Dashboard.Api/Models/JobResponse.cs`** -- Add `string ScheduleDescription` property for the human-readable cron description.
3. **`Source/DotNetWorkQueue.Dashboard.Api/Services/DashboardService.cs`** -- Populate `ScheduleDescription` when mapping job data to `JobResponse`.
4. **Logging in `JobScheduler.cs`** -- Where schedule strings are logged, append or replace with `IJobSchedule.Description` for human-readable output.

### Files Touched

- `Source/DotNetWorkQueue.Dashboard.Api/Models/JobResponse.cs`
- `Source/DotNetWorkQueue.Dashboard.Api/Services/DashboardService.cs`
- `Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs` (logging statements)
- Dashboard API test files if description assertions are needed

### Success Criteria

1. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds with 0 errors
2. `JobResponse` has a `ScheduleDescription` property
3. `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj"` -- all tests pass

---

## Phase 5: Cleanup, Documentation, and Version Bump

**Risk:** LOW -- file deletions, doc updates, and version bump. No functional code changes.
**Scope:** ~15% of total work.
**Depends on:** Phases 1, 2, 3, and 4 (all prior phases complete).

### What Changes

1. **Delete `Lib/` directory** -- Remove `Lib/Schyntax/net8.0/`, `Lib/Schyntax/net10.0/`, `Lib/Schyntax/README.md`, and the now-empty `Lib/` directory itself. (The Aq.ExpressionJsonSerializer was already moved to NuGet in a prior change, so `Lib/` should be empty after Schyntax removal.)
2. **`README.md`** -- Line 91: replace Schyntax format reference with cron format. Line 99: replace Schyntax link with Cronos link and cron format description. Line 144: remove Schyntax from custom libraries list in `/Lib`, add Cronos and CronExpressionDescriptor to NuGet dependencies list.
3. **`CLAUDE.md`** -- Update "Key Dependencies" section: remove Schyntax, add Cronos and CronExpressionDescriptor with versions. Update "Heartbeat defaults" entries to show cron format. Update any other Schyntax references.
4. **Version bump** -- Update `<Version>` in `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` from `0.9.19` to `0.9.3`.
5. **CHANGELOG.md** -- Add entry for 0.9.3 describing the Schyntax-to-Cronos migration as a breaking change.

### Files Touched

- `Lib/` (entire directory deleted)
- `README.md`
- `CLAUDE.md`
- `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (version only)
- `CHANGELOG.md`

### Success Criteria

1. `ls Lib/` -- directory does not exist
2. `grep -r "Schyntax\|schyntax" Source/ --include="*.cs" --include="*.csproj"` returns 0 matches
3. `grep -i "Schyntax" README.md` returns 0 matches
4. `grep -i "Schyntax" CLAUDE.md` returns 0 matches (except in Lessons Learned if still relevant)
5. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` -- 0 errors
6. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` -- 0 errors, 0 warnings
7. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"` -- all tests pass
8. Version in `DotNetWorkQueue.csproj` is `0.9.3`
9. `README.md` references Cronos and cron format, not Schyntax
10. `CLAUDE.md` lists Cronos and CronExpressionDescriptor in Key Dependencies

---

## Risk Summary

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `Previous()` nullable return breaks callers | High | Critical | Only one caller: `ScheduledJob.cs` line 98. Fix is a null-check before comparison. Addressed in Phase 1. |
| Cronos auto-detect (5 vs 6 field) wrong | Medium | High | Count space-separated fields: `expression.Split(' ').Length`. 5 = Standard, 6 = IncludeSeconds. Unit test both paths. |
| `GetOccurrences()` lookback window too small | Low | Medium | Default 48h is generous for typical schedules (minutes/hours). Make configurable via `PreviousLookbackWindow`. |
| Heartbeat cron equivalents produce different timing | Low | High | `sec(*%10)` = every 10 seconds = `*/10 * * * * *`. `min(*%2)` = every 2 minutes = `*/2 * * * *`. Exact equivalents verified. |
| CronExpressionDescriptor produces unclear descriptions | Low | Low | Used for display only (logging, dashboard). Does not affect scheduling logic. |
| Removing `Lib/` directory breaks other references | Low | Medium | Verified: `Lib/` only contains Schyntax after issue #101 removed JpLabs. No other references exist. |

## Execution Order

| Wave | Phases | Can Parallelize? | Notes |
|------|--------|-----------------|-------|
| 1 | Phase 1 | No -- foundation | Core interface + implementation must compile first |
| 2 | Phases 2, 3, 4 | Yes -- all 3 are independent | Transport defaults, test strings, dashboard enhancement |
| 3 | Phase 5 | No -- final cleanup | Depends on all prior phases; deletes Lib/, updates docs |

**Estimated plans per phase:**
- Phase 1: 2 plans (interface/config in plan 1, implementation/csproj in plan 2) -- highest complexity
- Phase 2: 1 plan (3 tasks, one per transport)
- Phase 3: 1 plan (2 tasks, one per test file)
- Phase 4: 1 plan (2-3 tasks for dashboard integration)
- Phase 5: 1 plan (3 tasks: delete Lib, update docs, version bump)
- **Total: 6 plans**
