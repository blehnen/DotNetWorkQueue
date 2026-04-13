# Review: Plan 1.3 — CORS Registration + DashboardAuthorizationConvention Tests

## Verdict: PASS

## Stage 1: Spec Compliance

### Task 1: CORS policy registers when EnableCors = true with origins
- Status: PASS
- Evidence: `DashboardExtensionsCorsAndAuthTests.cs:41-59` — test constructs a `ServiceCollection`, calls `AddDotNetWorkQueueDashboard` with `EnableCors = true` and two origins, resolves `IOptions<CorsOptions>`, and asserts `GetPolicy("DashboardCors")` is not null with matching origins. Production code at `DashboardExtensions.cs:80-91` confirms the tested path exists exactly as expected.
- Notes: The plan's advisory to also assert `Headers`/`Methods` was dropped in favor of origins-only assertion. This is acceptable — the plan itself flagged that assertion as fragile and offered the fallback. The origins assertion is sufficient to prove the branch executes.

### Task 2: CORS policy does NOT register when CorsOrigins is empty
- Status: PASS
- Evidence: `DashboardExtensionsCorsAndAuthTests.cs:62-79` — uses `GetService<IOptions<CorsOptions>>()` (nullable) with a null-guard before asserting `GetPolicy("DashboardCors").Should().BeNull()`. Matches the plan's defensive fallback pattern exactly.
- Notes: Correctly exercises the false branch of `options.EnableCors && options.CorsOrigins.Length > 0`.

### Task 3: AuthorizationPolicy convention is added when policy is set (PIVOTED)
- Status: PASS (post-pivot scope)
- Evidence: `DashboardExtensionsCorsAndAuthTests.cs:92-115` — two tests directly exercise `DashboardAuthorizationConvention.Apply()`:
  - Positive: constructs a `ControllerModel` from `ConnectionsController` (same assembly as production), calls `Apply`, asserts one `AuthorizeFilter` is added.
  - Negative: constructs a `ControllerModel` from `typeof(string)` (different assembly), calls `Apply`, asserts no filter is added.
- Production code at `DashboardExtensions.cs:304-311` confirms both branches are exactly what these tests cover.
- Notes: The `InternalsVisibleTo("DotNetWorkQueue.Dashboard.Api.Tests")` grant at `InternalsVisibleForTests.cs:21` makes `DashboardAuthorizationConvention` accessible without reflection. The tests use direct type references — no reflection — matching the acceptance criterion.

## Stage 2: Code Quality

### Critical
- None

### Minor
- None

### Positive
- LGPL-2.1 header is present and correctly formatted (lines 1-18).
- MSTest 3.x `[TestClass]` / `[TestMethod]` attributes are used correctly throughout; no deprecated `Assert.ThrowsException<T>` patterns.
- FluentAssertions 6.12.2 usage is idiomatic (`Should().NotBeNull()`, `Should().BeEquivalentTo()`, `Should().BeEmpty()`).
- The comment block at lines 81-89 honestly documents the pivot rationale inline — future maintainers won't wonder why the DashboardExtensions-level branch guard isn't directly asserted here.
- No `IConfiguration` namespace collision issue — the file uses only `Microsoft.AspNetCore.*` and `Microsoft.Extensions.*` types, none of which shadow `DotNetWorkQueue.IConfiguration`.
- The negative-branch assembly test using `typeof(string)` is a clean, zero-overhead way to exercise the `controller.ControllerType.Assembly != dashboardAssembly` path without any mocking infrastructure.

## Pivot Assessment

The PLAN-1.3 → PLAN-2.1 scope pivot is reasonable and complete. The 2-line branch guard at `DashboardExtensions.cs:101-105` that was dropped from unit-level testing is covered end-to-end by PLAN-2.1's `AuthorizationPolicyIntegrationTests` class, which exercises the full `AddDotNetWorkQueueDashboard` → `DashboardAuthorizationConvention` wiring through a real `WebApplication` pipeline. The integration test is a stronger guarantee than a unit-level `IOptions<MvcOptions>.Conventions` inspection would have been, given the documented ASP.NET Core behavior where conventions registered via `AddControllers(action)` do not surface in a bare `ServiceCollection` (filters do, conventions do not). The `DashboardAuthorizationConvention` class itself (13 lines, previously 0% covered) now has full line and branch coverage from the two direct-apply tests. No coverage gap remains unaccounted for.
