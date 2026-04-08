---
phase: core-library-cronos
plan: "1.2"
wave: 1
dependencies: ["1.1"]
must_haves:
  - JobSchedule.cs fully rewritten to use Cronos CronExpression instead of Schyntax.Schedule
  - Auto-detection of 5-field (Standard) vs 6-field (IncludeSeconds) cron format
  - Previous() implemented via GetOccurrences() with 48h lookback, returning DateTimeOffset?
  - Next() throws InvalidOperationException if no next occurrence found
  - Description property returns cached human-readable cron description via CronExpressionDescriptor
  - ScheduledJob.cs null-checks Previous() result before catch-up logic
  - Solution builds cleanly with 0 errors
files_touched:
  - Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs
  - Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs
tdd: false
---

# Plan 1.2: JobSchedule Rewrite and ScheduledJob Null-Safety

This plan rewrites the `JobSchedule` implementation from Schyntax to Cronos and fixes the `ScheduledJob` caller to handle the now-nullable `Previous()` return. After this plan completes, `DotNetWorkQueueNoTests.sln` should build.

## Context

- `JobSchedule` is `internal`, not DI-registered, and `new`'d at 3 call sites:
  - `JobScheduler.AddUpdateJob()` line 105 and 124
  - `ScheduledJob.UpdateSchedule(string)` line 139
- All 3 call sites pass `(string schedule, Func<DateTimeOffset> getCurrentOffset)` -- constructor signature is unchanged
- `ScheduledJob.Previous()` is called only at line 98 inside `StartSchedule()`, guarded by `window > TimeSpan.Zero`
- Per CONTEXT-1.md: the existing `Window` property on `ScheduledJob` serves as the lookback bound; however, `Previous()` uses a hardcoded 48h internal lookback because `ScheduledJob` already validates `prev > now - window` at line 100. A generous lookback is safe.
- Cronos `GetNextOccurrence()` returns `DateTimeOffset?` -- must handle null
- Cronos `GetOccurrences()` returns `IEnumerable<DateTimeOffset>` -- iterate to find last

## Uncertainty Flags (from RESEARCH.md)

- **GetOccurrences DateTimeOffset overload**: The exact parameter names for `fromInclusive`/`toInclusive` on the `DateTimeOffset` overload should be verified at compile time. The implementation may need minor signature adjustments.
- **CronExpressionDescriptor 6-field handling**: CronExpressionDescriptor claims to support 6-field expressions but it's unclear whether it treats field 6 as seconds (Cronos convention) or year (Quartz convention). Test with `"*/10 * * * * *"` after build.

## Tasks

<task id="1" files="Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs" tdd="false">
  <action>
  Rewrite `Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs` entirely. Replace the full class body.

  **Remove:** `using Schyntax;` (line 20), `Schedule _schedule` field, all method bodies that delegate to `_schedule`.

  **Add:** `using Cronos;` and `using CronExpressionDescriptor;`

  **New fields:**
  - `private readonly CronExpression _expression;`
  - `private readonly string _originalText;`
  - `private readonly Func<DateTimeOffset> _getCurrentOffset;`
  - `private readonly Lazy<string> _description;`

  **Constructor** `JobSchedule(string schedule, Func<DateTimeOffset> getCurrentOffset)`:
  - Store `schedule` as `_originalText`
  - Store `getCurrentOffset` as `_getCurrentOffset`
  - Auto-detect format: `schedule.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length` -- 5 = `CronFormat.Standard`, 6 = `CronFormat.IncludeSeconds`, else throw `ArgumentException`
  - Parse: `CronExpression.Parse(schedule, format)`
  - Init lazy description: `new Lazy<string>(() => ExpressionDescriptor.GetDescription(schedule))`

  **Properties:**
  - `string OriginalText => _originalText;`
  - `string Description => _description.Value;`

  **Methods:**
  - `DateTimeOffset Next()` -- call `_expression.GetNextOccurrence(_getCurrentOffset(), TimeZoneInfo.Utc)`, throw `InvalidOperationException("No next occurrence found for cron expression: {_originalText}")` if null
  - `DateTimeOffset Next(DateTimeOffset after)` -- same pattern with explicit offset
  - `DateTimeOffset? Previous()` -- call `PreviousInternal(_getCurrentOffset())`
  - `DateTimeOffset? Previous(DateTimeOffset atOrBefore)` -- call `PreviousInternal(atOrBefore)`
  - `private DateTimeOffset? PreviousInternal(DateTimeOffset before)` -- compute `from = before - TimeSpan.FromHours(48)`, iterate `_expression.GetOccurrences(from.UtcDateTime, before.UtcDateTime, TimeZoneInfo.Utc, fromInclusive: true, toInclusive: true)` keeping last element, return as `DateTimeOffset?` (null if none found)

  **Important:** The `GetOccurrences` overload that takes `DateTime` + `TimeZoneInfo` may have different parameter ordering than the `DateTimeOffset` overload. Use the `DateTime` overload (`from.UtcDateTime`, `before.UtcDateTime`) with `TimeZoneInfo.Utc` for reliability, then convert the result. If the overload signature does not match at compile time, adjust accordingly -- the key contract is: enumerate occurrences in [from, before] inclusive and return the last one.

  Preserve the LGPL license header (lines 1-18).
  </action>
  <verify>grep -c "using Cronos;" "Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs" && grep -c "CronExpression" "Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs" && ! grep -q "Schyntax" "Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs" && echo "PASS" || echo "FAIL"</verify>
  <done>JobSchedule.cs uses `Cronos.CronExpression` for parsing and scheduling. No Schyntax imports or references. Auto-detects 5 vs 6 field cron. `Previous()` returns `DateTimeOffset?`. `Description` returns a cached human-readable string.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs" tdd="false">
  <action>
  In `Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs`, update the `StartSchedule()` method to null-check the `Previous()` result.

  Replace lines 98-104:
  ```csharp
                    var prev = Schedule.Previous();
                    lastKnownEvent = lastKnownEvent.AddSeconds(1); // add a second for good measure
                    if (prev > lastKnownEvent && prev > new DateTimeOffset(_getTime.GetCurrentUtcDate()) - window)
                    {
                        firstEvent = prev;
                        firstEventSet = true;
                    }
  ```

  With:
  ```csharp
                    var prev = Schedule.Previous();
                    if (prev.HasValue)
                    {
                        lastKnownEvent = lastKnownEvent.AddSeconds(1); // add a second for good measure
                        if (prev.Value > lastKnownEvent && prev.Value > new DateTimeOffset(_getTime.GetCurrentUtcDate()) - window)
                        {
                            firstEvent = prev.Value;
                            firstEventSet = true;
                        }
                    }
  ```

  The key changes:
  1. Wrap the entire catch-up block in `if (prev.HasValue)`
  2. Use `prev.Value` instead of `prev` for all comparisons and assignments
  3. When `Previous()` returns null (no occurrence in lookback window), the catch-up block is skipped entirely and execution falls through to `Schedule.Next()` at line 108 -- which is the correct behavior (no missed event to catch up on)
  </action>
  <verify>grep -A8 "var prev = Schedule.Previous" "Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs" | grep -q "prev.HasValue" && grep -q "prev.Value" "Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs" && echo "PASS" || echo "FAIL"</verify>
  <done>`ScheduledJob.StartSchedule()` null-checks `Schedule.Previous()` with `prev.HasValue`. All usages of `prev` within the catch-up block use `prev.Value`. When `Previous()` returns null, execution falls through to `Schedule.Next()`.</done>
</task>

<task id="3" files="" tdd="false">
  <action>
  Build verification. After Tasks 1 and 2 of this plan AND all tasks of Plan 1.1 are complete, verify the solution compiles.

  Run: `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug`

  If there are compile errors:
  - **Missing Cronos overload**: Check `GetOccurrences` parameter order. The `DateTime` overload is `GetOccurrences(DateTime from, DateTime to, TimeZoneInfo zone, bool fromInclusive, bool toInclusive)`. The `DateTimeOffset` overload may differ. Adjust as needed.
  - **CronExpressionDescriptor namespace**: The `using` should be `using CronExpressionDescriptor;` and the call is `ExpressionDescriptor.GetDescription(string)`.
  - **IJobSchedule.Description not implemented**: Ensure `JobSchedule` implements `string Description => _description.Value;`.
  - **NuGet restore failure**: Run `dotnet restore "Source/DotNetWorkQueueNoTests.sln"` first.

  Then run: `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release`

  Fix any warnings (Release enables TreatWarningsAsErrors).

  Finally verify no Schyntax references remain:
  `grep -r "Schyntax\|schyntax" Source/DotNetWorkQueue/ --include="*.cs" --include="*.csproj"`
  Should return 0 matches.
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug && dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release && ! grep -rq "Schyntax" Source/DotNetWorkQueue/ --include="*.cs" --include="*.csproj" && echo "PASS" || echo "FAIL"</verify>
  <done>`DotNetWorkQueueNoTests.sln` builds with 0 errors in both Debug and Release. No Schyntax references exist in `Source/DotNetWorkQueue/` (*.cs or *.csproj). `IJobSchedule.Previous()` returns `DateTimeOffset?`. `IJobSchedule.Description` exists and is implemented. `ScheduledJob.cs` null-checks `Previous()`.</done>
</task>
