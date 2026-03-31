# Plan 1.1: Central Package Management (H-6)

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Centralize all NuGet package versions into a single `Directory.Packages.props` file and remove `Version=` attributes from all 36 `.csproj` files under `Source/`.
**Architecture:** MSBuild Central Package Management (CPM) uses a `Directory.Packages.props` file to declare all package versions centrally, and a `Directory.Build.props` to enable the feature. Each `.csproj` keeps its `<PackageReference>` elements but without `Version=` attributes. The conditional `Microsoft.AspNetCore.TestHost` (8.0.13 for net8.0, 10.0.3 for net10.0) requires TFM-conditional `<PackageVersion>` entries.
**Tech Stack:** MSBuild, NuGet Central Package Management

## Dependencies
None (Wave 1 -- foundation task)

## Tasks

### Task 1: Create Directory.Build.props and Directory.Packages.props
**Files:**
- Create: `Source/Directory.Build.props`
- Create: `Source/Directory.Packages.props`

**Steps:**

1. Create `Source/Directory.Build.props` with this exact content:
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```

2. Create `Source/Directory.Packages.props` with all 34 unique packages. Use the exact versions from the current `.csproj` files. The `Microsoft.AspNetCore.TestHost` entry needs TFM-conditional handling:

```xml
<Project>
  <ItemGroup>
    <!-- Core -->
    <PackageVersion Include="GuerrillaNtp" Version="3.1.0" />
    <PackageVersion Include="Microsoft.CSharp" Version="4.7.0" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.4" />
    <PackageVersion Include="OpenTelemetry" Version="1.14.0" />
    <PackageVersion Include="Polly.Core" Version="8.6.5" />
    <PackageVersion Include="SimpleInjector" Version="5.5.0" />
    <PackageVersion Include="System.Diagnostics.DiagnosticSource" Version="10.0.1" />

    <!-- Dashboard -->
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="7.2.0" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="9.0.3" />
    <PackageVersion Include="MudBlazor" Version="9.1.0" />

    <!-- Transport: SQL Server -->
    <PackageVersion Include="Microsoft.Data.SqlClient" Version="6.1.3" />

    <!-- Transport: PostgreSQL -->
    <PackageVersion Include="Npgsql" Version="8.0.8" />

    <!-- Transport: SQLite -->
    <PackageVersion Include="System.Data.SQLite.Core" Version="1.0.119" />

    <!-- Transport: Redis -->
    <PackageVersion Include="Microsoft.Extensions.Caching.Memory" Version="9.0.3" />
    <PackageVersion Include="Microsoft.IO.RecyclableMemoryStream" Version="3.0.1" />
    <PackageVersion Include="MsgPack.Cli" Version="1.0.1" />
    <PackageVersion Include="StackExchange.Redis" Version="2.10.1" />

    <!-- Transport: LiteDB -->
    <PackageVersion Include="LiteDB" Version="5.0.21" />

    <!-- Test Infrastructure -->
    <PackageVersion Include="AutoFixture" Version="4.18.1" />
    <PackageVersion Include="AutoFixture.AutoNSubstitute" Version="4.18.1" />
    <PackageVersion Include="CompareNETObjects" Version="4.84.0" />
    <PackageVersion Include="FluentAssertions" Version="6.12.2" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageVersion Include="MSTest.TestAdapter" Version="4.1.0" />
    <PackageVersion Include="MSTest.TestFramework" Version="4.1.0" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.14.0" />
    <PackageVersion Include="Tynamix.ObjectFiller" Version="1.5.9" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
  </ItemGroup>

  <!-- Microsoft.AspNetCore.TestHost: TFM-conditional versions -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageVersion Include="Microsoft.AspNetCore.TestHost" Version="8.0.13" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <PackageVersion Include="Microsoft.AspNetCore.TestHost" Version="10.0.3" />
  </ItemGroup>
</Project>
```

**Verify:** `test -f Source/Directory.Build.props && test -f Source/Directory.Packages.props && echo "PASS" || echo "FAIL"`

### Task 2: Strip Version= from all PackageReference elements in all .csproj files
**Files:**
- Modify: All 36 `.csproj` files under `Source/` (see full list below)

**Steps:**

For every `.csproj` file under `Source/`, find all `<PackageReference Include="..." Version="..." />` elements and remove the `Version="..."` attribute. The `Include="..."` attribute must remain.

Examples of the transformation:
```xml
<!-- Before -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
<!-- After -->
<PackageReference Include="Swashbuckle.AspNetCore" />

<!-- Before -->
<PackageReference Include="FluentAssertions" Version="6.12.2" />
<!-- After -->
<PackageReference Include="FluentAssertions" />
```

For TFM-conditional PackageReferences (in `DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj`), the `Condition` attribute stays but `Version` is removed:
```xml
<!-- Before -->
<PackageReference Include="Microsoft.AspNetCore.TestHost" Version="8.0.13" Condition="'$(TargetFramework)' == 'net8.0'" />
<!-- After -->
<PackageReference Include="Microsoft.AspNetCore.TestHost" Condition="'$(TargetFramework)' == 'net8.0'" />
```

**Full list of .csproj files (36 total):**
- `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`
- `Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj`
- `Source/DotNetWorkQueue.Dashboard.Client/DotNetWorkQueue.Dashboard.Client.csproj`
- `Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj`
- `Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDb.csproj`
- `Source/DotNetWorkQueue.Transport.Memory/DotNetWorkQueue.Transport.Memory.csproj`
- `Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj`
- `Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj`
- `Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj`
- `Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj`
- `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj`
- `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj`
- `Source/DotNetWorkQueue.Dashboard.Client.Tests/DotNetWorkQueue.Dashboard.Client.Tests.csproj`
- `Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj`
- `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj`
- `Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj`
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj`

**Verify:** `grep -r 'PackageReference.*Version=' Source/ --include="*.csproj" | wc -l` should return 0

### Task 3: Validate full solution build
**Files:** None (verification only)

**Steps:**

1. Run a full solution build to confirm all packages resolve correctly:
```bash
dotnet build "Source/DotNetWorkQueue.sln" -c Debug
```

2. If the build fails with package version errors, check that every package in every `.csproj` is listed in `Directory.Packages.props` with the correct version. Common failure modes:
   - Package missing from `Directory.Packages.props` entirely
   - Version mismatch (copy the exact version from the pre-CPM `.csproj`)
   - TFM-conditional packages not handled with conditional `<PackageVersion>` entries

3. Run unit tests for the core project to confirm no regression:
```bash
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --no-build
```

**Verify:**
```bash
dotnet build "Source/DotNetWorkQueue.sln" -c Debug && echo "BUILD PASS" || echo "BUILD FAIL"
```

## Verification

Run all of these in sequence:
```bash
# 1. Files exist
test -f Source/Directory.Build.props && test -f Source/Directory.Packages.props && echo "Files exist: PASS"

# 2. CPM is enabled
grep -q "ManagePackageVersionsCentrally" Source/Directory.Build.props && echo "CPM enabled: PASS"

# 3. No Version= on PackageReference in any .csproj
count=$(grep -r 'PackageReference.*Version=' Source/ --include="*.csproj" | wc -l)
[ "$count" -eq 0 ] && echo "No Version attrs: PASS ($count)" || echo "FAIL: $count PackageReference still have Version="

# 4. Full solution build
dotnet build "Source/DotNetWorkQueue.sln" -c Debug

# 5. Unit tests
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --no-build
```
