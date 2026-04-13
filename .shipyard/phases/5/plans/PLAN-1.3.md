# Plan 1.3: CORS + AuthorizationPolicy Registration Tests

## Context

Two uncovered conditional branches in `AddDotNetWorkQueueDashboard(Action)`:

1. **CORS registration** (lines 80-91): `if (options.EnableCors && options.CorsOrigins.Length > 0)` registers a CORS policy named `"DashboardCors"`. Neither the positive branch nor the branch guard (empty `CorsOrigins` when `EnableCors = true`) is tested.

2. **Authorization policy convention** (lines 101-105): `if (!string.IsNullOrEmpty(options.AuthorizationPolicy))` adds a `DashboardAuthorizationConvention` to `MvcOptions.Conventions`. The positive branch is uncovered.

This plan adds 3 unit tests to close both.

Estimated coverage delta: **~17 lines** (CORS registration body ~12 lines + AuthorizationPolicy convention add ~5 lines).

## Dependencies

None — runs in Wave 1, parallel with PLAN-1.1 and PLAN-1.2.

## Tasks

### Task 1: CORS policy registers when EnableCors = true with origins
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsCorsAndAuthTests.cs` (NEW)
**Action:** create
**Description:**
Create a new test file `DashboardExtensionsCorsAndAuthTests.cs` under the same `DotNetWorkQueue.Dashboard.Api.Tests.Extensions` namespace. Creating a feature-scoped file (rather than appending to `DashboardExtensionsTests.cs`) avoids file-level conflicts with parallel Wave 1 plan PLAN-1.2 and matches PLAN-1.1's pattern.

Add the first test to the new class `DashboardExtensionsCorsAndAuthTests`: assert that calling `AddDotNetWorkQueueDashboard` with `EnableCors = true` and a non-empty `CorsOrigins` array results in a CORS policy named `"DashboardCors"` being registered via ASP.NET Core's `AddCors` infrastructure.

CORS policies are stored in `CorsOptions.PolicyMap`, accessible by resolving `IOptions<CorsOptions>` from the provider:

```csharp
[TestMethod]
public void AddDotNetWorkQueueDashboard_Registers_CorsPolicy_When_Enabled_With_Origins()
{
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddDotNetWorkQueueDashboard(options =>
    {
        options.EnableSwagger = false;
        options.EnableCors = true;
        options.CorsOrigins = new[] { "https://example.com", "https://localhost:5001" };
    });

    var provider = services.BuildServiceProvider();
    var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>().Value;

    var policy = corsOptions.GetPolicy("DashboardCors");
    policy.Should().NotBeNull();
    policy!.Origins.Should().BeEquivalentTo(new[] { "https://example.com", "https://localhost:5001" });
    policy.Headers.Should().Contain("*");  // AllowAnyHeader
    policy.Methods.Should().Contain("*");  // AllowAnyMethod
}
```

**Required using directives:**
- `using Microsoft.AspNetCore.Cors.Infrastructure;` — for `CorsOptions`
- `using Microsoft.Extensions.Options;` — for `IOptions<T>` (if not already present from PLAN-1.2)

Note on assertion style: if `policy.Headers` / `policy.Methods` include `*` as a literal string, the assertion above works. If ASP.NET Core represents "allow any" differently (e.g., `policy.AllowAnyHeader` boolean), adjust to match what the actual `CorsPolicy` class exposes. Fall back to just asserting `policy.Origins` contains the expected origins if the header/method checks are framework-version-fragile.

**Acceptance Criteria:**
- Test passes, policy `"DashboardCors"` is present with the expected origins
- Test does not use reflection — only public `CorsOptions` API

### Task 2: CORS policy does NOT register when CorsOrigins is empty
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsCorsAndAuthTests.cs` (MODIFY — add to new class)
**Action:** modify
**Description:**
The branch guard on line 80 is `options.EnableCors && options.CorsOrigins.Length > 0`. Test the guard: `EnableCors = true` but `CorsOrigins = Array.Empty<string>()` must NOT register the CORS policy. This exercises the false branch.

```csharp
[TestMethod]
public void AddDotNetWorkQueueDashboard_Does_Not_Register_CorsPolicy_When_Origins_Empty()
{
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddDotNetWorkQueueDashboard(options =>
    {
        options.EnableSwagger = false;
        options.EnableCors = true;
        options.CorsOrigins = Array.Empty<string>();  // explicit empty
    });

    var provider = services.BuildServiceProvider();
    var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>();

    // Either IOptions<CorsOptions> isn't registered at all (AddCors was never called),
    // OR it's registered but GetPolicy("DashboardCors") returns null.
    // The code path is: AddCors is NOT called in this branch, so the options may still
    // be registered by the broader MVC stack with the default value. Assert GetPolicy returns null.
    corsOptions.Value.GetPolicy("DashboardCors").Should().BeNull();
}
```

**Note:** The assertion above is defensive. If `AddCors` is truly skipped and `IOptions<CorsOptions>` is not registered anywhere, `GetRequiredService<IOptions<CorsOptions>>` will throw. In that case, switch to `GetService<IOptions<CorsOptions>>()` (nullable) and assert it's either null OR has no `"DashboardCors"` policy:
```csharp
var corsOptions = provider.GetService<IOptions<CorsOptions>>();
if (corsOptions != null)
    corsOptions.Value.GetPolicy("DashboardCors").Should().BeNull();
```

**Acceptance Criteria:**
- Test passes under whichever behavior ASP.NET Core actually exhibits
- Test makes it clear from the assertion that the `DashboardCors` policy is NOT registered

### Task 3: AuthorizationPolicy convention is added when policy is set
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsCorsAndAuthTests.cs` (MODIFY)
**Action:** modify
**Description:**
When `options.AuthorizationPolicy` is a non-empty string, the code adds a `DashboardAuthorizationConvention` to `MvcOptions.Conventions`. Test this by resolving `IOptions<MvcOptions>` from the provider and inspecting the `Conventions` collection.

```csharp
[TestMethod]
public void AddDotNetWorkQueueDashboard_Adds_AuthorizationConvention_When_Policy_Set()
{
    var services = new ServiceCollection();
    services.AddLogging();

    services.AddDotNetWorkQueueDashboard(options =>
    {
        options.EnableSwagger = false;
        options.AuthorizationPolicy = "DashboardAdmin";
    });

    var provider = services.BuildServiceProvider();
    var mvcOptions = provider.GetRequiredService<IOptions<MvcOptions>>().Value;

    mvcOptions.Conventions
        .OfType<DashboardAuthorizationConvention>()
        .Should().HaveCount(1);
}
```

**Required using directives:**
- `using Microsoft.AspNetCore.Mvc;` — for `MvcOptions`
- `using DotNetWorkQueue.Dashboard.Api;` — already present in the file (confirm)

**Note on internal access:** The `DashboardAuthorizationConvention` class is declared `internal` in `DashboardExtensions.cs:295`, but it is accessible from the test project because `Source/DotNetWorkQueue.Dashboard.Api/InternalsVisibleForTests.cs:21` already grants `InternalsVisibleTo("DotNetWorkQueue.Dashboard.Api.Tests")`. No production change needed — the test can directly reference `DashboardAuthorizationConvention` by name.

**Acceptance Criteria:**
- Test passes
- Either `DashboardAuthorizationConvention` is directly referenceable (InternalsVisibleTo added) or a reflection-based name check is used
- The InternalsVisibleTo addition (if made) is in a separate commit from the test additions, so it can be reviewed independently

## Verification

```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" -c Debug --filter "FullyQualifiedName~Cors OR FullyQualifiedName~Authorization"
```

Expected: 3 new tests pass.

Full-suite regression:
```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" -c Debug
```

## Coverage Target

This plan is expected to cover:
- CORS Add-path block lines 80-91 (~12 lines)
- Authorization convention block lines 101-105 (~5 lines)

Cluster A + cluster B from RESEARCH.md section 6. Total ~17 lines, which pushes `DashboardExtensions` from ~60% (after PLAN-1.1 completes) toward ~64-65%.
