---
phase: taskscheduler-nuget-0.4.0
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - Confirm 0.3.0 Symbols badge is green on nuget.org before trusting the existing CI push workflow
  - Capture the exact 0.3.0 release commit shape and tag format so 0.4.0 mirrors them verbatim
files_touched:
  - .shipyard/phases/2/plans/PLAN-1.1.md
tdd: false
risk: medium
---

# PLAN-1.1 — Pre-flight: verify 0.3.0 publish health and capture release conventions

**Target repo for inspection:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Wave:** 1 (foundational — gate for the rest of Phase 2)
**Risk:** MEDIUM — if 0.3.0's symbols shipped red, the existing CI workflow's separate `.snupkg` push step is broken and must be fixed *before* the `v0.4.0` tag push (CLAUDE.md lesson: nuget.org rejects separate `.snupkg` pushes after the `.nupkg` is already published). If the lesson is also triggered on 0.4.0, the version number is burned.

This plan is a read-only reconnaissance gate. It writes no code in the sibling repo. Its outputs are:
1. A pass/fail signal on 0.3.0 symbols health (drives whether the conditional PLAN-2.2 runs).
2. An exact commit-message template + tag-command template captured for reuse by PLAN-3.1 and PLAN-5.1.

All three tasks here are procedural — not TDD.

<task id="1" files="(none - user-operated checklist)" tdd="false">
  <action>
Run the 0.3.0 nuget.org health check. This is a human checklist — the builder presents it, the user performs it, and the builder captures the result in this plan file below before proceeding.

Open in a browser:
  https://www.nuget.org/packages/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler/0.3.0

Verify each badge on the package page:
  - [ ] Package validation: GREEN
  - [ ] Source Link: GREEN
  - [ ] Deterministic build: GREEN
  - [ ] Symbols: GREEN (this is the load-bearing one)

If Symbols is GREEN: mark this task PASS and proceed to Task 2. PLAN-2.2 (workflow fix) will be skipped.

If Symbols is RED or missing: mark this task FAIL. The builder MUST NOT proceed to PLAN-3.1 until PLAN-2.2 has landed a workflow fix and been merged to master. Add a note to `.shipyard/phases/2/CONTEXT-2.md` recording the red symbols finding so future phases have the evidence.

Reasoning: 0.3.0's `.github/workflows/ci.yml` pushes `.nupkg` and `.snupkg` in two separate `dotnet nuget push` steps (research doc section 6.2). CLAUDE.md's main-repo lesson is that nuget.org does not allow pushing `.snupkg` separately after the `.nupkg` is already published. If 0.3.0 actually shipped green despite this, the workflow is safe. If 0.3.0 is red, re-running the same workflow for 0.4.0 will burn the version number on a broken symbol push.
  </action>
  <verify>
User responds in-thread with one of: "0.3.0 symbols: GREEN — proceed" or "0.3.0 symbols: RED — PLAN-2.2 required". Builder records the response in this plan file by appending a `## Task 1 Result` section with the verbatim user statement and timestamp before marking the task done.
  </verify>
  <done>
A `## Task 1 Result` section has been appended to this file with the user's verdict. If GREEN: PLAN-2.2 is marked SKIPPED in its own file and PLAN-3.1 may begin once PLAN-2.1 completes. If RED: PLAN-2.2 is marked REQUIRED and must complete before PLAN-3.1. No sibling-repo state is modified by this task.
  </done>
</task>

<task id="2" files="(read-only inspection of /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler)" tdd="false">
  <action>
Capture the exact shape of the 0.3.0 release commit and tag so PLAN-3.1 (release commit) and PLAN-5.1 (tag) can mirror them verbatim.

Run these commands and record the output inline in this plan file under a new `## Task 2 Result` section at the bottom:

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# 1. Full commit metadata for a392e62 (0.3.0 release commit)
git show a392e62 --stat --format=fuller

# 2. Subject line only (for the commit-message template)
git log -1 --format=%s a392e62

# 3. Body only (may be empty)
git log -1 --format=%b a392e62

# 4. Tag format: annotated or lightweight, signed or unsigned?
git for-each-ref refs/tags/v0.3.0 --format='%(objecttype) %(refname) %(*objecttype)'
# Lightweight tag -> objecttype=commit; annotated tag -> objecttype=tag with *objecttype=commit

# 5. If annotated, capture the tag message verbatim
git tag -l v0.3.0 -n99

# 6. Tag commit SHA (should equal a392e62 if the tag is on the release commit)
git rev-parse v0.3.0^{commit}
```

From that output, derive and record in this plan file:
  - **Release commit subject template** for v0.4.0. Mirror the v0.3.0 subject prose, substituting `0.4.0` for `0.3.0` and updating the summary to describe the lock-contention fix rather than modernization.
  - **Release commit body template** — copy the v0.3.0 body prose structure, substituting content from `DOCUMENTATION-1.md`'s [0.4.0] CHANGELOG entry.
  - **Tag command template** for v0.4.0:
      - If v0.3.0 is annotated + unsigned: `git tag -a v0.4.0 -m "<message mirrored from v0.3.0>"`
      - If v0.3.0 is lightweight: `git tag v0.4.0`
      - If v0.3.0 is signed: `git tag -s v0.4.0 -m "..."` (note this requires user's GPG key)
  - **Tag message template** — if annotated, capture v0.3.0's tag message prose and draft the v0.4.0 equivalent.

These templates are the load-bearing handoff to PLAN-3.1 Task 3 and PLAN-5.1 Task 1.
  </action>
  <verify>
A `## Task 2 Result` section has been appended to this plan file containing: (a) the raw `git show a392e62 --stat` output, (b) the raw `git for-each-ref` output, (c) the annotated-vs-lightweight determination, (d) the release-commit subject template for 0.4.0, (e) the release-commit body template for 0.4.0, (f) the tag command template for 0.4.0, (g) the tag message template (or "N/A — lightweight").
  </verify>
  <done>
The result section exists with all seven items captured. No files in the sibling repo have been modified. PLAN-3.1 Task 3 can now quote the commit-message template directly; PLAN-5.1 Task 1 can quote the tag-command template directly.
  </done>
</task>

<task id="3" files="(read-only inspection)" tdd="false">
  <action>
Sanity-check the sibling repo's tree state before the release sequence begins. Run and record:

```bash
cd /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler

# Current branch + status
git status --short --branch

# Confirm phase-1-lock-fix exists and is ahead of master
git rev-list --left-right --count master...phase-1-lock-fix

# Confirm the locked decision facts
grep -n '<Version>' Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj
grep -n 'Version="0.3.0"' README.md
sed -n '1,5p' CHANGELOG.md

# Stale bin artifacts presence (will be cleaned in PLAN-4.1)
ls Source/bin/Debug/net472 2>/dev/null || echo "net472 absent"
ls Source/bin/Debug/net48  2>/dev/null || echo "net48 absent"
```

Append a `## Task 3 Result` section capturing all outputs. Flag any surprise:
  - `<Version>` is NOT `0.3.0` -> STOP, escalate to user. Research claims it's `0.3.0` on line 9; a drift means something unexpected happened since Phase 1.
  - `README.md` no longer has `Version="0.3.0"` on line 30 -> STOP, escalate. PLAN-3.1 Task 2 assumes this string exists.
  - `CHANGELOG.md` line 3 is not `### 0.3.0 2026-04-10` -> STOP, escalate.
  - `phase-1-lock-fix` has zero commits ahead of master -> the merge has already happened; PLAN-2.1 becomes a no-op.
  </action>
  <verify>
`## Task 3 Result` section exists with the output of all six commands. No surprises flagged, OR any surprise is explicitly called out with `STOP — escalate to user` and the plan halts until the user decides.
  </verify>
  <done>
Result section captured. No surprises found, or surprises have been escalated and resolved. Wave 1 is complete; Wave 2 may begin.
  </done>
</task>
