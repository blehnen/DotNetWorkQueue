# Phase 2 Context: Design Decisions

## Client injection — Inject IMultiSourceDashboardApiClient

Pages `@inject IMultiSourceDashboardApiClient MultiSourceClient` and call `GetClientForSource(SourceSlug)` in `OnParametersSetAsync`. The resolved `IDashboardApiClient` is stored in a page-level field and passed to child tab components via `[Parameter]`. This keeps the existing `[Parameter] public IDashboardApiClient Api` pattern on all 7 shared components unchanged.

## Invalid source slug — MudAlert error on page

When a source slug doesn't match any configured source, the page renders its shell/layout with a `MudAlert Severity.Error` saying "Source not found" and a link back to home (`/`). No redirect, no custom 404 page.

## Single-source URL — Always show slug

All URLs include `/source/{slug}` even with a single source. The Home page at `/` redirects to `/source/{onlySlug}`. This avoids conditional route generation and keeps URL patterns consistent regardless of source count.

## Multi-source `/` route — Show source list

When multiple sources are configured and a user visits `/` (no slug), Home.razor shows a basic source list with health indicators. Each source is a clickable card/link navigating to `/source/{slug}`. Single source: redirect to `/source/{onlySlug}`. This gives Phase 2 a simple source overview; Phase 3 enhances it with grouped connections and partial failure UX.

## Scope from ROADMAP.md (unchanged)

1. `ISourceHealthMonitor` / `SourceHealthMonitor` as background `IHostedService` — polls each source every 30s with 5s timeout, caches `SourceHealthState` per source
2. Source-aware page routes — `/source/{SourceSlug}`, `/source/{SourceSlug}/connections/{ConnectionId:guid}`, `/source/{SourceSlug}/queues/{QueueId:guid}`
3. Home.razor gets dual route: `@page "/"` and `@page "/source/{SourceSlug}"`
4. Source resolution in pages via `IMultiSourceDashboardApiClient.GetClientForSource(slug)`
5. Navigation updates — all `NavigateTo()` calls and `href` attributes include source slug prefix
6. Single-source redirect — `"/"` redirects to `"/source/{onlySlug}"`
7. Breadcrumbs show source name as first crumb (when multiple sources)
8. Unit tests for health transitions, source resolution, navigation URLs

## Key technical notes from Phase 1

- `IMultiSourceDashboardApiClient.GetClientForSource(slug)` returns `IDashboardApiClient` — the same interface all 7 shared components already accept as `[Parameter]`
- `ISourceRegistry.GetAll()` returns all configured sources, `GetBySlug(slug)` returns a specific source or null
- `DotNetWorkQueue.IConfiguration` shadows `Microsoft.Extensions.Configuration.IConfiguration` — use `global::` prefix in any new code that references MS config types
- Use real `FeatureCollection` instead of NSubstitute mocks for `IFeatureCollection` testing
