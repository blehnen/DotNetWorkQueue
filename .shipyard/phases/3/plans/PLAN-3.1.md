---
phase: phase-3
plan: 3.1
wave: 3
dependencies: [2.1, 2.2, 2.3]
must_haves:
  - Full Phase 3 test project runs 5 consecutive times green locally (all three test classes together)
  - Release solution build with -p:CI=true completes with 0 errors, 0 warnings
  - No regressions in other DNQ test projects
  - Phase 3 success criteria from ROADMAP.md lines 229-241 satisfied
files_touched: []
tdd: false
risk: medium
---

# PLAN-3.1: Full suite verification + Release build

## Context

This is Phase 3's final gate. PLAN-2.1/2.2/2.3 each ran their own 5x per-class
flakiness loop; this plan runs the entire new project (all three classes
together) 5x to catch any interference between classes that only surfaces when
they share a test run, then does the Release-with-CI-on build that Phase 3's
success criterion #4 demands.

No source-file changes in this plan — pure verification.

<task id="1" files="" tdd="false">
  <action>
Full-project 5x flakiness loop. This runs ALL three test classes in one assembly
pass per iteration (with `[assembly: DoNotParallelize]` in effect from
PLAN-1.1), so cross-class interactions via shared UDP sockets or NetMQ state
will surface here even if the per-class loops passed.

Execute the exact command from ROADMAP.md line 236:

```bash
cd /mnt/f/git/dotnetworkqueue
for i in 1 2 3 4 5; do \
  dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" --nologo \
  || { echo "RUN $i FAILED"; break; }; \
done
```

All 5 runs must exit 0. If any run fails, stop and flag to the verifier with the
failing iteration number and test output — do NOT rerun until green, do NOT
relax assertions, do NOT raise timeouts.

Also confirm per `dotnet list package`:

```bash
dotnet list "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" package | grep -i TaskScheduling
```

— must show `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` at version `0.4.0` (ROADMAP success criterion #3).
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && for i in 1 2 3 4 5; do dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" --nologo || { echo "RUN $i FAILED"; exit 1; }; done && dotnet list "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" package | grep -i "DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler"</verify>
  <done>5 consecutive full-project runs green on both net8.0 and net10.0. `dotnet list package` output contains `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler` with version `0.4.0` (NuGet resolution, not a project reference).</done>
</task>

<task id="2" files="" tdd="false">
  <action>
Release build with CI=true — this is ROADMAP.md Phase 3 success criterion #4
(`dotnet build "Source/DotNetWorkQueue.sln" -c Release -p:CI=true` — 0 errors,
0 warnings). The `-p:CI=true` flag is critical (CLAUDE.md lesson: "Release
builds for NuGet must use `-p:CI=true`" — applies to validation too).

```bash
cd /mnt/f/git/dotnetworkqueue
dotnet build "Source/DotNetWorkQueue.sln" -c Release -p:CI=true --nologo 2>&1 | tee /tmp/p3-release.log
```

Verify the tail of the log shows `Build succeeded`, `0 Warning(s)`, `0 Error(s)`. If any warnings appear, inspect whether they come from the new project and fix them at the source before re-running. Remember `TreatWarningsAsErrors` is inherited from `Directory.Build.props` in Release, so warnings in the new project would already have broken the build — but double-check the entire solution's output.

Also run a targeted sanity pass on the Memory integration tests to confirm Phase 3 did not regress any pre-existing project:

```bash
dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" --nologo
```
  </action>
  <verify>cd /mnt/f/git/dotnetworkqueue && dotnet build "Source/DotNetWorkQueue.sln" -c Release -p:CI=true --nologo 2>&1 | tee /tmp/p3-release.log && grep -E "Build succeeded|0 Error" /tmp/p3-release.log && dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" --nologo 2>&1 | tail -10</verify>
  <done>`dotnet build Source/DotNetWorkQueue.sln -c Release -p:CI=true` exits 0, `Build succeeded`, `0 Warning(s)`, `0 Error(s)`. Memory integration tests still pass (no regressions). All Phase 3 ROADMAP success criteria (#1, #2, #3, #4, #5 in ROADMAP lines 229-241) are satisfied.</done>
</task>
