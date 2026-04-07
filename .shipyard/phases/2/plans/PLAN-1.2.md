---
phase: drop-net48-netstandard20
plan: "1.2"
wave: 1
dependencies: ["Phase 1 complete"]
must_haves:
  - Remove #if NETFULL block from CompileExceptionTests.cs
  - Remove net48 from DotNetWorkQueue.Tests csproj
  - Remove net48 from 7 unit test csproj files
  - Remove net48 from 6 base integration test csproj files
files_touched:
  - Source/DotNetWorkQueue.Tests/Exceptions/CompileExceptionTests.cs
  - Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj
  - Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj
  - Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj
  - Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj
  - Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj
  - Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj
  - Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj
  - Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj
  - Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj
  - Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj
  - Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj
tdd: false
risk: low
---

# PLAN-1.2: DotNetWorkQueue.Tests + 13 Mechanical Test/Integration csproj Cleanup

## Context

This plan covers the simpler half of Phase 2: one .cs file change in `DotNetWorkQueue.Tests` (removing a `#if NETFULL` guarded test method) and 14 csproj files (including DotNetWorkQueue.Tests itself) that need the same mechanical edit: remove `net48` from `TargetFrameworks` and delete the conditional PropertyGroup/ItemGroup blocks.

No file overlap with PLAN-1.1 -- these two plans can execute in parallel.

## Risk: LOW

All csproj changes follow the exact same pattern. The single .cs change removes a test for `GetObjectData` which was itself removed in Phase 1.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Tests/Exceptions/CompileExceptionTests.cs, Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" tdd="false">
  <action>
  **A. Edit `CompileExceptionTests.cs`:**

  DELETE the entire `#if NETFULL` / `#endif` block (lines 36-47):
  ```csharp
  #if NETFULL
      [TestMethod]
      public void GetObjectData_Test()
      {
          var e = new CompileException("error", "code");
          var info = new SerializationInfo(typeof(CompileException), new FormatterConverter());
          e.GetObjectData(info, new StreamingContext());
          Assert.AreEqual("code", info.GetString("CompileCode"));
          Assert.AreEqual("error", e.Message);
          Assert.AreEqual("code", e.CompileCode);
      }
  #endif
  ```

  Also remove the now-unused `using System.Runtime.Serialization;` on line 2 (since `SerializationInfo`, `FormatterConverter`, and `StreamingContext` are no longer used in this file).

  **B. Edit `DotNetWorkQueue.Tests.csproj`:**

  1. Change `<TargetFrameworks>net10.0;net48</TargetFrameworks>` to `<TargetFrameworks>net10.0</TargetFrameworks>`

  2. DELETE the two conditional PropertyGroup blocks (lines 6-13):
     ```xml
     <PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Debug|net48|AnyCPU'">
       <DefineConstants>TRACE;DEBUG;CODE_ANALYSIS;NETFULL</DefineConstants>
       <NoWarn>...</NoWarn>
     </PropertyGroup>
     <PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Release|net48|AnyCPU'">
       <DefineConstants>NETFULL</DefineConstants>
       <NoWarn>...</NoWarn>
     </PropertyGroup>
     ```

  3. DELETE the conditional ItemGroup (lines 32-35):
     ```xml
     <ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">
       <Reference Include="Microsoft.CSharp" />
     </ItemGroup>
     ```
  </action>
  <verify>
grep -c "NETFULL\|NETSTANDARD2_0" Source/DotNetWorkQueue.Tests/Exceptions/CompileExceptionTests.cs && echo "FAIL: .cs" || echo "PASS: .cs"
grep -c "net48\|NETFULL" Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj && echo "FAIL: csproj" || echo "PASS: csproj"
  </verify>
  <done>`CompileExceptionTests.cs` has 3 test methods (no `GetObjectData_Test`), no `#if` directives, no `System.Runtime.Serialization` using. `DotNetWorkQueue.Tests.csproj` has `<TargetFrameworks>net10.0</TargetFrameworks>`, no conditional blocks.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj, Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj, Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj, Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj, Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj, Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj, Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" tdd="false">
  <action>
  Apply the same mechanical edit to all 7 unit test csproj files. Each file follows the identical pattern seen in SqlServer.Tests.csproj:

  1. Change `<TargetFrameworks>net10.0;net48</TargetFrameworks>` to `<TargetFrameworks>net10.0</TargetFrameworks>`

  2. DELETE the two conditional PropertyGroup blocks that define `NETFULL` for Debug|net48 and Release|net48 configurations.

  3. DELETE any `<ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">` blocks (if present -- these contain `<Reference Include="Microsoft.CSharp" />` or similar framework references).

  No .cs files need changes in these projects.
  </action>
  <verify>
for f in \
  "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" \
  "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" \
  "Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj" \
  "Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj" \
  "Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj" \
  "Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj" \
  "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj"; do
  grep -c "net48" "$f" > /dev/null 2>&1 && echo "FAIL: $f" || echo "PASS: $f"
done
  </verify>
  <done>All 7 unit test csproj files have `<TargetFrameworks>net10.0</TargetFrameworks>` only. No net48 conditional PropertyGroups or ItemGroups remain.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj, Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj, Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj, Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj, Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj, Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" tdd="false">
  <action>
  Apply the same mechanical edit to all 6 base integration test csproj files:

  1. Change `<TargetFrameworks>net10.0;net48</TargetFrameworks>` to `<TargetFrameworks>net10.0</TargetFrameworks>`

  2. DELETE the two conditional PropertyGroup blocks that define `NETFULL` for Debug|net48 and Release|net48 configurations.

  3. DELETE any `<ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">` blocks (these typically contain `<Reference Include="Microsoft.CSharp" />` and/or `<Reference Include="System.Net.Http" />`).

  No .cs files need changes in these projects.
  </action>
  <verify>
for f in \
  "Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" \
  "Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" \
  "Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" \
  "Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj" \
  "Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj" \
  "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj"; do
  grep -c "net48" "$f" > /dev/null 2>&1 && echo "FAIL: $f" || echo "PASS: $f"
done
  </verify>
  <done>All 6 base integration test csproj files have `<TargetFrameworks>net10.0</TargetFrameworks>` only. No net48 conditional PropertyGroups or ItemGroups remain.</done>
</task>

## Phase 2 Final Verification (after both PLAN-1.1 and PLAN-1.2 complete)

Run these commands to confirm the full phase success criteria:

```bash
dotnet build "Source/DotNetWorkQueue.sln" -c Debug
dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj"
grep -r "NETFULL\|NETSTANDARD2_0" Source/DotNetWorkQueue.IntegrationTests.Shared/ Source/DotNetWorkQueue.Tests/ --include="*.cs" --include="*.csproj"
grep -r "net48" Source/DotNetWorkQueue.Tests/ Source/DotNetWorkQueue.IntegrationTests.Shared/ --include="*.csproj"
```
