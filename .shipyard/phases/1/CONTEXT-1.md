# Phase 1 Context: Design Decisions

## Test project — New UI test project

Create `DotNetWorkQueue.Dashboard.Ui.Tests` as a separate project. Clean separation between API and UI service tests. Follow the same MSTest 3.x + NSubstitute + AutoFixture + FluentAssertions stack as other test projects.

## Backward compat shim — Skip, break cleanly

Do NOT register `IDashboardApiClient` as a backward-compat shim resolving to the first source. Remove the old single-source DI registration entirely. Phase 1 may break page compilation — Phase 2 will fix all pages immediately. This avoids dead-code shims and keeps the DI container clean.

## In-process API local URL — IServer at startup

Use `IServer.Features.Get<IServerAddressesFeature>()` after the app starts to resolve the actual listen address for the in-process API source. This is the most accurate approach and avoids config/runtime divergence. Fallback to config-based address if IServer is unavailable.

## In-process URL resolution — IServer with hosted service

Use a `IHostedService` (or `ApplicationStarted` callback) to resolve the actual listen address via `IServer.Features.Get<IServerAddressesFeature>()` after the app starts. The `SourceRegistry` is populated with a placeholder local source at DI time; the hosted service updates it to the real address once `IServer` is available. This matches the original CONTEXT-1.md decision and handles dynamic port scenarios.

## Client caching — ConcurrentDictionary

`MultiSourceDashboardApiClient.GetClientForSource(slug)` caches `DashboardApiClient` instances in a `ConcurrentDictionary<string, IDashboardApiClient>`. `DashboardApiClient` is stateless, so caching is safe and avoids repeated object creation. Each cached instance wraps an `HttpClient` from `IHttpClientFactory.CreateClient(slug)`.

## Scope from ROADMAP.md (unchanged)

1. `DashboardApiSourceConfig` — Name, BaseUrl, ApiKey properties
2. `ISourceRegistry` / `SourceRegistry` — GetAll(), GetBySlug(), GetByName()
3. `IMultiSourceDashboardApiClient` / `MultiSourceDashboardApiClient` — GetClientForSource(slug)
4. Startup config validation — old flat format detection with migration instructions
5. In-process API auto-registration as "Local" source
6. DI registration refactor — named HttpClients per source
7. Unit tests in new DotNetWorkQueue.Dashboard.Ui.Tests project
