# Research: Tier B Moderate Fixes

## Context

DotNetWorkQueue is a .NET producer/distributed consumer library targeting .NET 10.0, .NET 8.0, .NET Framework 4.8, and .NET Standard 2.0. This research gathers precise codebase details for four Tier B fix areas: Central Package Management, Dashboard API Exception Filter improvements, Dashboard API Health Check endpoint, and TODO/HACK audit with binder fix.

---

## 1. Central Package Management (H-6)

### Current State

- **No `Directory.Packages.props` exists** anywhere in the repository.
- **No `Directory.Build.props` exists** in `/Source/` or the repo root.
- **36 `.csproj` files** exist under `Source/`.

### Complete Unique Package Inventory (34 distinct packages)

| Package | Version | Used In (count) |
|---------|---------|-----------------|
| AutoFixture | 4.18.1 | ~15 test projects |
| AutoFixture.AutoNSubstitute | 4.18.1 | ~15 test projects |
| CompareNETObjects | 4.84.0 | 1 (SQLite Integration Tests) |
| FluentAssertions | 6.12.2 | ~18 test projects |
| GuerrillaNtp | 3.1.0 | 1 (core) |
| LiteDB | 5.0.21 | 2 (Transport.LiteDB, LiteDB.Tests) |
| Microsoft.AspNetCore.TestHost | 8.0.13 | 1 (Dashboard.Api.Integration.Tests, net8.0 condition) |
| Microsoft.AspNetCore.TestHost | 10.0.3 | 1 (Dashboard.Api.Integration.Tests, net10.0 condition) |
| Microsoft.CSharp | 4.7.0 | 1 (core) |
| Microsoft.Data.SqlClient | 6.1.3 | 2 (Transport.SqlServer, SqlServer.Tests) |
| Microsoft.Extensions.Caching.Memory | 9.0.3 | 1 (Transport.Redis) |
| Microsoft.Extensions.Http | 9.0.3 | 1 (Dashboard.Client) |
| Microsoft.IO.RecyclableMemoryStream | 3.0.1 | 1 (Transport.Redis) |
| Microsoft.NET.Test.Sdk | 18.0.1 | ~18 test projects |
| MsgPack.Cli | 1.0.1 | 1 (Transport.Redis) |
| MSTest.TestAdapter | 4.1.0 | ~18 test projects |
| MSTest.TestFramework | 4.1.0 | ~18 test projects |
| MudBlazor | 9.1.0 | 1 (Dashboard.Ui) |
| Newtonsoft.Json | 13.0.4 | 1 (core) |
| Npgsql | 8.0.8 | 1 (Transport.PostgreSQL) |
| NSubstitute | 5.3.0 | ~15 test projects |
| OpenTelemetry | 1.14.0 | 1 (core) |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.14.0 | 1 (IntegrationTests.Shared) |
| Polly.Core | 8.6.5 | 1 (core) |
| SimpleInjector | 5.5.0 | 1 (core) |
| StackExchange.Redis | 2.10.1 | 1 (Transport.Redis) |
| Swashbuckle.AspNetCore | 7.2.0 | 1 (Dashboard.Api) |
| System.Data.SQLite.Core | 1.0.119 | 1 (Transport.SQLite) |
| System.Diagnostics.DiagnosticSource | 10.0.1 | 1 (core) |
| Tynamix.ObjectFiller | 1.5.9 | 1 (IntegrationTests.Shared) |
| xunit.runner.visualstudio | 3.1.5 | 1 (appears in some test projects) |

### Version Conflicts

**No true version conflicts found.** All packages use a single version across all projects. The only multi-version entry is `Microsoft.AspNetCore.TestHost` which intentionally uses TFM-conditional versions (8.0.13 for net8.0, 10.0.3 for net10.0) -- this is correct behavior and should be preserved with conditional `PackageVersion` entries in `Directory.Packages.props`.

### Projects Requiring Modification

All 36 `.csproj` files under `Source/` will need `Version=` attributes removed from `PackageReference` entries. Projects with no `PackageReference` entries (e.g., `DotNetWorkQueue.Transport.RelationalDatabase.csproj`) need no changes.

### Key Observations

- Test projects are the heaviest consumers of shared packages (AutoFixture, FluentAssertions, MSTest, NSubstitute all appear in 15-18 projects each).
- Production libraries mostly have unique dependencies (Npgsql only in PostgreSQL, StackExchange.Redis only in Redis, etc.).
- The `Microsoft.AspNetCore.TestHost` conditional version pattern will need special handling in `Directory.Packages.props`.

---

## 2. Dashboard API Exception Filter (H-4)

### Current Implementation

**File:** `Source/DotNetWorkQueue.Dashboard.Api/Middleware/DashboardExceptionFilter.cs`

```csharp
internal class DashboardExceptionFilter : IExceptionFilter
{
    private readonly ILogger<DashboardExceptionFilter> _logger;

    public DashboardExceptionFilter(ILogger<DashboardExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ObjectDisposedException:       // -> 503
            case InvalidOperationException:     // -> 404
            case NotSupportedException:         // -> 501
        }
        // NO default case -- unhandled exceptions fall through with no structured response
    }
}
```

### Issues Identified

1. **No default/catch-all handler** -- Any exception not matching the three types (e.g., `ArgumentException`, `KeyNotFoundException`, `OperationCanceledException`, or any unexpected runtime error) falls through unhandled. This produces raw ASP.NET error pages rather than structured JSON.
2. **No environment-aware detail control** -- Stack traces and internal details are always omitted. In Development mode, including exception details would aid debugging. `IHostEnvironment` / `IWebHostEnvironment` is **not injected anywhere** in the Dashboard.Api project currently.
3. **`InvalidOperationException` mapped to 404 is overly broad** -- `InvalidOperationException` is one of the most commonly thrown .NET exceptions (LINQ `Single()`, state errors, etc.). Mapping all of them to 404 could mask real bugs.

### Registration Point

**File:** `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs` (line 70)
```csharp
mvcOptions.Filters.Add<DashboardExceptionFilter>();
```
The filter is registered via DI (generic type parameter), so the constructor can accept any DI-resolvable service.

### Existing Tests

**Directory:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Middleware/`
- Only `ApiKeyAuthorizationFilterTests.cs` exists (no `DashboardExceptionFilterTests.cs`).

### Test Pattern (from `ApiKeyAuthorizationFilterTests.cs`)

Tests use MSTest + FluentAssertions. Pattern:
- Create `DefaultHttpContext`
- Build `ActionContext` from it with `RouteData` and `ActionDescriptor`
- Create filter-specific context (e.g., `AuthorizationFilterContext`, for exception filter it would be `ExceptionContext`)
- Assert on `context.Result` (type and status code)

For `DashboardExceptionFilter` tests, the pattern would be:
```csharp
var context = new ExceptionContext(
    new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
    new List<IFilterMetadata>())
{
    Exception = new SomeException("test")
};
```

### Configuration Object

**File:** `Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardOptions.cs`

Current properties:
- `EnableSwagger` (bool, default true)
- `AuthorizationPolicy` (string, nullable)
- `ApiKey` (string, nullable)
- `ReadOnly` (bool)
- `EnableConsumerTracking` (bool, default true)
- `ConsumerHeartbeatIntervalSeconds` (int, default 30)
- `ConsumerStaleThresholdSeconds` (int, default 90)
- `InterceptorProfiles` (Dictionary)
- `ConnectionRegistrations` (internal List)

No existing `IncludeExceptionDetails` or environment-related property.

---

## 3. Dashboard API Health Check (H-3)

### Current State

- **No health check packages** are referenced anywhere in the codebase (grep for `HealthCheck`, `IHealthCheck`, `AspNetCore.Diagnostics.HealthChecks` returned zero results).
- **No health check endpoints** exist.

### Controller Pattern

All existing controllers follow this exact pattern:

```csharp
[ApiController]
[Route("api/v1/dashboard/<resource>")]
[Produces("application/json")]
public class XxxController : ControllerBase
{
    private readonly IDashboardService _service;
    // optional: private readonly DashboardOptions _options;

    public XxxController(IDashboardService service /*, DashboardOptions options */)
    {
        _service = service;
        // _options = options;
    }
}
```

Controllers:
- `ConnectionsController` -- route `api/v1/dashboard/connections`, injects `IDashboardService` + `DashboardOptions`
- `QueuesController` -- route `api/v1/dashboard/queues`, injects `IDashboardService`
- `ConsumersController` -- route `api/v1/dashboard/consumers`, injects `IConsumerRegistry` + `DashboardOptions`

### Service Registration

`DashboardExtensions.AddDotNetWorkQueueDashboard()` registers:
- `DashboardOptions` as singleton
- `IDashboardApi` as singleton (factory)
- `IDashboardService` as singleton
- `IConsumerRegistry` as singleton
- `ConsumerPruningService` as hosted service (conditional)
- MVC controllers with filters

`DashboardExtensions.UseDotNetWorkQueueDashboard()` configures:
- Swagger middleware (conditional)

### Dashboard API README

**File:** `Source/DotNetWorkQueue.Dashboard.Api/README.md`
Lists features but does not mention health checks. Will need updating.

### Health Check Design Considerations

A health check should verify:
1. The `IDashboardApi` singleton is alive (not disposed)
2. Optionally, each registered connection is reachable (transport-level connectivity)

Two approaches:
- **ASP.NET Core built-in health checks** (`Microsoft.Extensions.Diagnostics.HealthChecks`) -- already included in the shared framework for net8.0/net10.0, no extra NuGet needed. Register via `services.AddHealthChecks().AddCheck<T>()`.
- **Simple controller endpoint** -- A plain `GET api/v1/dashboard/health` endpoint returning status JSON. Simpler, no new dependencies, follows existing controller pattern.

---

## 4. TODO/HACK Audit + Binder Fix (M-3, N-3)

### Confirmed TODO/HACK Locations (4 total)

| # | File | Line | Comment |
|---|------|------|---------|
| 1 | `Source/DotNetWorkQueue/Factory/InterceptorFactory.cs` | 52 | `//HACK for now - it's not clear to me if simple injector supports this pattern` |
| 2 | `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/QueryHandler/ReceiveMessage.cs` | 175 | `{ //TODO - cache based on route` |
| 3 | `Source/DotNetWorkQueue.Transport.SqlServer/Basic/Message/ReceiveMessage.cs` | 100 | `//TODO - we could consider using a task to update the status table` |
| 4 | `Source/DotNetWorkQueue.Transport.SqlServer/Basic/QueryHandler/CreateDequeueStatement.cs` | 237 | `{ //TODO - cache based on route` |

All four locations still exist as confirmed by grep.

### Binder Fix Analysis (N-3)

**The issue:** `SerializerThatWillCrashOnDeSerialization` in `Source/DotNetWorkQueue.IntegrationTests.Shared/Helpers.cs` (line 108-128) uses `TypeNameHandling.All` without a `SerializationBinder`:

```csharp
public class SerializerThatWillCrashOnDeSerialization : ISerializer
{
    private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.All
        // NO SerializationBinder set -- security gap
    };
    // ...
}
```

**Production code comparison:** `JsonSerializerInternal` (the production serializer) at `Source/DotNetWorkQueue/Serialization/JsonSerializerInternal.cs` line 43-55 properly uses the `DenyListSerializationBinder`:

```csharp
public JsonSerializerInternal(ISerializationBinder serializationBinder)
{
    _serializeSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        SerializationBinder = serializationBinder
    };
    _deserializeSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto,
        SerializationBinder = serializationBinder
    };
}
```

**DI Registration:** `DenyListSerializationBinder` is registered as a singleton implementation of `ISerializationBinder` at `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs` line 282:
```csharp
container.Register<ISerializationBinder, DenyListSerializationBinder>(LifeStyles.Singleton);
```

**`DenyListSerializationBinder` class** at `Source/DotNetWorkQueue/Serialization/DenyListSerializationBinder.cs`:
- Namespace: `DotNetWorkQueue.Serialization`
- Parameterless constructor: `public DenyListSerializationBinder()` -- initializes with default denied types
- Implements `ISerializationBinder` (Newtonsoft.Json)
- Has `AddDeniedType(string)` and `AddDeniedTypes(IEnumerable<string>)` for extension
- Default deny list includes ~15 known gadget types (ObjectDataProvider, WindowsIdentity, Process, FileInfo, DataSet, etc.)

**Fix approach:** The `SerializerThatWillCrashOnDeSerialization` test helper should add:
```csharp
SerializationBinder = new DenyListSerializationBinder()
```
to its `JsonSerializerSettings`. This is a test-only class designed to crash on deserialization (throws `AccessViolationException`), but it should still have the binder to prevent analyzer warnings and maintain consistent security patterns. The class already only serializes (the `ConvertBytesToMessage` always throws), so adding the binder has no functional impact but improves security posture.

**Existing tests for `DenyListSerializationBinder`:** `Source/DotNetWorkQueue.Tests/Serialization/DenyListSerializationBinderTests.cs` has comprehensive tests covering denied types and allowed types.

---

## Summary of Scope

| Item | Files to Create | Files to Modify | New Dependencies |
|------|----------------|-----------------|------------------|
| H-6: Central Package Management | 1 (`Directory.Packages.props`) | ~30 .csproj files (remove Version=) | None |
| H-4: Exception Filter | 0 | 1 (`DashboardExceptionFilter.cs`), 1 (new test file) | None |
| H-3: Health Check | 1 (new controller or health check class) | 1 (`DashboardExtensions.cs`), 1 (`README.md`) | None (built into shared framework) |
| M-3/N-3: TODO Audit + Binder | 0 | 1 (`Helpers.cs`) + 4 TODO comment files | None |
