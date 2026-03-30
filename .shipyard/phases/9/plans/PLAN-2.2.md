# Plan 2.2: Dashboard API Health Check + Docs (H-3)

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Add a custom health check class that reports Dashboard API liveness, and update the README with internal-only deployment recommendations.
**Architecture:** ASP.NET Core's built-in health check infrastructure (`Microsoft.Extensions.Diagnostics.HealthChecks`) is registered in `DashboardExtensions.cs` by Plan 2.1. This plan creates a custom `IHealthCheck` implementation that reports uptime and service status, registers it, and updates the README. This plan does NOT modify `DashboardExtensions.cs` (owned by Plan 2.1).
**Tech Stack:** ASP.NET Core Health Checks (built into shared framework), MSTest + NSubstitute + FluentAssertions

## Dependencies
- PLAN-1.1 (Wave 1 -- CPM must be complete)
- PLAN-2.1 (registers `services.AddHealthChecks()` and `app.UseHealthChecks()` in `DashboardExtensions.cs`)

## Tasks

### Task 1: Create DashboardHealthCheck implementing IHealthCheck
**Files:**
- Create: `Source/DotNetWorkQueue.Dashboard.Api/Services/DashboardHealthCheck.cs`

**Steps:**

1. Create `DashboardHealthCheck.cs` in the `Services` folder. It should implement `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck`. The check verifies that `IDashboardApi` is not disposed and reports uptime.

```csharp
// (LGPL-2.1 license header)
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotNetWorkQueue.Dashboard.Api.Services
{
    /// <summary>
    /// Health check that verifies the Dashboard API service is alive and reports uptime.
    /// </summary>
    internal class DashboardHealthCheck : IHealthCheck
    {
        private static readonly DateTime StartTime = DateTime.UtcNow;
        private readonly IDashboardApi _dashboardApi;

        public DashboardHealthCheck(IDashboardApi dashboardApi)
        {
            _dashboardApi = dashboardApi;
        }

        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Verify the service is accessible (not disposed)
                var connections = _dashboardApi.GetConnections();

                var data = new Dictionary<string, object>
                {
                    { "status", "Healthy" },
                    { "uptime", (DateTime.UtcNow - StartTime).ToString() },
                    { "connections", connections.Count }
                };

                return Task.FromResult(HealthCheckResult.Healthy("Dashboard API is running", data));
            }
            catch (ObjectDisposedException)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Dashboard API service has been disposed"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Dashboard API health check failed", ex));
            }
        }
    }
}
```

2. **Important:** Plan 2.1 registers `services.AddHealthChecks()` in `DashboardExtensions.cs`. The health check class needs to be registered there too. Since Plan 2.1 owns that file, the builder should add this line in `DashboardExtensions.AddDotNetWorkQueueDashboard()` right after the `services.AddHealthChecks()` line:
```csharp
services.AddHealthChecks()
    .AddCheck<DashboardHealthCheck>("dashboard");
```
If Plan 2.1 already added just `services.AddHealthChecks();`, modify it to chain `.AddCheck<DashboardHealthCheck>("dashboard")`. Add `using DotNetWorkQueue.Dashboard.Api.Services;` if not already present.

**Verify:**
```bash
dotnet build "Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj" -c Debug
```

### Task 2: Create unit tests for DashboardHealthCheck
**Files:**
- Create: `Source/DotNetWorkQueue.Dashboard.Api.Tests/Services/DashboardHealthCheckTests.cs`

**Steps:**

1. Create test file following the existing test patterns (MSTest + NSubstitute + FluentAssertions):

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Services
{
    [TestClass]
    public class DashboardHealthCheckTests
    {
        [TestMethod]
        public async Task CheckHealthAsync_When_Service_Healthy_Returns_Healthy()
        {
            var api = Substitute.For<IDashboardApi>();
            api.GetConnections().Returns(new List<DashboardConnectionInfo>());
            var check = new DashboardHealthCheck(api);

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Healthy);
            result.Data.Should().ContainKey("uptime");
            result.Data.Should().ContainKey("connections");
        }

        [TestMethod]
        public async Task CheckHealthAsync_When_Service_Disposed_Returns_Unhealthy()
        {
            var api = Substitute.For<IDashboardApi>();
            api.GetConnections().Throws(new ObjectDisposedException("test"));
            var check = new DashboardHealthCheck(api);

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Contain("disposed");
        }

        [TestMethod]
        public async Task CheckHealthAsync_When_Exception_Returns_Unhealthy()
        {
            var api = Substitute.For<IDashboardApi>();
            api.GetConnections().Throws(new InvalidOperationException("broken"));
            var check = new DashboardHealthCheck(api);

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Contain("failed");
        }
    }
}
```

**Note:** `DashboardHealthCheck` is `internal`. The project already has `InternalsVisibleForTests.cs` at `Source/DotNetWorkQueue.Dashboard.Api/InternalsVisibleForTests.cs` that exposes internals to the test project, so this will work.

**Verify:**
```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" --filter "FullyQualifiedName~DashboardHealthCheckTests"
```

### Task 3: Update Dashboard API README with internal-only recommendation
**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Api/README.md`

**Steps:**

1. Add a "Deployment" section after the "Quick Start" section and before "Documentation":

```markdown
## Deployment

> **Important:** The Dashboard API is designed for internal use only. Deploy it behind a VPN, firewall, or reverse proxy that restricts access to authorized operators.

**Infrastructure Concerns (not handled by the API):**
- **HTTPS/TLS** -- Terminate TLS at your reverse proxy (nginx, HAProxy, AWS ALB)
- **Rate limiting** -- Configure at the infrastructure layer
- **Authentication** -- Use the built-in API key (`ApiKey` option) or configure an ASP.NET Core authorization policy (`AuthorizationPolicy` option)
- **CORS** -- Configure allowed origins via `EnableCors` and `CorsOrigins` options when the Blazor UI runs on a different origin

**Health Check:**
The API exposes a health check endpoint at `/api/v1/dashboard/health` for use with load balancers and monitoring systems. Returns HTTP 200 when healthy with uptime and connection count data.
```

**Verify:**
```bash
grep -q "internal use only" Source/DotNetWorkQueue.Dashboard.Api/README.md && echo "README updated: PASS"
grep -q "health" Source/DotNetWorkQueue.Dashboard.Api/README.md && echo "Health documented: PASS"
```

## Verification

```bash
# 1. Health check class exists and builds
test -f Source/DotNetWorkQueue.Dashboard.Api/Services/DashboardHealthCheck.cs && echo "Health check file: PASS"
dotnet build "Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj" -c Debug

# 2. Health check tests pass
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" --filter "FullyQualifiedName~DashboardHealthCheckTests"

# 3. README has deployment section
grep -q "internal use only" Source/DotNetWorkQueue.Dashboard.Api/README.md && echo "README: PASS"

# 4. Full Dashboard API test suite passes
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj"
```
