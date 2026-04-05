# Multi-Target Transport Unit Test Projects

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Multi-target 5 transport-specific unit test projects to `net10.0;net48`.

**Architecture:** Each .csproj gets `net10.0` added to TargetFrameworks. No other changes needed -- all framework references in these projects are already inside net48-conditional ItemGroups (or absent). No net10.0 PropertyGroups needed.

**Tech Stack:** .NET SDK multi-targeting, MSBuild conditions

**Wave:** 1 (parallel with PLAN-1.1 -- zero file overlap)

---

<task id="1" name="Multi-target SqlServer.Tests + PostgreSQL.Tests + Redis.Tests">
  <description>Add net10.0 to TargetFrameworks for SqlServer.Tests, PostgreSQL.Tests, and Redis.Tests. All three are straightforward -- no unconditional framework references to fix.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj</modify>
  </files>
  <steps>
    <step>In each of the 3 .csproj files, change TargetFrameworks from net48 to net10.0;net48</step>
    <step>Build all 3 projects</step>
    <step>Run tests on net10.0 for all 3 projects</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet test "Source\DotNetWorkQueue.Transport.SqlServer.Tests\DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -f net10.0 &amp;&amp; dotnet test "Source\DotNetWorkQueue.Transport.PostgreSQL.Tests\DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -f net10.0 &amp;&amp; dotnet test "Source\DotNetWorkQueue.Transport.Redis.Tests\DotNetWorkQueue.Transport.Redis.Tests.csproj" -f net10.0</command>
    <expected>All tests passed on net10.0 for all 3 projects</expected>
  </verification>
</task>

### Task 1: Multi-target SqlServer.Tests + PostgreSQL.Tests + Redis.Tests

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj`

**Step 1: Change TargetFrameworks in SqlServer.Tests.csproj**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 2: Change TargetFrameworks in PostgreSQL.Tests.csproj**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 3: Change TargetFrameworks in Redis.Tests.csproj**

Find (note trailing semicolon):
```xml
<TargetFrameworks>net48;</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 4: Build all three**

Run:
```bash
dotnet build "Source\DotNetWorkQueue.Transport.SqlServer.Tests\DotNetWorkQueue.Transport.SqlServer.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.PostgreSQL.Tests\DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.Redis.Tests\DotNetWorkQueue.Transport.Redis.Tests.csproj"
```

Expected: All three build succeeded with 0 errors.

**Step 5: Run tests on net10.0**

Run:
```bash
dotnet test "Source\DotNetWorkQueue.Transport.SqlServer.Tests\DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -f net10.0
dotnet test "Source\DotNetWorkQueue.Transport.PostgreSQL.Tests\DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -f net10.0
dotnet test "Source\DotNetWorkQueue.Transport.Redis.Tests\DotNetWorkQueue.Transport.Redis.Tests.csproj" -f net10.0
```

Expected: All tests pass on net10.0.

**Step 6: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj Source/DotNetWorkQueue.Transport.Redis.Tests/DotNetWorkQueue.Transport.Redis.Tests.csproj
git commit -m "ci: multi-target SqlServer/PostgreSQL/Redis unit tests to net10.0;net48"
```

---

<task id="2" name="Multi-target SQLite.Tests + LiteDb.Tests">
  <description>Add net10.0 to TargetFrameworks for SQLite.Tests and LiteDb.Tests. Both have Microsoft.CSharp in net48-conditional ItemGroups already -- no fix needed.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj</modify>
    <modify>Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj</modify>
  </files>
  <steps>
    <step>In each .csproj, change TargetFrameworks from net48 to net10.0;net48</step>
    <step>Build both projects</step>
    <step>Run tests on net10.0 for both</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet test "Source\DotNetWorkQueue.Transport.SQLite.Tests\DotNetWorkQueue.Transport.SQLite.Tests.csproj" -f net10.0 &amp;&amp; dotnet test "Source\DotNetWorkQueue.Transport.LiteDb.Tests\DotNetWorkQueue.Transport.LiteDb.Tests.csproj" -f net10.0</command>
    <expected>All tests passed on net10.0 for both projects</expected>
  </verification>
</task>

### Task 2: Multi-target SQLite.Tests + LiteDb.Tests

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj`
- Modify: `Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj`

**Step 1: Change TargetFrameworks in SQLite.Tests.csproj**

Find:
```xml
<TargetFrameworks>net48</TargetFrameworks>
```

Replace with:
```xml
<TargetFrameworks>net10.0;net48</TargetFrameworks>
```

**Step 2: Change TargetFrameworks in LiteDb.Tests.csproj**

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
dotnet build "Source\DotNetWorkQueue.Transport.SQLite.Tests\DotNetWorkQueue.Transport.SQLite.Tests.csproj"
dotnet build "Source\DotNetWorkQueue.Transport.LiteDb.Tests\DotNetWorkQueue.Transport.LiteDb.Tests.csproj"
```

Expected: Both build succeeded with 0 errors.

**Step 4: Run tests on net10.0**

Run:
```bash
dotnet test "Source\DotNetWorkQueue.Transport.SQLite.Tests\DotNetWorkQueue.Transport.SQLite.Tests.csproj" -f net10.0
dotnet test "Source\DotNetWorkQueue.Transport.LiteDb.Tests\DotNetWorkQueue.Transport.LiteDb.Tests.csproj" -f net10.0
```

Expected: All tests pass on net10.0.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SQLite.Tests/DotNetWorkQueue.Transport.SQLite.Tests.csproj Source/DotNetWorkQueue.Transport.LiteDb.Tests/DotNetWorkQueue.Transport.LiteDb.Tests.csproj
git commit -m "ci: multi-target SQLite/LiteDb unit tests to net10.0;net48"
```
