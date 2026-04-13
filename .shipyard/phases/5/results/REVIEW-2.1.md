# Review: Plan 2.1

## Verdict: PASS

---

## Stage 1: Spec Compliance

### Task 1: Swagger endpoint integration test
- Status: PASS
- Evidence: `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/SwaggerEndpointTests.cs` exists. `SwaggerEndpointTests` class spins up a `DashboardTestServer` with `EnableSwagger = true` (line 43-47), hits `swagger/v1/swagger.json` (line 59), asserts `HttpStatusCode.OK` and `"openapi"` + `"DotNetWorkQueue Dashboard"` in content (lines 61-65). Matches plan spec exactly.
- Notes: The `SwaggerUI_ReturnsOk_WithHtmlContent` test from the plan template was dropped — this is acceptable per plan Task 1's explicit fallback: "If all fail, drop Task 1's second test." The JSON endpoint alone exercises the `UseSwagger()` branch.

### Task 2: CORS integration test
- Status: PASS
- Evidence: `CorsIntegrationTests` class in same file (lines 69-105). Spins up server with `EnableCors = true`, `CorsOrigins = ["https://example.com"]` (lines 77-83). Sends GET with `Origin` header, asserts `Access-Control-Allow-Origin` header contains the expected origin (lines 96-104). Uses the plan's "simpler alternative" approach (GET instead of OPTIONS preflight) — explicitly authorized by plan Task 2.
- Notes: CORS policy name `"DashboardCors"` matches `DashboardExtensions.cs:84` and `UseDotNetWorkQueueDashboard:199`.

### Task 3 (scope extension): AuthorizationPolicy end-to-end integration test
- Status: PASS
- Evidence: `AuthorizationPolicyIntegrationTests` class (lines 107-173). Uses the new 3-arg `DashboardTestServer.CreateAsync` overload. Registers a `NoAuthHandler` that always returns `AuthenticateResult.NoResult()`, wires `UseAuthentication()` and `UseAuthorization()` via the `configureApp` hook (lines 115-138). Hits `api/v1/dashboard/connections` (an MVC controller, not middleware health endpoint) and asserts 401 (lines 153-155). This correctly exercises the `DashboardAuthorizationConvention` path that PLAN-1.3 left uncovered at integration level.

### DashboardTestServer overload
- Status: PASS
- Evidence: `DashboardTestServer.cs` (lines 42-69). Original 1-arg overload now delegates to the new 3-arg version with null hooks (line 43-45). All existing call sites are unaffected. `DisposeAsync` properly stops and disposes the `WebApplication` and `HttpClient` (lines 72-80).

---

## Findings

### Critical
- None

### Minor

- **Pipeline ordering: `UseAuthentication`/`UseAuthorization` inserted AFTER `UseDotNetWorkQueueDashboard`** — `DashboardTestServer.CreateAsync` calls `app.UseDotNetWorkQueueDashboard()` then `configureApp?.Invoke(app)` (lines 63-64), so `UseAuthentication` and `UseAuthorization` are registered after the CORS/Swagger/HealthChecks middleware but before `MapControllers` (line 65). For the specific test this is sufficient because the authorization policy is enforced at the MVC filter level (via `AuthorizeFilter` in `DashboardAuthorizationConvention`), not at the routing middleware level. However, if a future test relies on `UseAuthorization()` as a terminal middleware before routing, the ordering would need adjustment. This is not a bug in current tests but is a structural fragility worth noting.
  - Remediation: Document in a comment in `DashboardTestServer.CreateAsync` that `configureApp` runs after `UseDotNetWorkQueueDashboard` and before `MapControllers`, so callers must account for that ordering.

- **`_server` field not initialized to null explicitly** — In all three test classes, `private DashboardTestServer _server;` (e.g., `SwaggerEndpointTests:38`) is declared without explicit `= null`. The null-guard in `CleanupAsync` (`if (_server != null)`) depends on C#'s default null initialization. This is technically correct but a minor style gap; explicit `= null!` with nullable reference types enabled would be cleaner.
  - Remediation: Add `= null!` initializer or enable `#nullable enable` in the file with a proper nullable annotation.

### Positive
- LGPL header present and correct in both new/modified files.
- `IAsyncDisposable` pattern on `DashboardTestServer` is sound: `StopAsync` before `DisposeAsync` follows the correct `WebApplication` lifecycle.
- `NoAuthHandler` idiom (private nested class, `AuthenticateResult.NoResult()`) is the standard ASP.NET Core test pattern — no live identity provider, no mocking of sealed types.
- Endpoint choice for the auth test (`api/v1/dashboard/connections` — an MVC controller) over `/api/v1/dashboard/health` (middleware) shows correct understanding of where `IControllerModelConvention` applies.
- FluentAssertions 6.12.2-compatible usage throughout (`.Should().Be()`, `.Should().Contain()`, `.Should().BeTrue()`).
- No `global::` qualification issues — the file operates in `DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests` and uses `Microsoft.AspNetCore.*` types with no `DotNetWorkQueue.IConfiguration` shadowing risk.
- MSTest 3.x attributes used correctly (`[TestInitialize]`, `[TestCleanup]`, `[TestMethod]`).
- No external transport services required — tests use only the Dashboard pipeline itself.

---

## Scope-Extension Assessment

The scope extension (Task 3: AuthorizationPolicy end-to-end) fully recovers the PLAN-1.3 gap. PLAN-1.3 proved `DashboardAuthorizationConvention.Apply()` works in isolation but left the `DashboardExtensions.cs:101-105` branch guard (`if (!string.IsNullOrEmpty(options.AuthorizationPolicy))`) uncovered. PLAN-2.1's integration test exercises that exact branch end-to-end: it passes `AuthorizationPolicy = "DashboardAdmin"` into `AddDotNetWorkQueueDashboard`, which triggers the `Conventions.Add(new DashboardAuthorizationConvention(...))` call at line 103-104, and the subsequent 401 response confirms the convention was wired into the real MVC pipeline.

The extension was implemented completely and correctly.

---

## Integration Test Soundness

These are genuine integration tests, not over-mocked unit tests:

- `DashboardTestServer` uses `WebApplication.CreateBuilder()` + `builder.WebHost.UseTestServer()` — a real in-process ASP.NET Core host, not a hand-wired mock.
- `app.UseDotNetWorkQueueDashboard()` is called on the live `IApplicationBuilder`, exercising the actual production middleware registration code paths.
- HTTP requests go through `app.GetTestClient()` — a real HTTP client bound to the TestServer's in-process transport.
- The CORS test asserts on real HTTP response headers produced by the ASP.NET Core CORS middleware, not a mocked header collection.
- The auth test asserts on the actual MVC authorization pipeline response code (401), confirming `AuthorizeFilter` was applied by the convention.

No mocking of ASP.NET Core pipeline components is present. The tests correctly exercise the full Dashboard middleware and MVC pipeline.
