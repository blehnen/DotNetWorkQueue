---
phase: taskscheduler-nuget-0.4.0
plan: 2.2
wave: 2
dependencies: [1.1, 2.1]
conditional: true
condition: PLAN-1.1 Task 1 reported 0.3.0 Symbols badge as RED
must_haves:
  - .github/workflows/ci.yml pushes .nupkg + .snupkg in a single combined step using deploy/*.nupkg form
  - Workflow fix lands on master BEFORE v0.4.0 tag push
files_touched:
  - /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/.github/workflows/ci.yml
tdd: false
risk: high
---

# PLAN-2.2 — CONDITIONAL: fix ci.yml NuGet push to combined deploy/*.nupkg form

**Target repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Wave:** 2 (parallelizable with PLAN-2.1 — disjoint branches)
**Risk:** HIGH — modifying the CI publish job immediately before a release is inherently risky. Skipping this plan when it is not needed is mandatory. Running this plan when it *is* needed is mandatory.

## Activation condition

This plan ONLY runs if `PLAN-1.1 Task 1 Result` explicitly states that the 0.3.0 Symbols badge on nuget.org is RED (or missing, or yellow). If PLAN-1.1 recorded "0.3.0 symbols: GREEN", mark this plan SKIPPED and do not perform any tasks. Write a one-line `## SKIPPED — 0.3.0 symbols green` marker at the bottom of this file and move on.

## Reasoning

The sibling repo's `.github/workflows/ci.yml` publish job (documented in `.shipyard/phases/2/RESEARCH.md` section 6.2) pushes `.nupkg` and `.snupkg` as two separate `dotnet nuget push` steps. CLAUDE.md's main-repo lesson: *"NuGet.org does not allow pushing `.snupkg` separately after the `.nupkg` is already published."* If that lesson holds for 0.3.0 (red symbols badge), the same workflow will fail for 0.4.0 and burn the version number. The fix is to pack into a `deploy/` directory and push with the combined `deploy/*.nupkg` form so the CLI picks up the sibling `.snupkg` automatically.

This plan also addresses research finding #7 (`-p:CI=true` not explicitly set on the pack step) because the same line is being edited.

<task id="1" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/.github/workflows/ci.yml" tdd="false">
  <action>
On master (after PLAN-2.1 merge lands — this task STACKS on top of the merge commit), edit `.github/workflows/ci.yml` publish job to use the combined form:

1. Change the Pack step to output to `deploy/` and pass `-p:CI=true` explicitly:

```yaml
    - name: Pack
      run: dotnet pack Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj -c Release -p:CI=true -o deploy --no-restore
```

2. Replace the two separate push steps with a single combined push step that lets the CLI auto-pair the `.snupkg`:

```yaml
    - name: Push package + symbols to NuGet
      run: dotnet nuget push "deploy/*.nupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

Delete the prior "Push snupkg to NuGet" step entirely. The CLI picks up matching `.snupkg` files from the same directory when given the `.nupkg` glob (see CLAUDE.md lesson on NuGet push form).

Commit on master as a standalone commit:

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler
git add .github/workflows/ci.yml
git commit -m "ci: push .nupkg + .snupkg together via deploy/*.nupkg form

The previous two-step push (nupkg then snupkg) triggers nuget.org's
rejection of separate .snupkg pushes after the main package is already
published. Use the single deploy/*.nupkg form so the CLI auto-picks
the matching .snupkg from the same directory. Also passes -p:CI=true
explicitly to pack so deterministic Source Link no longer relies on
GitHub Actions' implicit CI env var.

Required before v0.4.0 tag push to avoid burning the version number
on a broken symbol upload. Verified against 0.3.0's red Symbols badge
during Phase 2 pre-flight (PLAN-1.1 Task 1 result)."
```

Do NOT push to origin yet — PLAN-3.1 will push master once the release commit is also stacked.
  </action>
  <verify>
```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# 1. Confirm the combined push form is present
grep -c 'deploy/\*.nupkg' .github/workflows/ci.yml
# Expect: 1 (or more, but at least 1)

# 2. Confirm only one nuget push step remains
grep -c 'dotnet nuget push' .github/workflows/ci.yml
# Expect: 1

# 3. Confirm CI=true is now explicit on pack
grep 'dotnet pack' .github/workflows/ci.yml | grep -c 'CI=true'
# Expect: 1

# 4. Confirm pack output directory is deploy/
grep 'dotnet pack' .github/workflows/ci.yml | grep -c -- '-o deploy'
# Expect: 1

# 5. GitHub Actions workflow YAML still parses (optional: yamllint if installed)
python3 -c 'import yaml; yaml.safe_load(open(".github/workflows/ci.yml"))' && echo "YAML OK"

# 6. Commit exists on master
git log -1 --format='%s' master | grep -c 'ci: push .nupkg + .snupkg together'
# Expect: 1
```
  </verify>
  <done>
All six verify steps pass. The CI workflow has exactly one `dotnet nuget push` step using `"deploy/*.nupkg"`, and the pack step has `-p:CI=true -o deploy`. Commit is on master but not yet pushed to origin. PLAN-3.1 will push the release commit + this workflow-fix commit together in one `git push`.
  </done>
</task>
