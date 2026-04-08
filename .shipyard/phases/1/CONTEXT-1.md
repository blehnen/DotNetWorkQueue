# Phase 1 Context: Design Decisions

## PreviousLookbackWindow — Reuse existing Window property

No new `PreviousLookbackWindow` config needed. The existing `ScheduledJob.Window` property (TimeSpan) already defines how far back to look for missed events. Pass it to `GetOccurrences(now - window, now)` when computing `Previous()`. When `Window` is `TimeSpan.Zero`, `Previous()` isn't called at all (line 95 guard).

This removes a planned config addition from the roadmap.

## JobSchedule constructor — Keep Func<DateTimeOffset> parameter

Keep the `Func<DateTimeOffset> getCurrentOffset` constructor parameter on `JobSchedule`. Cronos doesn't need it (methods accept explicit DateTimeOffset), but the parameter-less `Next()` and `Previous()` overloads need a clock source. Store the func and call it internally.

## Previous() nullable — Confirmed

`IJobSchedule.Previous()` and `Previous(DateTimeOffset)` return `DateTimeOffset?`. `ScheduledJob.cs` null-checks the result before using it for catch-up.

## Cron format auto-detection — Confirmed

Count space-separated fields: 5 = `CronFormat.Standard`, 6 = `CronFormat.IncludeSeconds`. No configuration flag needed.

## Previous() window source in ScheduledJob

The `Previous()` call in `ScheduledJob.StartSchedule()` needs a lookback window. Since `Previous()` is only called when `Window > TimeSpan.Zero`, pass `Window` as the lookback bound. The call becomes: `Schedule.Previous(window)` or `Schedule.Previous(now, window)` — exact signature TBD during implementation, but the window value comes from the existing property.
