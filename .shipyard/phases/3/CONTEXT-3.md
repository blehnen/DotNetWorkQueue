# Phase 3 Context: Design Decisions

## Integration tests — Existing Dashboard.Api.Integration.Tests

Add multi-source integration test classes to the existing `DotNetWorkQueue.Dashboard.Api.Integration.Tests` project. It already has `DashboardTestServer` and Memory transport infrastructure. No new project needed.

## Single source — Flat connection list

Single-source deployments show connections directly in a flat table (no expansion panel). Identical to pre-multi-source behavior. Multi-source deployments show connections grouped under collapsible `MudExpansionPanel` per source with health indicators.

## Scope from ROADMAP.md

1. **Home page grouped display** — Multi-source: fetch connections from all sources in parallel, group under `MudExpansionPanel` per source. Header shows: source name, health indicator, connection count. Panels expanded by default.
2. **Single-source display** — Flat connection table, no expansion panel/header. Visually identical to current behavior.
3. **Offline source display** — Sources with Unhealthy status show panel header with `MudAlert Severity.Warning` "Source unreachable" and Retry button. Retry calls `GetConnectionsAsync()` for that source only.
4. **Partial request failure** — If a healthy source fails on `GetConnectionsAsync()`, catch per-source, show inline `MudAlert Severity.Error` in that source's section. Other sources unaffected.
5. **Integration tests** — 2-3 DashboardTestServer instances (Memory transport), test: grouped listing, source attribution, write routing, health monitoring, partial failure.
6. **Verify existing tests** — All 38+ existing Dashboard API integration tests pass unchanged.

## Key technical notes from prior phases
- Home.razor already has multi-source source list with health cards (Phase 2) — Phase 3 replaces this with grouped connections
- `IMultiSourceDashboardApiClient.GetClientForSource(slug)` returns per-source `IDashboardApiClient`
- `IMultiSourceDashboardApiClient.GetAllSources()` returns all configured sources
- `ISourceHealthMonitor.GetHealth(slug)` returns `SourceHealthState`
- `SourceConnectionGroup` view model mentioned in ROADMAP — create for grouped display data
- `DashboardTestServer` in `Helpers/DashboardTestServer.cs` — creates test API instance with Memory transport
