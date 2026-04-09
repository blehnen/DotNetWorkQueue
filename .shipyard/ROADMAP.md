# Roadmap: Dashboard UI -- Support Multiple API Sources (issue #96)

## Overview

Transform the Dashboard UI from a single-API-source deployment to a multi-source aggregation layer. A single UI instance will connect to N Dashboard API instances, group connections by source, route all operations (read and write) to the correct source, and handle partial failures gracefully.

This is a **breaking configuration change**. The flat `DashboardApi:BaseUrl`/`ApiKey` config is replaced by a `DashboardApi:Sources[]` array.

**Total scope:** 1 config model, 1 source registry, 1 multi-source client wrapper, 1 health monitor hosted service, 3 Blazor page rewrites, 1 Home page grouping overhaul, unit tests, integration tests.

## Dependency Graph

```
Phase 1  (Config model + multi-source client infrastructure)
   |
   v
Phase 2  (Health monitoring + source-aware page routing)
   |
   v
Phase 3  (Home page grouping + partial failure UX + integration tests)
```

Strictly sequential: each phase produces the interfaces/services the next phase consumes. No parallelism between phases because they share files (all 3 pages touch the same routing/client plumbing).

---

## Phase 1: Multi-Source Configuration and Client Infrastructure

**Risk: HIGH** -- This phase redefines the DI registration for the API client, which is the backbone of all UI data access. If the interface contract or source resolution model is wrong, every subsequent phase requires rework. Doing this first surfaces design mistakes before any UI work begins.

**Scope:** ~40% of total work. Highest complexity -- new config model, new service interfaces, refactored DI registration, startup validation, in-process source auto-registration.

**Depends on:** Nothing (foundation phase).

### What Changes

1. **Source configuration model** -- `DashboardApiSourceConfig` class with `Name` (string, required), `BaseUrl` (string, required), `ApiKey` (string, optional). Slug derived from Name via deterministic slugify (lowercase, replace spaces/special chars with hyphens, collapse consecutive hyphens).

2. **Source registry** -- `ISourceRegistry` / `SourceRegistry` singleton providing `GetAll()`, `GetBySlug(string)`, `GetByName(string)`. Populated at startup from `DashboardApi:Sources[]` config array. Validates: no duplicate names, no duplicate slugs, at least one source configured.

3. **Multi-source client wrapper** -- `IMultiSourceDashboardApiClient` / `MultiSourceDashboardApiClient` that holds a `Dictionary<string, IDashboardApiClient>` keyed by slug. Provides `GetClientForSource(string slug)` returning the per-source `IDashboardApiClient`. Each per-source client is the existing `DashboardApiClient` wrapping an `HttpClient` with source-specific `BaseAddress` and `X-Api-Key` header. The existing `IDashboardApiClient` interface remains unchanged -- shared tab components continue receiving it as a `[Parameter]`.

4. **Startup config validation** -- In `Program.cs`, detect old flat config format (`DashboardApi:BaseUrl` without `DashboardApi:Sources`) and throw `InvalidOperationException` with clear migration instructions and example JSON showing the new `Sources[]` format.

5. **In-process API auto-registration** -- When `Dashboard:Connections` section exists (self-contained mode), automatically add a source entry with `Name = config["DashboardApi:LocalSourceName"] ?? "Local"`, `BaseUrl = "http://localhost:{port}"` (resolved from Kestrel config or default 5000). This source is added to the registry alongside any external sources.

6. **DI registration refactor** -- Replace `AddHttpClient<IDashboardApiClient, DashboardApiClient>()` with: parse `Sources[]` config, create `ISourceRegistry`, register named `HttpClient` instances per source, register `IMultiSourceDashboardApiClient` as singleton. Keep `IDashboardApiClient` registered (resolving to the first/only source's client) for backward compatibility during this phase.

7. **Unit tests** -- Config validation (valid multi-source, old-format detection, missing required fields, duplicate name rejection), slug generation (spaces, special chars, unicode, empty string), source lookup by slug (found, not found), multi-source client (correct client returned per slug, unknown slug throws).

### Files Touched

- `Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardApiSourceConfig.cs` (new)
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/ISourceRegistry.cs` (new)
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/SourceRegistry.cs` (new)
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/IMultiSourceDashboardApiClient.cs` (new)
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/MultiSourceDashboardApiClient.cs` (new)
- `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs` (config parsing, DI registration, old-format detection)
- `Source/DotNetWorkQueue.Dashboard.Ui/appsettings.json` (new Sources[] format example)

### Success Criteria

1. `DashboardApiSourceConfig` model exists with Name, BaseUrl, ApiKey properties
2. `ISourceRegistry` provides `GetAll()`, `GetBySlug()`, `GetByName()` methods
3. `IMultiSourceDashboardApiClient.GetClientForSource(slug)` returns correct per-source `IDashboardApiClient`
4. Old flat config (`DashboardApi:BaseUrl` without `Sources`) produces `InvalidOperationException` at startup with migration instructions
5. In-process API registers as a source named "Local" (or configured name)
6. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds with 0 errors
7. All existing Dashboard API integration tests pass unchanged (API layer untouched)
8. Unit tests pass for: valid config, old-format detection, slug generation, duplicate rejection, source lookup

---

## Phase 2: Health Monitoring and Source-Aware Page Routing

**Risk: MEDIUM** -- Route changes touch all 3 pages and all internal navigation links. The main risk is getting source resolution right in the Blazor page lifecycle. Health monitoring is a standalone hosted service with low coupling.

**Scope:** ~35% of total work. Two workstreams that converge in the pages: health state display and source-aware data loading.

**Depends on:** Phase 1 (requires `ISourceRegistry` and `IMultiSourceDashboardApiClient`).

### What Changes

1. **Health monitoring service** -- `ISourceHealthMonitor` / `SourceHealthMonitor` as a background `IHostedService`. Polls each source's `GET /api/v1/dashboard/connections` every 30 seconds with a 5-second `HttpClient` timeout. Caches health state per source: `SourceHealthState` record with `Status` enum (Unknown, Healthy, Unhealthy), `LastChecked` timestamp, `ErrorMessage` string. Thread-safe via `ConcurrentDictionary`. Logs state transitions (healthy-to-unhealthy, unhealthy-to-healthy).

2. **Source-aware page routes** -- Update all 3 page routes to include source slug:
   - `Home.razor`: `@page "/"` and `@page "/source/{SourceSlug}"` (dual route -- `/` redirects based on source count)
   - `ConnectionDetail.razor`: `@page "/source/{SourceSlug}/connections/{ConnectionId:guid}"`
   - `QueueDetail.razor`: `@page "/source/{SourceSlug}/queues/{QueueId:guid}"`

3. **Source resolution in pages** -- Each page resolves the source slug from the route parameter, calls `IMultiSourceDashboardApiClient.GetClientForSource(slug)` to get the per-source `IDashboardApiClient`, and passes it to child components. If slug is invalid, show 404-style error. The `Api` parameter passed to all 7 shared tab components is now the resolved per-source client -- no changes needed in tab components.

4. **Navigation updates** -- All `NavigateTo()` calls and `href` attributes in pages include the source slug prefix. Breadcrumbs show source name as first crumb (when multiple sources configured).

5. **Single-source redirect** -- When only one source is configured, `"/"` redirects to `"/source/{onlySlug}"`. The Home page at `"/source/{slug}"` renders a flat connection list (no grouping) -- identical to current single-source behavior.

6. **Write operation routing** -- All write operations (delete, requeue, reset, cancel, edit body, purge history) already flow through the `IDashboardApiClient` passed as `[Parameter]` to tab components. Since that client is now source-specific, writes automatically route to the correct source with no tab component changes.

7. **Unit tests** -- Health state transitions (Unknown->Healthy, Healthy->Unhealthy, Unhealthy->Healthy), polling timer behavior, source resolution from URL slug, navigation URL generation with source prefix.

### Files Touched

- `Source/DotNetWorkQueue.Dashboard.Ui/Services/ISourceHealthMonitor.cs` (new)
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/SourceHealthMonitor.cs` (new)
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/SourceHealthState.cs` (new)
- `Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor` (route, source resolution, single-source redirect)
- `Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/ConnectionDetail.razor` (route, source resolution, navigation)
- `Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/QueueDetail.razor` (route, source resolution, breadcrumbs, navigation)
- `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs` (register health monitor hosted service)

### Success Criteria

1. `ISourceHealthMonitor.GetHealth(slug)` returns `SourceHealthState` with status, timestamp, error
2. Background service polls all sources every 30s with 5s timeout
3. Health state transitions logged at Information level
4. All page routes include `{SourceSlug}` parameter
5. Single-source: `"/"` redirects to `"/source/{slug}"`, flat connection list shown
6. All write operations route to the correct source (verified by source slug in URL)
7. Source slug never exposes BaseUrl or ApiKey in any URL or rendered HTML
8. Breadcrumbs include source name as first crumb (multi-source) or omit it (single-source)
9. All 7 shared tab components work unchanged (receive per-source `IDashboardApiClient` via `[Parameter]`)
10. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds with 0 errors
11. Unit tests pass for health transitions, source resolution, navigation URLs

---

## Phase 3: Home Page Grouping, Partial Failure UX, and Integration Tests

**Risk: LOW** -- By this phase, the infrastructure is proven. This is UI polish (MudBlazor grouping components) and test coverage. The main risk is MudBlazor's `MudExpansionPanels` behavior, which is well-documented.

**Scope:** ~25% of total work. UI refinement and comprehensive testing.

**Depends on:** Phase 2 (requires source-aware routing + health monitoring).

### What Changes

1. **Home page grouped display** -- When multiple sources are configured, the Home page fetches connections from all sources in parallel (via `IMultiSourceDashboardApiClient`). Displays connections grouped under collapsible `MudExpansionPanel` headers per source. Each header shows: source name, health indicator (green/red `MudIcon`), connection count. Panels are expanded by default.

2. **Single-source display** -- When only one source is configured, render the flat connection table (no expansion panel, no source header). Visually identical to current behavior.

3. **Offline source display** -- Sources with `SourceHealthState.Status == Unhealthy` show their panel header with a `MudAlert Severity.Warning` reading "Source unreachable" and a "Retry" button. Retry triggers a fresh `GetConnectionsAsync()` for that source only. Connections from healthy sources are unaffected.

4. **Partial request failure** -- If a source was healthy at poll time but fails when the Home page calls `GetConnectionsAsync()`, catch the exception per-source and show an inline `MudAlert Severity.Error` in that source's section with the error message. Other sources' data renders normally.

5. **Integration tests** -- New test class(es) in the existing `DotNetWorkQueue.Dashboard.Api.Integration.Tests` project (or a new UI integration test project if needed). Use `DashboardTestServer` to spin up 2-3 real Memory-transport API instances. Test:
   - Multi-source connection listing returns connections from all sources
   - Source attribution: connections from Source A are grouped under Source A
   - Write operation routing: delete/requeue on Source A's queue calls Source A's API
   - Health monitoring: dispose one `DashboardTestServer`, verify health state transitions to Unhealthy
   - Partial failure: one source offline, other source's connections still load

6. **Verify existing tests** -- Run all 38+ existing Dashboard API integration tests to confirm no regressions. These test each API instance independently and should pass unchanged since the API layer has no modifications.

### Files Touched

- `Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor` (grouped display, partial failure UX)
- `Source/DotNetWorkQueue.Dashboard.Ui/Models/SourceConnectionGroup.cs` (new -- view model for grouped display)
- Integration test files (new test classes for multi-source scenarios)

### Success Criteria

1. Multiple sources: Home page groups connections under collapsible panels with health indicators
2. Single source: flat list identical to current behavior (no panel/header visible)
3. Offline source: panel shown with "Source unreachable" warning and Retry button
4. Request failure on healthy source: inline error in that source's section, other sources unaffected
5. Integration test: 2+ Memory API instances, grouped listing verified
6. Integration test: write operations route to correct source instance
7. Integration test: source taken offline, health state transitions to Unhealthy
8. All 38+ existing Dashboard API integration tests pass unchanged
9. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` -- 0 errors
10. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` -- 0 errors, 0 warnings
11. No secrets (BaseUrl, ApiKey) leak into URLs or client-rendered HTML

---

## Phase Summary

| Phase | Name | Risk | Depends On | Key Deliverable |
|-------|------|------|------------|-----------------|
| 1 | Multi-Source Config & Client Infrastructure | High | -- | Source registry, multi-source client, config validation |
| 2 | Health Monitoring & Source-Aware Routing | Medium | Phase 1 | Background health polling, all pages route by source |
| 3 | Home Page Grouping, Partial Failure & Integration Tests | Low | Phase 2 | Grouped UI, graceful degradation, end-to-end tests |

## Design Decisions

### Why `GetClientForSource(slug)` instead of adding source parameter to every IDashboardApiClient method

The existing `IDashboardApiClient` interface has 26 methods. Adding a source parameter to every method would be a massive breaking change across all 7 shared tab components and 3 pages. Instead:
- `IMultiSourceDashboardApiClient.GetClientForSource(slug)` returns an `IDashboardApiClient` scoped to one source
- All 7 shared tab components (`MessagesTab`, `ErrorsTab`, `StaleTab`, `HistoryTab`, `ConfigTab`, `ConsumersTab`, `MessageDetailDrawer`) continue receiving `IDashboardApiClient` as a `[Parameter]` -- zero changes needed
- Only the 3 page-level components resolve the source from the URL slug and pass the correct per-source client down
- The existing `DashboardApiClient` class remains unchanged -- each instance wraps a different `HttpClient` with a different `BaseAddress`

### Why slugs instead of IDs

Source names are user-friendly ("Production SQL Server") while slugs are URL-friendly ("production-sql-server"). Slugs are derived deterministically from names, so they are stable across restarts. Using GUIDs would require users to know opaque IDs. Slugs never expose BaseUrl or ApiKey in URLs.

### Why background health polling instead of per-request checks

Per-request health checks would add latency to every page load and create cascading timeouts when sources are down. Background polling (30-second interval, 5-second timeout) means page loads are instant -- they read cached health state. The trade-off is up to 30 seconds of stale health data, which is acceptable for a monitoring dashboard.

### Why breaking config change without backward compatibility

Supporting both old flat format and new `Sources[]` format permanently would add complexity for a temporary migration convenience. A clear startup error with migration instructions and example JSON is the cleanest path. The dashboard feature is still in active development -- shipping a clean config model now avoids technical debt.

### Why strictly sequential phases (no parallelism)

All 3 Blazor pages (`Home.razor`, `ConnectionDetail.razor`, `QueueDetail.razor`) are modified in both Phase 2 and Phase 3. Parallelizing would create merge conflicts in these files. Additionally, Phase 2's routing changes must be complete before Phase 3's grouped display can work -- the Home page grouping depends on source-aware data fetching.

## Risk Summary

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Multi-source client interface wrong | Medium | Critical | Phase 1 is highest risk, done first. Build + test before any UI work. |
| Slug collisions (two names produce same slug) | Low | High | Startup validation rejects duplicate slugs with clear error. |
| Blazor page lifecycle issues with source resolution | Medium | Medium | Use `OnParametersSetAsync` (not `OnInitializedAsync`) for route parameter changes. |
| In-process source BaseUrl wrong (port mismatch) | Medium | Medium | Use `IServer` to resolve actual listen address at startup. Fallback to config. |
| Health polling storm with many sources | Low | Low | Sequential polling per source within each timer tick. 5s timeout per source. |
| MudBlazor expansion panel styling issues | Low | Low | Well-documented component. Fallback to manual `if/else` sections if needed. |

## Execution Order

| Wave | Phase | Notes |
|------|-------|-------|
| 1 | Phase 1 | Foundation: config model, source registry, multi-source client, DI |
| 2 | Phase 2 | Health monitoring + all page routing changes |
| 3 | Phase 3 | UI grouping polish + integration test coverage |

**Estimated plans per phase:**
- Phase 1: 2-3 plans (config model + registry in plan 1, multi-source client + DI in plan 2, unit tests in plan 3 or merged)
- Phase 2: 2-3 plans (health monitor in plan 1, page routing in plan 2, unit tests in plan 3 or merged)
- Phase 3: 2 plans (Home page grouping + partial failure in plan 1, integration tests in plan 2)
- **Total: 6-8 plans**
