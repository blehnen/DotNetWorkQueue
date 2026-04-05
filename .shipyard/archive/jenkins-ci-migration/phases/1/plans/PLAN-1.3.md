# Multi-Target Integration Test Projects

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Multi-target all 12 integration test projects to `net10.0;net48`, fixing 3 unconditional `Microsoft.CSharp` framework references.

**Architecture:** Same pattern as unit tests -- add `net10.0` to TargetFrameworks. Three projects (SQLite.Integration.Tests, SQLite.Linq.Integration.Tests, LiteDB.Linq.Integration.Tests) have `Microsoft.CSharp` in unconditional ItemGroups that must be conditioned to net48. All other projects already have their framework references properly conditioned.

**Tech Stack:** .NET SDK multi-targeting, MSBuild conditions

**Wave:** 2 (depends on PLAN-1.1 completing -- IntegrationTests.Shared must be multi-targeted first)

**Verification scope:** Only Memory integration tests can be verified locally (no external services). All others just need to build successfully -- actual test execution requires SQL Server, PostgreSQL, Redis at 192.168.0.2.

---

## Important: csproj filename vs directory name mismatches

Several integration test project directories have csproj filenames that don't match the directory name:

| Directory | Actual .csproj filename |
|-----------|------------------------|
| `DotNetWorkQueue.Transport.SqlServer.IntegrationTests/` | `DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj` |
| `DotNetWorkQueue.Transport.Redis.IntegrationTests/` | `DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj` |
| `DotNetWorkQueue.Transport.LiteDB.IntegrationTests/` | `DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj` |
| `DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/` | `DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj` |

Use the exact .csproj filename, not the directory name.

---

<task id="1" name="Multi-target Memory integration tests">
  <description>Add net10.0 to TargetFrameworks for Memory.Integration.Tests and Memory.Linq.Integration.Tests. Both have Microsoft.CSharp + System.Net.Http already in net48-conditional ItemGroups. These are verifiable locally without external services.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj</modify>
  </files>
  <steps>
    <step>In each .csproj, change TargetFrameworks from net48 to net10.0;net48</step>
    <step>Build both projects</step>
    <step>Run integration tests on net10.0 for both (these work without external services)</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet test "Source\DotNetWorkQueue.Transport.Memory.Integration.Tests\DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" -f net10.0 &amp;&amp; dotnet test "Source\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" -f net10.0</command>
    <expected>All integration tests pass on net10.0</expected>
  </verification>
</task>

### Task 1: Multi-target Memory integration tests

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj`

**Step 1: Change TargetFrameworks in Memory.Integration.Tests.csproj**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 2: Change TargetFrameworks in Memory.Linq.Integration.Tests.csproj**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 3: Build both**

Run:
```bash
dotnet build "Source\DotNetWorkQueue.Transport.Memory.Integration.Tests\DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj"
```

Expected: Both build succeeded with 0 errors.

**Step 4: Run integration tests on net10.0**

Run:
```bash
dotnet test "Source\DotNetWorkQueue.Transport.Memory.Integration.Tests\DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" -f net10.0
dotnet test "Source\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" -f net10.0
```

Expected: All tests pass on net10.0.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj Source/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests/DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj
git commit -m "ci: multi-target Memory integration tests to net10.0;net48"
```

---

<task id="2" name="Multi-target SQLite + LiteDB integration tests (fix unconditional refs)">
  <description>Add net10.0 to TargetFrameworks for 4 SQLite/LiteDB integration test projects. Fix 3 unconditional Microsoft.CSharp references by wrapping them in net48-conditional ItemGroups. LiteDB.IntegrationTests already has Microsoft.CSharp conditioned correctly.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj</modify>
  </files>
  <steps>
    <step>In each of the 4 .csproj files, change TargetFrameworks from net48 to net10.0;net48</step>
    <step>In SQLite.Integration.Tests.csproj: move the unconditional Microsoft.CSharp ItemGroup into a net48-conditional ItemGroup</step>
    <step>In SQLite.Linq.Integration.Tests.csproj: move the unconditional Microsoft.CSharp ItemGroup into a net48-conditional ItemGroup</step>
    <step>In LiteDB.Linq.Integration.Tests.csproj: move the unconditional Microsoft.CSharp ItemGroup into a net48-conditional ItemGroup</step>
    <step>LiteDB.IntegrationTests.csproj: no ref fix needed (Microsoft.CSharp already in net48-conditional group)</step>
    <step>Build all 4 projects</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet build "Source\DotNetWorkQueue.Transport.SQLite.Integration.Tests\DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj" &amp;&amp; dotnet build "Source\DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests\DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj" &amp;&amp; dotnet build "Source\DotNetWorkQueue.Transport.LiteDB.IntegrationTests\DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj" &amp;&amp; dotnet build "Source\DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests\DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj"</command>
    <expected>All 4 projects build succeeded with 0 errors on both net10.0 and net48</expected>
  </verification>
</task>

### Task 2: Multi-target SQLite + LiteDB integration tests (fix unconditional refs)

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj`

#### SQLite.Integration.Tests.csproj

**Step 1: Change TargetFrameworks**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 2: Condition the Microsoft.CSharp reference**

Find this unconditional ItemGroup:
```xml
  <ItemGroup>
    <Reference Include="Microsoft.CSharp" />
  </ItemGroup>
```

Replace with:
```xml
  <ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">
    <Reference Include="Microsoft.CSharp" />
  </ItemGroup>
```

#### SQLite.Linq.Integration.Tests.csproj

**Step 3: Change TargetFrameworks**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 4: Condition the Microsoft.CSharp reference**

Find this unconditional ItemGroup:
```xml
  <ItemGroup>
    <Reference Include="Microsoft.CSharp" />
  </ItemGroup>
```

Replace with:
```xml
  <ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">
    <Reference Include="Microsoft.CSharp" />
  </ItemGroup>
```

#### LiteDB.IntegrationTests (DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj)

**Step 5: Change TargetFrameworks**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

No Microsoft.CSharp fix needed -- already in a net48-conditional ItemGroup.

#### LiteDB.Linq.Integration.Tests (DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj)

**Step 6: Change TargetFrameworks**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 7: Condition the Microsoft.CSharp reference**

Find this unconditional ItemGroup:
```xml
  <ItemGroup>
    <Reference Include="Microsoft.CSharp" />
  </ItemGroup>
```

Replace with:
```xml
  <ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">
    <Reference Include="Microsoft.CSharp" />
  </ItemGroup>
```

#### Build verification

**Step 8: Build all 4**

Run:
```bash
dotnet build "Source\DotNetWorkQueue.Transport.SQLite.Integration.Tests\DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests\DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.LiteDB.IntegrationTests\DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests\DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj"
```

Expected: All 4 build succeeded with 0 errors.

**Step 9: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SQLite.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Integration.Tests.csproj Source/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests/DotNetWorkQueue.Transport.SQLite.Linq.Integration.Tests.csproj Source/DotNetWorkQueue.Transport.LiteDB.IntegrationTests/DotNetWorkQueue.Transport.LiteDb.IntegrationTests.csproj Source/DotNetWorkQueue.Transport.LiteDB.Linq.Integration.Tests/DotNetWorkQueue.Transport.LiteDb.Linq.Integration.Tests.csproj
git commit -m "ci: multi-target SQLite/LiteDB integration tests to net10.0;net48

Condition unconditional Microsoft.CSharp references to net48 in
SQLite.Integration.Tests, SQLite.Linq.Integration.Tests, and
LiteDB.Linq.Integration.Tests."
```

---

<task id="3" name="Multi-target SqlServer + PostgreSQL + Redis integration tests">
  <description>Add net10.0 to TargetFrameworks for 6 integration test projects. All framework references are already properly conditioned. These projects require external services to run tests, so verification is build-only.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj</modify>
  </files>
  <steps>
    <step>In each of the 6 .csproj files, change TargetFrameworks from net48 to net10.0;net48</step>
    <step>Build all 6 projects</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet build "Source\DotNetWorkQueue.Transport.SqlServer.IntegrationTests\DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj" &amp;&amp; dotnet build "Source\DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests\DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj" &amp;&amp; dotnet build "Source\DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests\DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj" &amp;&amp; dotnet build "Source\DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests\DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj" &amp;&amp; dotnet build "Source\DotNetWorkQueue.Transport.Redis.IntegrationTests\DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj" &amp;&amp; dotnet build "Source\DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests\DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj"</command>
    <expected>All 6 projects build succeeded with 0 errors on both net10.0 and net48</expected>
  </verification>
</task>

### Task 3: Multi-target SqlServer + PostgreSQL + Redis integration tests

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj`

**Step 1: Change TargetFrameworks in all 6 .csproj files**

In each file, find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Important:** Some files may have `<TargetFrameworks>net48;</TargetFrameworks>` (trailing semicolon). Replace either form with `<TargetFrameworks>net10.0;net48</TargetFrameworks>`.

**Step 2: Build all 6**

Run:
```bash
dotnet build "Source\DotNetWorkQueue.Transport.SqlServer.IntegrationTests\DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests\DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests\DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests\DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.Redis.IntegrationTests\DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests\DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj"
```

Expected: All 6 build succeeded with 0 errors. No test execution -- these require SQL Server, PostgreSQL, and Redis at 192.168.0.2.

**Step 3: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/DotNetWorkQueue.Transport.SqlServer.Integration.Tests.csproj Source/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests/DotNetWorkQueue.Transport.SqlServer.Linq.Integration.Tests.csproj Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj Source/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests/DotNetWorkQueue.Transport.PostgreSQL.Linq.Integration.Tests.csproj Source/DotNetWorkQueue.Transport.Redis.IntegrationTests/DotNetWorkQueue.Transport.Redis.Integration.Tests.csproj Source/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests/DotNetWorkQueue.Transport.Redis.Linq.Integration.Tests.csproj
git commit -m "ci: multi-target SqlServer/PostgreSQL/Redis integration tests to net10.0;net48"
```

---

## Final Verification (after all 3 tasks)

After all tasks complete, run the full solution build to confirm nothing is broken:

```bash
dotnet build "Source\DotNetWorkQueue.sln" -c Debug
```

Expected: `Build succeeded. 0 Error(s)` across all targets.

Then verify the key test suites on net10.0:

```bash
dotnet test "Source\DotNetWorkQueue.Transport.Memory.Integration.Tests\DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" -f net10.0
dotnet test "Source\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests\DotNetWorkQueue.Transport.Memory.Linq.Integration.Tests.csproj" -f net10.0
```

Expected: All in-memory integration tests pass on net10.0.
