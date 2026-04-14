---
phase: taskscheduler-nuget-0.4.0
plan: 5.1
wave: 5
dependencies: [4.1]
must_haves:
  - Annotated v0.4.0 tag created on master mirroring the v0.3.0 tag convention
  - Tag pushed to origin, triggering the GitHub Actions publish job
  - nuget.org shows 0.4.0 live with green Source Link / Symbols / Deterministic Build badges
  - dotnet add package --version 0.4.0 succeeds from a throwaway project
files_touched:
  - /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler (tag ref only)
tdd: false
risk: high
---

# PLAN-5.1 — Tag, trigger, and verify the 0.4.0 NuGet publish

**Target repo:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Wave:** 5 (depends on PLAN-4.1 local pack verification being green)
**Risk:** HIGH — this plan performs the irreversible action. Pushing `v0.4.0` to origin triggers `.github/workflows/ci.yml`'s publish job, which runs `dotnet nuget push`. Once nuget.org accepts the push, 0.4.0 is locked for life: no re-push, no downgrade. Do not start this plan until PLAN-4.1 has captured green local-pack verification.

## Preconditions

- PLAN-3.1 release commit is on `origin/master`.
- PLAN-4.1 Task 2 recorded green TFM + Source Link checks on a local `.nupkg` built from that commit.
- PLAN-1.1 Task 2 Result captured the tag-command template (annotated/lightweight, signed/unsigned, tag-message text).
- CI build job triggered by the PLAN-3.1 master push is GREEN (check GitHub Actions UI). This is a pre-flight safety check: if the build job is red on master, the tag push will fail to build and the package won't publish — but also won't get a chance to succeed.

<task id="1" files="/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler (tag ref)" tdd="false">
  <action>
Create the `v0.4.0` tag on master, mirroring the exact format captured in `PLAN-1.1.md` Task 2 Result (annotated vs lightweight, signed vs unsigned, tag-message prose).

Default template (annotated, unsigned) if PLAN-1.1 Task 2 confirmed `v0.3.0` is annotated/unsigned:

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# Confirm we are on master at the release commit
git checkout master
git rev-parse master
git log -1 --format='%s'
# Expect the release commit subject from PLAN-3.1 Task 3

# Create the annotated tag on the release commit
git tag -a v0.4.0 -m "Release 0.4.0 — TaskSchedulerJobCountSync lock-contention fix

Eliminates the _lockSocket contention between IncreaseCurrentTaskCount /
DecreaseCurrentTaskCount and the ProcessMessages receive loop. Socket
I/O now runs on a dedicated poller thread driving NetMQActor +
NetMQQueue<SetCountMsg>. GetCurrentTaskCount is lock-free via
Interlocked.Read.

Closes issue #6. See CHANGELOG.md for full release notes."

# Confirm tag is on the release commit
git rev-parse v0.4.0^{commit}
git rev-parse master
# Both SHAs must match
```

Alternative — if PLAN-1.1 Task 2 Result says `v0.3.0` is lightweight:
```bash
git tag v0.4.0
```

Alternative — if `v0.3.0` is signed:
```bash
git tag -s v0.4.0 -m "..."   # requires user's GPG key — user may need to perform this step themselves
```

Do NOT push the tag yet. Task 2 pushes it.
  </action>
  <verify>
```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# 1. Tag exists
git tag -l v0.4.0
# Expect: v0.4.0

# 2. Tag is on the same commit as master HEAD
[ "$(git rev-parse v0.4.0^{commit})" = "$(git rev-parse master)" ] && echo "tag on master HEAD OK"

# 3. Tag format matches v0.3.0 (both annotated or both lightweight)
V03_TYPE=$(git for-each-ref refs/tags/v0.3.0 --format='%(objecttype)')
V04_TYPE=$(git for-each-ref refs/tags/v0.4.0 --format='%(objecttype)')
echo "v0.3.0 objecttype: $V03_TYPE"
echo "v0.4.0 objecttype: $V04_TYPE"
[ "$V03_TYPE" = "$V04_TYPE" ] && echo "tag format matches"
```
  </verify>
  <done>
`v0.4.0` tag exists locally, points at master HEAD (the release commit), and its format (annotated/lightweight) matches `v0.3.0`'s format verbatim. Not yet pushed to origin.
  </done>
</task>

<task id="2" files="(origin push + CI trigger, no tracked files modified)" tdd="false">
  <action>
**THIS IS THE IRREVERSIBLE STEP.** Push the `v0.4.0` tag to origin, which fires the GitHub Actions `publish` job. Once CI accepts and pushes the package to nuget.org, version 0.4.0 is locked forever.

The user MUST be the one to run this step, because:
1. The user may need to enter credentials / 2FA for the push.
2. Making the user press the button is the last sanity gate: if there's any uncertainty about the package contents, now is the time to halt.

Builder presents this to the user:

> PLAN-4.1 local pack verification is GREEN. About to push `v0.4.0` to origin, which will trigger the GitHub Actions publish job and push the package to nuget.org. This is irreversible — version 0.4.0 cannot be re-pushed or downgraded once nuget.org accepts it.
>
> Run this when ready:
>
> ```bash
> cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler
> git push origin v0.4.0
> ```
>
> Then open:
>
>   https://github.com/blehnen/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/actions
>
> Watch the `publish` workflow run. It should complete green in ~3–5 minutes.
>
> Reply in-thread with one of:
>   - "tag pushed, publish workflow: GREEN"
>   - "tag pushed, publish workflow: FAILED" (include the step that failed)

Builder waits for the user response. If FAILED: the version number may or may not be burned depending on which step failed. If the `Push .nupkg` step failed BEFORE completing, 0.4.0 may be retryable via `--skip-duplicate`. If the main push succeeded but symbols failed, 0.4.0 is live but symbol-less — the workflow fix from PLAN-2.2 should have prevented this; re-open PLAN-2.2 and escalate to the user.
  </action>
  <verify>
User response recorded in-thread. The response is one of:
  - `tag pushed, publish workflow: GREEN` -> proceed to Task 3
  - `tag pushed, publish workflow: FAILED: <step>` -> halt, escalate to user, do NOT proceed to Task 3 until the failure is resolved

Builder appends a `## Task 2 Result` section to this plan with the user's verbatim statement and the GitHub Actions run URL.
  </verify>
  <done>
User reported "publish workflow: GREEN". The `## Task 2 Result` section exists with the run URL and verbatim user statement. Tag `v0.4.0` is on origin. Proceed to Task 3 verification.
  </done>
</task>

<task id="3" files="(human verification checklist, no tracked files modified)" tdd="false">
  <action>
Final verification: confirm the package is live on nuget.org with green validation indicators, AND a fresh restore in a scratch project can pull it. This is CONTEXT-2 decision #3 (manual checklist) implemented as the last Phase 2 task.

Builder presents this checklist to the user. The user runs each item and reports results:

### nuget.org page verification

Open in a browser (may need to wait 5–15 minutes after the publish workflow completes — nuget.org indexing is not instant):

  https://www.nuget.org/packages/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/0.4.0

Check each badge on the package page:
  - [ ] Page loads and shows `0.4.0` as the current version
  - [ ] Package validation: GREEN
  - [ ] Source Link: GREEN
  - [ ] Deterministic build: GREEN
  - [ ] Symbols: GREEN

If Symbols is red even after PLAN-2.2 ran, the workflow fix did not land correctly — halt and escalate. If Symbols is red and PLAN-2.2 was SKIPPED (because 0.3.0 was green), the lesson is inconsistent — escalate to user.

### Fresh restore verification

In a throwaway console project (so a stale local NuGet cache doesn't mask problems):

```bash
mkdir -p /tmp/nuget-verify-0.4.0
cd /tmp/nuget-verify-0.4.0
dotnet new console
dotnet add package DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler --version 0.4.0
dotnet restore
```

Expected: `dotnet add package` succeeds, `dotnet restore` succeeds, and the `obj/project.assets.json` references version `0.4.0`. If nuget.org hasn't indexed yet (NU1102 "Unable to find package"), wait 5 more minutes and retry.

### User response

User reports in-thread with one of:
  - `nuget.org: all green, restore OK` -> Phase 2 is complete
  - `nuget.org: <specific problem>` -> halt and escalate

Builder appends a `## Task 3 Result` section with the verbatim user statement, the nuget.org URL, and (if used) the scratch-project restore output.
  </action>
  <verify>
User has reported "all green, restore OK" in-thread. The `## Task 3 Result` section exists with all required evidence. Phase 2 completion criteria (ROADMAP.md lines 148–156):
  1. 0.4.0 publicly listed on nuget.org with symbols + deterministic Source Link — CHECK
  2. Fresh `dotnet restore` can pull 0.4.0 from nuget.org — CHECK
  3. CHANGELOG committed with fix description + issue link — CHECK (PLAN-3.1 Task 2)
  4. Git tag v0.4.0 applied — CHECK (PLAN-5.1 Task 1 + 2)
  </verify>
  <done>
All four Phase 2 success criteria met. Phase 2 is DONE. `.shipyard/STATE.json` should be updated by the orchestrator (not this plan) to mark phase 2 complete and unblock Phase 3 (DotNetWorkQueue integration test project referencing 0.4.0 from nuget.org).
  </done>
</task>
