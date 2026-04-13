# Plan 1.2: EnableSwagger + ApiKey Security Definition Tests

## Context

The `EnableSwagger` branch of `AddDotNetWorkQueueDashboard(Action)` (lines 109-146 of `DashboardExtensions.cs`) is completely uncovered because every existing test in `DashboardExtensionsTests.cs` sets `options.EnableSwagger = false`. This includes the nested `!string.IsNullOrEmpty(options.ApiKey)` branch that registers the `ApiKey` security scheme in Swagger (lines 121-144).

This plan adds 3 unit tests that flip `EnableSwagger = true` and assert the correct Swagger services get registered, including the API-key security definition in both states (set and unset).

Estimated coverage delta: **~38 lines** (Swagger registration body + API key security block + branch guards).

## Dependencies

None — runs in Wave 1, parallel with PLAN-1.1 and PLAN-1.3.

## Tasks

### Task 1: Swagger services register when EnableSwagger = true
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsSwaggerTests.cs` (NEW)
**Action:** create
**Description:**
Create a new test file `DashboardExtensionsSwaggerTests.cs` under the same `DotNetWorkQueue.Dashboard.Api.Tests.Extensions` namespace. Creating a feature-scoped file (rather than appending to the existing `DashboardExtensionsTests.cs`) matches PLAN-1.1's pattern and avoids file-level conflicts with parallel Wave 1 plans.

Add the first test to the new class `DashboardExtensionsSwaggerTests` that asserts Swagger services are registered when `EnableSwagger = true`. Swagger registration in ASP.NET Core adds a number of services (`ISwaggerProvider`, `IApiDescriptionGroupCollectionProvider`, `IOptions<SwaggerGenOptions>`, and more). The simplest robust assertion is to resolve `IOptions<SwaggerGenOptions>` from the provider and assert the `v1` SwaggerDoc was registered.

```csharp
[TestMethod]
public void AddDotNetWorkQueueDashboard_Registers_SwaggerServices_When_Enabled()
{
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddDotNetWorkQueueDashboard(options =>
    {
        options.EnableSwagger = true;
    });

    var provider = services.BuildServiceProvider();
    var swaggerOptions = provider
        .GetRequiredService<IOptions<SwaggerGenOptions>>()
        .Value;

    swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs.Should().ContainKey("v1");
    swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs["v1"].Title.Should().Be("DotNetWorkQueue Dashboard");
}
```

**Required using directives to add** (check the existing file and add only what's missing):
- `using Microsoft.Extensions.Options;` — for `IOptions<T>`
- `using Swashbuckle.AspNetCore.SwaggerGen;` — for `SwaggerGenOptions`
- FluentAssertions should already be imported by the existing tests

The `SwaggerGenOptions.SwaggerGeneratorOptions.SwaggerDocs` property is a `Dictionary<string, OpenApiInfo>` keyed by document name (`"v1"` in this case). If the Swashbuckle API type names differ between versions, fall back to the simpler assertion:
```csharp
provider.GetService<ISwaggerProvider>().Should().NotBeNull();
```
and pick whichever one actually compiles + runs.

**Acceptance Criteria:**
- Test compiles and passes
- Assertion actually fails if `EnableSwagger = false` (verify by temporarily flipping the option — don't commit the flip, just confirm the test is real)

### Task 2: ApiKey security definition registers when ApiKey is set
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsSwaggerTests.cs` (MODIFY — add to new class)
**Action:** modify
**Description:**
When both `EnableSwagger = true` AND `ApiKey` is a non-empty string, the code adds an `ApiKey` security scheme to the Swagger gen options (lines 121-144). Test that this happens.

```csharp
[TestMethod]
public void AddDotNetWorkQueueDashboard_Registers_ApiKeySecurityScheme_When_ApiKey_Set()
{
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddDotNetWorkQueueDashboard(options =>
    {
        options.EnableSwagger = true;
        options.ApiKey = "test-secret";
    });

    var provider = services.BuildServiceProvider();
    var swaggerOptions = provider
        .GetRequiredService<IOptions<SwaggerGenOptions>>()
        .Value;

    swaggerOptions.SwaggerGeneratorOptions.SecuritySchemes.Should().ContainKey("ApiKey");
    var scheme = swaggerOptions.SwaggerGeneratorOptions.SecuritySchemes["ApiKey"];
    scheme.Type.Should().Be(SecuritySchemeType.ApiKey);
    scheme.In.Should().Be(ParameterLocation.Header);
    scheme.Name.Should().Be("X-Api-Key");
}
```

**Additional using directives:**
- `using Microsoft.OpenApi.Models;` — for `SecuritySchemeType`, `ParameterLocation`

The Swashbuckle `SwaggerGenOptions.SwaggerGeneratorOptions.SecuritySchemes` is a `Dictionary<string, OpenApiSecurityScheme>`. If this name differs in the actual Swashbuckle version used, use the alternative property path that actually exists — grep the Swashbuckle assembly references if needed.

**Acceptance Criteria:**
- Test passes with `ApiKey = "test-secret"`
- All 4 scheme assertions pass (key present, type, location, name)

### Task 3: Negative test — no ApiKey security scheme when ApiKey is empty
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsSwaggerTests.cs` (MODIFY)
**Action:** modify
**Description:**
Branch guard test — the `!string.IsNullOrEmpty(options.ApiKey)` check should prevent the security scheme from registering when `ApiKey` is empty (which is the default). This is the negative branch.

```csharp
[TestMethod]
public void AddDotNetWorkQueueDashboard_Does_Not_Register_ApiKeySecurityScheme_When_ApiKey_Empty()
{
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddDotNetWorkQueueDashboard(options =>
    {
        options.EnableSwagger = true;
        // ApiKey deliberately left at default (empty string)
    });

    var provider = services.BuildServiceProvider();
    var swaggerOptions = provider
        .GetRequiredService<IOptions<SwaggerGenOptions>>()
        .Value;

    swaggerOptions.SwaggerGeneratorOptions.SecuritySchemes.Should().NotContainKey("ApiKey");
}
```

**Acceptance Criteria:**
- Test passes with default empty `ApiKey`
- Negative assertion works (dictionary does NOT contain the key)

## Verification

```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" -c Debug --filter "FullyQualifiedName~Swagger"
```

Expected: 3 new tests pass.

Regression check:
```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" -c Debug
```

Expected: all existing `DashboardExtensionsTests` + 3 new tests pass.

## Coverage Target

This plan is expected to cover the Swagger registration block (~38 lines: the `AddEndpointsApiExplorer` + `AddSwaggerGen` lambda body + the nested `ApiKey` security definition block). Cluster C + C1 from RESEARCH.md section 6.
