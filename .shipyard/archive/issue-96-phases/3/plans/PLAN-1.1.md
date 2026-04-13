---
phase: home-page-grouping
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - SourceConnectionGroup view model for grouped display
  - Multi-source Home page shows MudExpansionPanels grouped by source
  - Single-source Home page shows flat connection table (no panels)
  - Offline source shows MudAlert warning with Retry button
  - Partial request failure shows per-source inline error without affecting other sources
files_touched:
  - Source/DotNetWorkQueue.Dashboard.Ui/Models/SourceConnectionGroup.cs
  - Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor
tdd: false
risk: low
---

# Plan 1.1: SourceConnectionGroup Model + Home Page Grouped Display

## Context

Phase 2 left Home.razor with two modes: (1) multi-source shows a card grid of sources with health icons that navigates to `/source/{slug}`, and (2) `/source/{slug}` shows a flat connection table for one source. Phase 3 replaces the multi-source card grid with a grouped connection display — all connections from all sources shown on the home page, grouped under collapsible MudExpansionPanels per source. Single-source mode remains a flat table.

The current Home.razor (181 lines) has dual routes `@page "/"` and `@page "/source/{SourceSlug}"`. The multi-source branch (lines 16-41) renders `MudGrid`/`MudCard` per source. This must be replaced with `MudExpansionPanels` that fetch and display connections per source with parallel loading, health indicators, offline handling, and per-source error isolation.

## Dependencies

Depends on Phase 2 completion (source-aware routing, IMultiSourceDashboardApiClient, ISourceHealthMonitor, ISourceRegistry all exist).

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Dashboard.Ui/Models/SourceConnectionGroup.cs" tdd="false">
  <action>
  Create `Source/DotNetWorkQueue.Dashboard.Ui/Models/SourceConnectionGroup.cs` with LGPL-2.1 license header.

  Namespace: `DotNetWorkQueue.Dashboard.Ui.Models`

  Class `SourceConnectionGroup` with these properties:
  - `DashboardApiSourceConfig Source { get; init; }` — the source config (from `DotNetWorkQueue.Dashboard.Ui.Services`)
  - `SourceHealthState Health { get; init; }` — cached health state (from `DotNetWorkQueue.Dashboard.Ui.Services`)
  - `List<ConnectionResponse>? Connections { get; set; }` — loaded connections, null if not yet loaded
  - `string? ErrorMessage { get; set; }` — per-source error message from failed GetConnectionsAsync
  - `bool IsLoading { get; set; }` — true while fetching connections

  Add required `using` statements for `System.Collections.Generic`, `DotNetWorkQueue.Dashboard.Ui.Services`.
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj" --verbosity quiet 2>&1 | tail -3</verify>
  <done>SourceConnectionGroup.cs exists with all 5 properties. Project compiles with 0 errors.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor" tdd="false">
  <action>
  Rewrite the Home.razor page to support grouped multi-source display. The page keeps its dual routes (`@page "/"` and `@page "/source/{SourceSlug}"`).

  **Multi-source mode** (when `SourceSlug` is null/empty and source count > 1):
  1. In `OnParametersSetAsync`, build a `List<SourceConnectionGroup>` — one per source from `SourceRegistry.GetAll()`. Set `Health` from `HealthMonitor.GetHealth(source.Slug)`, `IsLoading = true`.
  2. After building the list, fire parallel `Task.WhenAll` to fetch connections from each source: for each group, call `MultiSourceClient.GetClientForSource(group.Source.Slug).GetConnectionsAsync()`. On success, set `group.Connections = result`, `group.IsLoading = false`. On exception, set `group.ErrorMessage = ex.Message`, `group.IsLoading = false`. Call `StateHasChanged()` after all tasks complete.
  3. Render using `MudExpansionPanels MultiExpansion="true"`:
     - For each `SourceConnectionGroup` in the list, render a `MudExpansionPanel` with `IsInitiallyExpanded="true"`.
     - **Panel header** (`TitleContent`): `MudStack Row="true"` containing:
       - Health icon: green `Icons.Material.Filled.CheckCircle` if `Health.Status == Healthy`, red `Icons.Material.Filled.Error` if `Unhealthy`, gray `Icons.Material.Filled.HelpOutline` if `Unknown`
       - Source name as `MudText Typo.subtitle1`
       - Connection count badge: `MudChip` or `MudText` showing `(N connections)` if Connections is loaded
     - **Panel body**:
       - If `IsLoading`: show `MudProgressCircular Indeterminate="true"`
       - If `Health.Status == Unhealthy` and `Connections == null` and `ErrorMessage == null`: show `MudAlert Severity.Warning` with text "Source unreachable" and a `MudButton` "Retry" that calls a `RetrySource(group)` method (which calls `GetConnectionsAsync` for that source only, updates the group, and calls `StateHasChanged`)
       - If `ErrorMessage != null`: show `MudAlert Severity.Error` with the error message and a Retry button
       - If `Connections` is empty: show `MudAlert Severity.Info` "No connections registered"
       - If `Connections` has items: show `MudSimpleTable` with Connection name and Queue count columns, each row clickable via `NavigateTo($"/source/{group.Source.Slug}/connections/{connection.Id}")`

  **Single-source mode** (when source count == 1):
  Keep existing behavior — redirect from `/` to `/source/{slug}`, which loads and displays the flat connection table (lines 43-92 of current Home.razor). No MudExpansionPanel used.

  **Per-source `/source/{SourceSlug}` route**:
  Keep existing behavior — resolve source, load connections, show flat table. This path is unchanged from Phase 2.

  **RetrySource method**:
  ```
  private async Task RetrySource(SourceConnectionGroup group)
  {
      group.IsLoading = true;
      group.ErrorMessage = null;
      StateHasChanged();
      try
      {
          var client = MultiSourceClient.GetClientForSource(group.Source.Slug);
          group.Connections = await client.GetConnectionsAsync();
      }
      catch (Exception ex)
      {
          group.ErrorMessage = ex.Message;
      }
      finally
      {
          group.IsLoading = false;
          StateHasChanged();
      }
  }
  ```

  **Field additions** to `@code` block:
  - `private List<SourceConnectionGroup>? _groups;` — populated in multi-source mode
  - `private bool _multiSourceMode;` — set true when sources.Count > 1 and SourceSlug is empty

  **PageTitle**: "Dashboard - Sources" for multi-source grouped view. "Dashboard - Connections" for single-source/per-source view.

  **Breadcrumbs**: Multi-source grouped view shows `Sources` as disabled crumb. Per-source view unchanged from Phase 2.
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj" --verbosity quiet 2>&1 | tail -3</verify>
  <done>Home.razor compiles. Multi-source mode renders MudExpansionPanels with health indicators. Single-source mode renders flat table. Offline sources show warning + Retry. Failed sources show error + Retry. Project builds with 0 errors.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Dashboard.Ui/Models/SourceConnectionGroup.cs, Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor" tdd="false">
  <action>
  Full solution build verification and existing test regression check.

  1. Build the full solution: `dotnet build "Source/DotNetWorkQueue.sln" -c Debug`
  2. Run all Dashboard UI unit tests: `dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj"`
  3. Verify the SourceConnectionGroup model has no secrets exposure — no BaseUrl or ApiKey properties are rendered in the Razor markup (only Source.Name and Source.Slug are used in rendered HTML).
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.sln" -c Debug --verbosity quiet 2>&1 | tail -5 && dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" --verbosity quiet 2>&1 | tail -5</verify>
  <done>Full solution builds with 0 errors. All 48 existing unit tests pass. No BaseUrl or ApiKey values appear in any Razor template rendering.</done>
</task>

## Verification

```bash
# Full solution build
dotnet build "Source/DotNetWorkQueue.sln" -c Debug

# Unit tests (should be 48 passing)
dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj"

# Verify no secrets in rendered HTML (grep for BaseUrl/ApiKey in Home.razor template sections)
grep -n 'BaseUrl\|ApiKey' "Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor"
# Should return 0 matches in template sections (only @code references if any)
```
