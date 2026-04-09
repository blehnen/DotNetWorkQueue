# Research: Phase 1 -- Replace Schyntax with Cronos (Core Library)

## 1. JobSchedule.cs

**File:** `Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs` (54 lines)

**Class:** `internal class JobSchedule : IJobSchedule`

**Constructor:**
```csharp
public JobSchedule(string schedule, Func<DateTimeOffset> getCurrentOffset)
```
- Creates a `Schyntax.Schedule` from the raw string and the clock function
- The `Func<DateTimeOffset>` is the clock source for parameterless `Next()` / `Previous()`

**Fields:**
- `private readonly Schedule _schedule;` -- the Schyntax `Schedule` object

**Members (all delegate to `_schedule`):**
- `string OriginalText => _schedule.OriginalText;`
- `DateTimeOffset Next()` -- parameterless, uses internal clock
- `DateTimeOffset Next(DateTimeOffset after)` -- explicit offset
- `DateTimeOffset Previous()` -- parameterless, uses internal clock
- `DateTimeOffset Previous(DateTimeOffset atOrBefore)` -- explicit offset

**Import:** `using Schyntax;` (line 20) -- only file importing Schyntax namespace.

**Cronos migration notes:**
- Replace `Schedule _schedule` with `CronExpression _expression` + `string _originalText` + `CronFormat _format`
- Store the `Func<DateTimeOffset>` for parameterless overloads
- `Next()` maps to `_expression.GetNextOccurrence(now, TimeZoneInfo.Utc)` -- returns `DateTimeOffset?`, must handle null (throw if no next occurrence)
- `Previous()` has no direct Cronos equivalent -- must use `GetOccurrences(from, to, TimeZoneInfo.Utc)` and take `LastOrDefault()`
- `OriginalText` must be stored manually (Cronos `CronExpression` has `ToString()` but it normalizes the expression)

---

## 2. IJobSchedule.cs

**File:** `Source/DotNetWorkQueue/IJobSchedule.cs` (59 lines)

**Interface:** `public interface IJobSchedule`

**Members:**
| Member | Return Type | XML Doc Summary |
|--------|-------------|-----------------|
| `OriginalText` | `string` (get) | Gets the original text |
| `Next()` | `DateTimeOffset` | Gets the next run time |
| `Next(DateTimeOffset after)` | `DateTimeOffset` | Gets the next run time after the offset |
| `Previous()` | `DateTimeOffset` | Gets the previous run time |
| `Previous(DateTimeOffset atOrBefore)` | `DateTimeOffset` | Gets the previous run time from before the offset |

**Phase 1 changes:**
- `Previous()` return type changes to `DateTimeOffset?`
- `Previous(DateTimeOffset)` return type changes to `DateTimeOffset?`
- Add `string Description { get; }` for human-readable cron description (from ROADMAP)

---

## 3. ScheduledJob.cs

**File:** `Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs` (276 lines)

### Schedule.Previous() call (line 98)

```csharp
// Lines 91-118 in StartSchedule():
var firstEvent = default(DateTimeOffset);
var firstEventSet = false;
var window = Window;
var lastKnownEvent = _queue.LastKnownEvent.Get(Name);
if (window > TimeSpan.Zero && lastKnownEvent != default)
{
    // check if we actually want to run the first event right away
    var prev = Schedule.Previous();                          // <-- LINE 98
    lastKnownEvent = lastKnownEvent.AddSeconds(1);
    if (prev > lastKnownEvent && prev > new DateTimeOffset(_getTime.GetCurrentUtcDate()) - window)
    {
        firstEvent = prev;
        firstEventSet = true;
    }
}

if (!firstEventSet)
    firstEvent = Schedule.Next();                            // <-- LINE 108

while (firstEvent <= PrevEvent)
{
    firstEvent = Schedule.Next(firstEvent);                  // <-- LINE 113
}
```

**Catch-up logic explained:** When `Window > TimeSpan.Zero` and a `lastKnownEvent` exists, it checks whether the most recent schedule occurrence (`prev`) was missed (happened after `lastKnownEvent + 1s`) and is within the Window. If so, it fires that event immediately instead of waiting for the next one.

**Null-safety fix needed:** When `Previous()` returns `DateTimeOffset?`, wrap the entire catch-up block:
```csharp
var prev = Schedule.Previous();
if (prev.HasValue)
{
    lastKnownEvent = lastKnownEvent.AddSeconds(1);
    if (prev.Value > lastKnownEvent && prev.Value > new DateTimeOffset(_getTime.GetCurrentUtcDate()) - window)
    {
        firstEvent = prev.Value;
        firstEventSet = true;
    }
}
```

### Other Schedule calls in ScheduledJob.cs

| Line | Call | Context |
|------|------|---------|
| 108 | `Schedule.Next()` | Default first event if catch-up not triggered |
| 113 | `Schedule.Next(firstEvent)` | Advance past already-processed events |
| 136 | `Schedule.OriginalText` | Equality check in `UpdateSchedule(string)` |
| 139 | `new JobSchedule(schedule, ...)` | Creates new schedule when updating |
| 230 | `Schedule.Next()` | Compute next event after running current one |
| 232 | `Schedule.Next(eventTime)` | Advance if next <= current event time |

### Window property

- `public TimeSpan Window { get; set; }` (line 48)
- Set by `JobScheduler.AddTaskImpl()` from the caller-provided `window` parameter
- Only used in `StartSchedule()` at line 93-95: guards the `Previous()` call
- Per CONTEXT-1.md, reuse this as the lookback window for `GetOccurrences()`

### IGetTime usage

- `_getTime` field is `IGetTime` (line 60), injected via constructor
- Used at line 100: `_getTime.GetCurrentUtcDate()` -- provides UTC clock
- Used at line 139: `new DateTimeOffset(_getTime.GetCurrentUtcDate())` -- clock for new JobSchedule

---

## 4. DotNetWorkQueue.csproj

**File:** `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` (85 lines)

### Current Schyntax references

**TFM-conditional assembly references (lines 61-71):**
```xml
<ItemGroup Condition=" '$(TargetFramework)' == 'net8.0' ">
    <Reference Include="Schyntax">
        <HintPath>..\..\Lib\Schyntax\net8.0\Schyntax.dll</HintPath>
    </Reference>
</ItemGroup>

<ItemGroup Condition=" '$(TargetFramework)' == 'net10.0' ">
    <Reference Include="Schyntax">
        <HintPath>..\..\Lib\Schyntax\net10.0\Schyntax.dll</HintPath>
    </Reference>
</ItemGroup>
```

**IncludeVendoredDllsInPack target (lines 79-84):**
```xml
<Target Name="IncludeVendoredDllsInPack" BeforeTargets="GenerateNuspec">
    <ItemGroup>
        <_PackageFiles Include="..\..\Lib\Schyntax\net8.0\Schyntax.dll" PackagePath="lib\net8.0" />
        <_PackageFiles Include="..\..\Lib\Schyntax\net10.0\Schyntax.dll" PackagePath="lib\net10.0" />
    </ItemGroup>
</Target>
```

**Main PackageReference ItemGroup (lines 49-59):** This is where new `<PackageReference Include="Cronos" />` and `<PackageReference Include="CronExpressionDescriptor" />` entries should go.

### What to remove
1. Both TFM-conditional `<ItemGroup>` blocks with `<Reference Include="Schyntax">` (lines 61-71)
2. The entire `<Target Name="IncludeVendoredDllsInPack">` block (lines 79-84)

### What to add
Add to the main `<ItemGroup>` (lines 49-59):
```xml
<PackageReference Include="Cronos" />
<PackageReference Include="CronExpressionDescriptor" />
```

---

## 5. Directory.Packages.props

**File:** `Source/Directory.Packages.props` (63 lines)

### Where to add entries

In the `<!-- Core -->` section (lines 3-11), add:
```xml
<PackageVersion Include="Cronos" Version="0.12.0" />
<PackageVersion Include="CronExpressionDescriptor" Version="2.45.0" />
```

These should go after the existing core entries (e.g., after `System.Diagnostics.DiagnosticSource` on line 11).

---

## 6. IHeartBeatConfiguration.cs

**File:** `Source/DotNetWorkQueue/IHeartBeatConfiguration.cs` (66 lines)

### Schyntax doc comment (lines 61-64)

```csharp
/// <summary>
/// How often the heartbeat will be updated.
/// </summary>
/// <remarks>
/// This is expected to be in schyntax format - https://github.com/schyntax/cs-schyntax
/// </remarks>
string UpdateTime { get; set; }
```

**Change:** Update `<remarks>` to reference standard cron format:
```xml
/// <remarks>
/// This is expected to be in standard cron format (5-field) or cron format with seconds (6-field).
/// </remarks>
```

---

## 7. JobScheduler.cs

**File:** `Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs` (435 lines)

### Logging that includes schedule text

No logging of schedule text was found in JobScheduler.cs. The class logs:
- Line 83: `$"Scheduler time is {_getTime.Create().GetCurrentUtcDate()}"` -- time factory clock
- Line 84: `$"Local time is {DateTime.UtcNow}"` -- local clock

### JobSchedule instantiation points

| Line | Code |
|------|------|
| 105 | `new JobSchedule(schedule, GetCurrentOffset)` -- in `AddUpdateJob<TTransportInit, TQueue>` |
| 124 | `new JobSchedule(schedule, GetCurrentOffset)` -- in `AddUpdateJob<TTransportInit>` |

Both pass the string schedule and the `GetCurrentOffset` delegate (lines 248-251):
```csharp
private DateTimeOffset GetCurrentOffset()
{
    return new DateTimeOffset(_getTime.Create().GetCurrentUtcDate());
}
```

### CronExpressionDescriptor integration

The ROADMAP mentions adding `Description` to `IJobSchedule`. For logging integration in JobScheduler.cs, there is no existing schedule-text logging to enhance. The `Description` property would be available on `IScheduledJob.Schedule.Description` for external consumers.

---

## 8. JobSchedulerInit.cs (DI Registration)

**File:** `Source/DotNetWorkQueue/JobScheduler/JobSchedulerInit.cs` (46 lines)

**Key registrations:**
```csharp
container.Register<IJobScheduler, JobScheduler>(LifeStyles.Singleton);
container.Register<IJobQueue, JobQueue>(LifeStyles.Singleton);
```

**JobSchedule is NOT registered in DI.** It is `new`'d directly by `JobScheduler.AddUpdateJob()` (line 105, 124) and by `ScheduledJob.UpdateSchedule()` (line 139). The `Func<DateTimeOffset>` is passed as a constructor argument at each call site.

**Implication:** No DI wiring changes needed for Phase 1. The `Window` property comes from the `ScheduledJob` instance, not from DI. If `Previous()` needs the window, `JobSchedule` needs either:
- A window parameter added to `Previous()` method signature, or
- The window injected via constructor (but this means `ScheduledJob.UpdateSchedule(string)` at line 139 would need to pass it)

Per CONTEXT-1.md, the approach is to pass window to the `Previous()` method or have `ScheduledJob` pass `now - window` as the lower bound when calling `Previous()`. The cleanest option: change `Previous()` signature on `IJobSchedule` to accept an optional `TimeSpan lookback` parameter, or have `ScheduledJob` compute the range and call a `GetOccurrences`-based method.

**Recommended approach:** Keep `Previous()` parameterless on the interface. In `JobSchedule` implementation, store a default lookback (e.g., 48h). In `ScheduledJob.StartSchedule()`, compute `Previous()` and validate it against `Window` -- the existing check at line 100 already validates `prev > now - window`, so even a large lookback won't cause false catch-ups.

---

## 9. HeartBeatWorker.cs

**File:** `Source/DotNetWorkQueue/Queue/HeartBeatWorker.cs` (333 lines)

**How UpdateTime is used:**
- Line 83: `_checkTime = configuration.UpdateTime;` -- stores the Schyntax format string
- Line 108: `_scheduler.AddUpdateJob(name, _checkTime, ...)` -- passes it to `IHeartBeatScheduler.AddUpdateJob`

**HeartBeatScheduler.cs** (line 82) passes it through to `_scheduler.AddUpdateJob<MemoryMessageQueueInit, JobQueueCreation>(..., schedule, ...)` which creates a `new JobSchedule(schedule, GetCurrentOffset)`.

**Impact:** The schedule string flows from transport init configuration through HeartBeatConfiguration to HeartBeatWorker to HeartBeatScheduler to JobScheduler to JobSchedule. Phase 1 changes JobSchedule to parse cron; Phase 2 changes the transport init default strings.

### Current Schyntax format strings used in transports

| Transport | File | UpdateTime Value | Meaning |
|-----------|------|------------------|---------|
| RelationalDatabase (SQL Server, PostgreSQL, SQLite) | `RelationalDatabaseMessageQueueInit.cs:144` | `"min(*%2)"` | Every 2 minutes |
| LiteDB | `LiteDbMessageQueueInit.cs:330` | `"sec(*%10)"` | Every 10 seconds |
| Redis | `RedisQueueInit.cs:321` | `"sec(*%10)"` | Every 10 seconds |

**Cron equivalents:**
| Schyntax | Cron (5-field) | Cron (6-field with seconds) |
|----------|----------------|----------------------------|
| `"min(*%2)"` | `"*/2 * * * *"` | N/A |
| `"sec(*%10)"` | N/A (no seconds in 5-field) | `"*/10 * * * * *"` |

### Integration test schedule strings

| File | Value | Meaning | Cron equivalent |
|------|-------|---------|-----------------|
| `JobSchedulerTestsShared.cs:40,44,100,104` | `"min(*)"` | Every minute | `"* * * * *"` |

---

## 10. Cronos API

**Package:** [Cronos](https://www.nuget.org/packages/Cronos) v0.12.0
**License:** MIT
**Total downloads:** ~52M across all versions
**Last published:** 2026-04-08 (v0.12.0)
**Target frameworks:** .NET 6.0, .NET Standard 1.0, .NET Standard 2.0, .NET Framework 4.0+ -- zero dependencies on net6.0+
**GitHub:** https://github.com/HangfireIO/Cronos

### Key API Surface

**Parse:**
```csharp
// 5-field standard cron (minute, hour, day-of-month, month, day-of-week)
CronExpression expr = CronExpression.Parse("*/5 * * * *");

// 6-field with seconds
CronExpression expr = CronExpression.Parse("*/10 * * * * *", CronFormat.IncludeSeconds);

// With jitter seed (v0.10+)
CronExpression expr = CronExpression.Parse("H H * * *", CronFormat.Standard, jitterSeed: 42);
```

**CronFormat enum:**
- `CronFormat.Standard` -- 5 fields (default)
- `CronFormat.IncludeSeconds` -- 6 fields (seconds first)

**GetNextOccurrence (returns nullable):**
```csharp
// UTC DateTime -- returns DateTime?
DateTime? next = expr.GetNextOccurrence(DateTime.UtcNow, inclusive: false);

// DateTimeOffset with TimeZone -- returns DateTimeOffset?
DateTimeOffset? next = expr.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc, inclusive: false);
```

**GetOccurrences (returns IEnumerable):**
```csharp
IEnumerable<DateTime> occurrences = expr.GetOccurrences(
    DateTime.UtcNow,
    DateTime.UtcNow.AddYears(1),
    fromInclusive: true,
    toInclusive: false);
```

**No Previous() method.** Cronos does not have a `GetPreviousOccurrence()`. To compute the previous occurrence, use `GetOccurrences(from, to)` over a lookback window and take the last element. This is the key API gap that requires the `Previous()` implementation strategy described in CONTEXT-1.md.

**ToString():** `CronExpression.ToString()` returns a normalized string representation. To preserve the original user-supplied text, store it separately.

**Thread safety:** `CronExpression` is immutable after parsing. Safe for concurrent reads.

**Performance benchmarks (from README):**
| Operation | Cronos | NCrontab | Quartz |
|-----------|--------|----------|--------|
| Parse simple | 30.8 ns | 1,813.7 ns | 48,157.8 ns |
| Parse complex | 81.5 ns | 3,174.4 ns | 33,700+ ns |
| GetNextOccurrence simple | 123.5 ns | 147.8 ns | 1,316.1 ns |
| GetNextOccurrence complex | 212.0 ns | 1,001.3 ns | 29,003.3 ns |

---

## 11. CronExpressionDescriptor API

**Package:** [CronExpressionDescriptor](https://www.nuget.org/packages/CronExpressionDescriptor) v2.45.0
**License:** MIT
**Total downloads:** ~4M across all versions
**Last published:** 2026-02-25 (v2.45.0)
**Target frameworks:** .NET 6.0, .NET Standard 1.1, .NET Standard 2.0 -- zero dependencies on net6.0+
**GitHub:** https://github.com/bradymholt/cron-expression-descriptor

### Key API

```csharp
using CronExpressionDescriptor;

// Basic usage
string desc = ExpressionDescriptor.GetDescription("* * * * *");
// => "Every minute"

// With options
string desc = ExpressionDescriptor.GetDescription("*/10 * * * * *", new Options {
    DayOfWeekStartIndexZero = true,
    Use24HourTimeFormat = true,
    Locale = "en"
});
```

**Options class properties:**
- `bool ThrowExceptionOnParseError` -- default: true
- `bool Verbose` -- default: false
- `bool DayOfWeekStartIndexZero` -- default: true
- `bool? Use24HourTimeFormat` -- default: false (some locales default to true)
- `string Locale` -- default: "en"

**Supports:** 5, 6 (with seconds or year), or 7 (with seconds and year) part cron expressions. 29 languages.

**Integration point:** `IJobSchedule.Description` property on the interface. Implementation in `JobSchedule` calls `ExpressionDescriptor.GetDescription(_originalText)` and caches the result (expression is immutable after construction).

---

## Summary of Changes Required for Phase 1

### Files to modify

| File | Change Type | Key Changes |
|------|-------------|-------------|
| `Source/Directory.Packages.props` | Add entries | Add Cronos 0.12.0 and CronExpressionDescriptor 2.45.0 |
| `Source/DotNetWorkQueue/DotNetWorkQueue.csproj` | Remove + Add | Remove Schyntax references + vendor target; Add Cronos + CronExpressionDescriptor PackageRefs |
| `Source/DotNetWorkQueue/IJobSchedule.cs` | Interface change | `Previous()` -> `DateTimeOffset?`; Add `Description` property |
| `Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs` | Rewrite | Replace Schyntax with Cronos; implement Previous via GetOccurrences; add Description via ExpressionDescriptor |
| `Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs` | Null-safety | Null-check `Schedule.Previous()` at line 98 |
| `Source/DotNetWorkQueue/IHeartBeatConfiguration.cs` | Doc comment | Update remarks from "schyntax format" to "cron format" |

### Files NOT modified in Phase 1

| File | Why deferred |
|------|-------------|
| Transport init files (3) | Phase 2 -- heartbeat default strings |
| Integration test files | Phase 3 -- test schedule strings |
| `Lib/Schyntax/` directory | Phase 5 -- cleanup after all phases pass |

### Critical design decisions (from CONTEXT-1.md)

1. **No new config class.** Reuse `ScheduledJob.Window` as the Previous() lookback bound.
2. **Keep `Func<DateTimeOffset>` constructor param.** Needed for parameterless `Next()`/`Previous()`.
3. **Auto-detect cron format.** Split on spaces: 5 fields = `CronFormat.Standard`, 6 fields = `CronFormat.IncludeSeconds`.
4. **Previous() returns `DateTimeOffset?`.** Null when no occurrence found in lookback window.

### Proposed JobSchedule implementation sketch

```csharp
using Cronos;
using CronExpressionDescriptor;

internal class JobSchedule : IJobSchedule
{
    private readonly CronExpression _expression;
    private readonly CronFormat _format;
    private readonly string _originalText;
    private readonly Func<DateTimeOffset> _getCurrentOffset;
    private readonly Lazy<string> _description;

    public JobSchedule(string schedule, Func<DateTimeOffset> getCurrentOffset)
    {
        _originalText = schedule;
        _getCurrentOffset = getCurrentOffset;
        _format = DetectFormat(schedule);
        _expression = CronExpression.Parse(schedule, _format);
        _description = new Lazy<string>(() => ExpressionDescriptor.GetDescription(schedule));
    }

    public string OriginalText => _originalText;
    public string Description => _description.Value;

    public DateTimeOffset Next()
    {
        var now = _getCurrentOffset();
        return _expression.GetNextOccurrence(now, TimeZoneInfo.Utc)
            ?? throw new InvalidOperationException("No next occurrence found.");
    }

    public DateTimeOffset Next(DateTimeOffset after)
    {
        return _expression.GetNextOccurrence(after, TimeZoneInfo.Utc)
            ?? throw new InvalidOperationException("No next occurrence found.");
    }

    public DateTimeOffset? Previous()
    {
        var now = _getCurrentOffset();
        return PreviousInternal(now);
    }

    public DateTimeOffset? Previous(DateTimeOffset atOrBefore)
    {
        return PreviousInternal(atOrBefore);
    }

    private DateTimeOffset? PreviousInternal(DateTimeOffset before)
    {
        // Default lookback of 48 hours; ScheduledJob.StartSchedule() already
        // validates against its own Window property, so a generous lookback is safe.
        var lookback = TimeSpan.FromHours(48);
        var from = before - lookback;

        DateTimeOffset? last = null;
        foreach (var occ in _expression.GetOccurrences(from.UtcDateTime, before.UtcDateTime, TimeZoneInfo.Utc, fromInclusive: true, toInclusive: true))
        {
            last = occ;
        }
        return last;
    }

    private static CronFormat DetectFormat(string schedule)
    {
        var fields = schedule.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return fields.Length switch
        {
            5 => CronFormat.Standard,
            6 => CronFormat.IncludeSeconds,
            _ => throw new ArgumentException(
                $"Cron expression must have 5 fields (standard) or 6 fields (with seconds). Got {fields.Length} fields: \"{schedule}\"")
        };
    }
}
```

### ScheduledJob.StartSchedule() null-safety fix

```csharp
// In StartSchedule(), replace lines 97-104:
var prev = Schedule.Previous();
if (prev.HasValue)
{
    lastKnownEvent = lastKnownEvent.AddSeconds(1);
    if (prev.Value > lastKnownEvent && prev.Value > new DateTimeOffset(_getTime.GetCurrentUtcDate()) - window)
    {
        firstEvent = prev.Value;
        firstEventSet = true;
    }
}
```

---

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `GetOccurrences` over 48h lookback is slow for second-granularity expressions | Low | Low | At `*/10 * * * * *` (every 10s), 48h = ~17,280 occurrences. Cronos iterates these in microseconds per its benchmarks. |
| `Next()` returns null for expressions that can never fire | Very Low | High | Throw `InvalidOperationException` with descriptive message. This mirrors Schyntax behavior (throws on impossible schedules). |
| CronExpressionDescriptor misinterprets 6-field cron (seconds) | Low | Low | CronExpressionDescriptor explicitly supports 5, 6, and 7-field expressions. Test with `"*/10 * * * * *"` during Phase 3. |
| Breaking API: callers of `IJobSchedule.Previous()` not updated | N/A for Phase 1 | High | `Previous()` is only called in `ScheduledJob.cs` (internal class). No external callers exist. Interface is public but Previous() is a niche method. |
| Cron format auto-detection fails on edge cases | Low | Medium | Only 5 and 6 fields are valid. Anything else throws immediately at parse time with a clear error message. |

---

## Sources

1. Cronos README: https://github.com/HangfireIO/Cronos/blob/master/README.md
2. Cronos NuGet: https://www.nuget.org/packages/Cronos (v0.12.0, accessed 2026-04-08)
3. CronExpressionDescriptor NuGet: https://www.nuget.org/packages/CronExpressionDescriptor (v2.45.0, accessed 2026-04-08)
4. CronExpressionDescriptor README: https://github.com/bradymholt/cron-expression-descriptor/blob/master/README.md
5. Cronos source (CronExpression.cs): https://github.com/HangfireIO/Cronos/blob/master/src/Cronos/CronExpression.cs

---

## Uncertainty Flags

- **GetOccurrences DateTimeOffset overload signature:** The Cronos README shows `DateTime` overloads. The `DateTimeOffset` overload with `TimeZoneInfo` was confirmed in source but the exact parameter order for `fromInclusive`/`toInclusive` on the `DateTimeOffset` overload should be verified at compile time. The implementation sketch above may need minor parameter adjustments.
- **CronExpressionDescriptor with 6-field seconds format:** The README says it supports 6-field expressions but does not specify whether it interprets the 6th field as seconds (Cronos convention) or year (Quartz convention). The `Options` class does not appear to have a format selector. This should be tested before finalizing Phase 1.
- **Cronos v0.12.0 just released today (2026-04-08):** The 0 downloads count suggests it is brand new. Consider pinning to v0.11.1 (3.8M downloads, 2025-08-12) for stability, unless v0.12.0 contains needed features.
