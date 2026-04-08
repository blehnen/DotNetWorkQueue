---
phase: drop-net48-netstandard20
plan: "1.2"
wave: 1
dependencies: []
must_haves:
  - Remove net48 and netstandard2.0 from TargetFrameworks in all 8 transport csproj files
  - Remove all net48/netstandard2.0 PropertyGroup conditions from all 8 transport csproj files
  - Remove net48-only ItemGroup conditions (Microsoft.CSharp reference) where present
  - Retain all net8.0 and net10.0 PropertyGroup blocks unchanged
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj
  - Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj
  - Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj
  - Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj
  - Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDb.csproj
  - Source/DotNetWorkQueue.Transport.Memory/DotNetWorkQueue.Transport.Memory.csproj
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj
  - Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj
tdd: false
risk: medium
---

# PLAN-1.2: Transport Library csproj Cleanup

## Context

All 8 transport library csproj files follow an identical pattern: they target `net10.0;net8.0;net48;netstandard2.0;` and have PropertyGroup conditions for Debug and Release builds of each TFM, with `NETFULL` defined for net48 and `NETSTANDARD2_0` defined for netstandard2.0. Some also have a net48-conditional ItemGroup for `Microsoft.CSharp` reference.

These files have NO .cs file changes -- none of the transport libraries have `#if NETFULL` in their source files (those are all in the core library handled by PLAN-1.1). This plan is purely csproj-level changes.

This plan has NO file overlap with PLAN-1.1 and can execute in parallel.

## Risk: MEDIUM

These are straightforward csproj edits following a consistent pattern, but they affect 8 projects that must all build against the already-modified core library.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj, Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj, Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj, Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj" tdd="false">
  <action>
  Edit 4 transport csproj files (SqlServer, PostgreSQL, SQLite, Redis). For each file, apply these changes:

  **SqlServer (`DotNetWorkQueue.Transport.SqlServer.csproj`):**
  1. Line 4: Change `TargetFrameworks` from `net10.0;net8.0;net48;netstandard2.0;` to `net10.0;net8.0;`
  2. DELETE lines 21-23: Debug|netstandard2.0 PropertyGroup
  3. DELETE lines 25-27: Debug|net48 PropertyGroup
  4. DELETE lines 29-34: Release|netstandard2.0 PropertyGroup
  5. DELETE lines 50-52: Release|net48 PropertyGroup
  6. DELETE lines 54-56: net48-conditional ItemGroup (Microsoft.CSharp reference)

  **PostgreSQL (`DotNetWorkQueue.Transport.PostgreSQL.csproj`):**
  1. Line 4: Change `TargetFrameworks` from `net10.0;net8.0;net48;netstandard2.0;` to `net10.0;net8.0;`
  2. DELETE lines 23-25: Debug|netstandard2.0 PropertyGroup
  3. DELETE lines 35-37: Debug|net48 PropertyGroup
  4. DELETE lines 39-44: Release|netstandard2.0 PropertyGroup
  5. DELETE lines 60-65: Release|net48 PropertyGroup
  6. DELETE lines 67-69: net48-conditional ItemGroup (Microsoft.CSharp reference)

  **SQLite (`DotNetWorkQueue.Transport.SQLite.csproj`):**
  1. Line 3: Change `TargetFrameworks` from `net10.0;net8.0;net48;netstandard2.0;` to `net10.0;net8.0;`
  2. DELETE lines 20-22: Debug|net48 PropertyGroup
  3. DELETE lines 24-26: Debug|netstandard2.0 PropertyGroup
  4. DELETE lines 36-41: Release|net48 PropertyGroup
  5. DELETE lines 43-48: Release|netstandard2.0 PropertyGroup

  **Redis (`DotNetWorkQueue.Transport.Redis.csproj`):**
  1. Line 4: Change `TargetFrameworks` from `net10.0;net8.0;net48;netstandard2.0;` to `net10.0;net8.0;`
  2. DELETE lines 21-23: Debug|netstandard2.0 PropertyGroup
  3. DELETE lines 33-35: Debug|net48 PropertyGroup
  4. DELETE lines 37-42: Release|netstandard2.0 PropertyGroup
  5. DELETE lines 58-63: Release|net48 PropertyGroup
  6. DELETE lines 65-67: net48-conditional ItemGroup (Microsoft.CSharp reference)
  </action>
  <verify>
  for proj in "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" "Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj" "Source/DotNetWorkQueue.Transport.Redis/DotNetWorkQueue.Transport.Redis.csproj"; do echo "=== $proj ==="; grep -c "net48\|netstandard2.0\|NETFULL\|NETSTANDARD2_0" "$proj" && echo "FAIL" || echo "PASS"; done
  </verify>
  <done>
  All 4 csproj files have `TargetFrameworks` of `net10.0;net8.0;` only. Zero mentions of net48, netstandard2.0, NETFULL, or NETSTANDARD2_0. No net48-conditional ItemGroups. All net8.0 and net10.0 PropertyGroups preserved.
  </done>
</task>

<task id="2" files="Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDb.csproj, Source/DotNetWorkQueue.Transport.Memory/DotNetWorkQueue.Transport.Memory.csproj, Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj, Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj" tdd="false">
  <action>
  Edit 4 transport csproj files (LiteDB, Memory, RelationalDatabase, Shared). For each file, apply these changes:

  **LiteDB (`DotNetWorkQueue.Transport.LiteDb.csproj`):**
  1. Line 4: Change `TargetFrameworks` from `net10.0;net8.0;net48;netstandard2.0;` to `net10.0;net8.0;`
  2. DELETE lines 24-26: Debug|netstandard2.0 PropertyGroup
  3. DELETE lines 36-38: Debug|net48 PropertyGroup
  4. DELETE lines 40-45: Release|netstandard2.0 PropertyGroup
  5. DELETE lines 63-68: Release|net48 PropertyGroup

  **Memory (`DotNetWorkQueue.Transport.Memory.csproj`):**
  1. Line 4: Change `TargetFrameworks` from `net10.0;net8.0;net48;netstandard2.0;` to `net10.0;net8.0;`
  2. DELETE lines 23-25: Debug|netstandard2.0 PropertyGroup
  3. DELETE lines 35-37: Debug|net48 PropertyGroup
  4. DELETE lines 39-44: Release|netstandard2.0 PropertyGroup
  5. DELETE lines 60-65: Release|net48 PropertyGroup

  **RelationalDatabase (`DotNetWorkQueue.Transport.RelationalDatabase.csproj`):**
  1. Line 4: Change `TargetFrameworks` from `net10.0;net8.0;net48;netstandard2.0;` to `net10.0;net8.0;`
  2. DELETE lines 19-21: Debug|netstandard2.0 PropertyGroup
  3. DELETE lines 27-29: Debug|net48 PropertyGroup
  4. DELETE lines 31-36: Release|netstandard2.0 PropertyGroup
  5. DELETE lines 46-51: Release|net48 PropertyGroup
  Note: This file is missing a Debug|net8.0 PropertyGroup (only has net10.0 and netstandard2.0 for Debug). This is fine -- just remove the net48 and netstandard2.0 ones.

  **Shared (`DotNetWorkQueue.Transport.Shared.csproj`):**
  1. Line 4: Change `TargetFrameworks` from `net10.0;net8.0;net48;netstandard2.0;` to `net10.0;net8.0;`
  2. DELETE lines 21-23: Debug|netstandard2.0 PropertyGroup
  3. DELETE lines 33-35: Debug|net48 PropertyGroup
  4. DELETE lines 37-42: Release|netstandard2.0 PropertyGroup
  5. DELETE lines 58-63: Release|net48 PropertyGroup
  </action>
  <verify>
  for proj in "Source/DotNetWorkQueue.Transport.LiteDB/DotNetWorkQueue.Transport.LiteDb.csproj" "Source/DotNetWorkQueue.Transport.Memory/DotNetWorkQueue.Transport.Memory.csproj" "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" "Source/DotNetWorkQueue.Transport.Shared/DotNetWorkQueue.Transport.Shared.csproj"; do echo "=== $proj ==="; grep -c "net48\|netstandard2.0\|NETFULL\|NETSTANDARD2_0" "$proj" && echo "FAIL" || echo "PASS"; done
  </verify>
  <done>
  All 4 csproj files have `TargetFrameworks` of `net10.0;net8.0;` only. Zero mentions of net48, netstandard2.0, NETFULL, or NETSTANDARD2_0. All net8.0 and net10.0 PropertyGroups preserved.
  </done>
</task>

<task id="3" files="" tdd="false">
  <action>
  Final verification: After both PLAN-1.1 and PLAN-1.2 are complete, run the full build and grep checks to confirm phase 1 success criteria.
  </action>
  <verify>
  dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug 2>&1 | tail -5
  dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release 2>&1 | tail -5
  grep -r "NETFULL\|NETSTANDARD2_0" Source/DotNetWorkQueue/ --include="*.cs" --include="*.csproj" && echo "FAIL: stale references in core" || echo "PASS: core clean"
  grep -r "net48\|netstandard2.0" Source/DotNetWorkQueue/ --include="*.csproj" && echo "FAIL: stale TFMs in core csproj" || echo "PASS: core csproj clean"
  grep -r "net48\|netstandard2.0" Source/DotNetWorkQueue.Transport.SqlServer/ Source/DotNetWorkQueue.Transport.PostgreSQL/ Source/DotNetWorkQueue.Transport.SQLite/ Source/DotNetWorkQueue.Transport.Redis/ Source/DotNetWorkQueue.Transport.LiteDB/ Source/DotNetWorkQueue.Transport.Memory/ Source/DotNetWorkQueue.Transport.RelationalDatabase/ Source/DotNetWorkQueue.Transport.Shared/ --include="*.csproj" && echo "FAIL: stale TFMs in transport csproj" || echo "PASS: transport csproj clean"
  test -d "Lib/JpLabs.DynamicCode" && echo "FAIL" || echo "PASS: JpLabs deleted"
  test -d "Lib/Schyntax/net48" && echo "FAIL" || echo "PASS: Schyntax/net48 deleted"
  test -d "Lib/Schyntax/netstandard2.0" && echo "FAIL" || echo "PASS: Schyntax/netstandard2.0 deleted"
  </verify>
  <done>
  Both Debug and Release builds of `DotNetWorkQueueNoTests.sln` succeed with 0 errors and 0 warnings. All 7 success criteria from the phase definition are met.
  </done>
</task>
