# Plan 2.1: Dashboard API Exception Filter + CORS (H-4, H-3/M-9)

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Harden the Dashboard API exception filter to stop leaking internal exception details in non-Development environments, add a catch-all handler for unhandled exceptions, and add configurable CORS policy support.
**Architecture:** The existing `DashboardExceptionFilter` returns raw `context.Exception.Message` for `InvalidOperationException` and `NotSupportedException`. In non-Development environments, these must return a generic error message while still logging the full exception server-side. A catch-all `default` case is added for any unhandled exception type. CORS is wired via `DashboardOptions.CorsOrigins` and `DashboardOptions.EnableCors` properties, registered in `DashboardExtensions.AddDotNetWorkQueueDashboard()`. This plan also owns `DashboardExtensions.cs` and includes the health check service registration line for Plan 2.2.
**Tech Stack:** ASP.NET Core MVC Filters, CORS middleware, IHostEnvironment, MSTest + FluentAssertions

## Dependencies
- PLAN-1.1 (Wave 1 -- CPM must be complete so `.csproj` files are stable)

## Tasks

### Task 1: Harden DashboardExceptionFilter with environment-aware error messages and catch-all
**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Api/Middleware/DashboardExceptionFilter.cs`
- Create: `Source/DotNetWorkQueue.Dashboard.Api.Tests/Middleware/DashboardExceptionFilterTests.cs`

**Steps:**

1. Modify `DashboardExceptionFilter.cs`:
   - Add `using Microsoft.Extensions.Hosting;` to the usings
   - Add `IHostEnvironment` as a second constructor parameter and store it in `_environment`
   - For `InvalidOperationException` (404) and `NotSupportedException` (501): use `_environment.IsDevelopment()` to decide whether to include `context.Exception.Message` or the generic string `"An internal error occurred"`
   - Add a `default` case that catches any unhandled exception, logs it as `LogError`, and returns a 500 with `"An internal error occurred"`
   - Keep `ObjectDisposedException` unchanged (it already returns a generic "Service unavailable" message)

The modified `OnException` method should look like:
```csharp
public void OnException(ExceptionContext context)
{
    var isDev = _environment.IsDevelopment();

    switch (context.Exception)
    {
        case ObjectDisposedException:
            _logger.LogWarning(context.Exception, "Dashboard service was disposed while handling {Method} {Path}",
                context.HttpContext.Request.Method, context.HttpContext.Request.Path);
            context.Result = new ObjectResult(new { error = "Service unavailable" })
            {
                StatusCode = 503
            };
            context.ExceptionHandled = true;
            break;

        case InvalidOperationException:
            _logger.LogWarning(context.Exception, "Resource not found: {Message}", context.Exception.Message);
            context.Result = new ObjectResult(new { error = isDev ? context.Exception.Message : "An internal error occurred" })
            {
                StatusCode = 404
            };
            context.ExceptionHandled = true;
            break;

        case NotSupportedException:
            _logger.LogWarning(context.Exception, "Unsupported operation: {Message}", context.Exception.Message);
            context.Result = new ObjectResult(new { error = isDev ? context.Exception.Message : "An internal error occurred" })
            {
                StatusCode = 501
            };
            context.ExceptionHandled = true;
            break;

        default:
            _logger.LogError(context.Exception, "Unhandled exception in dashboard API: {Method} {Path}",
                context.HttpContext.Request.Method, context.HttpContext.Request.Path);
            context.Result = new ObjectResult(new { error = isDev ? context.Exception.Message : "An internal error occurred" })
            {
                StatusCode = 500
            };
            context.ExceptionHandled = true;
            break;
    }
}
```

2. Create `DashboardExceptionFilterTests.cs` with these tests:
   - `InvalidOperationException_In_Development_Returns_Detailed_Message` -- verify 404 with actual exception message
   - `InvalidOperationException_In_Production_Returns_Generic_Message` -- verify 404 with "An internal error occurred"
   - `NotSupportedException_In_Development_Returns_Detailed_Message` -- verify 501 with actual message
   - `NotSupportedException_In_Production_Returns_Generic_Message` -- verify 501 with "An internal error occurred"
   - `ObjectDisposedException_Always_Returns_Service_Unavailable` -- verify 503 with "Service unavailable" regardless of environment
   - `UnhandledException_In_Production_Returns_Generic_500` -- verify default case returns 500 with "An internal error occurred"
   - `UnhandledException_In_Development_Returns_Detailed_500` -- verify default case returns 500 with actual message

Use `NSubstitute` to mock `IHostEnvironment` (set `EnvironmentName` to `"Development"` or `"Production"`). Use `NSubstitute` to mock `ILogger<DashboardExceptionFilter>`. Follow the `ExceptionContext` pattern:
```csharp
var context = new ExceptionContext(
    new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
    new List<IFilterMetadata>())
{
    Exception = new InvalidOperationException("Queue not found")
};
```

Assert on `context.Result` being `ObjectResult` with the expected `StatusCode` and deserialize the anonymous object to check the `error` property.

**Verify:**
```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" --filter "FullyQualifiedName~DashboardExceptionFilterTests"
```

### Task 2: Add CORS configuration to DashboardOptions and wire in DashboardExtensions
**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardOptions.cs`
- Modify: `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs`
- Modify: `Source/DotNetWorkQueue.Dashboard.Api.Tests/Configuration/DashboardOptionsTests.cs`

**Steps:**

1. Add two new properties to `DashboardOptions.cs` (after the `ReadOnly` property, before `EnableConsumerTracking`):
```csharp
/// <summary>
/// Gets or sets whether CORS is enabled.
/// When true, the CORS middleware is added with origins from <see cref="CorsOrigins"/>.
/// </summary>
public bool EnableCors { get; set; }

/// <summary>
/// Gets or sets the allowed CORS origins.
/// Only used when <see cref="EnableCors"/> is true.
/// Example: <c>new[] { "http://localhost:5000", "https://dashboard.example.com" }</c>
/// </summary>
public string[] CorsOrigins { get; set; } = Array.Empty<string>();
```

2. In `DashboardExtensions.AddDotNetWorkQueueDashboard()`, add CORS registration **before** the `services.AddControllers()` call (after the consumer tracking block):
```csharp
if (options.EnableCors && options.CorsOrigins.Length > 0)
{
    services.AddCors(corsOptions =>
    {
        corsOptions.AddPolicy("DashboardCors", policy =>
        {
            policy.WithOrigins(options.CorsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}
```

3. In `DashboardExtensions.UseDotNetWorkQueueDashboard()`, add CORS middleware **before** the Swagger block:
```csharp
if (options.EnableCors && options.CorsOrigins.Length > 0)
{
    app.UseCors("DashboardCors");
}
```

4. Also in `DashboardExtensions.AddDotNetWorkQueueDashboard()`, add health check service registration (for Plan 2.2) **after** the CORS block, before `services.AddControllers()`:
```csharp
services.AddHealthChecks();
```

5. Also in `DashboardExtensions.UseDotNetWorkQueueDashboard()`, add health check endpoint mapping. Since `IApplicationBuilder` does not have `MapHealthChecks`, add a comment noting that the consuming application should call `app.MapHealthChecks("/health")` on its `WebApplication` or `IEndpointRouteBuilder`. Alternatively, add after the Swagger block:
```csharp
app.UseHealthChecks("/health");
```
This requires adding `using Microsoft.AspNetCore.Diagnostics.HealthChecks;` -- but since `UseHealthChecks` is in `Microsoft.AspNetCore.Diagnostics.HealthChecks` namespace which is part of the shared framework, this works. Actually, `UseHealthChecks` is an extension on `IApplicationBuilder` from `Microsoft.AspNetCore.Builder` namespace in `Microsoft.AspNetCore.Diagnostics.HealthChecks` package. Since the Dashboard targets `net8.0`/`net10.0` with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, the health check middleware is available without additional packages. Add:
```csharp
using Microsoft.AspNetCore.Builder;  // already present
```
And in `UseDotNetWorkQueueDashboard`:
```csharp
app.UseHealthChecks("/api/v1/dashboard/health");
```

6. Add tests to `DashboardOptionsTests.cs`:
```csharp
[TestMethod]
public void EnableCors_Defaults_To_False()
{
    var opts = new DashboardOptions();
    opts.EnableCors.Should().BeFalse();
}

[TestMethod]
public void CorsOrigins_Defaults_To_Empty()
{
    var opts = new DashboardOptions();
    opts.CorsOrigins.Should().BeEmpty();
}

[TestMethod]
public void CorsOrigins_Can_Be_Set()
{
    var opts = new DashboardOptions
    {
        EnableCors = true,
        CorsOrigins = new[] { "http://localhost:5000" }
    };
    opts.EnableCors.Should().BeTrue();
    opts.CorsOrigins.Should().ContainSingle().Which.Should().Be("http://localhost:5000");
}
```

**Verify:**
```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" --filter "FullyQualifiedName~DashboardOptionsTests"
```

## Verification

```bash
# 1. All Dashboard API unit tests pass
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj"

# 2. Exception filter tests exist and pass
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" --filter "FullyQualifiedName~DashboardExceptionFilterTests"

# 3. CORS properties exist
grep -q "EnableCors" Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardOptions.cs && echo "CORS property: PASS"
grep -q "CorsOrigins" Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardOptions.cs && echo "CORS origins: PASS"

# 4. CORS wired in extensions
grep -q "AddCors\|UseCors" Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs && echo "CORS middleware: PASS"

# 5. Health checks registered
grep -q "AddHealthChecks\|UseHealthChecks" Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs && echo "Health checks: PASS"

# 6. Solution builds
dotnet build "Source/DotNetWorkQueue.sln" -c Debug
```
