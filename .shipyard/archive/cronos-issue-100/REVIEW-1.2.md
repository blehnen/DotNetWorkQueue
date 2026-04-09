# Review: Plan 1.2

## Verdict: PASS

## Stage 1: Spec Compliance

### Task 1: Rewrite JobSchedule.cs to use Cronos
- Status: PASS
- Evidence:
  - `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs` lines 21-22: `using Cronos;` and `using CronExpressionDescriptor;` present. No Schyntax imports.
  - Line 28: `private readonly CronExpression _expression;` field declared.
  - Lines 29-31: `_originalText`, `_getCurrentOffset`, and `_description` (Lazy) fields match the plan.
  - Lines 38-46: Auto-detect logic splits on spaces with `StringSplitOptions.RemoveEmptyEntries`, uses switch expression for 5 -> `CronFormat.Standard`, 6 -> `CronFormat.IncludeSeconds`, else throws `ArgumentException` with field count in message.
  - Line 48: `CronExpression.Parse(schedule, format)` -- correct Cronos API usage.
  - Line 49: `new Lazy<string>(() => ExpressionDescriptor.GetDescription(schedule))` -- cached description.
  - Lines 52-53: `OriginalText` and `Description` properties implemented.
  - Lines 56-72: `Next()` and `Next(DateTimeOffset)` call `GetNextOccurrence` with `TimeZoneInfo.Utc`, throw `InvalidOperationException` with descriptive message if null.
  - Lines 74-81: `Previous()` and `Previous(DateTimeOffset)` return `DateTimeOffset?`, delegate to `PreviousInternal`.
  - Lines 84-100: `PreviousInternal` uses 48h lookback, calls `GetOccurrences(DateTime, DateTime, TimeZoneInfo, fromInclusive, toInclusive)` with `.UtcDateTime` conversion, iterates to find last occurrence, converts back to `DateTimeOffset` with `TimeSpan.Zero` offset.
  - LGPL license header preserved (lines 1-18).
- Notes: Implementation matches the plan precisely. The `DateTime` overload of `GetOccurrences` is used as recommended in the plan's "Important" note. The foreach loop to find the last element is functionally equivalent to LINQ `.LastOrDefault()` but avoids allocations -- reasonable choice.

### Task 2: ScheduledJob.cs null-check Previous()
- Status: PASS
- Evidence:
  - `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs` lines 98-107: The catch-up block is wrapped in `if (prev.HasValue)`. Inside the block, `prev.Value` is used for both comparisons (line 102) and assignment (line 104).
  - When `Previous()` returns null, execution falls through to line 111 (`Schedule.Next()`), which is the correct behavior.
  - The code matches the exact replacement specified in the plan.
- Notes: The null-handling is correct and complete. No other callers of `Previous()` exist that would need similar fixes.

### Task 3: Build verification
- Status: PASS
- Evidence:
  - Release build of `DotNetWorkQueue.csproj` succeeded with 0 warnings, 0 errors (verified live during this review).
  - Git history shows commits `2b177e23` (JobSchedule rewrite) and `dc83c889` (ScheduledJob null-check) landed cleanly.
  - Grep for `Schyntax|schyntax` across `Source/DotNetWorkQueue/**/*.{cs,csproj}` returned 0 matches.
- Notes: Both net10.0 and net8.0 TFMs built successfully.

## Stage 2: Code Quality

### Critical
None.

### Important
None.

### Suggestions
- **Unused `using System.Linq` directive** at `/mnt/f/git/dotnetworkqueue/Source/DotNetWorkQueue/JobScheduler/JobSchedule.cs` line 20: The `System.Linq` namespace is imported but no LINQ methods are called (the `PreviousInternal` method uses a manual `foreach` loop). While this does not cause a build warning (likely suppressed by ImplicitUsings), it is dead code.
  - Remediation: Remove `using System.Linq;` from line 20.

### Positive
- Cronos API usage is correct throughout: `GetNextOccurrence(DateTimeOffset, TimeZoneInfo)` for Next(), `GetOccurrences(DateTime, DateTime, TimeZoneInfo, bool, bool)` for Previous(). The DateTime overload with explicit TimeZoneInfo is the most reliable approach per the plan's guidance.
- The 48h hardcoded lookback in `PreviousInternal` is the right design per CONTEXT-1.md: `ScheduledJob` already validates `prev > now - window` at line 102, so a generous internal lookback is safe and avoids coupling `JobSchedule` to the `Window` concept.
- The `Lazy<string>` for Description avoids calling CronExpressionDescriptor until needed, which is good since not all code paths need human-readable descriptions.
- Thread safety in `ScheduledJob` is unchanged -- the `_scheduleLock` and `Interlocked` patterns are preserved.
- Switch expression for format detection (lines 39-46) is clean and produces a clear error message including the actual field count and the input expression.
- LGPL headers preserved on both files.

## Summary
**Verdict:** APPROVE
All three tasks implemented exactly as specified. JobSchedule fully rewritten from Schyntax to Cronos with correct API usage, auto-detection of 5/6-field formats, nullable Previous() with 48h lookback, cached Description via CronExpressionDescriptor, and InvalidOperationException on null Next(). ScheduledJob null-checks Previous() correctly. Release build passes with 0 warnings/errors and no Schyntax references remain.
Critical: 0 | Important: 0 | Suggestions: 1
