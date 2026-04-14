# Plan 2.1: Swagger + CORS Integration Tests (Use-Path Coverage)

## Context

`UseDotNetWorkQueueDashboard(IApplicationBuilder)` currently sits at **41.17%** line coverage. The covered portion comes from existing integration tests running through `DashboardTestServer`, which hits the always-executed paths (`UseHealthChecks`, the return statement). The two uncovered conditional branches are:

1. `if (options.EnableCors && options.CorsOrigins.Length > 0) app.UseCors("DashboardCors");` — lines 197-200
2. `if (options.EnableSwagger) { app.UseSwagger(); app.UseSwaggerUI(...); }` — lines 202-209

Existing integration tests all set `EnableSwagger = false` (see `DashboardTestServer.CreateAsync` usages in `MemoryEndpointTests.cs:55` and similar), so the UseSwagger branch is unexercised.

This plan adds a small integration test file that spins up a Dashboard server with Swagger enabled and CORS configured, hits the relevant endpoints, and asserts the middleware is actually wired into the pipeline.

Estimated coverage delta: **~12 lines** (the UseCors branch + the UseSwagger/UseSwaggerUI block). Pushes total coverage from ~64% toward ~67%.

## Dependencies

Wave 1 must complete first (all three unit-test plans). Rationale: Wave 1 proves the `Add` path registers the services correctly; this plan proves the `Use` path maps them to the pipeline. Running in sequence means any failure in Wave 1 is caught before we compound it with Wave 2's integration work.

(If the architect disagrees, this plan can be made wave 1 too — there's no hard dependency, just a sequencing preference.)

## Tasks

### Task 1: Swagger endpoint integration test
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/SwaggerEndpointTests.cs` (NEW)
**Action:** create
**Description:**
Create a new test file mirroring the structure of existing integration tests like `HealthEndpointTests.cs`. Use `DashboardTestServer.CreateAsync(...)` to spin up a server with `EnableSwagger = true`, then hit `/swagger/v1/swagger.json` via the test HTTP client and assert a 200 OK response with valid OpenAPI JSON content.

```csharp
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Tests
{
    [TestClass]
    public class SwaggerEndpointTests
    {
        private DashboardTestServer _server;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            _server = await DashboardTestServer.CreateAsync(options =>
            {
                options.EnableSwagger = true;
                // No connections needed — we're only testing the Swagger pipeline,
                // not a dashboard feature
            });
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            if (_server != null)
                await _server.DisposeAsync();
        }

        [TestMethod]
        public async Task SwaggerJson_ReturnsOk_WithValidOpenApiShape()
        {
            var response = await _server.Client.GetAsync("/swagger/v1/swagger.json");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("\"openapi\"");
            content.Should().Contain("DotNetWorkQueue Dashboard");
        }

        [TestMethod]
        public async Task SwaggerUI_ReturnsOk_WithHtmlContent()
        {
            var response = await _server.Client.GetAsync("/swagger/index.html");

            // Swashbuckle serves the Swagger UI at /swagger/index.html by default,
            // or at /swagger/ depending on routing. If 404, try "/swagger/" or "/swagger".
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("swagger", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
```

**If the Swagger UI path returns 404,** try these alternatives in order: `/swagger/`, `/swagger`, `/swagger/index.html`. The exact path depends on how `app.UseSwaggerUI(c => {...})` is configured in `UseDotNetWorkQueueDashboard` — it uses default routing, so `/swagger/index.html` is most likely correct. If all fail, drop Task 1's second test (SwaggerUI) and rely on the JSON test alone — it already exercises the `UseSwagger()` branch which is what we need for coverage.

**Acceptance Criteria:**
- At least the `SwaggerJson_ReturnsOk_WithValidOpenApiShape` test passes (hits the `UseSwagger()` branch on line 204)
- The test file compiles cleanly with existing integration test infrastructure
- No external services required (Memory transport or no transport)

### Task 2: CORS integration test
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/SwaggerEndpointTests.cs` (MODIFY — add a second test class OR combine into the same file)
**Action:** modify
**Description:**
The CORS branch in `UseDotNetWorkQueueDashboard` (lines 197-200) needs to be exercised. Add a second test — either in the same `SwaggerEndpointTests.cs` file or in a sibling `CorsIntegrationTests.cs`. Given they're both "pipeline middleware enablement" tests and the file is small, **put them in the same file** for simplicity.

Add a second `[TestClass]` in the same file:

```csharp
[TestClass]
public class CorsIntegrationTests
{
    private DashboardTestServer _server;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _server = await DashboardTestServer.CreateAsync(options =>
        {
            options.EnableSwagger = false;
            options.EnableCors = true;
            options.CorsOrigins = new[] { "https://example.com" };
        });
    }

    [TestCleanup]
    public async Task CleanupAsync()
    {
        if (_server != null)
            await _server.DisposeAsync();
    }

    [TestMethod]
    public async Task CorsPreflight_ReturnsAllowedOrigin_WhenOriginMatches()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/dashboard/health");
        request.Headers.Add("Origin", "https://example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _server.Client.SendAsync(request);

        // A proper CORS preflight should return 200 or 204 with Access-Control-Allow-Origin header
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins).Should().BeTrue();
        origins.Should().Contain("https://example.com");
    }
}
```

**Alternative simpler assertion if preflight is flaky:** instead of sending an OPTIONS preflight, send a GET with an `Origin` header and assert the response includes `Access-Control-Allow-Origin`:
```csharp
var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/dashboard/health");
request.Headers.Add("Origin", "https://example.com");
var response = await _server.Client.SendAsync(request);
response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins).Should().BeTrue();
```
Pick whichever reliably exercises the `UseCors("DashboardCors")` call on line 199.

**Note on file organization:** Two `[TestClass]` declarations in one `.cs` file is valid C# and is used elsewhere in this test project. If the existing project layout is strictly one class per file, split into `SwaggerEndpointTests.cs` + `CorsIntegrationTests.cs`.

**Acceptance Criteria:**
- CORS test passes and asserts at least one of: `Access-Control-Allow-Origin` header present, or the preflight returns a valid CORS response
- Test file compiles, existing integration tests remain untouched

### Task 3: Regression verification against existing integration tests
**Files:** none (verification-only task)
**Action:** test
**Description:**
After tasks 1 and 2 compile, run the filtered Dashboard integration test suite to ensure the new tests don't regress any existing tests:

```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" -c Debug --filter "FullyQualifiedName~Memory|FullyQualifiedName~Sqlite|FullyQualifiedName~LiteDb|FullyQualifiedName~Health|FullyQualifiedName~Swagger|FullyQualifiedName~Cors"
```

The `Memory|Sqlite|LiteDb` filter is the no-external-services safe filter from CLAUDE.md. Adding `Health|Swagger|Cors` ensures the new Swagger + CORS tests are included even though their class names don't match a transport name.

**Acceptance Criteria:**
- All matched tests pass
- New Swagger test covers the `UseSwagger()` branch — verifiable via a fresh coverage run after merge (not required as a plan acceptance criterion, but nice-to-have for the phase verification step)
- No existing tests regress

## Verification

```bash
# Run new tests specifically
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" -c Debug --filter "FullyQualifiedName~SwaggerEndpointTests|FullyQualifiedName~CorsIntegrationTests"

# Regression suite (Memory/Sqlite/LiteDb only — no external services)
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" -c Debug --filter "FullyQualifiedName~Memory|FullyQualifiedName~Sqlite|FullyQualifiedName~LiteDb|FullyQualifiedName~Swagger|FullyQualifiedName~Cors|FullyQualifiedName~Health"
```

Expected: new tests pass, existing Memory/Sqlite/LiteDb tests continue to pass.

## Coverage Target

This plan covers clusters F (UseCors branch) and G (UseSwagger branch) from RESEARCH.md section 6. Expected delta: ~12 lines in `UseDotNetWorkQueueDashboard`. After this plan, `DashboardExtensions` should be at approximately **~67% line coverage**, comfortably in the "balanced 60-70%" target band from CONTEXT-5.md Decision 2.

## Deferred / Out of Scope

- **Cluster H — `DashboardAuthorizationConvention.Apply` integration test** is NOT included in this plan. Per RESEARCH.md section 9 and the "stretch goal" classification, wiring up a real authorization policy scheme in the test host is more complex than the other clusters and the coverage target is already met without it. If the phase verifier reports coverage below target after Wave 2 completes, re-plan can add a `PLAN-3.1` for cluster H.
