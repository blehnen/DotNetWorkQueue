# Plan 1.1: IConfiguration Overload + Transport Switch Tests

## Context

`DashboardExtensions.AddDotNetWorkQueueDashboard(IServiceCollection, IConfiguration)` currently has **0% line coverage** despite being the production code path used by `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs:45`. The private helper `AddConnectionByTransport` is also at 0% because it is only reachable through this overload.

This plan closes both gaps with unit tests that build an in-memory `IConfiguration` (no JSON files, no disk I/O), invoke the overload, and assert the DI state. Transitively, the transport-name switch (`SqlServer`, `PostgreSql`, `SQLite`, `LiteDb`, `Redis`) gets covered via parameterization.

Estimated coverage delta: **~50 lines** (the IConfiguration overload body + all 5 transport arms + error branches). This is the highest-ROI plan in Phase 5.

## Dependencies

None — runs in Wave 1, parallel with PLAN-1.2 and PLAN-1.3.

## Tasks

### Task 1: Happy-path Memory-transport test
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsFromConfigurationTests.cs` (NEW)
**Action:** create
**Description:**
Create a new test class `DashboardExtensionsFromConfigurationTests` in the same `DotNetWorkQueue.Dashboard.Api.Tests.Extensions` namespace as the existing `DashboardExtensionsTests`. Add the first test:

```csharp
[TestMethod]
public void AddDotNetWorkQueueDashboard_FromConfiguration_Memory_RegistersConnection()
{
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Dashboard:EnableSwagger"] = "false",
            ["Dashboard:Connections:0:Transport"] = "SQLite",
            ["Dashboard:Connections:0:ConnectionString"] = "Data Source=:memory:",
            ["Dashboard:Connections:0:DisplayName"] = "TestDb",
            ["Dashboard:Connections:0:Queues:0"] = "test-queue"
        })
        .Build();

    var services = new ServiceCollection();
    services.AddLogging();
    services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard"));

    var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<DashboardOptions>();
    options.EnableSwagger.Should().BeFalse();
    options.ConnectionRegistrations.Should().NotBeEmpty();
}
```

**Note on internal access:** `DashboardOptions.ConnectionRegistrations` is declared `internal` (in `DashboardOptions.cs:96`). This is accessible from the test project because `Source/DotNetWorkQueue.Dashboard.Api/InternalsVisibleForTests.cs` already grants `InternalsVisibleTo("DotNetWorkQueue.Dashboard.Api.Tests")`. No production change needed for visibility.

**Critical — IConfiguration namespace shadowing (CLAUDE.md lesson):**
- `using Microsoft.Extensions.Configuration;` at the top of the file is NOT sufficient because the test lives under `DotNetWorkQueue.Dashboard.Api.Tests.*` namespace, so `DotNetWorkQueue.IConfiguration` shadows via namespace walk-up
- **Use `global::Microsoft.Extensions.Configuration.IConfiguration`** explicitly anywhere the type is referenced by name. For the test above, the `ConfigurationBuilder` local is safe because the C# compiler infers the `IConfigurationRoot` return type; but if you add a helper method parameter typed as `IConfiguration`, use the fully-qualified type.
- Verify the using directive for the configuration section getter: `ConfigurationExtensions.GetSection(…)` is in `Microsoft.Extensions.Configuration`

**Use `SQLite` as the happy-path transport**, not `Memory`. The `AddConnectionByTransport` switch has no `Memory` case — only `SqlServer`, `PostgreSql`, `SQLite`, `LiteDb`, `Redis`. SQLite with `:memory:` gives a valid-looking connection string without needing a real database.

**Acceptance Criteria:**
- Test file compiles with no warnings-as-errors
- Test passes: `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" --filter "FullyQualifiedName~AddDotNetWorkQueueDashboard_FromConfiguration_Memory_RegistersConnection"`
- No use of `using DotNetWorkQueue;` that would import the shadowing `IConfiguration`
- Test does NOT call `BuildServiceProvider()` in a way that triggers actual transport connections (we're only asserting DI registration)

### Task 2: Parameterized transport-switch test (all 5 arms + default error)
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsFromConfigurationTests.cs` (MODIFY — add to the new class)
**Action:** modify
**Description:**
Add a parameterized test that iterates all 5 valid transport names and one invalid name to exercise every arm of the `AddConnectionByTransport` switch statement.

Use MSTest `[DataRow]` attributes (the existing tests don't appear to use `[DynamicData]`, so `[DataRow]` is the simpler choice). The default case of the switch throws `ArgumentException`, so the invalid-transport row expects a throw.

```csharp
[DataTestMethod]
[DataRow("SqlServer", "Server=localhost;Database=Test;Integrated Security=true")]
[DataRow("PostgreSql", "Host=localhost;Database=test;Username=test")]
[DataRow("SQLite", "Data Source=:memory:")]
[DataRow("LiteDb", "Filename=:memory:")]
[DataRow("Redis", "localhost:6379")]
public void AddDotNetWorkQueueDashboard_FromConfiguration_AllTransports_RegisterCleanly(
    string transport, string connectionString)
{
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Dashboard:EnableSwagger"] = "false",
            ["Dashboard:Connections:0:Transport"] = transport,
            ["Dashboard:Connections:0:ConnectionString"] = connectionString
        })
        .Build();

    var services = new ServiceCollection();
    services.AddLogging();

    // Must not throw for any valid transport name
    services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard"));

    var provider = services.BuildServiceProvider();
    var options = provider.GetRequiredService<DashboardOptions>();
    options.ConnectionRegistrations.Should().NotBeEmpty();
}

[TestMethod]
public void AddDotNetWorkQueueDashboard_FromConfiguration_UnknownTransport_Throws()
{
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Dashboard:Connections:0:Transport"] = "MongoDB",
            ["Dashboard:Connections:0:ConnectionString"] = "mongodb://localhost"
        })
        .Build();

    var services = new ServiceCollection();
    services.AddLogging();

    Assert.ThrowsExactly<ArgumentException>(() =>
        services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard")));
}
```

**Note on test values:**
- Connection strings are throwaway — the `AddConnectionByTransport` switch only stores them in `DashboardOptions.Connections[]`, it never opens them. The tests must not attempt to resolve a consumer/producer that would try to connect.
- Use `Assert.ThrowsExactly<ArgumentException>` per CLAUDE.md MSTest 3.x lesson (NOT `Assert.ThrowsException`).

**Acceptance Criteria:**
- All 5 `[DataRow]` cases pass
- The invalid-transport test passes (assertion catches `ArgumentException`)
- Test method count increases by 6 (5 DataRow iterations as MSTest counts them as 1 parameterized method + 1 separate error method = 2 methods shown in test output, but 6 test cases)

### Task 3: IConfiguration missing-Transport / missing-ConnectionString error tests
**Files:** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Extensions/DashboardExtensionsFromConfigurationTests.cs` (MODIFY — add to the new class)
**Action:** modify
**Description:**
The `IConfiguration` overload has two defensive guards at lines 178–181:
```csharp
if (string.IsNullOrEmpty(transport))
    throw new ArgumentException("Each Dashboard connection must specify a Transport.");
if (string.IsNullOrEmpty(connectionString))
    throw new ArgumentException($"Dashboard connection '{displayName}' must specify a ConnectionString.");
```

Add two tests, one for each branch:

```csharp
[TestMethod]
public void AddDotNetWorkQueueDashboard_FromConfiguration_MissingTransport_Throws()
{
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            // Transport missing
            ["Dashboard:Connections:0:ConnectionString"] = "Data Source=:memory:"
        })
        .Build();

    var services = new ServiceCollection();
    services.AddLogging();

    var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard")));

    StringAssert.Contains(ex.Message, "Transport");
}

[TestMethod]
public void AddDotNetWorkQueueDashboard_FromConfiguration_MissingConnectionString_Throws()
{
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Dashboard:Connections:0:Transport"] = "SQLite",
            ["Dashboard:Connections:0:DisplayName"] = "Broken"
            // ConnectionString missing
        })
        .Build();

    var services = new ServiceCollection();
    services.AddLogging();

    var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard")));

    StringAssert.Contains(ex.Message, "ConnectionString");
}
```

Use `StringAssert.Contains` (MSTest built-in) or FluentAssertions `ex.Message.Should().Contain("Transport")`. Match the pattern already used in `DashboardExtensionsTests.cs` which prefers FluentAssertions for readability.

**Acceptance Criteria:**
- Both tests pass
- Error messages actually contain the words "Transport" and "ConnectionString" respectively (verifying we hit the right branch, not just any `ArgumentException`)

## Verification

Run the new test file:
```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" -c Debug --filter "FullyQualifiedName~DashboardExtensionsFromConfigurationTests"
```

Expected: all tests pass, 8 total (1 happy-path + 5 parameterized transport rows + 1 unknown-transport + 2 error-branch = 8 test case results).

Also confirm no regressions:
```bash
dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" -c Debug
```

Expected: all existing `Dashboard.Api.Tests` pass (baseline before plan was green).

## Coverage Target

This plan is expected to take `DashboardExtensions` from 33.3% to approximately **60%** line coverage on its own (covers ~50 lines: the full IConfiguration overload body + the transport switch body + the 2 error-branch paths). Cluster D + cluster D-err + cluster E from RESEARCH.md section 6.
