# Project: Dashboard Improvements

## Description

Two focused improvements to the DotNetWorkQueue Dashboard. First, tighten the UI layout to eliminate wasted space on the Connections and Queue list pages — the queue detail pages are already well-laid-out and don't need changes. Second, create a Docker image that lets users run the Dashboard as a standalone container, connecting to their existing queue infrastructure via a mounted `appsettings.json` configuration file.

The Docker work requires moving the transport resolution logic (transport name string → `ITransportInit` type) from the sample project into the base library, so any host — including a Docker container — can use JSON-driven transport configuration without duplicating code.

## Goals

1. Tighten UI density on the Connections page and Queue list page to eliminate wasted vertical space
2. Remove the left nav rail — use breadcrumbs for navigation instead
3. Move the JSON-driven transport registration pattern from the sample project into the base `DashboardExtensions` class
4. Add an `IConfiguration`-based overload of `AddDotNetWorkQueueDashboard` for config-file-driven setup
5. Create a Dockerfile that produces a standalone Dashboard image with all transports included
6. Provide an example `appsettings.json` showing all transport configuration options

## Non-Goals

- Redesigning the queue detail pages (Messages, Errors, Stale, Consumers, History, Configuration tabs) — these are fine
- Adding new Dashboard API features or endpoints
- Docker Compose stacks with transport containers
- Kubernetes manifests or Helm charts
- Mobile-first responsive redesign — MudBlazor's built-in responsive utilities are sufficient
- Automated Docker Hub builds (manual push to start)

## Requirements

### UI Polish

- **Connections page:** Replace oversized connection cards with a compact list or table. Each row shows transport name, display name, and queue count. Rows are clickable to navigate to the connection's queue list.
- **Queue list page:** Replace oversized queue cards with a compact list or table. Each row shows queue name with a clickable link to the queue detail page.
- **Left nav rail removal:** Remove the `MudDrawer`/`MudNavMenu` containing the single "Connections" link. Navigation is handled by breadcrumbs (already partially in place as "SQL Server / sampleQueueExample") and clickable list items.
- **No changes to:** Queue detail pages, status cards, tab layout, tables, configuration view, or any other existing UI elements.

### Docker Build

- **Transport resolution in base library:** Move the `AddConnectionByTransport()` switch pattern from the sample project (`DotNetWorkQueue.Samples`) into `DashboardExtensions` in `DotNetWorkQueue.Dashboard.Api`. This maps transport name strings ("SqlServer", "PostgreSQL", "Redis", "SQLite", "LiteDB") to their `ITransportInit` implementations.
- **IConfiguration overload:** Add `AddDotNetWorkQueueDashboard(IConfiguration dashboardSection)` that reads the `Dashboard:Connections[]` JSON structure the sample already uses: `{ "Transport": "SqlServer", "ConnectionString": "...", "DisplayName": "...", "Queues": ["queue1"] }`.
- **Dockerfile:** Multi-stage build targeting `DotNetWorkQueue.Dashboard.Ui`. Produces a self-contained ASP.NET Core image. Users mount config via `-v ./appsettings.json:/app/appsettings.json -p 8080:8080`.
- **All transports included:** The Docker image references SqlServer, PostgreSQL, Redis, SQLite, and LiteDB transport assemblies so any configured transport works.
- **Example config:** Ship `appsettings.example.json` alongside the Dockerfile showing all transport options with notes that SQLite/LiteDB require volume-mounted paths and don't work over network shares.

## Non-Functional Requirements

- UI changes must not break existing functionality — all Dashboard API integration tests must continue to pass
- Docker image should be reasonably small (multi-stage build, no SDK in final image)
- The `IConfiguration` overload must coexist with the existing fluent C# API — it's additive, not a replacement

## Success Criteria

1. Connections page shows connections in a compact list/table with no oversized cards
2. Queue list page shows queues in a compact list/table with no oversized cards
3. Left nav rail is removed; breadcrumb navigation works
4. `AddDotNetWorkQueueDashboard(IConfiguration)` overload exists and reads the JSON connection format
5. `docker build` produces a working image
6. `docker run` with a mounted `appsettings.json` connects to configured transports and serves the Dashboard UI
7. All existing Dashboard API integration tests pass

## Constraints

- Dashboard UI uses Blazor Server with MudBlazor 9.1.0 — all UI changes must use MudBlazor components
- The transport resolution must handle all 5 supported transports (SqlServer, PostgreSQL, Redis, SQLite, LiteDB)
- Docker image must include both .NET 8 and .NET 10 runtimes (Dashboard targets both)
- SQLite/LiteDB transports in Docker require volume-mounted paths — document this in the example config
