---
phase: drop-net48-netstandard20
plan: "1.1"
wave: 1
dependencies: []
must_haves:
  - Remove net48 and netstandard2.0 from DotNetWorkQueue.csproj TargetFrameworks
  - Remove all net48/netstandard2.0 PropertyGroup conditions and DefineConstants from DotNetWorkQueue.csproj
  - Remove net48/netstandard2.0 Schyntax ItemGroups from DotNetWorkQueue.csproj
  - Remove JpLabs.DynamicCode reference and _PackageFiles entry from DotNetWorkQueue.csproj
  - Remove net48/netstandard2.0 _PackageFiles entries for Schyntax from DotNetWorkQueue.csproj
  - Update Description text in DotNetWorkQueue.csproj to remove net48/netstandard2.0 mentions
  - Delete DynamicCodeCompiler.cs (sole consumer of JpLabs)
  - Rewrite LinqCompiler.cs to remove DynamicCodeCompiler dependency and throw NotSupportedException
  - Remove DynamicCodeCompiler object pool registration from ComponentRegistration.cs
  - Remove all #if NETFULL blocks (delete enclosed code) from 11 .cs files
  - Remove #if !NETFULL guards keeping only the modern code path in ScheduledJob.cs
  - Remove CompileException GetObjectData NETFULL-only override
  - Remove LinqExpressionToRun constructor and field from ScheduledJob.cs
  - Remove LinqExpressionToRun public AddUpdateJob overloads and private AddTaskImpl expressionToRun branches from JobScheduler.cs
  - Delete Lib/JpLabs.DynamicCode/ directory
  - Delete Lib/Schyntax/net48/ directory
  - Delete Lib/Schyntax/netstandard2.0/ directory
files_touched:
  - Source/DotNetWorkQueue/DotNetWorkQueue.csproj
  - Source/DotNetWorkQueue/LinqCompile/DynamicCodeCompiler.cs
  - Source/DotNetWorkQueue/LinqCompile/LinqCompiler.cs
  - Source/DotNetWorkQueue/IoC/ComponentRegistration.cs
  - Source/DotNetWorkQueue/ASendJobToQueue.cs
  - Source/DotNetWorkQueue/ISendJobToQueue.cs
  - Source/DotNetWorkQueue/IProducerMethodQueue.cs
  - Source/DotNetWorkQueue/IProducerMethodJobQueue.cs
  - Source/DotNetWorkQueue/IJobScheduler.cs
  - Source/DotNetWorkQueue/Exceptions/CompileException.cs
  - Source/DotNetWorkQueue/Queue/ProducerMethodQueue.cs
  - Source/DotNetWorkQueue/Queue/ProducerMethodJobQueue.cs
  - Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs
  - Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs
  - Source/DotNetWorkQueue/Trace/Decorator/ProducerMethodJobQueueDecorator.cs
  - Lib/JpLabs.DynamicCode/
  - Lib/Schyntax/net48/
  - Lib/Schyntax/netstandard2.0/
tdd: false
risk: high
---

# PLAN-1.1: Core Library Cleanup -- csproj, JpLabs Removal, Conditional Removal, and Vendored DLL Deletion

## Context

DotNetWorkQueue.csproj currently targets `net10.0;net8.0;net48;netstandard2.0`. The `NETFULL` define constant is set for net48 builds and `NETSTANDARD2_0` for netstandard2.0 builds. Eleven .cs files use `#if NETFULL` to guard dynamic LINQ (`LinqExpressionToRun`) overloads, SoapFormatter serialization (`GetObjectData`), and related code paths. These are all dead code after TFM removal.

Additionally, `JobScheduler.cs` (not in the original file list but discovered during analysis) has two public `AddUpdateJob` overloads taking `LinqExpressionToRun` (lines 102-119 and 135-152) that implement the `#if NETFULL` interface members from `IJobScheduler.cs`, plus two private `AddTaskImpl` methods (lines 209-264 and 283-337) with `LinqExpressionToRun` parameters and `if (expressionToRun != null)` branches. All of this must be cleaned up.

The vendored `Lib/JpLabs.DynamicCode/` directory (3 files) and `Lib/Schyntax/net48/` and `Lib/Schyntax/netstandard2.0/` directories are referenced from the csproj and must be deleted in the same plan to keep the build green.

**Critical dependency chain (build failure fix):** The JpLabs reference in the csproj is unconditional (not guarded by net48). Three files depend on JpLabs outside any `#if NETFULL` guard:
- `DynamicCodeCompiler.cs` -- `using JpLabs.DynamicCode;`, directly uses `Compiler` class
- `LinqCompiler.cs` -- holds `IObjectPool<DynamicCodeCompiler>` field and constructor parameter
- `ComponentRegistration.cs` (lines 111-114) -- registers `IObjectPool<DynamicCodeCompiler>` in DI

All three must be fixed in Task 1 alongside the csproj and vendored directory changes to avoid a broken build between tasks.

## Risk: HIGH

This plan touches the core library that every other project depends on. If the csproj or .cs edits are inconsistent, the entire solution breaks.

## Tasks

<task id="1" files="Source/DotNetWorkQueue/DotNetWorkQueue.csproj, Source/DotNetWorkQueue/LinqCompile/DynamicCodeCompiler.cs, Source/DotNetWorkQueue/LinqCompile/LinqCompiler.cs, Source/DotNetWorkQueue/IoC/ComponentRegistration.cs, Lib/JpLabs.DynamicCode/, Lib/Schyntax/net48/, Lib/Schyntax/netstandard2.0/" tdd="false">
  <action>
  This task removes JpLabs entirely and adjusts all code that depends on it, alongside the csproj TFM cleanup. All changes must be applied atomically (single commit) to keep the build green.

  **A. Edit `Source/DotNetWorkQueue/DotNetWorkQueue.csproj`:**

  1. Change `TargetFrameworks` (line 4) from `net10.0;net8.0;net48;netstandard2.0;` to `net10.0;net8.0;`

  2. DELETE these PropertyGroup blocks entirely:
     - Lines 24-26: Debug|netstandard2.0 PropertyGroup (defines NETSTANDARD2_0)
     - Lines 36-38: Debug|net48 PropertyGroup (defines NETFULL)
     - Lines 40-45: Release|netstandard2.0 PropertyGroup (defines NETSTANDARD2_0)
     - Lines 62-67: Release|net48 PropertyGroup (defines NETFULL)

  3. DELETE these ItemGroup blocks entirely:
     - Lines 94-99: net48 Schyntax + Microsoft.CSharp ItemGroup
     - Lines 101-105: netstandard2.0 Schyntax ItemGroup
     - Lines 111-115: JpLabs.DynamicCode reference ItemGroup

  4. In the `IncludeVendoredDllsInPack` target (lines 119-127), DELETE these three `_PackageFiles` lines:
     - Line 123: `Lib\Schyntax\net48\Schyntax.dll` (PackagePath lib\net48)
     - Line 124: `Lib\JpLabs.DynamicCode\JpLabs.DynamicCode.dll` (PackagePath lib\net48)
     - Line 125: `Lib\Schyntax\netstandard2.0\Schyntax.dll` (PackagePath lib\netstandard2.0)
     Also remove the duplicate XML comment on line 118 (`<!-- Pack vendored DLLs into the correct lib/ TFM folders in the nupkg -->`).

  5. Update the `Description` element (lines 8-10) to remove "dot net 4.8, dot net standard 2.0," -- change to:
     `Work queue for dot net 8.0 and 10.0. Supports scheduling, delayed processing, prioritized queues, message expiration, retries with configurable back-off and more.`

  **B. Delete `Source/DotNetWorkQueue/LinqCompile/DynamicCodeCompiler.cs` entirely.**

  This file is the sole consumer of JpLabs. It contains `using JpLabs.DynamicCode;` and directly instantiates `Compiler`. No other file in the solution references `DynamicCodeCompiler` except `LinqCompiler.cs` and `ComponentRegistration.cs`, both handled below.

  **C. Rewrite `Source/DotNetWorkQueue/LinqCompile/LinqCompiler.cs`:**

  The current file depends on `IObjectPool<DynamicCodeCompiler>` which will no longer exist. Rewrite the class body as follows:

  - Remove `using DotNetWorkQueue.Exceptions;` (CompileException no longer thrown here)
  - Remove `using DotNetWorkQueue.Validation;` (Guard no longer used)
  - Remove `using System.Threading;` (Interlocked no longer used)
  - Keep `using System;` and `using DotNetWorkQueue.Messages;`
  - Remove the `_objectPool` field and the constructor parameter. Make the constructor parameterless.
  - Change `CompileAction(LinqExpressionToRun linqExpression)` to:
    ```csharp
    public Action<object, object> CompileAction(LinqExpressionToRun linqExpression)
    {
        throw new NotSupportedException(
            "Dynamic LINQ string compilation is no longer supported. Use compiled Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> instead.");
    }
    ```
  - Remove the entire `#region IDisposable Support` block (the `_disposeCount` field, `Dispose(bool)`, and `Dispose()`). Replace with a no-op dispose:
    ```csharp
    public void Dispose()
    {
        // Nothing to dispose -- no pooled resources
    }
    ```

  The resulting file keeps the license header, `namespace DotNetWorkQueue.LinqCompile`, the `internal class LinqCompiler : ILinqCompiler` declaration, the parameterless constructor, the `CompileAction` method (throwing `NotSupportedException`), and the no-op `Dispose()`. The `ILinqCompiler` interface contract (`CompileAction` + `IDisposable`) is satisfied.

  **D. Edit `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs`:**

  DELETE lines 110-114 (the object pool registration for `DynamicCodeCompiler`):
  ```csharp
  //object pool for linq 
  container.Register<IObjectPool<DynamicCodeCompiler>>(
      () =>
          new ObjectPool<DynamicCodeCompiler>(20,
              () => new DynamicCodeCompiler(container.GetInstance<ILogger>())), LifeStyles.Singleton);
  ```

  Keep line 25 (`using DotNetWorkQueue.LinqCompile;`) -- it is still needed for `LinqCompiler` referenced on line 133.

  The `ILinqCompiler` -> `LinqCompiler` registration (line 133) stays as-is. The decorator registrations for `LinqCompileCacheDecorator` (line 345) and `LinqCompilerDecorator` (line 489) stay as-is -- they depend on `ILinqCompiler`, not `DynamicCodeCompiler`.

  **E. Delete these directories entirely:**
  - `Lib/JpLabs.DynamicCode/` (contains JpLabs.DynamicCode.dll, .pdb, README.md)
  - `Lib/Schyntax/net48/` (contains Schyntax.dll, Schyntax.pdb)
  - `Lib/Schyntax/netstandard2.0/` (contains Schyntax.dll, Schyntax.pdb, Schyntax.deps.json)
  </action>
  <verify>
  grep -c "net48\|netstandard2.0" "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" && echo "FAIL: stale TFM references remain" || echo "PASS: no net48/netstandard2.0 in csproj"
  grep -c "JpLabs" "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" && echo "FAIL: JpLabs reference remains" || echo "PASS: no JpLabs in csproj"
  test -f "Source/DotNetWorkQueue/LinqCompile/DynamicCodeCompiler.cs" && echo "FAIL: DynamicCodeCompiler.cs still exists" || echo "PASS: DynamicCodeCompiler.cs deleted"
  grep -c "DynamicCodeCompiler" "Source/DotNetWorkQueue/LinqCompile/LinqCompiler.cs" && echo "FAIL: LinqCompiler still references DynamicCodeCompiler" || echo "PASS: no DynamicCodeCompiler in LinqCompiler"
  grep -c "NotSupportedException" "Source/DotNetWorkQueue/LinqCompile/LinqCompiler.cs" || echo "FAIL: LinqCompiler.CompileAction does not throw NotSupportedException"
  grep -c "DynamicCodeCompiler" "Source/DotNetWorkQueue/IoC/ComponentRegistration.cs" && echo "FAIL: ComponentRegistration still references DynamicCodeCompiler" || echo "PASS: no DynamicCodeCompiler in ComponentRegistration"
  test -d "Lib/JpLabs.DynamicCode" && echo "FAIL: JpLabs directory still exists" || echo "PASS: JpLabs directory deleted"
  test -d "Lib/Schyntax/net48" && echo "FAIL: Schyntax/net48 still exists" || echo "PASS: Schyntax/net48 deleted"
  test -d "Lib/Schyntax/netstandard2.0" && echo "FAIL: Schyntax/netstandard2.0 still exists" || echo "PASS: Schyntax/netstandard2.0 deleted"
  test -d "Lib/Schyntax/net8.0" && echo "PASS: Schyntax/net8.0 preserved" || echo "FAIL: Schyntax/net8.0 accidentally deleted"
  test -d "Lib/Schyntax/net10.0" && echo "PASS: Schyntax/net10.0 preserved" || echo "FAIL: Schyntax/net10.0 accidentally deleted"
  </verify>
  <done>
  DotNetWorkQueue.csproj has TargetFrameworks `net10.0;net8.0;` only. Zero mentions of net48, netstandard2.0, NETFULL, NETSTANDARD2_0, or JpLabs. Description updated. `DynamicCodeCompiler.cs` is deleted. `LinqCompiler.cs` has a parameterless constructor, throws `NotSupportedException` from `CompileAction`, and has no-op `Dispose()`. `ComponentRegistration.cs` has no `DynamicCodeCompiler` references; `ILinqCompiler` -> `LinqCompiler` registration preserved. `Lib/JpLabs.DynamicCode/`, `Lib/Schyntax/net48/`, and `Lib/Schyntax/netstandard2.0/` do not exist. `Lib/Schyntax/net8.0/` and `Lib/Schyntax/net10.0/` are preserved.
  </done>
</task>

<task id="2" files="Source/DotNetWorkQueue/ASendJobToQueue.cs, Source/DotNetWorkQueue/ISendJobToQueue.cs, Source/DotNetWorkQueue/IProducerMethodQueue.cs, Source/DotNetWorkQueue/IProducerMethodJobQueue.cs, Source/DotNetWorkQueue/IJobScheduler.cs, Source/DotNetWorkQueue/Exceptions/CompileException.cs, Source/DotNetWorkQueue/Queue/ProducerMethodQueue.cs, Source/DotNetWorkQueue/Queue/ProducerMethodJobQueue.cs, Source/DotNetWorkQueue/Trace/Decorator/ProducerMethodJobQueueDecorator.cs" tdd="false">
  <action>
  Remove `#if NETFULL` / `#endif` blocks from 9 .cs files. In each case, DELETE the entire block including the `#if NETFULL` and `#endif` directives and all code between them. The modern (non-NETFULL) code outside the blocks is kept as-is.

  **File-by-file instructions:**

  1. `ASendJobToQueue.cs` -- DELETE lines 107-124 (the `#if NETFULL` block containing `SendAsync(IScheduledJob, DateTimeOffset, LinqExpressionToRun)`). Keep line 125 onward (`StartSend` method).

  2. `ISendJobToQueue.cs` -- DELETE lines 34-43 (the `#if NETFULL` block containing `SendAsync` with `LinqExpressionToRun`). The `using System.Runtime.Serialization;` on line 22 can be removed if present (check -- it is NOT present in this file, so no action).

  3. `IProducerMethodQueue.cs` -- DELETE lines 82-126 (the `#if NETFULL` block containing 6 `LinqExpressionToRun` method signatures: `Send(LinqExpressionToRun, ...)`, `Send(List<LinqExpressionToRun>)`, `Send(List<QueueMessage<LinqExpressionToRun, ...>>)`, and their async equivalents).

  4. `IProducerMethodJobQueue.cs` -- DELETE lines 50-59 (the `#if NETFULL` block containing `SendAsync(IScheduledJob, DateTimeOffset, LinqExpressionToRun)`).

  5. `IJobScheduler.cs` -- DELETE lines 40-90 (the `#if NETFULL` block containing two `AddUpdateJob` overloads that accept `LinqExpressionToRun`).

  6. `CompileException.cs` -- DELETE lines 66-83 (the `#if NETFULL` block containing the `GetObjectData` override). Also remove the `using System.Runtime.Serialization;` on line 21 since `SerializationInfo` and `StreamingContext` are no longer used.

  7. `ProducerMethodQueue.cs` -- DELETE lines 125-180 (the `#if NETFULL` block containing 6 `LinqExpressionToRun` method implementations).

  8. `ProducerMethodJobQueue.cs` -- DELETE lines 77-84 (the `#if NETFULL` block containing `SendAsync(IScheduledJob, DateTimeOffset, LinqExpressionToRun)`).

  9. `ProducerMethodJobQueueDecorator.cs` -- DELETE lines 79-95 (the `#if NETFULL` block containing `SendAsync(IScheduledJob, DateTimeOffset, LinqExpressionToRun)` with tracing).
  </action>
  <verify>
  grep -r "#if NETFULL\|#if NETSTANDARD2_0\|#if !NETFULL" Source/DotNetWorkQueue/ASendJobToQueue.cs Source/DotNetWorkQueue/ISendJobToQueue.cs Source/DotNetWorkQueue/IProducerMethodQueue.cs Source/DotNetWorkQueue/IProducerMethodJobQueue.cs Source/DotNetWorkQueue/IJobScheduler.cs Source/DotNetWorkQueue/Exceptions/CompileException.cs Source/DotNetWorkQueue/Queue/ProducerMethodQueue.cs Source/DotNetWorkQueue/Queue/ProducerMethodJobQueue.cs Source/DotNetWorkQueue/Trace/Decorator/ProducerMethodJobQueueDecorator.cs && echo "FAIL: conditional directives remain" || echo "PASS: no conditional directives in these 9 files"
  </verify>
  <done>
  All 9 files have zero `#if NETFULL`, `#if NETSTANDARD2_0`, or `#if !NETFULL` directives. All `LinqExpressionToRun` method overloads, `GetObjectData` override, and `SoapFormatter` code are removed.
  </done>
</task>

<task id="3" files="Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs, Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs" tdd="false">
  <action>
  Clean up the two JobScheduler files which have more complex conditional/LinqExpressionToRun interleaving than simple `#if NETFULL` blocks.

  **ScheduledJob.cs:**

  1. DELETE lines 59-61: the `#if NETFULL` / `private readonly LinqExpressionToRun _expressionToRun;` / `#endif` block.

  2. DELETE lines 65-84: the entire first constructor that takes `LinqExpressionToRun expressionToRun` parameter. This constructor is:
     ```
     internal ScheduledJob(JobScheduler scheduler, string name, IJobSchedule schedule,
         IProducerMethodJobQueue queue, LinqExpressionToRun expressionToRun, IGetTime time, string route)
     ```
     It is only called from JobScheduler.cs when `expressionToRun != null`, which is the NETFULL path being removed.

  3. In the `RunPendingEventAsync` method, replace the `#if NETFULL` / `#else` / `#endif` block (lines 213-217):
     ```csharp
     #if NETFULL
                         var result = _expressionToRun != null ? await _queue.SendAsync(this, eventTime, _expressionToRun).ConfigureAwait(false) : await _queue.SendAsync(this, eventTime, _actionToRun, RawExpression).ConfigureAwait(false);
     #else
                         var result = await _queue.SendAsync(this, eventTime, _actionToRun, RawExpression).ConfigureAwait(false);
     #endif
     ```
     with just the modern path (no directives):
     ```csharp
                         var result = await _queue.SendAsync(this, eventTime, _actionToRun, RawExpression).ConfigureAwait(false);
     ```

  **JobScheduler.cs:**

  1. DELETE the first public `AddUpdateJob<TTransportInit, TQueue>` overload taking `LinqExpressionToRun` (lines 88-119, including the XML doc comment starting at line 88). This method is on lines 102-119 with docs from ~88.

  2. DELETE the second public `AddUpdateJob<TTransportInit>` overload taking `LinqExpressionToRun` (lines 121-152, including the XML doc comment starting at line 121). This method is on lines 135-152 with docs from ~121.

  3. In the private `AddTaskImpl<TTransportInit, TQueue>` method (lines 209-264):
     - Remove the `LinqExpressionToRun expressionToRun` parameter (line 216)
     - Remove the `if (expressionToRun != null)` branch (lines 237-244), keeping only the `else` branch body (lines 248-253) but without the `else` keyword. The result is unconditionally creating a `ScheduledJob` with the `actionToRun` constructor.

  4. In the private `AddTaskImpl<TTransportInit>` method (lines 283-337):
     - Remove the `LinqExpressionToRun expressionToRun` parameter (line 291)
     - Remove the `if (expressionToRun != null)` branch (lines 310-317), keeping only the `else` branch body (lines 321-326) but without the `else` keyword.

  5. Update the two call sites of `AddTaskImpl` in the remaining `AddUpdateJob` methods (the Expression-based ones):
     - Line 171: Remove the `null` argument that was passed for the now-removed `expressionToRun` parameter.
     - Line 190: Remove the `null` argument that was passed for the now-removed `expressionToRun` parameter.

  6. Update the XML doc comments on the two `AddTaskImpl` methods to remove the `expressionToRun` param docs.
  </action>
  <verify>
  grep -c "LinqExpressionToRun\|#if NETFULL\|#if !NETFULL\|NETSTANDARD2_0" Source/DotNetWorkQueue/JobScheduler/ScheduledJob.cs Source/DotNetWorkQueue/JobScheduler/JobScheduler.cs && echo "FAIL: stale references remain" || echo "PASS: no LinqExpressionToRun or conditional directives in JobScheduler files"
  grep -r "NETFULL\|NETSTANDARD2_0" Source/DotNetWorkQueue/ --include="*.cs" --include="*.csproj" && echo "FAIL: stale references remain in core library" || echo "PASS: no NETFULL/NETSTANDARD2_0 in any core library file"
  dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug 2>&1 | tail -5
  </verify>
  <done>
  ScheduledJob.cs has one constructor (the `Expression<Action<...>>` one), no `LinqExpressionToRun` field, and no conditional directives. JobScheduler.cs has two public `AddUpdateJob` overloads (Expression-based only), two simplified `AddTaskImpl` methods with no `LinqExpressionToRun` parameter, and no conditional branches. `grep -r "NETFULL\|NETSTANDARD2_0" Source/DotNetWorkQueue/ --include="*.cs" --include="*.csproj"` returns 0 matches. `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Debug` succeeds with 0 errors.
  </done>
</task>
