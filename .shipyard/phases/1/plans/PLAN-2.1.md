---
phase: multi-source-config
plan: "2.1"
wave: 2
dependencies: ["1.1"]
must_haves:
  - IMultiSourceDashboardApiClient interface with GetClientForSource(slug) and GetAllSources()
  - MultiSourceDashboardApiClient implementation using ConcurrentDictionary cache and IHttpClientFactory
  - LocalSourceHostedService resolving actual listen address via IServer after startup
  - Program.cs refactored to parse Sources[] config, register named HttpClients, validate old format
  - Old single-source IDashboardApiClient DI registration removed
  - Unit tests for multi-source client and config validation
files_touched:
  - Source/DotNetWorkQueue.Dashboard.Ui/Services/IMultiSourceDashboardApiClient.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui/Services/MultiSourceDashboardApiClient.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui/Services/LocalSourceHostedService.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui/Program.cs (modify)
  - Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/MultiSourceDashboardApiClientTests.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/ConfigValidationTests.cs (new)
tdd: true
risk: high
---

# Plan 2.1: Multi-Source Client, In-Process Registration, and DI Refactor

## Context

This plan implements the multi-source client wrapper that returns per-source `IDashboardApiClient` instances, the hosted service for in-process API URL resolution, and the Program.cs refactor that replaces the single-source DI registration with multi-source config parsing, validation, and named HttpClient registration.

This is the highest-risk plan in Phase 1 because it modifies Program.cs -- the DI composition root. Getting the HttpClient registration wrong breaks all API communication. The old `IDashboardApiClient` DI registration is removed entirely (per CONTEXT-1.md design decision), which will break the 3 page files that `@inject IDashboardApiClient` -- Phase 2 fixes those pages.

## Dependencies

- **PLAN-1.1** (Wave 1): Requires `DashboardApiSourceConfig`, `ISourceRegistry`, and `SourceRegistry` to exist.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Dashboard.Ui/Services/IMultiSourceDashboardApiClient.cs, Source/DotNetWorkQueue.Dashboard.Ui/Services/MultiSourceDashboardApiClient.cs, Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/MultiSourceDashboardApiClientTests.cs" tdd="true">
  <action>
  Create the multi-source client interface, implementation, and tests. Write tests first.

  **Tests first** -- create `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/MultiSourceDashboardApiClientTests.cs`:
  - `GetClientForSource_Returns_Client_For_Valid_Slug` -- register 2 sources, GetClientForSource("local") returns non-null IDashboardApiClient
  - `GetClientForSource_Returns_Same_Instance_On_Second_Call` -- call twice with same slug, verify same reference (caching)
  - `GetClientForSource_Returns_Different_Clients_For_Different_Slugs` -- two slugs return different IDashboardApiClient instances
  - `GetClientForSource_Throws_KeyNotFoundException_For_Unknown_Slug` -- unknown slug throws KeyNotFoundException with message containing the slug
  - `GetClientForSource_Throws_ArgumentNullException_For_Null_Slug` -- null slug throws ArgumentNullException
  - `GetAllSources_Returns_All_Registry_Sources` -- delegates to ISourceRegistry.GetAll()

  For testing, use NSubstitute to mock `IHttpClientFactory` (return a new `HttpClient` from `CreateClient(name)`), and create a real `SourceRegistry` with test `DashboardApiSourceConfig` instances. Verify `IHttpClientFactory.CreateClient` is called with the correct slug string.

  **Interface** -- create `Source/DotNetWorkQueue.Dashboard.Ui/Services/IMultiSourceDashboardApiClient.cs`:
  - Namespace: `DotNetWorkQueue.Dashboard.Ui.Services`
  - LGPL-2.1 license header
  - ```csharp
    public interface IMultiSourceDashboardApiClient
    {
        IDashboardApiClient GetClientForSource(string slug);
        IReadOnlyList<DashboardApiSourceConfig> GetAllSources();
    }
    ```
  - XML doc comments on interface and all methods

  **Implementation** -- create `Source/DotNetWorkQueue.Dashboard.Ui/Services/MultiSourceDashboardApiClient.cs`:
  - Namespace: `DotNetWorkQueue.Dashboard.Ui.Services`
  - LGPL-2.1 license header
  - Public class `MultiSourceDashboardApiClient : IMultiSourceDashboardApiClient`
  - Constructor takes `ISourceRegistry sourceRegistry` and `IHttpClientFactory httpClientFactory`
  - Null-guard both constructor parameters (throw `ArgumentNullException`)
  - Private fields:
    - `readonly ISourceRegistry _sourceRegistry`
    - `readonly IHttpClientFactory _httpClientFactory`
    - `readonly ConcurrentDictionary<string, IDashboardApiClient> _clients = new()`
  - `GetClientForSource(string slug)`:
    - Throw `ArgumentNullException` if slug is null
    - Use `_clients.GetOrAdd(slug, CreateClient)` for thread-safe lazy creation
    - Private method `CreateClient(string slug)`:
      - Call `_sourceRegistry.GetBySlug(slug)` -- if null, throw `KeyNotFoundException($"No API source configured with slug '{slug}'")`
      - Call `_httpClientFactory.CreateClient(slug)` to get a named HttpClient
      - Return `new DashboardApiClient(httpClient)`
  - `GetAllSources()`: delegates to `_sourceRegistry.GetAll()`
  - XML doc comments on class and all public members
  - Use `System.Collections.Concurrent` for `ConcurrentDictionary`
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" --filter "FullyQualifiedName~MultiSourceDashboardApiClientTests" -c Debug</verify>
  <done>All MultiSourceDashboardApiClientTests pass. Client caches instances per slug, returns different clients for different slugs, throws KeyNotFoundException for unknown slugs.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Dashboard.Ui/Services/LocalSourceHostedService.cs, Source/DotNetWorkQueue.Dashboard.Ui/Program.cs, Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/ConfigValidationTests.cs" tdd="true">
  <action>
  Create the in-process source hosted service, refactor Program.cs DI registration, and add config validation tests.

  **Tests first** -- create `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/ConfigValidationTests.cs`:

  These tests verify the config-parsing logic that will live in a static helper method (not inline in Program.cs) so it can be unit tested. Create a static class `DashboardConfigParser` in `Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardConfigParser.cs` to hold the parsing logic.

  Tests (using `Microsoft.Extensions.Configuration.ConfigurationBuilder` to build in-memory config):
  - `ParseSources_Returns_Sources_From_Config` -- config with `DashboardApi:Sources:0:Name=Local`, `DashboardApi:Sources:0:BaseUrl=http://localhost:5000` returns 1 source
  - `ParseSources_Returns_Multiple_Sources` -- 2 sources configured, returns 2
  - `ParseSources_With_ApiKey` -- source with ApiKey populates the property
  - `ParseSources_Without_ApiKey` -- source without ApiKey leaves it null
  - `ValidateNoLegacyConfig_Throws_On_Old_Format` -- config with `DashboardApi:BaseUrl=http://x` but no `DashboardApi:Sources` section throws `InvalidOperationException` with message containing "Sources" and "migration"
  - `ValidateNoLegacyConfig_Does_Not_Throw_When_Sources_Present` -- config with both `DashboardApi:BaseUrl` and `DashboardApi:Sources` does NOT throw (Sources takes precedence)
  - `ValidateNoLegacyConfig_Does_Not_Throw_When_Neither_Present` -- empty config does not throw

  **DashboardConfigParser** -- create `Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardConfigParser.cs`:
  - Namespace: `DotNetWorkQueue.Dashboard.Ui.Services`
  - LGPL-2.1 license header
  - Public static class `DashboardConfigParser`
  - `public static List<DashboardApiSourceConfig> ParseSources(IConfiguration configuration)`:
    - Bind `DashboardApi:Sources` section to `List<DashboardApiSourceConfig>`
    - Return the list (may be empty -- caller decides whether to error)
  - `public static void ValidateNoLegacyConfig(IConfiguration configuration)`:
    - Check if `DashboardApi:BaseUrl` exists AND `DashboardApi:Sources` section has no children
    - If so, throw `InvalidOperationException` with a message like:
      ```
      Legacy single-source configuration detected. The flat 'DashboardApi:BaseUrl' / 'DashboardApi:ApiKey' format is no longer supported.

      Migrate to the new multi-source format:

      "DashboardApi": {
        "Sources": [
          {
            "Name": "Local",
            "BaseUrl": "http://localhost:5000",
            "ApiKey": "your-key-here"
          }
        ]
      }

      Remove the 'DashboardApi:BaseUrl' and 'DashboardApi:ApiKey' keys after migrating.
      ```
  - XML doc comments

  **LocalSourceHostedService** -- create `Source/DotNetWorkQueue.Dashboard.Ui/Services/LocalSourceHostedService.cs`:
  - Namespace: `DotNetWorkQueue.Dashboard.Ui.Services`
  - LGPL-2.1 license header
  - Public class `LocalSourceHostedService : IHostedService`
  - Constructor takes `IServer server`, `ISourceRegistry sourceRegistry`, `ILogger<LocalSourceHostedService> logger`
  - `StartAsync(CancellationToken)`:
    - Get `IServerAddressesFeature` from `server.Features`
    - If available and has addresses, get the first address
    - Find the "Local" source in registry via `GetByName("Local")` (or the configured local source name)
    - Update its `BaseUrl` property with the actual address
    - Log at Information level: "Local API source URL resolved to {address}"
    - If `IServerAddressesFeature` is unavailable or has no addresses, log a Warning and leave the placeholder URL unchanged
  - `StopAsync(CancellationToken)`: return `Task.CompletedTask`
  - XML doc comments

  **Program.cs refactor** -- modify `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs`:

  Replace lines 44-52 (the old `AddHttpClient<IDashboardApiClient, DashboardApiClient>` block) with:

  1. Call `DashboardConfigParser.ValidateNoLegacyConfig(builder.Configuration)` early (before any DI registration).

  2. Parse sources: `var sources = DashboardConfigParser.ParseSources(builder.Configuration);`

  3. If self-contained mode (`selfContained` is true) AND no source named "Local" already exists in the parsed list:
     - Create a `DashboardApiSourceConfig { Name = builder.Configuration["DashboardApi:LocalSourceName"] ?? "Local", BaseUrl = "http://localhost:5000" }`
     - Add it to the sources list
     - Register `LocalSourceHostedService` as a hosted service: `builder.Services.AddHostedService<LocalSourceHostedService>();`

  4. If sources list is empty after the above, add a default source: `new DashboardApiSourceConfig { Name = "Local", BaseUrl = "http://localhost:5000" }` (preserves current default behavior for unconfigured deployments).

  5. Create and register the source registry: `var registry = new SourceRegistry(sources); builder.Services.AddSingleton<ISourceRegistry>(registry);`

  6. Register named HttpClients per source:
     ```csharp
     foreach (var source in sources)
     {
         builder.Services.AddHttpClient(source.Slug, client =>
         {
             client.BaseAddress = new Uri(source.BaseUrl);
             if (!string.IsNullOrEmpty(source.ApiKey))
                 client.DefaultRequestHeaders.Add("X-Api-Key", source.ApiKey);
         });
     }
     ```

  7. Register the multi-source client: `builder.Services.AddSingleton<IMultiSourceDashboardApiClient, MultiSourceDashboardApiClient>();`

  8. **Remove** the old `builder.Services.AddHttpClient<IDashboardApiClient, DashboardApiClient>(...)` registration entirely. Do NOT add a backward-compat shim for `IDashboardApiClient`. (Pages will break -- Phase 2 fixes them.)

  9. Add `using Microsoft.AspNetCore.Hosting.Server;` and `using Microsoft.AspNetCore.Hosting.Server.Features;` to the top of Program.cs (needed for LocalSourceHostedService registration context, though the types are used in the hosted service file itself).

  Add `using DotNetWorkQueue.Dashboard.Ui.Services;` if not already present (it already is at line 24).
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" --filter "FullyQualifiedName~ConfigValidationTests" -c Debug</verify>
  <done>All ConfigValidationTests pass. Program.cs uses DashboardConfigParser for source parsing and legacy validation. Named HttpClients registered per source. Old IDashboardApiClient registration removed. LocalSourceHostedService created. Solution builds (pages may have @inject warnings -- expected, fixed in Phase 2).</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/LocalSourceHostedServiceTests.cs" tdd="true">
  <action>
  Create unit tests for LocalSourceHostedService.

  **Tests** -- create `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/LocalSourceHostedServiceTests.cs`:

  - `StartAsync_Updates_BaseUrl_When_ServerAddressesFeature_Available`:
    - Create a mock `IServer` (NSubstitute)
    - Create a mock `IServerAddressesFeature` that returns `new[] { "http://localhost:5123" }`
    - Set up `server.Features.Get<IServerAddressesFeature>()` to return the mock feature
    - Create a real `SourceRegistry` with one source: Name="Local", BaseUrl="http://localhost:5000"
    - Create `LocalSourceHostedService` with the mock server, registry, and a mock logger
    - Call `StartAsync(CancellationToken.None)`
    - Assert the Local source's `BaseUrl` is now `"http://localhost:5123"`

  - `StartAsync_Leaves_BaseUrl_When_No_ServerAddressesFeature`:
    - Mock `IServer` where `Features.Get<IServerAddressesFeature>()` returns null
    - Source starts with BaseUrl="http://localhost:5000"
    - After `StartAsync`, BaseUrl remains "http://localhost:5000"

  - `StartAsync_Leaves_BaseUrl_When_No_Addresses`:
    - Mock `IServerAddressesFeature` with empty addresses collection
    - After `StartAsync`, BaseUrl unchanged

  - `StartAsync_Logs_Warning_When_Cannot_Resolve`:
    - When IServerAddressesFeature is null, verify logger received a Warning-level log

  - `StopAsync_Returns_Completed_Task`:
    - `StopAsync(CancellationToken.None)` returns immediately without error

  Note: `IServer.Features` returns `IFeatureCollection`. Use NSubstitute to mock it. The `Get<T>()` method is an extension method on `IFeatureCollection` -- it delegates to indexer `this[typeof(T)]`. Mock the indexer: `featureCollection[typeof(IServerAddressesFeature)].Returns(mockFeature)`.

  Use MSTest attributes and FluentAssertions.
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" --filter "FullyQualifiedName~LocalSourceHostedServiceTests" -c Debug</verify>
  <done>All LocalSourceHostedServiceTests pass. Hosted service correctly updates BaseUrl from IServerAddressesFeature, handles missing feature gracefully, and logs warnings.</done>
</task>

## Verification

After all three tasks complete:

```bash
# All unit tests pass
dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" -c Debug

# Solution builds (expect Blazor page @inject warnings for IDashboardApiClient -- that is expected and fixed in Phase 2)
dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj" -c Debug

# Existing Dashboard API tests still pass (API layer untouched)
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" -c Debug
```

**Known expected breakage:** The 3 Blazor pages (`Home.razor`, `ConnectionDetail.razor`, `QueueDetail.razor`) use `@inject IDashboardApiClient Api`. Since the old DI registration for `IDashboardApiClient` is removed, these pages will fail at runtime (or possibly at build if Blazor compile-time DI checking catches it). This is intentional per the CONTEXT-1.md design decision -- Phase 2 rewrites these pages to use `IMultiSourceDashboardApiClient`.
