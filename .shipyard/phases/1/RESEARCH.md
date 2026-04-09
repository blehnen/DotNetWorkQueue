# Research: Phase 1 — Multi-Source Configuration and Client Infrastructure

## Context

DotNetWorkQueue Dashboard UI (`DotNetWorkQueue.Dashboard.Ui`) is a Blazor Server application using MudBlazor, currently configured for a single API source. Phase 1 transforms it to support multiple API sources via a new config model, source registry, and multi-source client wrapper.

## Investigation Results

### 1. Current DI Registration and Program.cs

**File:** `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs`

**Current DI setup (lines 37-52):**
- Self-contained mode detection: checks `Dashboard:Connections` section for children (line 38). If present, calls `builder.Services.AddDotNetWorkQueueDashboard(dashboardSection)` (line 42).
- API client registration (lines 45-52): reads `DashboardApi:BaseUrl` (default `http://localhost:5000`) and `DashboardApi:ApiKey`, then registers via `AddHttpClient<IDashboardApiClient, DashboardApiClient>()` — typed HttpClient pattern from `Microsoft.Extensions.Http`. The HttpClient gets `BaseAddress` and optionally `X-Api-Key` header configured in the factory lambda.
- Authentication (lines 55-84): reads `DashboardAuth:Username` and `DashboardAuth:PasswordHash`, registers `DashboardAuthConfig` singleton, sets up cookie auth.
- After `builder.Build()` (line 86): conditionally calls `app.UseDotNetWorkQueueDashboard()` and `app.MapControllers()` for self-contained mode.

**Key observation:** The typed HttpClient pattern (`AddHttpClient<IDashboardApiClient, DashboardApiClient>`) means `DashboardApiClient` gets a pre-configured `HttpClient` injected via constructor. For multi-source, we need named HttpClients per source, each with different `BaseAddress` and `X-Api-Key`.

**IServer timing:** `IServer.Features.Get<IServerAddressesFeature>()` is only available after `app.Start()` / `app.Run()`. Current `Program.cs` calls `app.Run()` at line 141, which blocks. In-process source registration needs an `IHostedService` or `ApplicationStarted` callback.

### 2. Existing DashboardApiClient

**File:** `Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardApiClient.cs`

- Constructor takes `HttpClient` (line 33) — typed client pattern
- Uses relative path `api/v1/dashboard` as base for all endpoints (line 32)
- **26 public methods** matching `IDashboardApiClient.cs`
- All methods use `System.Net.Http.Json` extension methods (`GetFromJsonAsync`, `PutAsJsonAsync`, `PostAsync`, `DeleteAsync`)
- Null-coalescing to empty collections/objects throughout
- No retry/resilience logic
- **Stateless** — safe to cache instances

### 3. Services Directory

Three files exist:
- `DashboardApiClient.cs` — the implementation
- `IDashboardApiClient.cs` — the interface (26 methods)
- `DashboardAuthConfig.cs` — simple POCO with `IsEnabled`, `Username`, `PasswordHash`

New files to create here: `DashboardApiSourceConfig.cs`, `ISourceRegistry.cs`, `SourceRegistry.cs`, `IMultiSourceDashboardApiClient.cs`, `MultiSourceDashboardApiClient.cs`, `DashboardConfigParser.cs`, `LocalSourceHostedService.cs`.

### 4. Current Config Format

**File:** `Source/DotNetWorkQueue.Dashboard.Ui/appsettings.json`

```json
{
  "DashboardApi": {
    "BaseUrl": "http://localhost:5000",
    "ApiKey": ""
  },
  "DashboardAuth": {
    "Username": "",
    "PasswordHash": ""
  }
}
```

New format replaces `DashboardApi:BaseUrl`/`ApiKey` with `DashboardApi:Sources[]` array.

### 5. Blazor Page Injection Pattern

**Pages injecting `IDashboardApiClient`:**
- `Home.razor` (line 2): `@inject IDashboardApiClient Api`
- `ConnectionDetail.razor` (line 2): `@inject IDashboardApiClient Api`
- `QueueDetail.razor` (line 2): `@inject IDashboardApiClient Api`

**Shared components (parameter, not injected):**
- `MessagesTab.razor`, `StaleTab.razor`, `HistoryTab.razor`, `ErrorsTab.razor`, `ConfigTab.razor`, `ConsumersTab.razor`, `MessageDetailDrawer.razor` — all use `[Parameter] public IDashboardApiClient Api { get; set; } = default!;`

**Global imports** (`_Imports.razor` line 16): `@using DotNetWorkQueue.Dashboard.Ui.Services` — new services auto-available.

**Phase 1 impact:** Removing `IDashboardApiClient` DI breaks 3 pages at runtime. Expected per CONTEXT-1.md. Phase 2 fixes pages. 7 shared components use `[Parameter]` and are unaffected.

### 6. Test Project Structure

**Reference:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj`
- `<TargetFrameworks>net10.0;net8.0</TargetFrameworks>`
- Central package management (no Version attributes)
- Packages: AutoFixture, AutoFixture.AutoNSubstitute, FluentAssertions (6.12.2), Microsoft.NET.Test.Sdk, coverlet.collector, MSTest.TestFramework, MSTest.TestAdapter, NSubstitute
- No existing `DotNetWorkQueue.Dashboard.Ui.Tests` project in either `.sln`

### 7. Dashboard.Ui Project File

**File:** `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj`
- SDK: `Microsoft.NET.Sdk.Web` (Blazor Server)
- Targets: `net10.0;net8.0`
- `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`
- Dependencies: `MudBlazor` (9.1.0 via central management)
- Project reference: `DotNetWorkQueue.Dashboard.Api`

### 8. Dashboard API Integration Test Server

**File:** `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Helpers/DashboardTestServer.cs`
- Uses `WebApplication.CreateBuilder()` + `builder.WebHost.UseTestServer()` + `Microsoft.AspNetCore.TestHost`
- `IServer` IS available after `StartAsync()` — confirms the hosted service approach works
- No existing usage of `IServerAddressesFeature` anywhere in the codebase

## Recommendations

- **DI strategy:** Use `IHttpClientFactory` named clients per source. `MultiSourceDashboardApiClient` takes `IHttpClientFactory` + `ISourceRegistry`, creates `DashboardApiClient` instances via `GetOrAdd` on ConcurrentDictionary.
- **Config parsing:** Extract to static `DashboardConfigParser` class for testability (not inline in Program.cs).
- **appsettings.json:** Must be updated to new `Sources[]` format during build to avoid startup exception.
- **DashboardApiClient is public** — can be directly instantiated with `HttpClient` from factory.
- **DashboardAuthConfig pattern** — follow same POCO style for `DashboardApiSourceConfig`.
