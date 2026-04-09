# Review: Plan 2.1

## Verdict: PASS

## Findings

### Critical
None.

### Important
- **Regex not compiled/cached in Slugify** (`DashboardApiSourceConfig.cs:59-60`): Pre-existing from Plan 1.1. `Slug` property recomputes on every access with two uncompiled `Regex.Replace` calls. Should use `[GeneratedRegex]` source generators or `private static readonly Regex` with `RegexOptions.Compiled`.

### Minor
- **LocalSourceHostedService hardcodes "Local" name** (`LocalSourceHostedService.cs:68`): `GetByName("Local")` is hardcoded, but Program.cs supports configurable `DashboardApi:LocalSourceName`. If someone configures a different name, the hosted service won't find the source to update. Should inject the configured name.
- **Undisposed HttpClient in tests** (`MultiSourceDashboardApiClientTests.cs:43`): `new HttpClient()` per call, never disposed. Harmless in tests.
- **Extra test beyond spec** (`MultiSourceDashboardApiClientTests.cs:120`): `GetClientForSource_Calls_HttpClientFactory_With_Slug` — valuable addition.

### Positive
- Program.cs refactor is clean — multi-source registration, legacy detection, named HttpClients per source
- `DashboardConfigParser` is well-separated as a static testable class
- `global::` prefix correctly handles `DotNetWorkQueue.IConfiguration` namespace conflict
- Real `FeatureCollection` instead of fragile NSubstitute indexer mock — pragmatic decision
- All 40 tests pass on both net10.0 and net8.0
- LGPL-2.1 headers present on all new files
- appsettings.json properly updated to new format
