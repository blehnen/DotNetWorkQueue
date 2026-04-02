# Roadmap: Dashboard Improvements

## Overview

Two focused improvements to the DotNetWorkQueue Dashboard: (1) tighten UI density on the Connections and Queue list pages, removing the left nav rail in favor of breadcrumbs; (2) create a Docker image for standalone Dashboard deployment with JSON-driven transport configuration moved into the base library.

## Dependency Graph

```
Phase 1 (UI Polish) ──────────────────────────────────────────> Done
                                                                  |
Phase 2 (Config-Driven Transport Registration) ───> Phase 3 (Docker Image) ──> Done
```

Phases 1 and 2 are independent and can execute in parallel. Phase 3 depends on Phase 2.

---

## Phase 1: UI Polish -- Compact Layout and Nav Rail Removal

- **Scope:** ~30% of total project. Replace MudCard grid layouts on the Connections page (`Home.razor`) and Queue list page (`ConnectionDetail.razor`) with compact MudSimpleTable/MudList. Remove the MudDrawer and NavMenu from `MainLayout.razor`. Add breadcrumbs to `Home.razor` for consistency. No changes to queue detail pages, tab views, or any other existing UI.
- **Dependencies:** None
- **Risk:** Low -- purely presentational changes. Three Razor files and one layout file. No API or backend changes. Existing Dashboard integration tests exercise API endpoints, not rendered markup, so they remain unaffected.
- **Files touched:**
  - `Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor` -- replace MudGrid/MudCard with MudSimpleTable rows (transport name, display name, queue count; clickable rows)
  - `Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/ConnectionDetail.razor` -- replace MudGrid/MudCard with MudSimpleTable rows (queue name, consumer count; clickable rows). Keep the existing Scheduled Jobs table and breadcrumbs unchanged.
  - `Source/DotNetWorkQueue.Dashboard.Ui/Components/Layout/MainLayout.razor` -- remove MudDrawer, NavMenu reference, hamburger MudIconButton, and `ToggleDrawer` method. The MudAppBar title becomes the clickable link back to Connections.
  - `Source/DotNetWorkQueue.Dashboard.Ui/Components/Layout/NavMenu.razor` -- delete this file (single "Connections" link no longer needed)
- **Success criteria:**
  - Connections page renders a compact table/list instead of cards, with each row showing transport name, display name, and queue count
  - Queue list page renders a compact table/list instead of cards, with each row showing queue name and consumer count
  - No left nav drawer or hamburger icon is visible
  - Clicking a connection row navigates to that connection's queue list
  - Clicking a queue row navigates to that queue's detail page
  - Breadcrumb navigation works on all pages (Home shows "Connections", ConnectionDetail shows "Connections > {name}", QueueDetail remains unchanged)
  - `dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"` succeeds
  - `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory"` passes (confirms API unaffected)

---

## Phase 2: Config-Driven Transport Registration

- **Scope:** ~40% of total project. Move the `AddConnectionByTransport` switch pattern from the sample project into `DashboardExtensions` in the Dashboard API library. Add an `IConfiguration`-based overload of `AddDotNetWorkQueueDashboard`. This requires the Dashboard API project to gain package references to all five transport assemblies. Add unit tests for the new overload and the transport name resolution.
- **Dependencies:** None (independent of Phase 1)
- **Risk:** Medium -- the Dashboard API csproj currently only references `DotNetWorkQueue` and `DotNetWorkQueue.Transport.RelationalDatabase`. Adding references to all five transport packages (`SqlServer`, `PostgreSQL`, `Redis`, `SQLite`, `LiteDb`) increases the dependency surface of the API library. This is an intentional trade-off: the Dashboard is a monitoring tool that needs to connect to any transport, so carrying all transport references is appropriate for this assembly. The alternative (a separate `DashboardExtensions.AllTransports` package) adds packaging complexity for no user benefit.
- **Files touched:**
  - `Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj` -- add ProjectReferences to all five transport projects
  - `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs` -- add `AddDotNetWorkQueueDashboard(IServiceCollection, IConfiguration)` overload and internal `ResolveTransportInitType(string)` method mapping transport name strings to `ITransportInit` types
  - `Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardConnectionConfig.cs` -- new POCO for JSON binding: `Transport`, `ConnectionString`, `DisplayName`, `Queues[]`
  - `Source/DotNetWorkQueue.Dashboard.Api.Tests/DashboardExtensionsTests.cs` -- unit tests for transport name resolution (valid names, case handling, unknown transport throws) and IConfiguration overload (reads connections from config, registers expected types)
- **Success criteria:**
  - `DashboardExtensions.AddDotNetWorkQueueDashboard(IServiceCollection, IConfiguration)` exists and compiles
  - Calling it with an `IConfiguration` section containing `Connections[]` entries registers the correct transport types (verified via unit tests inspecting `DashboardOptions.ConnectionRegistrations`)
  - Transport name resolution handles: "SqlServer" -> `SqlServerMessageQueueInit`, "PostgreSql" -> `PostgreSqlMessageQueueInit`, "SQLite" -> `SqLiteMessageQueueInit`, "LiteDb" -> `LiteDbMessageQueueInit`, "Redis" -> `RedisQueueInit`
  - Unknown transport name throws `ArgumentException` with clear message
  - The existing `Action<DashboardOptions>` overload continues to work unchanged
  - `dotnet build "Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj"` succeeds
  - `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj"` passes
  - `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory"` passes

---

## Phase 3: Docker Image and Example Configuration

- **Scope:** ~30% of total project. Create a multi-stage Dockerfile for the Dashboard UI that produces a standalone ASP.NET Core image with all transports included. Ship an `appsettings.example.json` documenting all configuration options. Update the Dashboard UI's `Program.cs` to use the new `IConfiguration` overload from Phase 2 so it can be configured entirely via mounted JSON.
- **Dependencies:** Phase 2 (requires the `IConfiguration` overload and transport resolution in the base library)
- **Risk:** Medium -- Docker multi-stage builds with multi-targeted .NET projects (net8.0 + net10.0) require choosing a single runtime for the container image. The Dockerfile should target net10.0 for the container since it is the primary target. The existing `docker/Dockerfile` is a Jenkins CI agent image, not a Dashboard image -- this is a new Dockerfile at a different path. SQLite/LiteDB require native libraries (`libsqlite3-0`) and volume-mounted database paths inside the container.
- **Files touched:**
  - `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs` -- add conditional path: if `Dashboard:Connections` config section exists, use the new `IConfiguration` overload from Phase 2 instead of requiring compile-time configuration
  - `docker/dashboard/Dockerfile` -- new multi-stage Dockerfile: SDK stage builds the Dashboard UI project for net10.0; runtime stage uses `mcr.microsoft.com/dotnet/aspnet:10.0`, installs `libsqlite3-0`, copies published output, exposes port 8080
  - `docker/dashboard/appsettings.example.json` -- example config showing all five transport connection entries with comments about SQLite/LiteDB volume mount requirements
  - `docker/dashboard/README.md` -- build and run instructions (`docker build`, `docker run` with volume mount)
- **Success criteria:**
  - `docker build -t dotnetworkqueue-dashboard -f docker/dashboard/Dockerfile .` succeeds from the repository root
  - The resulting image does not contain the .NET SDK (only the ASP.NET runtime)
  - `docker run --rm -v ./docker/dashboard/appsettings.example.json:/app/appsettings.json -p 8080:8080 dotnetworkqueue-dashboard` starts without errors (will fail to connect to transports without real connection strings, but the app should start and serve the UI)
  - `appsettings.example.json` contains entries for all five transports with placeholder connection strings
  - SQLite and LiteDB entries include comments or notes about volume-mounted paths
  - The Dashboard UI `Program.cs` still works without a `Dashboard:Connections` config section (falls back to the existing fluent API pattern for non-Docker hosts)
  - `dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"` succeeds
