# Project: Replace Schyntax with Cronos (issue #100)

## Description

Schyntax is a bundled `/Lib` DLL with no XML docs, no symbols, and no NuGet package. It uses a custom DSL format that nobody outside this project knows, and it causes NuGet Package Explorer health warnings on the core DotNetWorkQueue package.

This milestone replaces Schyntax with Cronos (MIT, zero dependencies, standard cron expressions) and adds CronExpressionDescriptor for human-readable schedule descriptions in logging, the dashboard, and API responses. This is a breaking change — schedule strings change from Schyntax DSL to standard cron format, and the `Previous()` API becomes nullable. Version bumps to 0.9.3.

## Goals

1. Replace Schyntax with Cronos as the schedule parser in `JobSchedule.cs`
2. Add CronExpressionDescriptor for human-readable cron descriptions (logging, dashboard, API)
3. Support both 5-field (standard) and 6-field (with seconds) cron expressions, auto-detected by field count
4. Make `IJobSchedule.Previous()` return `DateTimeOffset?` (nullable) since Cronos has no native `Previous()`
5. Add configurable `PreviousLookbackWindow` (default 48h) to job scheduler configuration
6. Delete `Lib/Schyntax/` and the now-empty `Lib/` directory
7. Remove all Schyntax DLL references and NuGet pack entries from `DotNetWorkQueue.csproj`
8. Update all heartbeat default schedule strings from Schyntax to cron format
9. Update all test schedule strings from Schyntax to cron format
10. Update README.md, CLAUDE.md, and documentation
11. Bump version to 0.9.3

## Non-Goals

- Publishing Schyntax as a NuGet package (was considered, replaced by this approach)
- Changing the job scheduler architecture or API beyond what's needed for the Cronos swap
- Adding new scheduling features (e.g., natural language scheduling)
- Changing heartbeat intervals — only the format changes, not the values

## Requirements

### Core Library

- `JobSchedule.cs`: Replace `Schyntax.Schedule` wrapper with `CronExpression` from Cronos
- Auto-detect cron format: count space-separated fields — 5 = `CronFormat.Standard`, 6 = `CronFormat.IncludeSeconds`
- `Next()` / `Next(DateTimeOffset)` → `CronExpression.GetNextOccurrence()`
- `Previous()` / `Previous(DateTimeOffset)` → `CronExpression.GetOccurrences(now - lookback, now).LastOrDefault()`, returning `DateTimeOffset?`
- `IJobSchedule` interface: change `Previous()` and `Previous(DateTimeOffset)` return types to `DateTimeOffset?`
- Add `Description` property to `IJobSchedule` using CronExpressionDescriptor
- Add `PreviousLookbackWindow` property (type `TimeSpan`, default 48h) to job scheduler configuration
- `ScheduledJob.cs`: null-check `Previous()` result before using it for catch-up logic

### Heartbeat & Transports

- Update `IHeartBeatConfiguration.UpdateTime` doc comment from Schyntax reference to cron format
- LiteDB default: `"sec(*%10)"` → `"*/10 * * * * *"`
- Redis default: `"sec(*%10)"` → `"*/10 * * * * *"`
- RelationalDatabase default: `"min(*%2)"` → `"*/2 * * * *"`

### Tests

- `JobSchedulerTestsShared.cs`: `"min(*)"` → `"* * * * *"`
- `HeartBeatWorkerTests.cs`: `"sec(*%2)"` → `"*/2 * * * * *"`, `"sec(*%59)"` → `"*/59 * * * * *"`
- All per-transport `JobSchedulerTests.cs` files: Schyntax → cron equivalents
- `SharedSetup.cs`: any Schyntax strings → cron equivalents
- No test logic changes — string replacements only

### Cleanup

- Delete `Lib/Schyntax/` (net8.0 + net10.0 DLLs)
- Delete `Lib/` directory (empty after Schyntax + issue #102 removal)
- Remove TFM-conditional `<Reference Include="Schyntax">` ItemGroups from `DotNetWorkQueue.csproj`
- Remove `_PackageFiles` entries for Schyntax DLLs; remove `IncludeVendoredDllsInPack` target if empty

### Documentation

- README.md: replace Schyntax references with cron, update examples, update dependency list
- CLAUDE.md: update Key Dependencies section
- Add Cronos and CronExpressionDescriptor to dependency lists

## Non-Functional Requirements

- All existing tests must pass on net10.0 and net8.0 after migration
- Solution must build cleanly in both Debug and Release configurations
- No orphaned files or dead references left behind
- CronExpressionDescriptor used in logging, dashboard, and API responses

## Success Criteria

1. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` — 0 errors
2. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` — 0 errors, 0 warnings
3. `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"` — all tests pass
4. `grep -r "Schyntax\|schyntax" Source/ --include="*.cs" --include="*.csproj"` — 0 matches
5. `grep -r "Schyntax" Lib/` — directory does not exist
6. `ls Lib/` — directory does not exist
7. `IJobSchedule.Previous()` returns `DateTimeOffset?`
8. All heartbeat defaults use cron format
9. Version in DotNetWorkQueue.csproj is `0.9.3`

## Constraints

- Breaking change — version 0.9.3
- Schyntax `dayOfYear` support is dropped (no cron equivalent) — accepted
- `Previous()` implemented via `GetOccurrences()` lookback — configurable window (default 48h)
- Cronos is MIT, zero dependencies, targets netstandard1.0+ — no compatibility concerns
