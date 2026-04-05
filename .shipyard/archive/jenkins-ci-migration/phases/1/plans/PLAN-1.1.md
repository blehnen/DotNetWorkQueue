# Multi-Target IntegrationTests.Shared + Core Unit Tests

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Multi-target IntegrationTests.Shared and 3 core unit test projects to `net10.0;net48`, fixing the unconditional Soap reference in DotNetWorkQueue.Tests.

**Architecture:** Each .csproj gets `net10.0` added to TargetFrameworks. No net10.0-specific PropertyGroups needed -- NETFULL is defined only via existing net48-conditional PropertyGroups, and net10.0 uses SDK defaults. The Soap framework reference must be moved into the existing net48-conditional ItemGroup.

**Tech Stack:** .NET SDK multi-targeting, MSBuild conditions

**Wave:** 1 (parallel with PLAN-1.2)

**Note:** AppMetrics.Tests does not exist (removed in a prior milestone). The roadmap's count of 22 is actually 21.

---

<task id="1" name="Multi-target IntegrationTests.Shared">
  <description>Change IntegrationTests.Shared from net48 to net10.0;net48. This is the critical dependency -- all 12 integration test projects reference this library. Also remove dead netstandard2.0 PropertyGroups (netstandard2.0 was never in TargetFrameworks).</description>
  <files>
    <modify>Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj</modify>
  </files>
  <steps>
    <step>Open the .csproj file</step>
    <step>Change `<TargetFrameworks>net48;</TargetFrameworks>` to `<TargetFrameworks>net10.0;net48</TargetFrameworks>`</step>
    <step>Remove the two netstandard2.0 PropertyGroups (Debug and Release) -- they are dead config since netstandard2.0 was never in TargetFrameworks</step>
    <step>Build the project to verify: `dotnet build "Source\DotNetWorkQueue.IntegrationTests.Shared\DotNetWorkQueue.IntegrationTests.Shared.csproj"`</step>
    <step>Commit: "ci: multi-target IntegrationTests.Shared to net10.0;net48"</step>
  </steps>
  <verification>
    <command>dotnet build "Source\DotNetWorkQueue.IntegrationTests.Shared\DotNetWorkQueue.IntegrationTests.Shared.csproj"</command>
    <expected>Build succeeded. 0 Error(s) for both net10.0 and net48 targets</expected>
  </verification>
</task>

### Task 1: Multi-target IntegrationTests.Shared

**Files:**
- Modify: `Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj`

**Step 1: Change TargetFrameworks**

Find:
```xml
<TargetFrameworks>net48;</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 2: Remove dead netstandard2.0 PropertyGroups**

Delete these two PropertyGroup blocks entirely (netstandard2.0 was never in TargetFrameworks, so these never activated):

```xml
  <PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Debug|netstandard2.0|AnyCPU'">
    <DefineConstants>TRACE;DEBUG;NETSTANDARD2_0;CODE_ANALYSIS;</DefineConstants>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Release|netstandard2.0|AnyCPU'">
    <DefineConstants>NETSTANDARD2_0;</DefineConstants>
  </PropertyGroup>
```

**Step 3: Build to verify**

Run: `dotnet build "Source\DotNetWorkQueue.IntegrationTests.Shared\DotNetWorkQueue.IntegrationTests.Shared.csproj"`

Expected: `Build succeeded. 0 Error(s)` -- builds for both net10.0 and net48.

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj
git commit -m "ci: multi-target IntegrationTests.Shared to net10.0;net48"
```

---

<task id="2" name="Fix Soap reference + multi-target DotNetWorkQueue.Tests">
  <description>Move the unconditional System.Runtime.Serialization.Formatters.Soap reference into the existing net48-conditional ItemGroup (it's a net48-only framework assembly). Then add net10.0 to TargetFrameworks. No .cs code uses SoapFormatter -- only the csproj reference exists.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj</modify>
  </files>
  <steps>
    <step>Open the .csproj file</step>
    <step>Remove the unconditional Soap ItemGroup entirely</step>
    <step>Add the Soap reference into the existing net48-conditional ItemGroup (alongside Microsoft.CSharp and System.Net.Http)</step>
    <step>Change TargetFrameworks from net48 to net10.0;net48</step>
    <step>Build the project: `dotnet build "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"`</step>
    <step>Run tests on net10.0: `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" -f net10.0 --no-build`</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" -f net10.0</command>
    <expected>All tests passed on net10.0</expected>
  </verification>
</task>

### Task 2: Fix Soap reference + multi-target DotNetWorkQueue.Tests

**Files:**
- Modify: `Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj`

**Step 1: Change TargetFrameworks**

Find:
```xml
<TargetFrameworks>net48;</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 2: Move Soap reference into net48-conditional ItemGroup**

Remove this unconditional ItemGroup:
```xml
  <ItemGroup>
    <Reference Include="System.Runtime.Serialization.Formatters.Soap" />
  </ItemGroup>
```

Add the Soap reference into the existing net48-conditional ItemGroup so it becomes:
```xml
  <ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">
    <Reference Include="Microsoft.CSharp" />
    <Reference Include="System.Net.Http" />
    <Reference Include="System.Runtime.Serialization.Formatters.Soap" />
  </ItemGroup>
```

**Step 3: Build to verify**

Run: `dotnet build "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"`

Expected: `Build succeeded. 0 Error(s)` -- builds for both net10.0 and net48.

**Step 4: Run tests on net10.0**

Run: `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj" -f net10.0`

Expected: All tests pass. Some tests guarded by `#if NETFULL` will be excluded on net10.0 -- this is correct.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj
git commit -m "ci: multi-target DotNetWorkQueue.Tests to net10.0;net48

Move System.Runtime.Serialization.Formatters.Soap into net48-conditional
ItemGroup -- this assembly does not exist on net10.0."
```

---

<task id="3" name="Multi-target RelationalDatabase.Tests + Memory.Tests">
  <description>Add net10.0 to TargetFrameworks for RelationalDatabase.Tests and Memory.Tests. Both are straightforward -- no unconditional framework references to fix.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj</modify>
  </files>
  <steps>
    <step>In RelationalDatabase.Tests.csproj, change `<TargetFrameworks>net48</TargetFrameworks>` to `<TargetFrameworks>net10.0;net48</TargetFrameworks>`</step>
    <step>In Memory.Tests.csproj, change `<TargetFrameworks>net48</TargetFrameworks>` to `<TargetFrameworks>net10.0;net48</TargetFrameworks>`</step>
    <step>Build both: `dotnet build "Source\DotNetWorkQueue.Transport.RelationalDatabase.Tests\DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj"` and `dotnet build "Source\DotNetWorkQueue.Transport.Memory.Tests\DotNetWorkQueue.Transport.Memory.Tests.csproj"`</step>
    <step>Run tests on net10.0 for both projects</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet test "Source\DotNetWorkQueue.Transport.RelationalDatabase.Tests\DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -f net10.0 &amp;&amp; dotnet test "Source\DotNetWorkQueue.Transport.Memory.Tests\DotNetWorkQueue.Transport.Memory.Tests.csproj" -f net10.0</command>
    <expected>All tests passed on net10.0 for both projects</expected>
  </verification>
</task>

### Task 3: Multi-target RelationalDatabase.Tests + Memory.Tests

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj`

**Step 1: Change TargetFrameworks in RelationalDatabase.Tests.csproj**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 2: Change TargetFrameworks in Memory.Tests.csproj**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 3: Build both**

Run: `dotnet build "Source\DotNetWorkQueue.Transport.RelationalDatabase.Tests\DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj"`
Run: `dotnet build "Source\DotNetWorkQueue.Transport.Memory.Tests\DotNetWorkQueue.Transport.Memory.Tests.csproj"`

Expected: Both build succeeded with 0 errors.

**Step 4: Run tests on net10.0**

Run: `dotnet test "Source\DotNetWorkQueue.Transport.RelationalDatabase.Tests\DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -f net10.0`
Run: `dotnet test "Source\DotNetWorkQueue.Transport.Memory.Tests\DotNetWorkQueue.Transport.Memory.Tests.csproj" -f net10.0`

Expected: All tests pass on net10.0.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj Source/DotNetWorkQueue.Transport.Memory.Tests/DotNetWorkQueue.Transport.Memory.Tests.csproj
git commit -m "ci: multi-target RelationalDatabase.Tests + Memory.Tests to net10.0;net48"
```
