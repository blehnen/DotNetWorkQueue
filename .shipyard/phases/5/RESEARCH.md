# Phase 5 Research: Dashboard.Api DashboardExtensions Coverage

**Authored:** 2026-04-13 (orchestrator-direct; dispatched researcher agent stalled mid-work)
**Branch:** `phase-5-dashboard-coverage`

## 1. File Overview

- **Path:** `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs`
- **Total lines:** 313 (including 18-line LGPL header + blank lines + braces)
- **Coverable lines (per ReportGenerator):** 183
- **Structure:** a single public static class `DashboardExtensions` containing:
  - 3 public methods (2 extension-method overloads on `IServiceCollection`, 1 extension method on `IApplicationBuilder`)
  - 2 private helper methods
  - 1 nested internal class `DashboardAuthorizationConvention : IControllerModelConvention` (MVC convention, applied when `AuthorizationPolicy` is set)

The file is DI/startup wiring code — no business logic.

## 2. Coverage Baseline

From `Code_20Coverage_20Report/DotNetWorkQueue.Dashboard.Api_DashboardExtensions.html`:

| Metric | Value |
|---|---|
| Total lines | 313 |
| Coverable lines | 183 |
| Covered lines | 61 |
| Uncovered lines | 122 |
| **Line coverage** | **33.3%** |
| Branches covered | 17 / 34 |
| **Branch coverage** | **50%** |
| Methods covered (any) | 3 / 5 |
| Methods fully covered | 1 / 5 |

### Per-method breakdown

| Method | Lines | Line cov | Branches | Uncov Br |
|---|---|---|---|---|
| `AddDotNetWorkQueueDashboard(IServiceCollection, Action<DashboardOptions>)` | 53–149 | **42.85%** | 20 | 8 |
| `AddDotNetWorkQueueDashboard(IServiceCollection, IConfiguration)` | 158–186 | **0%** | 2 | 1 |
| `UseDotNetWorkQueueDashboard(IApplicationBuilder)` | 193–214 | **41.17%** | 13 | 6 |
| `AddConnectionByTransport(…)` (private) | 216–265 | **0%** | (N/A — not reached) | — |
| `PreloadAssemblies(string[])` (private) | 267–289 | **100%** | 10 | 10 |

Note: `DashboardAuthorizationConvention.Apply` coverage is not reported as a separate method because the class itself is not reachable from existing tests (the `AuthorizationPolicy`-is-set branch is never taken in tests).

### Uncovered branch clusters (semantic)

Walking the file top-to-bottom, these are the branches/lines that are not covered:

| Cluster | Line range | Description | Blocked on |
|---|---|---|---|
| **A. EnableCors (Add path)** | 80–91 | `if (options.EnableCors && options.CorsOrigins.Length > 0)` registers CORS policy | No test sets `EnableCors = true` with a non-empty `CorsOrigins` |
| **B. AuthorizationPolicy** | 101–105 | Adds `DashboardAuthorizationConvention` MVC convention when policy is set | No test sets `AuthorizationPolicy` |
| **C. EnableSwagger (Add path)** | 109–146 | Full Swagger registration: `AddEndpointsApiExplorer`, `AddSwaggerGen`, security definitions | All existing tests set `EnableSwagger = false` |
| **C1. ApiKey security definition (nested)** | 121–144 | Inside EnableSwagger: adds `ApiKey` security scheme + requirement when `ApiKey` is non-empty | Depends on Cluster C + `ApiKey` set |
| **D. IConfiguration overload** | 158–186 | Reads config section, iterates `Connections[]`, throws on missing `Transport`/`ConnectionString`, delegates to `AddConnectionByTransport` | **Zero test coverage** |
| **E. AddConnectionByTransport switch** | 216–264 | 5 transport cases (`SqlServer`, `PostgreSql`, `SQLite`, `LiteDb`, `Redis`) + default ArgumentException | Only reachable via Cluster D |
| **F. UseCors branch** | 197–200 | `UseCors("DashboardCors")` when `EnableCors && CorsOrigins.Length > 0` | Depends on Cluster A |
| **G. UseSwagger branch** | 202–209 | `UseSwagger() + UseSwaggerUI()` when `EnableSwagger` | Depends on Cluster C |
| **H. DashboardAuthorizationConvention.Apply** | 295–312 | Assembly-match filter adds `AuthorizeFilter` to dashboard controllers | Depends on Cluster B |

## 3. Public API Surface Inventory

| Method | Signature | Classification | Callsites |
|---|---|---|---|
| `AddDotNetWorkQueueDashboard` (Action) | `(IServiceCollection, Action<DashboardOptions>)` | **USED — production + tests** | `Dashboard.Api.Tests/Extensions/DashboardExtensionsTests.cs` (11), `Dashboard.Api.Integration.Tests/Helpers/DashboardTestServer.cs` (1 → transitively ~50 integration tests), `DashboardExtensions.cs:165` (self, delegation from IConfiguration overload) |
| `AddDotNetWorkQueueDashboard` (IConfiguration) | `(IServiceCollection, IConfiguration)` | **USED — production only, zero tests** | `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs:45` — **this is the real Dashboard UI entrypoint**. It reads config from `appsettings.json` and wires up the dashboard from user-supplied connection definitions. |
| `UseDotNetWorkQueueDashboard` | `(IApplicationBuilder)` | **USED — production + tests** | `DashboardTestServer.cs:49`, `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs` (likely — not re-confirmed but it's the middleware pipeline pair) |

## 4. Dead-Overload Candidates

**NONE.** Phase 5's CONTEXT-5.md Decision 4 ("delete dead overloads") was drafted before research; it turned out to be a false alarm based on the ROADMAP's speculative hint. There are only **3 public methods** on `DashboardExtensions`, and all 3 are actively used:

- Action overload → used by unit tests + integration test fixture + delegated to by IConfiguration overload
- IConfiguration overload → **used by the real production Dashboard.Ui entrypoint** (`Dashboard.Ui/Program.cs:45`)
- Use middleware → used by integration test fixture + Dashboard.Ui

**The `IConfiguration` overload deserves special attention:** it currently has **0% coverage** despite being the production code path that real Dashboard UI users rely on. Adding tests for it is not just a coverage-metric exercise — it's meaningful risk reduction. Any bug in the JSON config parsing, the transport-name switch, or the error handling ships to users undetected.

## 5. Public API Overloads Flagged for User Review

**NONE.** There is no "overload exists for API completeness but unused in practice" situation in `DashboardExtensions`. Every public method has a concrete caller in either the test suite or the production Dashboard.Ui.

→ **Decision 4 is void.** No deletion plan is needed for Phase 5. The architect should not produce a dead-code-deletion plan.

## 6. Test Layer Recommendation

**Recommendation: MIXED — favor unit tests for DI assertions, integration tests for real-pipeline behavior.**

### Why

- The existing `DashboardExtensionsTests.cs` already uses the right pattern for DI-surface assertions: build a `ServiceCollection`, call the extension method, inspect via `BuildServiceProvider().GetRequiredService<T>()` or `services.Where(sd => sd.ServiceType == …)`. This is fast, deterministic, and sufficient for proving "service X is registered when option Y is set."
- Integration-level behavior (the pipeline actually serving requests through Swagger, CORS preflight responses, API key filtering reaching the controller) requires a `WebApplication` + `TestServer`, which `DashboardTestServer` already provides. Adding new integration scenarios there is cheap.
- The `IConfiguration` overload is special — it bridges a JSON config shape to the Action overload. The right test seam for it is to build an `IConfiguration` from an in-memory dictionary (using `ConfigurationBuilder.AddInMemoryCollection(…)`) and assert both:
  1. That the right transport-specific connection gets registered (DI inspection)
  2. That a fully-configured JSON blob round-trips cleanly to a running server (integration test — optional, maybe 1 happy-path only)

### Per-cluster assignments

| Cluster | Layer | Project | Notes |
|---|---|---|---|
| A — EnableCors (Add) | Unit | `Dashboard.Api.Tests` | Assert `CorsOptions.PolicyMap` contains `"DashboardCors"` after `AddDotNetWorkQueueDashboard(opts => opts.EnableCors = true; opts.CorsOrigins = new[] {"https://example"})`. Also test the guard: `EnableCors = true` with `CorsOrigins = Array.Empty<string>()` should NOT register the policy (branch coverage). |
| B — AuthorizationPolicy | Unit | `Dashboard.Api.Tests` | Assert `MvcOptions.Conventions` contains a `DashboardAuthorizationConvention` when policy is non-null/non-empty. Assert it does NOT when policy is null/empty. |
| C — EnableSwagger (Add) | Unit | `Dashboard.Api.Tests` | Assert Swagger services registered via `services.Any(sd => sd.ServiceType.Name.Contains("SwaggerGenerator"))` or similar. Don't need a running TestServer for this. |
| C1 — ApiKey security definition | Unit | `Dashboard.Api.Tests` | Same approach: resolve `IOptions<SwaggerGenOptions>` from the provider and assert the security scheme was added. |
| D — IConfiguration overload | Unit + 1 integration | Both | Unit test: build in-memory `IConfiguration` with a `Dashboard:Connections` section for Memory transport, call the overload, assert a `DashboardOptions` with the expected connection is registered. Integration: one happy-path test via `DashboardTestServer` to confirm the pipeline actually serves requests when bootstrapped from config. |
| D-err — IConfiguration error branches | Unit | `Dashboard.Api.Tests` | Two ArgumentException branches (missing `Transport`, missing `ConnectionString`). Straightforward `Assert.ThrowsExactly<ArgumentException>`. |
| E — AddConnectionByTransport | Covered via D | `Dashboard.Api.Tests` | Parameterize the unit test in cluster D with each transport name (`SqlServer`, `PostgreSql`, `SQLite`, `LiteDb`, `Redis`) + one default-case assertion. The switch is private but will be exercised transitively when the IConfiguration overload is called with each transport name. **No need to reflect on the private method.** |
| F — UseCors | Integration | `Dashboard.Api.Integration.Tests` | Configure `EnableCors = true` + origins, hit the test client with an `Origin` header, assert the CORS preflight reponse. OR skip and accept as covered-by-AddPath (since the `Use` branch is a one-liner delegating to ASP.NET Core's built-in CORS middleware). |
| G — UseSwagger | Integration | `Dashboard.Api.Integration.Tests` | Configure `EnableSwagger = true` in one new test (OR flip the default in one existing test), hit `/swagger/v1/swagger.json` with the test client, assert 200 OK and valid OpenAPI JSON. |
| H — DashboardAuthorizationConvention.Apply | Integration | `Dashboard.Api.Integration.Tests` | Configure `AuthorizationPolicy = "TestPolicy"`, register the policy in services, hit any dashboard endpoint, assert 401. This is the most complex one — may be worth deferring as a stretch goal if the balanced target is already met by clusters A–G. |

## 7. Test Infrastructure Notes

- **Bootstrap:** Unit tests use raw `ServiceCollection` → `BuildServiceProvider()`. Integration tests use `DashboardTestServer.CreateAsync(configure)` which internally runs `WebApplication.CreateBuilder() + UseTestServer() + AddDotNetWorkQueueDashboard(configure) + Build() + UseDotNetWorkQueueDashboard() + MapControllers() + StartAsync()`.
- **Transport filter:** integration tests restrict to `Memory`/`Sqlite`/`LiteDb` via `--filter "FullyQualifiedName~Memory|FullyQualifiedName~Sqlite|FullyQualifiedName~LiteDb"` when no external services are available. Any new integration tests added for Phase 5 should use **Memory transport only** (per CONTEXT-5.md and ROADMAP).
- **`DashboardTestServer` currently always sets `EnableSwagger = false`** in every integration test. To test Swagger in integration, a new test would need to override this. Since the existing infrastructure is mature, a single new `SwaggerEndpointTests.cs` file with 2–3 tests (Swagger JSON returns 200, optionally ApiKey security definition, optionally UI HTML) would suffice.
- **`IConfiguration` namespace shadowing** (CLAUDE.md lesson): any test code in `DotNetWorkQueue.Dashboard.Api.Tests` or `DotNetWorkQueue.Dashboard.Api.Integration.Tests` that needs `Microsoft.Extensions.Configuration.IConfiguration` MUST use `global::Microsoft.Extensions.Configuration.IConfiguration` — `DotNetWorkQueue.IConfiguration` shadows via namespace walk-up. The researcher didn't verify whether this affects existing test files, but any new unit test building an in-memory `IConfiguration` (cluster D) will hit this and must use the fully-qualified type.
- **`using` directives for new tests:** existing `DashboardExtensionsTests.cs` uses `using Microsoft.Extensions.DependencyInjection;` and `using Microsoft.VisualStudio.TestTools.UnitTesting;`. New unit tests should mirror this. For the `IConfiguration` overload test, add `using Microsoft.Extensions.Configuration;` AND use `global::Microsoft.Extensions.Configuration.IConfiguration` at the call site to avoid shadowing.
- **MSTest 3.x API:** per CLAUDE.md lesson, use `Assert.ThrowsExactly<T>` not `Assert.ThrowsException<T>` for the error-branch tests in cluster D-err.

## 8. Balanced-Coverage Target Budget

Current: 33.3% (61/183 covered lines). Target: 60–70% (110–128 covered lines). **Gap: 49–67 more lines to cover.**

Greedy ROI ordering (most lines-per-test first):

1. **Cluster D (IConfiguration overload) + Cluster E (transitively)** → estimated **~45–50 lines** covered. Highest ROI by far because D is 100% uncovered and E is only reachable through D. One parameterized unit test over 5 transport names covers all 5 switch arms.
2. **Cluster C (EnableSwagger Add path) + C1 (ApiKey security)** → estimated **~38 lines** covered. Unit-testable.
3. **Cluster A (EnableCors Add path)** → **~12 lines** covered. Unit-testable.
4. **Cluster B (AuthorizationPolicy)** → **~5 lines** covered. Unit-testable.
5. **Cluster G (UseSwagger integration)** → **~8 lines** covered in UseDashboard. Integration test.
6. **Cluster F (UseCors integration)** → **~4 lines** covered. Integration test.
7. **Cluster D-err (IConfiguration error branches)** → **~6 lines** covered. Unit test.
8. **Cluster H (DashboardAuthorizationConvention.Apply)** → **~10 lines** covered. Integration test. Stretch goal.

Running total after each: 50, 88, 100, 105, 113, 117, 123, 133. → Reaching ~70% at step 6 (~117 lines, 64%), exceeding 70% by step 8.

**Recommended stop point: after step 6.** That gives ~117 covered / 183 = **~64% line coverage, 32 percentage-point improvement** — squarely in the balanced band. Steps 7–8 are available if cheap to add, but the architect should treat them as stretch, not must-have.

## 9. Plan Shape Recommendation for Architect

Given the clusters and the constraint that each plan has ≤3 tasks:

**Proposed plan shape (4 plans in 2 waves):**

### Wave 1 — Unit tests (parallel, 3 plans)

- **PLAN-1.1 — IConfiguration overload tests** (clusters D, D-err, E transitively)
  - Task 1: Add `AddDotNetWorkQueueDashboard_FromConfiguration_Memory_RegistersConnection` happy-path test using in-memory `IConfiguration`
  - Task 2: Add parameterized test exercising all 5 transport names (SqlServer, PostgreSql, SQLite, LiteDb, Redis) — asserts no exception is thrown during registration (connection-string values can be throwaway strings since the switch only reads the `Transport` key)
  - Task 3: Add error-branch tests for missing `Transport` and missing `ConnectionString` (both `Assert.ThrowsExactly<ArgumentException>`)
  - Files: `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsFromConfigurationTests.cs` (NEW)

- **PLAN-1.2 — EnableSwagger + ApiKey Add-path tests** (clusters C, C1)
  - Task 1: Add `AddDotNetWorkQueueDashboard_Registers_SwaggerServices_When_Enabled` — build provider, resolve `IOptions<SwaggerGenOptions>`, assert a SwaggerDoc for `v1` is present
  - Task 2: Add `AddDotNetWorkQueueDashboard_Registers_ApiKeySecurityScheme_When_ApiKey_Set` — same setup + `options.ApiKey = "secret"`, assert the `ApiKey` security definition exists
  - Task 3: Add `AddDotNetWorkQueueDashboard_Does_Not_Register_ApiKeySecurityScheme_When_ApiKey_Empty` (branch guard)
  - Files: extend `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsTests.cs`

- **PLAN-1.3 — CORS + AuthorizationPolicy Add-path tests** (clusters A, B)
  - Task 1: Add `AddDotNetWorkQueueDashboard_Registers_CorsPolicy_When_Enabled_With_Origins` — build provider, assert `CorsOptions` contains a policy named `"DashboardCors"` with the expected origins
  - Task 2: Add `AddDotNetWorkQueueDashboard_Does_Not_Register_Cors_When_Origins_Empty` (branch guard for `CorsOrigins.Length == 0`)
  - Task 3: Add `AddDotNetWorkQueueDashboard_Adds_AuthorizationConvention_When_Policy_Set` — resolve `IOptions<MvcOptions>`, assert a `DashboardAuthorizationConvention` is present in `Conventions`
  - Files: extend `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsTests.cs`

### Wave 2 — Integration tests (sequential after Wave 1, 1 plan)

- **PLAN-2.1 — Swagger + CORS integration tests** (clusters G, F)
  - Task 1: Create `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/SwaggerEndpointTests.cs` — spin up `DashboardTestServer` with `EnableSwagger = true`, hit `/swagger/v1/swagger.json`, assert 200 OK + valid OpenAPI shape
  - Task 2: Add a second test in the same file for CORS: configure `EnableCors = true` + test origin, hit any dashboard endpoint with an `Origin` header, assert the CORS response headers
  - Task 3: Verify existing integration tests still pass (regression check)
  - Files: `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/SwaggerEndpointTests.cs` (NEW)

**Wave 2 is sequential-after-Wave-1** only because the architect may want to verify Wave 1 hit the balanced band before deciding whether Wave 2 is worth the extra effort. If Wave 1 already reaches ~60% on its own, Wave 2 can be trimmed or deferred.

### Deferred / not planned

- **Cluster H (`DashboardAuthorizationConvention.Apply`)** — integration test is complex (requires registering an auth policy scheme in the test host). Defer as a stretch goal; flag in the architect's plan but don't require it for Phase 5 success.
- **Dead-overload deletion plan** — void per section 4/5 above. No deletion work.

## 10. Open Questions for Architect

1. **Should cluster H be included or deferred?** My recommendation is defer (stretch goal), but the architect may decide differently based on whether it materially affects the balanced-coverage target.
2. **Integration test project conventions** — do existing integration tests prefer per-transport filenames (`MemoryEndpointTests.cs`) or per-feature filenames (`SwaggerEndpointTests.cs`)? The existing layout suggests per-transport, but Phase 5's new tests are per-feature. Architect should either (a) name new files per-feature (easier for a reader to find Swagger tests) or (b) add Swagger assertions to an existing per-transport file.
3. **Parameterized testing** — the existing tests don't appear to use `[DataRow]` / `[DynamicData]` patterns. The architect should either adopt `[DataRow]` for cluster D's 5-transport iteration, or expand to 5 separate test methods (verbose but consistent with existing style).
4. **Should Wave 2 actually be sequential or parallel with Wave 1?** Parallel is fine if the target is ≥60% regardless; sequential only matters if the architect wants Wave 1's measurement to drive the Wave 2 scope decision. I lean parallel — simpler.

## 11. Key Facts the Architect MUST Respect

- **No dead code to delete** — don't produce a deletion plan.
- **Memory transport only** for any integration tests.
- **`global::Microsoft.Extensions.Configuration.IConfiguration`** in test code that needs MS config (per CLAUDE.md IConfiguration shadowing lesson).
- **`Assert.ThrowsExactly<T>`** not `Assert.ThrowsException<T>` (MSTest 3.x).
- **Each plan has ≤ 3 tasks** (Shipyard plan structure rule).
- **Target:** 60–70% line coverage (balanced per CONTEXT-5.md Decision 2), not 50% minimum and not 80%+.
- **Feature branch:** work happens on `phase-5-dashboard-coverage`, will be PR'd later (per CONTEXT-5.md Decision 5).
