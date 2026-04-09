---
phase: multi-source-config
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - DashboardApiSourceConfig POCO with Name, BaseUrl, ApiKey, and deterministic Slug property
  - ISourceRegistry interface with GetAll(), GetBySlug(), GetByName()
  - SourceRegistry implementation validating no duplicate names/slugs and at least one source
  - Test project scaffold added to solution and CI
  - Unit tests for slug generation and source registry
files_touched:
  - Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj (new)
  - Source/DotNetWorkQueue.sln (modify - add test project)
  - Source/DotNetWorkQueueNoTests.sln (no change - tests excluded)
  - .github/workflows/ci.yml (modify - add test step)
  - Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardApiSourceConfig.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui/Services/ISourceRegistry.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui/Services/SourceRegistry.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/DashboardApiSourceConfigTests.cs (new)
  - Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/SourceRegistryTests.cs (new)
tdd: true
risk: medium
---

# Plan 1.1: Test Project Scaffold, Config Model, and Source Registry

## Context

This plan creates the foundational types for multi-source configuration: the config POCO with slug derivation, and the source registry that holds and validates all configured sources. These are pure types with no dependency on DI, HttpClient, or Program.cs. The test project is scaffolded first so tests can be written alongside implementation (TDD).

This is Wave 1 -- no dependencies on other plans. All subsequent plans depend on these types.

## Dependencies

None -- this is the foundation plan.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj, Source/DotNetWorkQueue.sln, .github/workflows/ci.yml" tdd="false">
  <action>
  Create the test project scaffold and integrate it into the build.

  1. Create `Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj` mirroring `Dashboard.Api.Tests.csproj`:
     - `Microsoft.NET.Sdk` (NOT `Microsoft.NET.Sdk.Web`)
     - TargetFrameworks: `net10.0;net8.0`
     - PropertyGroups for Debug/Release per TFM matching Dashboard.Api.Tests pattern (DefineConstants, NoWarn)
     - PackageReferences (all using central package management -- no Version attributes):
       - `AutoFixture`
       - `AutoFixture.AutoNSubstitute`
       - `FluentAssertions`
       - `Microsoft.NET.Test.Sdk`
       - `coverlet.collector`
       - `MSTest.TestFramework`
       - `MSTest.TestAdapter`
       - `NSubstitute`
     - ProjectReference to `../DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj`

  2. Add the project to `Source/DotNetWorkQueue.sln` using `dotnet sln Source/DotNetWorkQueue.sln add Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj`.

  3. Create directory `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/` (empty, for test files in subsequent tasks).

  4. Add a CI step in `.github/workflows/ci.yml` after the "Unit Tests - Dashboard.Client" step:
     ```yaml
     - name: Unit Tests - Dashboard.Ui
       run: dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" --no-build -c Debug
     ```
  </action>
  <verify>dotnet build "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" -c Debug</verify>
  <done>Test project builds successfully. `dotnet sln Source/DotNetWorkQueue.sln list` shows `DotNetWorkQueue.Dashboard.Ui.Tests`. CI yml contains the new test step.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardApiSourceConfig.cs, Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/DashboardApiSourceConfigTests.cs" tdd="true">
  <action>
  Create the config model with slug derivation. Write tests first.

  **Tests first** -- create `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/DashboardApiSourceConfigTests.cs`:
  - `Slug_From_Simple_Name` -- Name="Local" produces Slug="local"
  - `Slug_From_Name_With_Spaces` -- Name="Production SQL Server" produces Slug="production-sql-server"
  - `Slug_From_Name_With_Special_Chars` -- Name="My Server (US-East)" produces Slug="my-server-us-east"
  - `Slug_Collapses_Consecutive_Hyphens` -- Name="test--name" produces Slug="test-name"
  - `Slug_Trims_Leading_Trailing_Hyphens` -- Name=" -Test- " produces Slug="test"
  - `Slug_From_Name_With_Numbers` -- Name="Server 42" produces Slug="server-42"
  - `Name_Set_Get` -- verifies Name property round-trips
  - `BaseUrl_Set_Get` -- verifies BaseUrl property round-trips
  - `ApiKey_Set_Get` -- verifies ApiKey property round-trips (nullable)
  - `ApiKey_Defaults_To_Null` -- verifies ApiKey is null by default

  Use MSTest `[TestClass]`/`[TestMethod]` attributes. Use FluentAssertions for assertions.

  **Implementation** -- create `Source/DotNetWorkQueue.Dashboard.Ui/Services/DashboardApiSourceConfig.cs`:
  - Namespace: `DotNetWorkQueue.Dashboard.Ui.Services`
  - LGPL-2.1 license header (copy from DashboardAuthConfig.cs)
  - Public class `DashboardApiSourceConfig`
  - Properties:
    - `public string Name { get; set; } = string.Empty;`
    - `public string BaseUrl { get; set; } = string.Empty;`
    - `public string? ApiKey { get; set; }`
  - Read-only computed property `Slug`:
    - `public string Slug => Slugify(Name);`
  - Private static method `Slugify(string name)`:
    - Trim whitespace
    - Convert to lowercase (invariant)
    - Replace any character that is NOT a-z, 0-9, or hyphen with a hyphen
    - Collapse consecutive hyphens into a single hyphen
    - Trim leading/trailing hyphens
    - Use `System.Text.RegularExpressions.Regex` for the replacements
  - XML doc comments on class and all public members (required for Release build)
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" --filter "FullyQualifiedName~DashboardApiSourceConfigTests" -c Debug</verify>
  <done>All DashboardApiSourceConfigTests pass. Slug generation handles spaces, special characters, consecutive hyphens, leading/trailing hyphens, and simple names correctly.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Dashboard.Ui/Services/ISourceRegistry.cs, Source/DotNetWorkQueue.Dashboard.Ui/Services/SourceRegistry.cs, Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/SourceRegistryTests.cs" tdd="true">
  <action>
  Create the source registry interface and implementation. Write tests first.

  **Tests first** -- create `Source/DotNetWorkQueue.Dashboard.Ui.Tests/Services/SourceRegistryTests.cs`:
  - `GetAll_Returns_All_Sources` -- construct with 2 sources, GetAll() returns both
  - `GetBySlug_Returns_Correct_Source` -- construct with 2 sources, GetBySlug("local") returns the Local source
  - `GetBySlug_Returns_Null_For_Unknown` -- GetBySlug("nonexistent") returns null
  - `GetByName_Returns_Correct_Source` -- GetByName("Local") returns the Local source
  - `GetByName_Returns_Null_For_Unknown` -- GetByName("nonexistent") returns null
  - `Constructor_Throws_On_Empty_Sources` -- empty list throws ArgumentException
  - `Constructor_Throws_On_Null_Sources` -- null throws ArgumentNullException
  - `Constructor_Throws_On_Duplicate_Names` -- two sources with same Name throws ArgumentException with message containing "duplicate" and the name
  - `Constructor_Throws_On_Duplicate_Slugs` -- two sources whose Names produce the same slug (e.g., "My Server" and "my--server") throws ArgumentException with message containing "slug"
  - `GetAll_Returns_ReadOnly_Collection` -- returned collection cannot be modified (IReadOnlyList)
  - `GetByName_Is_Case_Insensitive` -- GetByName("LOCAL") returns source named "Local"

  Use MSTest attributes. Use FluentAssertions. Construct `SourceRegistry` directly with `IReadOnlyList<DashboardApiSourceConfig>`.

  **Interface** -- create `Source/DotNetWorkQueue.Dashboard.Ui/Services/ISourceRegistry.cs`:
  - Namespace: `DotNetWorkQueue.Dashboard.Ui.Services`
  - LGPL-2.1 license header
  - ```csharp
    public interface ISourceRegistry
    {
        IReadOnlyList<DashboardApiSourceConfig> GetAll();
        DashboardApiSourceConfig? GetBySlug(string slug);
        DashboardApiSourceConfig? GetByName(string name);
    }
    ```
  - XML doc comments on interface and all methods

  **Implementation** -- create `Source/DotNetWorkQueue.Dashboard.Ui/Services/SourceRegistry.cs`:
  - Namespace: `DotNetWorkQueue.Dashboard.Ui.Services`
  - LGPL-2.1 license header
  - Public class `SourceRegistry : ISourceRegistry`
  - Constructor takes `IReadOnlyList<DashboardApiSourceConfig> sources`
  - Constructor validation:
    - Throw `ArgumentNullException` if sources is null
    - Throw `ArgumentException("At least one API source must be configured.")` if empty
    - Throw `ArgumentException` with descriptive message if duplicate Names found (case-insensitive comparison using `StringComparer.OrdinalIgnoreCase`)
    - Throw `ArgumentException` with descriptive message if duplicate Slugs found
  - Store sources in a private `IReadOnlyList<DashboardApiSourceConfig>` field
  - Build a `Dictionary<string, DashboardApiSourceConfig>` keyed by slug for O(1) lookup
  - Build a `Dictionary<string, DashboardApiSourceConfig>` keyed by name (case-insensitive, `StringComparer.OrdinalIgnoreCase`) for O(1) lookup
  - `GetAll()` returns the stored `IReadOnlyList`
  - `GetBySlug(string slug)` returns from slug dictionary or null
  - `GetByName(string name)` returns from name dictionary or null
  - XML doc comments on class and all public members
  </action>
  <verify>dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" --filter "FullyQualifiedName~SourceRegistryTests" -c Debug</verify>
  <done>All SourceRegistryTests pass. Registry validates duplicates, handles empty/null input, supports case-insensitive name lookup, and slug-based lookup returns correct sources.</done>
</task>

## Verification

After all three tasks complete:

```bash
# Full test project builds and all tests pass
dotnet test "Source/DotNetWorkQueue.Dashboard.Ui.Tests/DotNetWorkQueue.Dashboard.Ui.Tests.csproj" -c Debug

# Full solution still builds
dotnet build "Source/DotNetWorkQueue.sln" -c Debug

# Verify test project is in solution
dotnet sln "Source/DotNetWorkQueue.sln" list | grep Dashboard.Ui.Tests

# Verify CI yml has the new step
grep -A1 "Dashboard.Ui" .github/workflows/ci.yml
```
