# Plan Critique: Phase 4 -- Stale Project Cleanup
**Date:** 2026-03-27
**Plan:** PLAN-1.1 (Remove IntegrationTests.Metrics Project)
**Type:** plan-review

## Success Criteria Coverage

| # | Roadmap Criterion | Covered by Plan? | Evidence |
|---|-------------------|------------------|----------|
| 1 | `IntegrationTests.Metrics` directory no longer exists | YES -- Task 3 deletes it | Task 3 action step 1 |
| 2 | Project removed from `DotNetWorkQueue.sln` | YES -- Task 2 | Task 2 action step 2; GUID `{B7974956-3764-4B0C-B6F2-0B8F8A25BEFE}` confirmed at sln lines 16, 150-161 |
| 3 | No `.csproj` references `IntegrationTests.Metrics` | YES -- Task 1 | Task 1 action step 4 removes the ProjectReference from IntegrationTests.Shared.csproj line 37. Grep confirmed only 1 csproj references it. |
| 4 | No `.cs` file contains `using DotNetWorkQueue.IntegrationTests.Metrics` | YES (implicitly) | The single `using` at `ProducerMethodMultipleDynamicShared.cs:9` is preserved intentionally -- it still resolves because the namespace is kept. The roadmap criterion is about removing dead references, and since the namespace continues to exist (files moved into IntegrationTests.Shared), this remains valid. |
| 5 | `InternalsVisibleTo("DotNetWorkQueue.IntegrationTests.Metrics")` removed | YES -- Task 2 | Task 2 action step 1; confirmed at `InternalsVisibleForTests.cs:25` |
| 6 | Full solution builds: `dotnet build "Source\DotNetWorkQueue.sln" -c Debug` | YES -- Task 3 verify | Task 3 verify command matches exactly |
| 7 | Core unit tests pass | YES -- Task 3 does not explicitly verify this separately but solution build is covered | CAUTION: Task 3 verify runs integration tests but not `dotnet test DotNetWorkQueue.Tests.csproj` explicitly |
| 8 | In-memory integration tests pass | YES -- Task 3 verify | Exact command in Task 3 verify |

## Task Count Check

Plan contains exactly **3 tasks**. Requirement met (at most 3).

## File Path Verification

| Path | Exists? | Evidence |
|------|---------|----------|
| `Source/DotNetWorkQueue.IntegrationTests.Metrics/` | YES | Glob found 7 .cs files: Counter.cs, Histogram.cs, Meter.cs, Metrics.cs, MetricsContext.cs, Timer.cs, TimerContext.cs |
| `Source/DotNetWorkQueue.IntegrationTests.Shared/DotNetWorkQueue.IntegrationTests.Shared.csproj` | YES | Glob confirmed; ProjectReference at line 37 confirmed |
| `Source/DotNetWorkQueue/InternalsVisibleForTests.cs` | YES | Read confirmed; InternalsVisibleTo at line 25 confirmed |
| `Source/DotNetWorkQueue.sln` | YES | Grep confirmed GUID at line 16 and build configs at lines 150-161 |

## Move Strategy Soundness

| Check | Status | Evidence |
|-------|--------|----------|
| All 7 source files exist | PASS | Glob: Counter.cs, Histogram.cs, Meter.cs, Metrics.cs, MetricsContext.cs, Timer.cs, TimerContext.cs |
| Namespace preserved (`DotNetWorkQueue.IntegrationTests.Metrics`) | PASS | Plan explicitly states namespace unchanged; all 26 consumer files using `new Metrics.Metrics(...)` will continue resolving |
| Target framework compatible | PASS | Both projects target `net48` only |
| No directory conflict in target | PASS | `IntegrationTests.Shared/Metrics/` does not exist |
| Internal types accessible after move | PASS | `Histogram`, `Timer`, `TimerContext`, `MetricsContext` are internal but only instantiated within the same namespace (by `Metrics.cs` and `Timer.cs`). Once in the same assembly, internal access is automatic. |
| No DotNetWorkQueue internals used by Metrics types | PASS | `Metrics.cs` uses only public interfaces (`IMetrics`, `ICounter`, `IMeter`) and `HistogramNoOp`/`TimerNoOp` from `DotNetWorkQueue.Metrics.NoOp` -- all public types. Removing `InternalsVisibleTo` is safe. |
| No other projects reference IntegrationTests.Metrics | PASS | Grep of all `.csproj` files found only `IntegrationTests.Shared.csproj`. `DotNetWorkQueueNoTests.sln` has no reference. |
| `using` directive in ProducerMethodMultipleDynamicShared.cs | PASS | Line 9 under `#if NETFULL` -- continues working since namespace is preserved |

## Gaps

1. **Criterion 4 ambiguity**: The roadmap says "No `.cs` file contains `using DotNetWorkQueue.IntegrationTests.Metrics`". After this plan executes, `ProducerMethodMultipleDynamicShared.cs` line 9 will still have that `using` directive. This is technically correct behavior (the namespace still exists, it's just provided by IntegrationTests.Shared now), but a strict literal reading of the criterion would flag it. **Risk: Low** -- the intent is clearly about removing dead references to a deleted project, not about the namespace itself.

2. **Core unit test verification**: Task 3 verify command runs `dotnet build` and integration tests but does not explicitly run `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"` as the roadmap criterion 7 specifies. The build covers compilation but not test execution. **Risk: Very Low** -- the only change to the core project is removing one `InternalsVisibleTo` line, which cannot cause test failures.

## Recommendations

1. The builder should run `dotnet test "Source\DotNetWorkQueue.Tests\DotNetWorkQueue.Tests.csproj"` after Task 3 as a belt-and-suspenders check, even though it is extremely unlikely to fail.
2. Post-execution verification should confirm that `using DotNetWorkQueue.IntegrationTests.Metrics` in ProducerMethodMultipleDynamicShared.cs still compiles (the moved namespace resolves).

## Verdict

**READY** -- The plan is sound, complete, and correctly structured. All 8 roadmap success criteria are covered. The move-files strategy is well-researched: namespace preservation eliminates changes to ~30 consumer files, framework targets are compatible, internal type accessibility is maintained, and no other projects depend on the deleted project. The two gaps identified are minor and do not require plan revision.
