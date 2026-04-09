# Research: Phase 3 — Home Page Grouping, Partial Failure UX, Integration Tests

## 1. Current Home.razor (Post-Phase 2)
- Dual routes: `@page "/"` and `@page "/source/{SourceSlug}"`
- Injects: `IMultiSourceDashboardApiClient`, `ISourceRegistry`, `ISourceHealthMonitor`, `NavigationManager`
- When `SourceSlug` is null: single source → redirect; multi source → shows basic source list with health cards
- When `SourceSlug` is set: loads connections via `Api.GetConnectionsAsync()`, shows flat table
- Phase 3 must: replace the basic multi-source list with grouped connections per source, add MudExpansionPanels, add partial failure handling

## 2. DashboardTestServer
- File: `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Helpers/DashboardTestServer.cs`
- `CreateAsync(Action<DashboardOptions> configure)` — creates WebApplication with TestServer, registers dashboard, starts, returns HttpClient
- `IAsyncDisposable` — stops and disposes app
- Uses `Microsoft.AspNetCore.TestHost`
- Configuration via `DashboardOptions` callback (AddConnection pattern)

## 3. Integration Test Patterns
- Tests in `Tests/` subdirectory, use MSTest `[TestClass]`/`[TestMethod]`
- `TransportFixture` provides shared `DashboardTestServer` via `[AssemblyInitialize]`
- Test classes use `DashboardApiClient(server.Client)` to call endpoints
- Memory transport tests need no external services

## 4. Key API Surface
- `IDashboardApiClient.GetConnectionsAsync()` returns `Task<List<ConnectionResponse>>`
- `ConnectionResponse` has at minimum: `Id` (Guid), name/connection info
- `IDashboardApiClient.GetSettingsAsync()` used for health polling

## 5. MudBlazor Components Available
- `_Imports.razor` imports `@using MudBlazor` — all components available
- Needed: `MudExpansionPanels`, `MudExpansionPanel`, `MudAlert`, `MudIcon`, `MudButton`, `MudText`
- Already used in pages: `MudBreadcrumbs`, `MudAlert`, `MudCard`, `MudTable`

## 6. SourceConnectionGroup View Model
Design for grouped display:
```csharp
public class SourceConnectionGroup
{
    public DashboardApiSourceConfig Source { get; init; }
    public SourceHealthState Health { get; init; }
    public List<ConnectionResponse>? Connections { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLoading { get; set; }
}
```

## 7. Multi-Source Integration Test Design
- Create 2 `DashboardTestServer` instances with Memory transport (different connection names)
- Create `DashboardApiClient` for each test server's HttpClient
- Verify: each server returns its own connections
- Verify: connections from server A don't appear in server B
- For health test: dispose one server, verify health transitions
- For partial failure: dispose one server, verify other's connections still load

## 8. Project Reference Note
`Dashboard.Api.Integration.Tests.csproj` references `DotNetWorkQueue.Dashboard.Api` but NOT `DotNetWorkQueue.Dashboard.Ui`. The multi-source integration tests operate at the API client level (creating `DashboardApiClient` instances directly), not the Blazor page level. This is correct — we're testing that the multi-source client routing works with real API instances.
