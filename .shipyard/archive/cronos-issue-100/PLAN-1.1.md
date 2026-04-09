---
phase: core-library-cronos
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - Cronos and CronExpressionDescriptor NuGet packages added to central package management
  - Schyntax assembly references and vendored DLL pack target removed from DotNetWorkQueue.csproj
  - Cronos and CronExpressionDescriptor PackageReferences added to DotNetWorkQueue.csproj
  - IJobSchedule.Previous() return types changed to DateTimeOffset?
  - IJobSchedule.Description property added
  - IHeartBeatConfiguration.UpdateTime doc comment updated from Schyntax to cron format
files_touched:
  - Source/Directory.Packages.props
  - Source/DotNetWorkQueue/DotNetWorkQueue.csproj
  - Source/DotNetWorkQueue/IJobSchedule.cs
  - Source/DotNetWorkQueue/IHeartBeatConfiguration.cs
tdd: false
---

# Plan 1.1: NuGet Dependencies, Interface Contract, and Doc Comment

This plan sets up the dependency foundation and changes the public API contract. It touches 4 files with no overlap with Plan 1.2. The solution will NOT compile after this plan alone -- Plan 1.2 (implementation rewrite) is required to reach a compilable state.

## Context

- `JobSchedule.cs` is the only file that imports `Schyntax` (line 20)
- `IJobSchedule` is a public interface; changing `Previous()` to nullable is a breaking API change
- `ScheduledJob.cs` is the only caller of `Previous()` (line 98)
- Cronos v0.12.0 was just published today (2026-04-08) with 0 downloads; RESEARCH.md flags this as a risk. Consider pinning to v0.11.1 (3.8M downloads) for stability unless v0.12.0 features are needed.
- CronExpressionDescriptor v2.45.0 is stable (4M total downloads)

## Tasks

<task id="1" files="Source/Directory.Packages.props, Source/DotNetWorkQueue/DotNetWorkQueue.csproj" tdd="false">
  <action>
  1. In `Source/Directory.Packages.props`, add two entries to the `<!-- Core -->` section (after line 11, the `System.Diagnostics.DiagnosticSource` entry):
     ```xml
     <PackageVersion Include="Cronos" Version="0.11.1" />
     <PackageVersion Include="CronExpressionDescriptor" Version="2.45.0" />
     ```
     Note: Using Cronos 0.11.1 (stable, 3.8M downloads) instead of 0.12.0 (just released, 0 downloads). If 0.12.0 features are needed later, bump in a follow-up.

  2. In `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`:
     - Add two PackageReference entries to the main ItemGroup (lines 49-59), after the existing entries:
       ```xml
       <PackageReference Include="Cronos" />
       <PackageReference Include="CronExpressionDescriptor" />
       ```
     - DELETE the two TFM-conditional Schyntax reference ItemGroups (lines 61-71):
       ```xml
       <ItemGroup Condition=" '$(TargetFramework)' == 'net8.0' ">
         <Reference Include="Schyntax">...</Reference>
       </ItemGroup>
       <ItemGroup Condition=" '$(TargetFramework)' == 'net10.0' ">
         <Reference Include="Schyntax">...</Reference>
       </ItemGroup>
       ```
     - DELETE the entire `IncludeVendoredDllsInPack` target block (lines 78-84):
       ```xml
       <!-- Pack vendored DLLs into the correct lib/ TFM folders in the nupkg -->
       <Target Name="IncludeVendoredDllsInPack" BeforeTargets="GenerateNuspec">...</Target>
       ```
  </action>
  <verify>grep -c "Cronos\|CronExpressionDescriptor" "Source/Directory.Packages.props" && grep -c "Cronos\|CronExpressionDescriptor" "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" && ! grep -q "Schyntax" "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" && echo "PASS" || echo "FAIL"</verify>
  <done>Directory.Packages.props has Cronos 0.11.1 and CronExpressionDescriptor 2.45.0 entries. DotNetWorkQueue.csproj has Cronos and CronExpressionDescriptor PackageReferences. No Schyntax references remain in the csproj. The IncludeVendoredDllsInPack target is gone.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue/IJobSchedule.cs" tdd="false">
  <action>
  In `Source/DotNetWorkQueue/IJobSchedule.cs`:

  1. Change the return type of `Previous()` (line 51) from `DateTimeOffset` to `DateTimeOffset?`
  2. Change the return type of `Previous(DateTimeOffset atOrBefore)` (line 57) from `DateTimeOffset` to `DateTimeOffset?`
  3. Add a new `Description` property after the `OriginalText` property (after line 34). Include XML doc:
     ```csharp
     /// <summary>
     /// Gets a human-readable description of the schedule.
     /// </summary>
     /// <value>
     /// The human-readable description.
     /// </value>
     string Description { get; }
     ```

  The resulting interface should have: `OriginalText`, `Description`, `Next()`, `Next(DateTimeOffset)`, `Previous()` returning `DateTimeOffset?`, `Previous(DateTimeOffset)` returning `DateTimeOffset?`.
  </action>
  <verify>grep -n "DateTimeOffset?" "Source/DotNetWorkQueue/IJobSchedule.cs" | grep -c "Previous" && grep -c "Description" "Source/DotNetWorkQueue/IJobSchedule.cs" && echo "PASS" || echo "FAIL"</verify>
  <done>`Previous()` and `Previous(DateTimeOffset)` both return `DateTimeOffset?`. A `Description` property with XML doc exists on the interface. `Next()` and `Next(DateTimeOffset)` remain non-nullable `DateTimeOffset`.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue/IHeartBeatConfiguration.cs" tdd="false">
  <action>
  In `Source/DotNetWorkQueue/IHeartBeatConfiguration.cs`, update the `<remarks>` doc comment on the `UpdateTime` property (lines 61-63).

  Change from:
  ```xml
  /// <remarks>
  /// This is expected to be in schyntax format - https://github.com/schyntax/cs-schyntax
  /// </remarks>
  ```

  Change to:
  ```xml
  /// <remarks>
  /// This is expected to be in standard cron format (5-field) or cron format with seconds (6-field).
  /// </remarks>
  ```
  </action>
  <verify>grep -A1 "remarks" "Source/DotNetWorkQueue/IHeartBeatConfiguration.cs" | grep -q "cron format" && ! grep -q "schyntax" "Source/DotNetWorkQueue/IHeartBeatConfiguration.cs" && echo "PASS" || echo "FAIL"</verify>
  <done>The `UpdateTime` doc comment references "standard cron format (5-field) or cron format with seconds (6-field)". No Schyntax references remain in the file.</done>
</task>
