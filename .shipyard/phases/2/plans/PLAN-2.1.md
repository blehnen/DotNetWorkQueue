---
phase: taskscheduler-nuget-0.4.0
plan: 2.1
wave: 2
dependencies: [1.1]
must_haves:
  - phase-1-lock-fix merged into master cleanly in the sibling repo
  - Full test suite green on master after the merge (net8.0 + net10.0, Debug + Release)
files_touched:
  - /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler (branch operations only)
tdd: false
risk: medium
---

# PLAN-2.1 — Merge phase-1-lock-fix into master in the sibling repo

**Target repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Wave:** 2 (depends on PLAN-1.1 pre-flight)
**Risk:** MEDIUM — merge conflicts would be surprising (Phase 1 is the only active branch) but re-running the test suite on master is non-negotiable because a merge-time regression would ship straight into 0.4.0.

This plan performs CONTEXT-2 decision #1 (merge as Phase 2 Task 0). It is a single focused task. The release commit itself lands in PLAN-3.1 as a second commit on master on top of the merge.

Do NOT run this plan until PLAN-1.1 Task 1 has a recorded verdict. If PLAN-1.1 flagged red symbols, PLAN-2.2 must run BEFORE this plan's merge reaches the release tag — but PLAN-2.2 and PLAN-2.1 can run in parallel within Wave 2 because they touch disjoint branches (PLAN-2.2 creates a short-lived workflow-fix branch; this plan merges phase-1-lock-fix).

<task id="1" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler (master branch)" tdd="false">
  <action>
Merge `phase-1-lock-fix` into `master` on the sibling repo with a no-fast-forward merge commit to preserve the feature-branch topology, mirroring the existing branching convention for feature work on this repo.

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# Ensure working tree has no TRACKED changes. Pre-existing untracked
# directories (e.g., NuGetScratchbrian/ — a known user scratch dir) are
# tolerated because they don't affect the merge. Tracked modifications
# would; halt on those.
git status --porcelain --untracked-files=no
# Expect: empty output (no modifications to tracked files). If non-empty, STOP and escalate.

# Switch to master and fast-forward to origin/master
git checkout master
git fetch origin
git merge --ff-only origin/master

# Merge the feature branch with a no-fast-forward commit so the topology is preserved
git merge --no-ff phase-1-lock-fix -m "Merge phase-1-lock-fix for 0.4.0 release"

# Confirm master has advanced and the merge commit exists
git log --oneline -5
```

Do NOT push yet. PLAN-3.1 pushes master to origin after the release commit is stacked on top of this merge commit.

If the merge produces conflicts: STOP. Conflicts are unexpected (Phase 1 was the only active branch) and indicate either upstream drift on master or a wrong baseline on phase-1-lock-fix. Escalate to the user; do not attempt a speculative resolution.
  </action>
  <verify>
```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# 1. Confirm merge commit exists at master HEAD
git log -1 --format='%H %s' master | grep 'Merge phase-1-lock-fix for 0.4.0 release'

# 2. Confirm the lock-fix work is present on master
grep -c '_lockSocket' Source/TaskSchedulerJobCountSync.cs
# Expect: 0

# 3. Clean build + tests on master, both Debug and Release, with CI=true for Release
rm -rf Source/bin Source/obj
dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Debug
dotnet test  "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" -c Debug --no-build

rm -rf Source/bin Source/obj
dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Release -p:CI=true
dotnet test  "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Tests.csproj" -c Release --no-build
```
  </verify>
  <done>
Master HEAD is a no-ff merge commit titled "Merge phase-1-lock-fix for 0.4.0 release". `grep -c '_lockSocket' Source/TaskSchedulerJobCountSync.cs` returns 0. Both Debug and Release builds complete with 0 errors, 0 warnings (TreatWarningsAsErrors is on in Release). Full test suite passes (9/9 or whatever Phase 1 landed) on both configurations, net8.0 + net10.0. No push to origin yet — that happens in PLAN-3.1.
  </done>
</task>
