# Plan 1.1: Health Monitoring Service with Unit Tests

---
phase: phase-2
plan: "1.1"
wave: 1
dependencies: [phase-1]
must_haves:
  - ISourceHealthMonitor interface with GetHealth(slug) returning SourceHealthState
  - SourceHealthMonitor BackgroundService polling GetSettingsAsync every 30s with 5s timeout
  - SourceHealthState record with Status enum (Unknown/Healthy/Unhealthy), LastChecked, ErrorMessage
  - Thread-safe ConcurrentDictionary for health state caching
  - State transition logging (healthy-to-unhealthy, unhealthy-to-healthy)
  - Unit tests covering all health state transitions and polling behavior
  - Registration in Program.cs as hosted service
files_touched:
  - Source/DotNetWorkQueue.Dashboard.Ui/Services/ISourceHealthMonitor.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui/Services/SourceHealthMonitor.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui/Services/SourceHealthState.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui/Program.cs (add hosted service registration)
  - Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/SourceHealthMonitorTests.cs (new)
tdd: true
risk: low
---

## Context

This plan creates the background health monitoring service that polls each configured API source to determine its availability. The health state is consumed by the Home page (in Wave 2) to display source health indicators. This is a standalone service with no UI coupling -- it reads from `ISourceRegistry` and `IMultiSourceDashboardApiClient` (both delivered in Phase 1) and exposes cached health state via `ISourceHealthMonitor.GetHealth(slug)`.

The health check uses `GetSettingsAsync()` (GET api/v1/dashboard/settings) as a lightweight probe. A 5-second `HttpClient` timeout prevents slow sources from blocking the polling loop. All sources are polled sequentially within each 30-second timer tick to avoid health poll storms.

## Dependencies

- Phase 1 complete: `ISourceRegistry`, `IMultiSourceDashboardApiClient`, `DashboardApiSourceConfig` all exist
- Phase 1 test project: `DotNetWorkQueue.Dashboard.Ui.Tests` exists with MSTest + NSubstitute + FluentAssertions

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Dashboard.Ui/Services/SourceHealthState.cs, Source/DotNetWorkQueue.Dashboard.Ui/Services/ISourceHealthMonitor.cs" tdd="false">
  <action>
Create two new files:

**SourceHealthState.cs:** A record (or class) in `DotNetWorkQueue.Dashboard.Ui.Services` namespace with:
- `SourceHealthStatus` enum: `Unknown = 0`, `Healthy = 1`, `Unhealthy = 2`
- `SourceHealthState` class/record with properties: `SourceHealthStatus Status`, `DateTimeOffset LastChecked`, `string? ErrorMessage`
- Include LGPL-2.1 license header

**ISourceHealthMonitor.cs:** Interface in same namespace with:
- `SourceHealthState GetHealth(string slug)` — returns cached health state for a source slug. Returns a default `Unknown` state if slug has not been polled yet.
- `IReadOnlyDictionary<string, SourceHealthState> GetAllHealth()` — returns health state for all sources.
- Include LGPL-2.1 license header
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"</verify>
  <done>Build succeeds with 0 errors. `ISourceHealthMonitor.cs` and `SourceHealthState.cs` exist in `Source/DotNetWorkQueue.Dashboard.Ui/Services/` with correct namespace, LGPL header, and the specified members.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Dashboard.Ui/Services/SourceHealthMonitor.cs, Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/SourceHealthMonitorTests.cs" tdd="true">
  <action>
**Write tests first** in `SourceHealthMonitorTests.cs` (`#nullable enable`, LGPL header, namespace `DotNetWorkQueue.Dashboard.Ui.Tests.Services`):

1. `GetHealth_Returns_Unknown_For_Unpolled_Source` — before any polling, `GetHealth("some-slug")` returns state with `Status == Unknown`
2. `GetAllHealth_Returns_Empty_Before_Polling` — `GetAllHealth()` returns empty dictionary before first poll
3. `PollAsync_Sets_Healthy_When_GetSettingsAsync_Succeeds` — mock `IMultiSourceDashboardApiClient` to return a source with `GetSettingsAsync()` succeeding. After one poll cycle, `GetHealth(slug)` returns `Healthy` with a `LastChecked` timestamp
4. `PollAsync_Sets_Unhealthy_When_GetSettingsAsync_Throws` — mock `GetSettingsAsync()` to throw `HttpRequestException`. After one poll cycle, `GetHealth(slug)` returns `Unhealthy` with the exception message in `ErrorMessage`
5. `PollAsync_Transitions_Healthy_To_Unhealthy` — first call succeeds (Healthy), second call throws (Unhealthy). Verify state transitions correctly.
6. `PollAsync_Transitions_Unhealthy_To_Healthy` — first call throws (Unhealthy), second call succeeds (Healthy). Verify recovery.
7. `PollAsync_Logs_State_Transitions` — verify `ILogger` receives log calls on Healthy-to-Unhealthy and Unhealthy-to-Healthy transitions (not on same-state polls)

**Then implement** `SourceHealthMonitor.cs` in `DotNetWorkQueue.Dashboard.Ui.Services`:
- Extends `BackgroundService` (inherits from `Microsoft.Extensions.Hosting.BackgroundService`)
- Constructor takes: `IMultiSourceDashboardApiClient multiSourceClient`, `ISourceRegistry sourceRegistry`, `ILogger<SourceHealthMonitor> logger`
- `ConcurrentDictionary<string, SourceHealthState>` for cached state
- `ExecuteAsync` loop: every 30 seconds, iterate all sources from `sourceRegistry.GetAll()`, call `multiSourceClient.GetClientForSource(slug)` then `client.GetSettingsAsync()` with a 5-second `CancellationTokenSource` timeout
- On success: set state to Healthy, clear ErrorMessage
- On exception (any): set state to Unhealthy, capture `ex.Message`
- Log state transitions at `LogLevel.Information` (e.g., "Source '{SourceName}' is now Healthy" / "Source '{SourceName}' is now Unhealthy: {ErrorMessage}")
- `GetHealth(slug)`: return from dictionary, or `new SourceHealthState { Status = SourceHealthStatus.Unknown }` if not present
- `GetAllHealth()`: return a snapshot of the dictionary as `IReadOnlyDictionary`
- Expose a `internal` method `PollAllSourcesAsync(CancellationToken)` that tests can call directly instead of waiting for the timer loop. The `ExecuteAsync` loop calls this same method.
- Include LGPL-2.1 license header

To make testing possible without real timers: the `ExecuteAsync` method should call `PollAllSourcesAsync` in a loop with `Task.Delay(TimeSpan.FromSeconds(30), stoppingToken)`. Tests call `PollAllSourcesAsync` directly.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" --filter "FullyQualifiedName~SourceHealthMonitorTests"</verify>
  <done>All 7 SourceHealthMonitorTests pass on both net10.0 and net8.0. `SourceHealthMonitor` compiles as a `BackgroundService` with `ConcurrentDictionary` caching, 30s polling, 5s timeout, and state transition logging.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Dashboard.Ui/Program.cs" tdd="false">
  <action>
In `Program.cs`, add the hosted service registration for `SourceHealthMonitor`. Insert the line after the existing `IMultiSourceDashboardApiClient` singleton registration (around line 79):

```csharp
builder.Services.AddSingleton<ISourceHealthMonitor, SourceHealthMonitor>();
builder.Services.AddHostedService(sp => (SourceHealthMonitor)sp.GetRequiredService<ISourceHealthMonitor>());
```

This pattern registers `SourceHealthMonitor` once as both `ISourceHealthMonitor` (for page injection) and as a hosted service (for background polling). The cast-based `AddHostedService` overload avoids creating a second instance.

No other changes to Program.cs in this plan.
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.sln" -c Debug</verify>
  <done>Full solution builds with 0 errors. `ISourceHealthMonitor` is registered as a singleton and `SourceHealthMonitor` is registered as a hosted service in Program.cs. All 40+ existing tests plus the 7 new health monitor tests pass.</done>
</task>

## Verification

```bash
# Build entire solution
dotnet build "Source/DotNetWorkQueue.sln" -c Debug

# Run all Dashboard UI tests (Phase 1 + Phase 2 health monitor)
dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj"

# Verify health monitor tests specifically
dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" --filter "FullyQualifiedName~SourceHealthMonitorTests"
```

Expected: 47+ tests pass (40 from Phase 1 + 7 new). Solution builds with 0 errors.
