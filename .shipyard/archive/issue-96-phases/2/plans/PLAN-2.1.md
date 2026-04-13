# Plan 2.1: Source-Aware Page Routing and Navigation

---
phase: phase-2
plan: "2.1"
wave: 2
dependencies: ["1.1"]
must_haves:
  - All 3 pages include {SourceSlug} route parameter
  - Source resolution via IMultiSourceDashboardApiClient.GetClientForSource(slug) in OnParametersSetAsync
  - Invalid slug shows MudAlert error with link to home
  - Guard against redundant re-resolution using _lastSourceSlug field
  - All NavigateTo calls and href attributes include /source/{slug} prefix
  - Home dual route handles single-source redirect and multi-source source list
  - Breadcrumbs include source name as first crumb when multiple sources configured
  - All 7 shared tab components receive resolved per-source IDashboardApiClient unchanged
files_touched:
  - Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor (modify)
  - Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/ConnectionDetail.razor (modify)
  - Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/QueueDetail.razor (modify)
tdd: false
risk: medium
---

## Context

This plan rewrites all 3 Blazor pages to be source-aware. Each page gets a `{SourceSlug}` route parameter, resolves the correct per-source `IDashboardApiClient` via `IMultiSourceDashboardApiClient.GetClientForSource(slug)`, and passes it to child components. All internal navigation links are updated to include the source slug prefix.

The Home page gets dual routes: `"/"` (for redirect/source-list logic) and `"/source/{SourceSlug}"` (for single-source connection list). ConnectionDetail and QueueDetail get single routes with the slug prefix.

All 7 shared tab components (`MessagesTab`, `ErrorsTab`, `StaleTab`, `HistoryTab`, `ConfigTab`, `ConsumersTab`, `MessageDetailDrawer`) continue receiving `IDashboardApiClient` as a `[Parameter]` with zero changes required.

**Key lifecycle note:** Use `OnParametersSetAsync` (not `OnInitializedAsync`) because Blazor re-invokes it when route parameters change. Guard with `_lastSourceSlug` to avoid redundant re-resolution.

## Dependencies

- Plan 1.1 complete: `ISourceHealthMonitor` registered and available for injection (needed by Home page for health indicators)
- Phase 1 complete: `IMultiSourceDashboardApiClient`, `ISourceRegistry` registered in DI

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor" tdd="false">
  <action>
Rewrite Home.razor with the following changes:

**Routes:**
```razor
@page "/"
@page "/source/{SourceSlug}"
```

**Injections** (replace `@inject IDashboardApiClient Api`):
```razor
@inject IMultiSourceDashboardApiClient MultiSourceClient
@inject ISourceRegistry SourceRegistry
@inject ISourceHealthMonitor HealthMonitor
@inject NavigationManager Navigation
```

**Parameters and fields:**
```csharp
[Parameter] public string? SourceSlug { get; set; }

private IDashboardApiClient? Api;
private bool _sourceError;
private string? _lastSourceSlug;
private List<ConnectionResponse>? _connections;
private bool _loading = true;
private string? _error;
```

**Lifecycle — replace `OnInitializedAsync` with `OnParametersSetAsync`:**

```csharp
protected override async Task OnParametersSetAsync()
{
    var sources = SourceRegistry.GetAll();

    // Route "/" with no slug
    if (string.IsNullOrEmpty(SourceSlug))
    {
        if (sources.Count == 1)
        {
            // Single source: redirect to /source/{onlySlug}
            Navigation.NavigateTo($"/source/{sources[0].Slug}", replace: true);
            return;
        }
        // Multi-source: show source list (no API client needed)
        _loading = false;
        return;
    }

    // Guard: skip if same slug as last time
    if (string.Equals(SourceSlug, _lastSourceSlug, StringComparison.Ordinal))
        return;
    _lastSourceSlug = SourceSlug;

    // Resolve source
    _sourceError = false;
    try
    {
        Api = MultiSourceClient.GetClientForSource(SourceSlug);
    }
    catch (KeyNotFoundException)
    {
        _sourceError = true;
        _loading = false;
        return;
    }

    await LoadConnections();
}
```

**Template changes:**

1. **Source error state** — before `_loading` check, add:
```razor
@if (_sourceError)
{
    <MudAlert Severity="Severity.Error" Class="mb-4">
        Source "@SourceSlug" not found.
        <MudLink Href="/">Return to source list</MudLink>
    </MudAlert>
}
```

2. **Multi-source list** (when `SourceSlug` is null and multiple sources) — after source error check, before `_loading`:
```razor
else if (string.IsNullOrEmpty(SourceSlug))
{
    <PageTitle>Dashboard - Sources</PageTitle>
    <MudBreadcrumbs Items="@(new List<BreadcrumbItem> { new("Sources", href: null, disabled: true) })" Class="mb-2" />
    <MudGrid>
        @foreach (var source in SourceRegistry.GetAll())
        {
            var health = HealthMonitor.GetHealth(source.Slug);
            <MudItem xs="12" sm="6" md="4">
                <MudCard Elevation="2" Class="cursor-pointer" @onclick="@(() => Navigation.NavigateTo($"/source/{source.Slug}"))">
                    <MudCardContent>
                        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
                            <MudIcon Icon="@(health.Status == SourceHealthStatus.Healthy ? Icons.Material.Filled.CheckCircle : health.Status == SourceHealthStatus.Unhealthy ? Icons.Material.Filled.Error : Icons.Material.Filled.HelpOutline)"
                                     Color="@(health.Status == SourceHealthStatus.Healthy ? Color.Success : health.Status == SourceHealthStatus.Unhealthy ? Color.Error : Color.Default)"
                                     Size="Size.Medium" />
                            <MudText Typo="Typo.h6">@source.Name</MudText>
                        </MudStack>
                        @if (health.Status == SourceHealthStatus.Unhealthy && !string.IsNullOrEmpty(health.ErrorMessage))
                        {
                            <MudText Typo="Typo.caption" Color="Color.Error" Class="mt-1">@health.ErrorMessage</MudText>
                        }
                    </MudCardContent>
                </MudCard>
            </MudItem>
        }
    </MudGrid>
}
```

3. **Single-source connections view** — the existing `_loading`/`_error`/table markup stays mostly the same, wrapped in an `else` block. Key changes:
   - Breadcrumbs: if multiple sources, add source name as first crumb: `new BreadcrumbItem(sourceName, href: "/")` then `new BreadcrumbItem("Connections", href: null, disabled: true)`. If single source, keep just `new BreadcrumbItem("Connections", href: null, disabled: true)`.
   - `NavigateToConnection` changes to: `Navigation.NavigateTo($"/source/{SourceSlug}/connections/{connectionId}")`

4. **Remove** `[Inject] private NavigationManager Navigation { get; set; } = default!;` from `@code` block (now using `@inject` at top).

Keep `LoadConnections()` method unchanged (it uses the page-level `Api` field).
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"</verify>
  <done>Home.razor compiles. Has dual routes `"/"` and `"/source/{SourceSlug}"`. Single-source redirects to `/source/{slug}`. Multi-source shows source list with health indicators. Invalid slug shows error alert. Navigation to connections includes `/source/{slug}` prefix.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/ConnectionDetail.razor" tdd="false">
  <action>
Rewrite ConnectionDetail.razor with the following changes:

**Route:**
```razor
@page "/source/{SourceSlug}/connections/{ConnectionId:guid}"
```

**Injections** (replace `@inject IDashboardApiClient Api`):
```razor
@inject IMultiSourceDashboardApiClient MultiSourceClient
@inject ISourceRegistry SourceRegistry
@inject NavigationManager Navigation
```

**Parameters and fields:**
```csharp
[Parameter] public string SourceSlug { get; set; } = default!;
[Parameter] public Guid ConnectionId { get; set; }

private IDashboardApiClient? Api;
private bool _sourceError;
private string? _lastSourceSlug;
// ... keep all existing fields
```

**Lifecycle — replace `OnInitializedAsync` with `OnParametersSetAsync`:**
```csharp
protected override async Task OnParametersSetAsync()
{
    // Guard: skip if same slug
    if (string.Equals(SourceSlug, _lastSourceSlug, StringComparison.Ordinal))
        return;
    _lastSourceSlug = SourceSlug;

    _sourceError = false;
    try
    {
        Api = MultiSourceClient.GetClientForSource(SourceSlug);
    }
    catch (KeyNotFoundException)
    {
        _sourceError = true;
        _loading = false;
        return;
    }

    await LoadData();
}
```

**Template changes:**

1. Add source error block before `_loading` check:
```razor
@if (_sourceError)
{
    <MudAlert Severity="Severity.Error" Class="mb-4">
        Source "@SourceSlug" not found.
        <MudLink Href="/">Return to source list</MudLink>
    </MudAlert>
}
else if (_loading)
...
```

2. **Breadcrumbs** — update `_breadcrumbs` construction in `LoadData()`:
```csharp
var crumbs = new List<BreadcrumbItem>();
var sources = SourceRegistry.GetAll();
if (sources.Count > 1)
{
    var sourceName = SourceRegistry.GetBySlug(SourceSlug)?.Name ?? SourceSlug;
    crumbs.Add(new BreadcrumbItem(sourceName, href: "/"));
}
crumbs.Add(new BreadcrumbItem("Connections", href: $"/source/{SourceSlug}"));
crumbs.Add(new BreadcrumbItem(_connectionName ?? "Connection", href: null, disabled: true));
_breadcrumbs = crumbs;
```

3. **Default breadcrumbs** field initialization — update to use lazy initialization or a simple default:
```csharp
private List<BreadcrumbItem> _breadcrumbs = new()
{
    new BreadcrumbItem("Connections", href: null, disabled: true),
    new BreadcrumbItem("Connection", href: null, disabled: true)
};
```
(The real breadcrumbs with proper hrefs are set in `LoadData()` after `SourceSlug` is available.)

4. **NavigateToQueue** — update to include source slug:
```csharp
Navigation.NavigateTo($"/source/{SourceSlug}/queues/{queue.Id}?conn={connName}&connId={ConnectionId}&queue={queueName}");
```

5. **Guard API calls** — in `LoadData()`, add early return if `Api` is null (source error case).
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"</verify>
  <done>ConnectionDetail.razor compiles. Route is `/source/{SourceSlug}/connections/{ConnectionId:guid}`. Source resolution via `OnParametersSetAsync`. Breadcrumbs include source name when multiple sources. NavigateToQueue includes source slug. Invalid slug shows error alert.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/QueueDetail.razor" tdd="false">
  <action>
Rewrite QueueDetail.razor with the following changes:

**Route:**
```razor
@page "/source/{SourceSlug}/queues/{QueueId:guid}"
```

**Injections** (replace `@inject IDashboardApiClient Api`):
```razor
@inject IMultiSourceDashboardApiClient MultiSourceClient
@inject ISourceRegistry SourceRegistry
@inject ISnackbar Snackbar
@inject NavigationManager Navigation
```

**Parameters and fields:**
```csharp
[Parameter] public string SourceSlug { get; set; } = default!;
[Parameter] public Guid QueueId { get; set; }

private IDashboardApiClient? Api;
private bool _sourceError;
private string? _lastSourceSlug;
// ... keep all existing fields
```

**Lifecycle — replace `OnInitializedAsync` with `OnParametersSetAsync`:**
```csharp
protected override async Task OnParametersSetAsync()
{
    // Guard: skip if same slug
    if (string.Equals(SourceSlug, _lastSourceSlug, StringComparison.Ordinal))
        return;
    _lastSourceSlug = SourceSlug;

    _sourceError = false;
    try
    {
        Api = MultiSourceClient.GetClientForSource(SourceSlug);
    }
    catch (KeyNotFoundException)
    {
        _sourceError = true;
        _initialLoading = false;
        return;
    }

    // Parse query params (same as existing OnInitializedAsync logic)
    var uri = new Uri(Navigation.Uri);
    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
    var connName = query["conn"];
    _connectionId = query["connId"];
    _queueName = query["queue"];
    _connectionDisplayName = connName;
    var connId = _connectionId;

    // Fallback: look up names from the API if not in query string
    if (string.IsNullOrEmpty(_queueName) || string.IsNullOrEmpty(connName))
    {
        try
        {
            var connections = await Api.GetConnectionsAsync();
            foreach (var conn in connections)
            {
                var queues = await Api.GetQueuesAsync(conn.Id);
                var match = queues.FirstOrDefault(q => q.Id == QueueId);
                if (match != null)
                {
                    _queueName ??= match.QueueName;
                    _connectionDisplayName ??= conn.DisplayName;
                    connName = conn.DisplayName;
                    connId = conn.Id.ToString();
                    _connectionId = connId;
                    break;
                }
            }
        }
        catch { }
    }

    // Build breadcrumbs with source slug prefix
    var crumbs = new List<BreadcrumbItem>();
    var sources = SourceRegistry.GetAll();
    if (sources.Count > 1)
    {
        var sourceName = SourceRegistry.GetBySlug(SourceSlug)?.Name ?? SourceSlug;
        crumbs.Add(new BreadcrumbItem(sourceName, href: "/"));
    }
    crumbs.Add(new BreadcrumbItem("Connections", href: $"/source/{SourceSlug}"));
    if (!string.IsNullOrEmpty(connName) && !string.IsNullOrEmpty(connId))
        crumbs.Add(new BreadcrumbItem(connName, href: $"/source/{SourceSlug}/connections/{connId}"));
    crumbs.Add(new BreadcrumbItem(_queueName ?? "Queue", href: null, disabled: true));
    _breadcrumbs = crumbs;

    await LoadInitial();
}
```

**Template changes:**

1. Add source error block before `_initialLoading` check:
```razor
@if (_sourceError)
{
    <MudAlert Severity="Severity.Error" Class="mb-4">
        Source "@SourceSlug" not found.
        <MudLink Href="/">Return to source list</MudLink>
    </MudAlert>
}
else if (_initialLoading)
...
```

2. **Connection link in header** (around line 15) — update href to include source slug:
```razor
<MudLink Typo="Typo.h5" Href="@($"/source/{SourceSlug}/connections/{_connectionId}")">@_connectionDisplayName</MudLink>
```

3. **Default breadcrumbs** — simplify initialization:
```csharp
private List<BreadcrumbItem> _breadcrumbs = new()
{
    new BreadcrumbItem("Connections", href: null, disabled: true),
    new BreadcrumbItem("Queue", href: null, disabled: true)
};
```

4. **Guard API calls** — in `LoadInitial()`, `RefreshCurrentTab()`, and `RefreshCounts()`, add early return if `Api` is null.

5. **Tab components** — no changes needed. They already receive `Api="Api"` as a parameter. Since `Api` is now the resolved per-source client field, they automatically get the correct source's client.
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.sln" -c Debug</verify>
  <done>QueueDetail.razor compiles. Route is `/source/{SourceSlug}/queues/{QueueId:guid}`. Source resolution via `OnParametersSetAsync`. All 7 tab components and MessageDetailDrawer receive per-source client via `Api="Api"`. Breadcrumbs include source name when multiple sources. Connection link in header includes source slug. Full solution builds with 0 errors. All existing unit tests still pass.</done>
</task>

## Verification

```bash
# Build entire solution (confirms all 3 pages compile with new routes)
dotnet build "Source/DotNetWorkQueue.sln" -c Debug

# Run all Dashboard UI tests (Phase 1 + Phase 2)
dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj"

# Run existing Dashboard API integration tests (memory-based, no external services)
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory"
```

Expected:
- Solution builds with 0 errors
- All 47+ Dashboard UI tests pass (40 Phase 1 + 7 health monitor)
- All existing Dashboard API integration tests pass unchanged (API layer untouched)
- Navigating to `/` with single source redirects to `/source/{slug}`
- Navigating to `/` with multiple sources shows source list with health icons
- All page URLs follow pattern `/source/{slug}/...`
- Invalid source slug shows error alert on any page
