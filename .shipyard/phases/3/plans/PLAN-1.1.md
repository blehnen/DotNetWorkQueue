---
phase: test-schedule-strings
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - HeartBeatWorkerTests schedule strings converted to cron format
  - JobSchedulerTestsShared schedule strings converted to cron format
  - Unit tests pass
files_touched:
  - Source/DotNetWorkQueue.Tests/Queue/HeartBeatWorkerTests.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/JobSchedulerTestsShared.cs
tdd: false
---

# Plan 1.1: Unit and Integration Test Schedule Strings

Mechanical string replacements in 2 test files. No logic changes.

## Context

Test files contain Schyntax schedule strings for heartbeat and job scheduler tests. These must be converted to cron equivalents now that the core library uses Cronos.

## Dependencies

None (Phase 1 already complete).

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Tests/Queue/HeartBeatWorkerTests.cs" tdd="false">
  <action>
  In `Source/DotNetWorkQueue.Tests/Queue/HeartBeatWorkerTests.cs`, replace all Schyntax schedule strings:

  - `"sec(*%2)"` → `"*/2 * * * * *"` (every 2 seconds, 6-field cron)
  - `"sec(*%59)"` → `"*/59 * * * * *"` (every 59 seconds, 6-field cron)

  Search for all occurrences — there may be multiple calls using these strings.
  </action>
  <verify>grep -n 'sec(\*%' "Source/DotNetWorkQueue.Tests/Queue/HeartBeatWorkerTests.cs"; echo "Should be empty"</verify>
  <done>All Schyntax strings in HeartBeatWorkerTests.cs converted to cron format.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/JobSchedulerTestsShared.cs" tdd="false">
  <action>
  In `Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/JobSchedulerTestsShared.cs`, replace all Schyntax schedule strings:

  - `"min(*)"` → `"* * * * *"` (every minute, 5-field cron)

  There are 4 occurrences per the research.
  </action>
  <verify>grep -n 'min(\*' "Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/JobSchedulerTestsShared.cs"; echo "Should be empty"</verify>
  <done>All Schyntax strings in JobSchedulerTestsShared.cs converted to cron format.</done>
</task>

<task id="3" files="" tdd="false">
  <action>
  Run the unit test suite to verify nothing broke:

  `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --verbosity normal`

  All 878 tests should pass.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --verbosity quiet 2>&1 | tail -5</verify>
  <done>All unit tests pass with cron schedule strings.</done>
</task>

## Verification

```bash
# No Schyntax strings in test files
grep -rn 'sec(\*%\|min(\*' Source/DotNetWorkQueue.Tests/ Source/DotNetWorkQueue.IntegrationTests.Shared/ --include="*.cs"
# Should return 0 matches

# Unit tests pass
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"
```
