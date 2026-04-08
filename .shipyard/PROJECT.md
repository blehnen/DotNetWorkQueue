# Project: Dashboard UI — Support Multiple API Sources (issue #96)

## Description

The Dashboard UI currently connects to a single API instance (either in-process or one external `DashboardApi:BaseUrl`). This forces users with queues on multiple machines to run separate dashboard UIs for each. For example, a SQLite queue on one machine can't share its `.db3` file over a network — it needs a local API instance — while Redis and SQL Server queues are accessed from a central API.

This project adds multi-source support to the Dashboard UI, allowing a single UI deployment to aggregate queues from multiple Dashboard API instances. The UI handles connection routing and display aggregation, while each API instance retains full ownership of its transport logic. This is a breaking configuration change.

## Goals

1. Allow the Dashboard UI to connect to multiple Dashboard API sources from a single deployment
2. Add per-source configuration (name, baseUrl, apiKey) via a `Sources[]` array in config
3. Route all API calls (reads and writes) to the correct source based on URL context
4. Show source health/reachability status via background polling
5. Group connections by source on the Home page (hidden when only one source)
6. Treat in-process API as just another source (no special code path)
7. Maintain full read + write operations across all sources

## Non-Goals

- Changing the Dashboard API contract or transport logic — each API instance stays as-is
- Adding a proxy/gateway layer in the API — aggregation lives in the UI
- Cross-source operations (e.g., moving a message from one source to another)
- Source discovery or auto-detection — sources are explicitly configured
- New transport support — this is purely a UI aggregation feature

## Requirements

### Configuration

- Replace flat `DashboardApi:BaseUrl` / `ApiKey` with `DashboardApi:Sources[]` array
- Each source has: `Name` (string), `BaseUrl` (string), `ApiKey` (string, optional)
- Breaking change: old flat config format is no longer supported
- Startup validation detects old config format and throws a clear error message with migration instructions and example JSON
- In-process API (when `Dashboard:Connections` exists) registers as a source automatically (name configurable, defaults to "Local")

### Source-Aware Routing

- All Blazor page URLs include source context: `/source/{sourceSlug}/connection/{id}/queue/{queueId}`
- Source slug is derived from the configured `Name` (slugified) — never exposes BaseUrl or ApiKey
- `DashboardApiClient` methods accept a source identifier and resolve it internally to BaseUrl + credentials
- Write operations (delete, requeue, reset, cancel) route through the same source-aware path as reads

### Health Monitoring

- A background hosted service polls each source on a timer (e.g., every 30 seconds)
- Polling calls `GET /api/v1/dashboard/connections` with a short timeout
- Health state cached in memory: reachable, unreachable, last-checked timestamp
- UI reads cached health state — page loads never block on health checks

### Home Page — Grouped by Source

- Multiple sources: connections grouped under collapsible source name headers with health indicator
- Single source: group header hidden, flat list identical to current behavior
- Offline sources: group header shown with "Source unreachable" warning and retry button

### Partial Failure Handling

- Display data from healthy sources normally
- Offline sources show their group/section with an inline warning — no blocking, no spinners
- If a source was healthy at poll time but fails on a specific request, show an error in that source's section
- Other sources remain fully functional and unaffected

### Backwards Compatibility

- This is a **breaking config change** — old flat `BaseUrl`/`ApiKey` format is removed
- Startup detection of old config produces a clear, actionable error message with the new format example
- All existing API endpoints, controllers, and transport logic remain unchanged
- Single-source deployments work identically to today (just wrapped in `Sources[]` array)

## Non-Functional Requirements

- Page loads must not block on unhealthy sources — background polling handles health
- Source credentials (BaseUrl, ApiKey) must never appear in URLs, HTML, or client-side state
- All existing Dashboard API integration tests must continue to pass unchanged
- Solution must build cleanly on net10.0 and net8.0 in Debug and Release configurations

## Success Criteria

1. `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` — 0 errors
2. `dotnet build "Source/DotNetWorkQueue.sln" -c Release` — 0 errors, 0 warnings
3. Dashboard UI connects to 2+ configured sources and displays connections grouped by source
4. Write operations (delete, requeue, reset, cancel) route correctly to the owning source
5. An offline source shows as unreachable without blocking other sources
6. Old flat config format produces a clear startup error with migration instructions
7. Single-source config renders identically to current behavior (no group header)
8. In-process API works as a source alongside external sources
9. No secrets (BaseUrl, ApiKey) leak into URLs or client-rendered HTML
10. All existing Dashboard API integration tests pass without modification

## Testing Strategy

### Unit Tests (mocked HTTP handlers)
- `DashboardApiClient` multi-source aggregation logic
- Source routing: correct source receives each request
- Partial failure: 2 of 3 sources respond, UI shows available data + error for failed source
- Timeout handling: slow source doesn't block others
- Health polling: state transitions (healthy → unhealthy → healthy)
- Config validation: old format detection, missing required fields

### Integration Tests (Memory transport)
- Spin up 2-3 real API instances using Memory transport (no external services)
- Verify end-to-end: grouped connection listing, correct source attribution
- Write operations routed to correct source instance
- Health monitoring with simulated source failure

### Existing Transport Integration Tests
- All 38 existing Dashboard API integration test classes (Memory, SQLite, LiteDb, Redis, SqlServer, PostgreSQL) must pass unchanged
- These validate each API instance independently — no changes needed unless API contract changes
- If API response shape changes, run full transport integration suite to catch divergence

## Constraints

- Breaking configuration change — requires documentation and clear migration error
- UI-side aggregation only — no API gateway/proxy layer
- Each Dashboard API instance is standalone and unmodified
- Transport-specific logic stays entirely within the API layer
- Blazor Server (server-side rendering) — all HTTP calls are server-side C#
