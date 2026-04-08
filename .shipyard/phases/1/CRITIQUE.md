# Phase 1 Plan Critique (Feasibility Stress Test)
**Phase:** Core Library -- IJobSchedule, JobSchedule, csproj, and Configuration
**Date:** 2026-04-08
**Type:** plan-review

---

## Plan 1.1: NuGet Dependencies, Interface Contract, and Doc Comment

### File Paths Exist

| File | Exists | Notes |
|------|--------|-------|
| `Source/Directory.Packages.props` | YES | 63 lines, `<!-- Core -->` section at lines 3-11 |
| `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` | YES | 85 lines |
| `Source/DotNetWorkQueue/IJobSchedule.cs` | YES | 59 lines |
| `Source/DotNetWorkQueue/IHeartBeatConfiguration.cs` | YES | 66 lines |

### Line Number Accuracy

| Claim | Actual | Status |
|-------|--------|--------|
| csproj main ItemGroup at lines 49-59 | Lines 49-59 | MATCH |
| csproj Schyntax ItemGroups at lines 61-71 | Lines 61-71 | MATCH |
| csproj IncludeVendoredDllsInPack at lines 78-84 | Lines 78-84 | MATCH |
| IJobSchedule `OriginalText` at line 34 | Line 34 | MATCH |
| IJobSchedule `Previous()` at line 51 | Line 51 | MATCH |
| IJobSchedule `Previous(DateTimeOffset)` at line 57 | Line 57 | MATCH |
| IHeartBeatConfiguration remarks at lines 61-63 | Lines 61-63 | MATCH |
| Directory.Packages.props `DiagnosticSource` at line 11 | Line 11 | MATCH |

### API Surface Match

| Claim | Actual | Status |
|-------|--------|--------|
| `Previous()` returns `DateTimeOffset` | `DateTimeOffset Previous();` at line 51 | MATCH |
| `Previous(DateTimeOffset)` returns `DateTimeOffset` | `DateTimeOffset Previous(DateTimeOffset atOrBefore);` at line 57 | MATCH |
| csproj has `<Reference Include="Schyntax">` blocks | Two TFM-conditional blocks at lines 61-71 | MATCH |
| csproj has `IncludeVendoredDllsInPack` target | Lines 78-84 | MATCH |

### Verification Commands

| Task | Command | Runnable | Notes |
|------|---------|----------|-------|
| Task 1 | `grep -c "Cronos\|CronExpressionDescriptor" ...` | YES | Bash-compatible, checks correct files |
| Task 2 | `grep -n "DateTimeOffset?" ... \| grep -c "Previous"` | YES | Correct pipe chain |
| Task 3 | `grep -A1 "remarks" ... \| grep -q "cron format"` | YES | Correct pattern |

### Issues Found

**NONE** -- Plan 1.1 is clean. All file paths, line numbers, and API references are accurate.

---

## Plan 1.2: JobSchedule Rewrite and ScheduledJob Null-Safety

### File Paths Exist

| File | Exists | Notes |
|------|--------|-------|
| `Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs` | YES | 54 lines, `using Schyntax;` at line 20 |
| `Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs` | YES | 276 lines |

### Line Number Accuracy

| Claim | Actual | Status |
|-------|--------|--------|
| JobSchedule `using Schyntax;` at line 20 | Line 20 | MATCH |
| ScheduledJob `Schedule.Previous()` at line 98 | Line 98 | MATCH |
| ScheduledJob catch-up block lines 98-104 | Lines 98-104 | MATCH |
| ScheduledJob `UpdateSchedule` `new JobSchedule` at line 139 | Line 139 | MATCH |
| JobScheduler `new JobSchedule` at lines 105, 124 | Lines 105, 124 | MATCH |

### API Surface Match

| Claim | Actual | Status |
|-------|--------|--------|
| `JobSchedule` is `internal class` | `internal class JobSchedule : IJobSchedule` at line 24 | MATCH |
| Constructor is `(string schedule, Func<DateTimeOffset> getCurrentOffset)` | Line 28 | MATCH |
| `Schedule _schedule` field | `private readonly Schedule _schedule;` at line 26 | MATCH |
| `ScheduledJob.Previous()` usage at line 98 | `var prev = Schedule.Previous();` | MATCH |
| 3 call sites for `new JobSchedule(...)` | Lines 105, 124 (JobScheduler.cs), 139 (ScheduledJob.cs) | MATCH |

### Verification Commands

| Task | Command | Runnable | Notes |
|------|---------|----------|-------|
| Task 1 | `grep -c "using Cronos;" ...` | YES | Correct |
| Task 2 | `grep -A8 "var prev = Schedule.Previous" ... \| grep -q "prev.HasValue"` | YES | Correct |
| Task 3 | `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug` | YES | Solution file verified to exist |

### Issues Found

#### ISSUE 1: CONTEXT-1.md Design Divergence on Lookback Window (LOW RISK)

**CONTEXT-1.md says:** "No new `PreviousLookbackWindow` config needed. The existing `ScheduledJob.Window` property already defines how far back to look. Pass it to `GetOccurrences(now - window, now)`."

**Plan 1.2 implements:** A hardcoded 48h lookback in `PreviousInternal()`: `var lookback = TimeSpan.FromHours(48);`

**Assessment:** The plan's approach is **functionally correct and arguably better** than the CONTEXT-1.md design. Reason: `JobSchedule` is an internal class that does not have access to `ScheduledJob.Window`. Passing `Window` would require either (a) adding a parameter to `Previous()` on the public `IJobSchedule` interface, or (b) injecting the window via constructor. The plan avoids this by using a generous internal lookback and relying on `ScheduledJob`'s existing validation at line 100 (`prev > now - window`) to filter out stale results. This is the approach recommended in RESEARCH.md section 8. 

**Verdict:** Not a blocker. The divergence is intentional and well-reasoned. CONTEXT-1.md could be updated to reflect this, but it does not block execution.

#### ISSUE 2: ROADMAP Requirement #7 Not Implemented (LOW RISK)

**ROADMAP Phase 1 says:** "Add `PreviousLookbackWindow` configuration -- Add a `TimeSpan PreviousLookbackWindow` property (default 48h) to the job scheduler configuration."

**Plans implement:** A hardcoded 48h lookback, no configurable property.

**Assessment:** CONTEXT-1.md explicitly supersedes this: "No new `PreviousLookbackWindow` config needed." The ROADMAP item 7 is intentionally dropped. However, the ROADMAP's "Files Touched" section still lists `JobSchedulerInit.cs` and `Configuration/` as potentially affected. Neither plan touches these files, which is correct given the simplified design.

**Verdict:** Not a blocker. CONTEXT-1.md is the authoritative design decision. The ROADMAP could be updated to remove item 7, but it does not block execution.

#### ISSUE 3: GetOccurrences Overload Uncertainty (LOW RISK)

The plan uses `_expression.GetOccurrences(from.UtcDateTime, before.UtcDateTime, TimeZoneInfo.Utc, fromInclusive: true, toInclusive: true)` -- a `DateTime` + `TimeZoneInfo` overload. RESEARCH.md flags that the exact parameter ordering for the `DateTimeOffset` overload is uncertain.

**Assessment:** The plan already addresses this by using the `DateTime` overload (which is documented in the Cronos README) rather than the `DateTimeOffset` overload. The plan's Task 3 (build verification) explicitly lists this as a possible compile error with a fix: "Check `GetOccurrences` parameter order." This is adequately handled.

**Verdict:** Not a blocker. The uncertainty is acknowledged and has a concrete fallback.

#### ISSUE 4: CronExpressionDescriptor 6-Field Handling Uncertainty (LOW RISK)

The plan flags that CronExpressionDescriptor may interpret the 6th field as "year" (Quartz convention) instead of "seconds" (Cronos convention). This could produce incorrect descriptions for 6-field expressions like `"*/10 * * * * *"`.

**Assessment:** This is a display-only concern (Description property). It does not affect scheduling logic. The plan correctly flags it as an uncertainty to test after build. Phase 3 (tests) would catch any incorrect descriptions.

**Verdict:** Not a blocker. The builder should test `ExpressionDescriptor.GetDescription("*/10 * * * * *")` after the build to confirm correct output.

---

## Forward References and Hidden Dependencies

### Plan 1.1 -> Plan 1.2 Dependency

Plan 1.2 declares `dependencies: ["1.1"]`. This is correct:
- Plan 1.2 needs Cronos NuGet packages (added by Plan 1.1 Task 1) to compile.
- Plan 1.2 needs `IJobSchedule.Previous()` to return `DateTimeOffset?` (changed by Plan 1.1 Task 2) for the nullable implementation.
- Plan 1.2 needs `IJobSchedule.Description` property (added by Plan 1.1 Task 2) to implement.

**However**, both plans are in **Wave 1**, which normally implies parallel execution. The dependency declaration correctly prevents this -- Plan 1.2 must execute after Plan 1.1.

### File Overlap Check

| File | Plan 1.1 | Plan 1.2 | Conflict? |
|------|----------|----------|-----------|
| `Source/Directory.Packages.props` | Task 1 | -- | NO |
| `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` | Task 1 | -- | NO |
| `Source/DotNetWorkQueue/IJobSchedule.cs` | Task 2 | -- | NO |
| `Source/DotNetWorkQueue/IHeartBeatConfiguration.cs` | Task 3 | -- | NO |
| `Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs` | -- | Task 1 | NO |
| `Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs` | -- | Task 2 | NO |

**No file conflicts.** Plans touch completely disjoint file sets.

---

## Complexity Assessment

| Plan | Files Touched | Directories Crossed | Risk Level |
|------|---------------|---------------------|------------|
| Plan 1.1 | 4 files | 2 dirs (`Source/`, `Source/DotNetWorkQueue/`) | LOW |
| Plan 1.2 | 2 files | 1 dir (`Source/DotNetWorkQueue/JobScheduler/`) | MEDIUM (full class rewrite) |

Plan 1.2 rewrites an entire class (JobSchedule.cs, 54 lines -> ~60 lines). This is the highest-risk task but the class is small and well-isolated (internal, 3 call sites, no DI registration).

---

## Phase 1 Success Criteria Coverage

| # | ROADMAP Criterion | Covered By | Status |
|---|-------------------|------------|--------|
| 1 | `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug` succeeds with 0 errors | Plan 1.2 Task 3 | COVERED |
| 2 | `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release` succeeds with 0 errors, 0 warnings | Plan 1.2 Task 3 | COVERED |
| 3 | `grep -r "Schyntax\|schyntax" Source/DotNetWorkQueue/ --include="*.cs" --include="*.csproj"` returns 0 matches | Plan 1.1 Task 1 (csproj), Plan 1.2 Task 1 (JobSchedule.cs) | COVERED |
| 4 | `IJobSchedule.Previous()` returns `DateTimeOffset?` | Plan 1.1 Task 2 | COVERED |
| 5 | `IJobSchedule.Description` property exists | Plan 1.1 Task 2 | COVERED |
| 6 | `ScheduledJob.cs` null-checks the `Previous()` result | Plan 1.2 Task 2 | COVERED |

**All 6 success criteria are covered by the plans.**

### ROADMAP Item Not Covered (Intentionally)

| # | ROADMAP Item | Status | Reason |
|---|-------------|--------|--------|
| 7 | Add `PreviousLookbackWindow` configuration | DROPPED | CONTEXT-1.md design decision: reuse existing Window + hardcoded 48h internal lookback. Not needed. |

---

## Regression Risk

- **24 files** reference `IJobSchedule` in the core library. Of these, only `ScheduledJob.cs` calls `Previous()`. The interface change (`Previous()` -> `DateTimeOffset?`) will cause compile errors in test mocks if any exist -- but grep confirmed **no test files mock or call `Previous()`** on `IJobSchedule`. The `DotNetWorkQueue.xml` documentation file contains references but will be regenerated on build.
- The `DotNetWorkQueueNoTests.sln` build (Plan 1.2 Task 3) will catch any missed compile errors across all non-test projects.
- Transport init files still use Schyntax strings (`"sec(*%10)"`, `"min(*%2)"`), but these are parsed at runtime, not compile time. Phase 2 replaces them. After Phase 1, the solution builds but **runtime usage of heartbeat scheduling will fail** until Phase 2 is complete. This is expected and documented.

---

## Recommendations

1. **Update CONTEXT-1.md** to align with the 48h hardcoded lookback approach used in Plan 1.2, instead of the "pass Window to GetOccurrences" design it currently describes. This avoids confusion for the builder.
2. **After build, manually test** `ExpressionDescriptor.GetDescription("*/10 * * * * *")` to confirm 6-field handling. If it produces "Every 10 seconds" (correct) vs "Every 10 minutes, every second" (wrong), the CronExpressionDescriptor integration is validated.
3. **The builder should be aware** that after Phase 1, the solution compiles but transport heartbeat defaults still use Schyntax strings. Integration tests will fail until Phase 2 is done. Unit tests (Phase 3) will also fail for the same reason. This is the intended execution order.

---

## Verdict

**READY** -- Both plans are feasible and well-constructed. All file paths exist, line numbers are accurate, API surface references match reality, verification commands are syntactically correct and runnable, there are no file conflicts between plans, and all 6 ROADMAP success criteria are covered. The CONTEXT-1.md divergence on the lookback window is intentional and well-reasoned (not a blocking issue). The Cronos API uncertainties (GetOccurrences overload, CronExpressionDescriptor 6-field handling) are acknowledged in the plans with concrete fallbacks. Proceed to build.
