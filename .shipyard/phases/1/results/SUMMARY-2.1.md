# Build Summary: Plan 2.1

## Status: complete

## Tasks Completed
- Task 1: IMultiSourceDashboardApiClient with ConcurrentDictionary caching — complete (committed by builder agent `0cb27427`)
  - Files: `IMultiSourceDashboardApiClient.cs`, `MultiSourceDashboardApiClient.cs`, `MultiSourceDashboardApiClientTests.cs`
- Task 2: DashboardConfigParser, LocalSourceHostedService, Program.cs refactor — complete (committed `0f1c0939`)
  - Files: `DashboardConfigParser.cs`, `LocalSourceHostedService.cs`, `Program.cs`, `appsettings.json`, `ConfigValidationTests.cs`
- Task 3: LocalSourceHostedService unit tests — complete (committed `c1616d37`)
  - Files: `LocalSourceHostedServiceTests.cs`

## Files Created
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/IMultiSourceDashboardApiClient.cs` — interface with GetClientForSource(slug) and GetAllSources()
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/MultiSourceDashboardApiClient.cs` — ConcurrentDictionary-cached implementation
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardConfigParser.cs` — static config parsing and legacy format validation
- `Source/DotNetWorkQueue.Dashboard.Ui/Services/LocalSourceHostedService.cs` — IHostedService resolving IServer address after startup
- `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/MultiSourceDashboardApiClientTests.cs` — 6 tests
- `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/ConfigValidationTests.cs` — 7 tests
- `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/LocalSourceHostedServiceTests.cs` — 5 tests (net8.0 + net10.0 verified)

## Files Modified
- `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs` — replaced single-source AddHttpClient with multi-source config parsing, named HttpClients, ISourceRegistry, IMultiSourceDashboardApiClient registration. Removed old IDashboardApiClient DI.
- `Source/DotNetWorkQueue.Dashboard.Ui/appsettings.json` — updated to new `DashboardApi:Sources[]` format (empty array; self-contained mode auto-adds "Local")

## Decisions Made
- **IConfiguration namespace conflict:** `DotNetWorkQueue.IConfiguration` shadows `Microsoft.Extensions.Configuration.IConfiguration` in test files. Fixed with `global::Microsoft.Extensions.Configuration.IConfiguration` fully-qualified return type in ConfigValidationTests. Production code uses `global::` prefix in parameter types.
- **IFeatureCollection mocking:** NSubstitute indexer mocking doesn't work reliably with `IFeatureCollection`. Used real `FeatureCollection` with `Set<T>()` instead — cleaner and more reliable.
- **appsettings.json format:** Updated to empty `Sources[]` array. Self-contained mode auto-adds "Local" source; external deployments configure their own sources array.

## Issues Encountered
- Builder agent stalled on Task 2 due to `DotNetWorkQueue.IConfiguration` namespace conflict — the compiler resolves `IConfiguration` via namespace hierarchy (walks up to `DotNetWorkQueue` namespace) before considering `using` directives. Fixed manually with `global::` fully-qualified types.
- NSubstitute indexer mock for `IFeatureCollection[typeof(T)]` returned null instead of the configured substitute on net8.0. Replaced with concrete `FeatureCollection` class.

## Verification Results
- 40 tests passing on both net10.0 and net8.0
- Full solution build: 0 errors, 8 warnings (pre-existing SYSLIB0012 + 1 nullable annotation in test, fixed)
- `dotnet build "Source/DotNetWorkQueue.sln" -c Debug` — success
