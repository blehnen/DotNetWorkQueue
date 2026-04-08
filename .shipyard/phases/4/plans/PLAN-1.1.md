---
phase: dashboard-cron-description
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - JobScheduler logs human-readable schedule description when jobs are added
  - Solution builds cleanly
files_touched:
  - Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs
tdd: false
---

# Plan 1.1: CronExpressionDescriptor Logging Integration

Add human-readable schedule descriptions to JobScheduler logging using the `IJobSchedule.Description` property added in Phase 1.

## Context

Phase 1 added `IJobSchedule.Description` which returns a human-readable cron description via CronExpressionDescriptor (e.g., `"*/5 * * * *"` → `"Every 5 minutes"`).

The Dashboard API cannot show schedule descriptions because `DashboardJob` (the stored data model) doesn't include the schedule expression — it only has `JobName`, `JobEventTime`, `JobScheduledTime`. Adding the expression to storage would require schema changes across all transports, which is out of scope for this milestone. A future enhancement could add schedule expression to `DashboardJob` and populate it during job registration.

For now, the integration point is **logging in JobScheduler.cs** — the one place with live access to `IJobSchedule` instances.

## Dependencies

None (Phase 1 already complete — `IJobSchedule.Description` exists).

## Tasks

<task id="1" files="Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs" tdd="false">
  <action>
  In `Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs`, add log statements that include the human-readable schedule description when jobs are added or updated.

  Find the `AddUpdateJob` methods (there are two overloads). After a job's schedule is created (where `new JobSchedule(...)` is called), add a log line:

  ```csharp
  _log.LogInformation("Job {jobName} scheduled: {scheduleText} ({scheduleDescription})", name, schedule, job.Schedule.Description);
  ```

  This gives operators both the raw cron expression and the human-readable description in their logs. Use structured logging with named parameters (not string interpolation) per best practice.

  Also check the `Start()` method — if it logs scheduler startup, include any relevant schedule info.
  </action>
  <verify>grep -n "Description" "Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs" | grep -q "log\|Log" && echo "PASS" || echo "FAIL"</verify>
  <done>JobScheduler logs human-readable schedule descriptions when jobs are added/updated.</done>
</task>

<task id="2" files="" tdd="false">
  <action>
  Build verification:
  1. `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release` — 0 errors, 0 warnings
  2. Verify the log statement is correctly structured (uses named parameters, not interpolation)
  </action>
  <verify>dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release --verbosity quiet 2>&1 | tail -3</verify>
  <done>Solution builds cleanly with CronExpressionDescriptor logging.</done>
</task>

## Verification

```bash
# Log statement exists
grep -n "Description" "Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs"
# Should show log lines referencing schedule description

# Builds
dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release
# 0 errors, 0 warnings
```
