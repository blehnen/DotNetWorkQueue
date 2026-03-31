# Roadmap: CONCERNS.md Tier B — Moderate Effort Fixes

## Overview

Address all moderate-effort items from CONCERNS.md in a single PR: Dashboard API hardening (exception disclosure, CORS, health check), centralized NuGet package version management, TODO/HACK comment audit, and integration test serialization binder fix.

**Prerequisite**: Tier A (quick wins) merged on 2026-03-30.

---

## Phase Summary

| Phase | Name | Complexity | Dependencies | Plans | Risk |
|-------|------|-----------|-------------|-------|------|
| 1 | Tier B Moderate Fixes | Medium | Tier A merged | 4 | Medium -- CPM touches all 36 .csproj files; Dashboard API adds new middleware and endpoints |

---

## Phase 1: Tier B Moderate Fixes

**Complexity**: Medium
**Dependencies**: Tier A PR merged to master
**Risk**: Medium overall. Central Package Management is mechanically large (36 .csproj files) and will break the build if any version is missed. Dashboard API changes add new middleware (CORS) and a new endpoint (/health) but are additive -- no existing behavior changes. TODO audit and binder fix are trivial.

### Task Grouping

| Task | Item IDs | Description | Count |
|------|----------|-------------|-------|
| Task 1: Central Package Management | H-6 | Create Directory.Packages.props, update Directory.Build.props, strip Version= from all .csproj | 1 |
| Task 2: Dashboard API Hardening — Exception Filter + CORS | H-4, H-3 (CORS), M-9 | Fix exception disclosure in non-Dev environments; add configurable CORS policy | 3 |
| Task 3: Dashboard API Hardening — Health Check + Docs | H-3 (Health), H-3 (Docs) | Add /health endpoint; update README with internal-only recommendation | 2 |
| Task 4: TODO/HACK Audit + Binder Fix | M-3, N-3 | Replace 4 TODO/HACK comments with NOTE; fix integration test serialization binder | 2 |

### Wave Assignment

```
Wave 1:  [Task 1: Central Package Management]
              |
              v
Wave 2:  [Task 2: Dashboard API Exception + CORS]  ||  [Task 3: Dashboard API Health + Docs]  ||  [Task 4: TODO Audit + Binder Fix]
```

**Reasoning**:
- **Wave 1 -- Task 1 (CPM)** must go first. It changes how every .csproj references packages. All subsequent changes should be made against the post-CPM file structure so there are no merge conflicts from .csproj edits.
- **Wave 2 -- Tasks 2, 3, 4** are independent of each other. Task 2 modifies the exception filter and DashboardExtensions/DashboardOptions. Task 3 adds a new health controller and edits the README. Task 4 touches only production source comments and one integration test file. No file overlap between the three.
- Within Dashboard API, Task 2 (exception filter) and Task 3 (health endpoint) could theoretically be one task, but they are split because the exception filter modifies existing middleware while the health check is a new endpoint -- different verification strategies.

### Key Files

#### Task 1: Central Package Management (H-6)

| File | Change |
|------|--------|
| `Source/Directory.Packages.props` | **New file.** All consolidated package versions |
| `Source/Directory.Build.props` | **New file.** Add `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` |
| All 36 `*.csproj` files under `Source/` | Remove `Version=` attribute from every `<PackageReference>` |

**Key packages to consolidate**: SimpleInjector 5.5.0, Polly 8.6.5, Newtonsoft.Json 13.0.4, Microsoft.Data.SqlClient 6.1.3, OpenTelemetry 1.14.0, MSTest 3.x, NSubstitute, AutoFixture, FluentAssertions 6.12.2, LiteDB, StackExchange.Redis, Npgsql, Microsoft.Data.Sqlite, plus all transitive test infrastructure packages.

#### Task 2: Dashboard API Exception Filter + CORS (H-4, H-3/M-9)

| File | Change |
|------|--------|
| `Source/DotNetWorkQueue.Dashboard.Api/Middleware/DashboardExceptionFilter.cs` | Add IHostEnvironment check; return generic error messages for InvalidOperationException and NotSupportedException in non-Development; keep full messages in Development |
| `Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardOptions.cs` | Add `CorsOrigins` property (string array, default empty) and `EnableCors` bool |
| `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs` | Wire up `services.AddCors()` with policy from options; call `app.UseCors()` in middleware pipeline |
| `Source/DotNetWorkQueue.Dashboard.Api.Tests/Middleware/DashboardExceptionFilterTests.cs` | Add tests for generic vs detailed error based on environment |
| `Source/DotNetWorkQueue.Dashboard.Api.Tests/Configuration/DashboardOptionsTests.cs` | Add tests for new CORS properties |

#### Task 3: Dashboard API Health Check + Docs (H-3)

| File | Change |
|------|--------|
| `Source/DotNetWorkQueue.Dashboard.Api/Controllers/HealthController.cs` | **New file.** GET /health returning 200 with uptime and connection status |
| `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs` | Register health check services if needed |
| `Source/DotNetWorkQueue.Dashboard.Api.Tests/Controllers/HealthControllerTests.cs` | **New file.** Unit tests for health endpoint |
| `Source/DotNetWorkQueue.Dashboard.Api/README.md` | Add internal-only deployment recommendation; document HTTPS/rate-limiting as infrastructure concerns |

#### Task 4: TODO/HACK Audit + Integration Test Binder Fix (M-3, N-3)

| File | Change |
|------|--------|
| `Source/DotNetWorkQueue/Factory/InterceptorFactory.cs` (line 52) | Replace `//HACK for now...` with `//NOTE: SimpleInjector decorator pattern limitation...` |
| `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/QueryHandler/ReceiveMessage.cs` (line 175) | Replace `//TODO - cache based on route` with `//NOTE: Route-based caching deferred; see CONCERNS.md L-4` |
| `Source/DotNetWorkQueue.Transport.SqlServer/Basic/QueryHandler/CreateDequeueStatement.cs` (line 237) | Replace `//TODO - cache based on route` with `//NOTE: Route-based caching deferred; see CONCERNS.md L-4` |
| `Source/DotNetWorkQueue.Transport.SqlServer/Basic/Message/ReceiveMessage.cs` (line 100) | Replace `//TODO - we could consider using a task...` with `//NOTE: Synchronous status update is intentional; async would add complexity without measurable benefit at current scale` |
| `Source/DotNetWorkQueue.IntegrationTests.Shared/Helpers.cs` (line 112) | Add `SerializationBinder = new DenyListSerializationBinder()` to the JsonSerializerSettings alongside `TypeNameHandling.All` |

### Success Criteria

1. **CPM active**: `Source/Directory.Packages.props` exists; `Source/Directory.Build.props` contains `ManagePackageVersionsCentrally`; `grep -r "Version=" Source/**/*.csproj` returns zero hits on `<PackageReference>` elements
2. **Solution builds**: `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` succeeds with no errors
3. **Exception filter hardened**: DashboardExceptionFilter returns `{"error": "An internal error occurred"}` for InvalidOperationException/NotSupportedException in non-Development; returns detailed message in Development; full exception always logged server-side
4. **CORS configurable**: DashboardOptions has CorsOrigins property; CORS middleware wired in DashboardExtensions; Blazor UI can connect cross-origin when configured
5. **Health endpoint live**: GET /health returns 200 OK with `{"status": "Healthy", "uptime": "..."}` (at minimum)
6. **README updated**: Dashboard API README includes internal-only deployment recommendation and infrastructure-layer note for HTTPS/rate-limiting
7. **Zero TODO/HACK in production code**: `grep -rn "TODO\|HACK" Source/ --include="*.cs" | grep -v "Tests" | grep -v "IntegrationTests"` returns zero hits
8. **Binder fix applied**: `grep "DenyListSerializationBinder" Source/DotNetWorkQueue.IntegrationTests.Shared/Helpers.cs` returns a hit
9. **All unit tests pass**: `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"` and `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj"` exit 0

---

## Parallelism Notes

- **Task 1 is the critical path** (Wave 1). It modifies every .csproj in the repo. All other tasks must wait for it to complete to avoid merge conflicts in .csproj files and to ensure the build system is stable.
- **Tasks 2, 3, and 4 are fully independent** (Wave 2). Task 2 modifies DashboardExceptionFilter.cs, DashboardOptions.cs, and DashboardExtensions.cs. Task 3 adds a new HealthController.cs and edits the README. Task 4 touches only source comments in 4 files and one integration test helper. No file overlap between any pair.
- **Exception**: Task 2 and Task 3 both touch DashboardExtensions.cs (Task 2 for CORS wiring, Task 3 for health check registration). If executed by parallel agents, one should claim DashboardExtensions.cs and the other should note the needed additions for the claimer to integrate. Alternatively, assign DashboardExtensions.cs changes entirely to Task 2 and have Task 3 only create the HealthController and README -- the health check registration in DashboardExtensions.cs is minimal and Task 2 can include it.

## Breaking Changes

None. All changes are additive (new CORS config, new health endpoint) or internal (exception message masking, comment rewording, package management restructuring). No public API surface is removed or altered.

## Risk Assessment

- **Highest risk: Task 1 (Central Package Management)**. Touches all 36 .csproj files. If any version is wrong or a package is missed from Directory.Packages.props, the entire solution will fail to build. Mitigation: extract versions programmatically from existing .csproj files before creating the props file; verify with full solution build immediately after.
- **Medium risk: Task 2 (Exception Filter + CORS)**. CORS misconfiguration can silently block the Blazor UI. Mitigation: default CORS to permissive for localhost origins; integration tests verify cross-origin requests.
- **Low risk: Tasks 3 and 4**. Health endpoint is a new additive controller. TODO audit and binder fix are single-line changes with clear verification.
