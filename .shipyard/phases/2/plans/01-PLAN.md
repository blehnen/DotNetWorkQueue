---
phase: swap-to-packagereference
plan: 01
wave: 1
dependencies: []
must_haves:
  - PackageReference replaces all 4 per-TFM Reference+HintPath blocks
  - _PackageFiles entries for Aq.ExpressionJsonSerializer removed
  - Directory.Packages.props has the new package version
  - Lib/Aq.ExpressionJsonSerializer/ directory deleted
  - Solution builds and all tests pass
files_touched:
  - Source/Directory.Packages.props
  - Source/DotNetWorkQueue/DotNetWorkQueue.csproj
  - Lib/Aq.ExpressionJsonSerializer/ (deleted)
tdd: false
---

# Plan 01 -- Swap DotNetWorkQueue to PackageReference

**Target repo**: `/mnt/f/git/dotnetworkqueue`

## Context

`DotNetWorkQueue.Aq.ExpressionJsonSerializer` v1.0.0 is now live on nuget.org. Replace the vendored DLL references with a proper PackageReference.

---

<task id="1" files="Source/Directory.Packages.props, Source/DotNetWorkQueue/DotNetWorkQueue.csproj, Lib/Aq.ExpressionJsonSerializer/" tdd="false">
  <action>
  **Step 1a: Add to Central Package Management.** Edit `Source/Directory.Packages.props` — add a new `<PackageVersion>` entry in alphabetical order:
  ```xml
  <PackageVersion Include="DotNetWorkQueue.Aq.ExpressionJsonSerializer" Version="1.0.0" />
  ```

  **Step 1b: Add PackageReference.** In `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`, add a new unconditional PackageReference in the existing `<ItemGroup>` that contains other PackageReferences (around line 71-78):
  ```xml
  <PackageReference Include="DotNetWorkQueue.Aq.ExpressionJsonSerializer" />
  ```

  **Step 1c: Remove vendored Reference blocks.** In `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`, remove the `<Reference Include="Aq.ExpressionJsonSerializer">` + `<HintPath>` blocks from all 4 TFM-conditional ItemGroups:
  - Lines 85-87 (net8.0 ItemGroup)
  - Lines 94-96 (net10.0 ItemGroup)
  - Lines 104-106 (net48 ItemGroup)
  - Lines 113-115 (netstandard2.0 ItemGroup)

  Keep the Schyntax references and all other content in those ItemGroups intact.

  **Step 1d: Remove _PackageFiles entries.** In the `IncludeVendoredDllsInPack` target (line 130-142), remove the 4 `<_PackageFiles>` lines for Aq.ExpressionJsonSerializer:
  - Line 133: `..\..\Lib\Aq.ExpressionJsonSerializer\net8.0\...`
  - Line 135: `..\..\Lib\Aq.ExpressionJsonSerializer\net10.0\...`
  - Line 137: `..\..\Lib\Aq.ExpressionJsonSerializer\net48\...`
  - Line 140: `..\..\Lib\Aq.ExpressionJsonSerializer\netstandard2.0\...`

  Keep all Schyntax and JpLabs _PackageFiles entries.

  **Step 1e: Delete vendored DLLs.** Remove the entire `Lib/Aq.ExpressionJsonSerializer/` directory (contains README.md + 4 TFM subdirectories with DLLs).
  </action>
  <verify>
  cd /mnt/f/git/dotnetworkqueue
  dotnet restore "Source/DotNetWorkQueue/DotNetWorkQueue.csproj"
  dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" -c Debug
  dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" -c Release
  dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" -c Debug
  test ! -d "Lib/Aq.ExpressionJsonSerializer"
  </verify>
  <done>
  1. `dotnet restore` resolves `DotNetWorkQueue.Aq.ExpressionJsonSerializer` from nuget.org (not local /Lib).
  2. `dotnet build -c Debug` succeeds for all 4 TFMs.
  3. `dotnet build -c Release` succeeds with 0 warnings (TreatWarningsAsErrors).
  4. All unit tests pass.
  5. `Lib/Aq.ExpressionJsonSerializer/` directory no longer exists.
  6. `grep -r "Aq.ExpressionJsonSerializer" Source/DotNetWorkQueue/DotNetWorkQueue.csproj` shows only the PackageReference line.
  </done>
</task>
