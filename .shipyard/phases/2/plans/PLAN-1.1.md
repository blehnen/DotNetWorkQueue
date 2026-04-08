---
phase: drop-net48-netstandard20
plan: "1.1"
wave: 1
dependencies: ["Phase 1 complete"]
must_haves:
  - Remove #if NETFULL blocks from 19 .cs files in IntegrationTests.Shared (dynamic LINQ test cases)
  - Remove LinqMethodTypes.Dynamic enum member from SharedSetup.cs
  - Remove net48 from IntegrationTests.Shared csproj TargetFrameworks
  - Remove all net48 conditional PropertyGroup and ItemGroup blocks from IntegrationTests.Shared csproj
files_touched:
  - Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/Implementation/SimpleMethodProducer.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/Implementation/MultiMethodProducer.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodShared.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodMultipleDynamicShared.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodAsyncShared.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/Implementation/JobSchedulerTests.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/JobSchedulerTestsShared.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/SimpleMethodConsumer.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodRollBack.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodPoisonMessage.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodMultipleDynamic.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodHeartbeat.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodExpiredMessage.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodErrorTable.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodCancelWork.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethodAsync/Implementation/ConsumerMethodAsyncRollBack.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethodAsync/Implementation/ConsumerMethodAsyncPoisonMessage.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethodAsync/Implementation/ConsumerMethodAsyncErrorTable.cs
  - Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj
tdd: false
risk: medium
---

# PLAN-1.1: IntegrationTests.Shared -- Remove NETFULL Conditionals and net48 Target

## Context

`DotNetWorkQueue.IntegrationTests.Shared` contains shared test infrastructure used by all transport-specific Linq integration tests. It defines the `LinqMethodTypes` enum (with a `Dynamic` member guarded by `#if NETFULL`) and 18 test implementation files that branch on `LinqMethodTypes.Dynamic` inside `#if NETFULL` blocks. Removing these is prerequisite for Phase 3 (Linq integration tests).

The csproj has the standard pattern: `net10.0;net48` TargetFrameworks, two conditional PropertyGroups defining `NETFULL`, and one conditional ItemGroup for `Microsoft.CSharp`.

## Risk: MEDIUM

These shared helpers define patterns consumed by every Linq integration test project. Getting the `LinqMethodTypes` enum change wrong would break all downstream consumers.

## Tasks

<task id="1" files="Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs" tdd="false">
  <action>
  In `SharedSetup.cs`, remove the `LinqMethodTypes.Dynamic` enum member and its `#if NETFULL` / `#endif` guard.

  The `LinqMethodTypes` enum (lines 234-240) currently reads:
  ```csharp
  public enum LinqMethodTypes
  {
      Compiled,
  #if NETFULL
      Dynamic
  #endif
  }
  ```

  Change to:
  ```csharp
  public enum LinqMethodTypes
  {
      Compiled
  }
  ```

  No other changes needed in this file -- the rest of the code has no `#if` directives.
  </action>
  <verify>grep -c "NETFULL\|NETSTANDARD2_0\|Dynamic" Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs && echo "FAIL" || echo "PASS"</verify>
  <done>`LinqMethodTypes` enum has only `Compiled`. No `#if` directives remain in SharedSetup.cs.</done>
</task>

<task id="2" files="Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/Implementation/SimpleMethodProducer.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/Implementation/MultiMethodProducer.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodShared.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodMultipleDynamicShared.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ProducerMethod/ProducerMethodAsyncShared.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/Implementation/JobSchedulerTests.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/JobScheduler/JobSchedulerTestsShared.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/SimpleMethodConsumer.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodRollBack.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodPoisonMessage.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodMultipleDynamic.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodHeartbeat.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodExpiredMessage.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodErrorTable.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethod/Implementation/ConsumerMethodCancelWork.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethodAsync/Implementation/ConsumerMethodAsyncRollBack.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethodAsync/Implementation/ConsumerMethodAsyncPoisonMessage.cs, Source/DotNetWorkQueue.IntegrationTests.Shared/ConsumerMethodAsync/Implementation/ConsumerMethodAsyncErrorTable.cs" tdd="false">
  <action>
  Remove `#if NETFULL` / `#endif` blocks from all 18 remaining .cs files. The pattern is consistent across all files:

  **Pattern A (most files):** An `if (linqMethodTypes == LinqMethodTypes.Compiled)` block followed by a `#if NETFULL` / `else` block that handles `LinqMethodTypes.Dynamic`. Example from `ConsumerMethodErrorTable.cs`:
  ```csharp
  if (linqMethodTypes == LinqMethodTypes.Compiled)
  {
      producer.RunTestCompiled<TTransportInit>(...);
  }
  #if NETFULL
  else
  {
      producer.RunTestDynamic<TTransportInit>(...);
  }
  #endif
  ```
  **Action:** Delete the `#if NETFULL` line, the entire `else { ... }` block, and the `#endif` line. Keep the `if (linqMethodTypes == LinqMethodTypes.Compiled)` block intact. Since `Compiled` is now the only enum value, the `if` check is redundant but harmless -- keep it for clarity and minimal diff.

  **Pattern B (ProducerMethodShared, ProducerMethodAsyncShared, ProducerMethodMultipleDynamicShared):** These files contain `RunTestDynamic` method definitions guarded by `#if NETFULL` / `#endif`. Delete the entire `#if NETFULL` ... `#endif` blocks including the method definitions inside them.

  **Pattern C (JobSchedulerTestsShared):** Contains `#if NETFULL` blocks with `LinqExpressionToRun`-based job scheduling test code. Delete the entire `#if NETFULL` ... `#endif` blocks.

  Apply the same approach to all 18 files: delete `#if NETFULL` directive, all code through the matching `#endif`, keep everything outside those guards.
  </action>
  <verify>grep -r "NETFULL\|NETSTANDARD2_0" Source/DotNetWorkQueue.IntegrationTests.Shared/ --include="*.cs" && echo "FAIL: conditional directives remain" || echo "PASS: no conditional directives in IntegrationTests.Shared .cs files"</verify>
  <done>All 18 .cs files have zero `#if NETFULL` or `#endif` directives. All `RunTestDynamic` call sites and method definitions are removed. All `LinqExpressionToRun` test paths are removed.</done>
</task>

<task id="3" files="Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj" tdd="false">
  <action>
  Edit the csproj to remove net48 targeting:

  1. Change `<TargetFrameworks>net10.0;net48</TargetFrameworks>` to `<TargetFrameworks>net10.0</TargetFrameworks>`

  2. DELETE the two conditional PropertyGroup blocks (lines 7-13):
     ```xml
     <PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Debug|net48|AnyCPU'">
       <DefineConstants>TRACE;DEBUG;CODE_ANALYSIS;NETFULL</DefineConstants>
     </PropertyGroup>
     <PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Release|net48|AnyCPU'">
       <DefineConstants>NETFULL</DefineConstants>
     </PropertyGroup>
     ```

  3. DELETE the conditional ItemGroup (lines 24-26):
     ```xml
     <ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">
       <Reference Include="Microsoft.CSharp" />
     </ItemGroup>
     ```
  </action>
  <verify>grep -c "net48\|NETFULL\|NETSTANDARD2_0" Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj && echo "FAIL" || echo "PASS"</verify>
  <done>IntegrationTests.Shared.csproj has `<TargetFrameworks>net10.0</TargetFrameworks>`. No net48 conditional PropertyGroups or ItemGroups remain.</done>
</task>
