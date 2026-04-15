# Phase 4 Verification — CI Wiring

**Mode:** A (Coverage & Structure)  
**Date:** 2026-04-15  
**Status:** Pre-execution plan review

## Plan Coverage Check

| Plan | Wave | Tasks | Files | Outcome |
|------|------|-------|-------|---------|
| PLAN-1.1 | 1 | 1 | `.github/workflows/ci.yml` | READY |
| PLAN-1.2 | 1 | 1 | `Jenkinsfile` | READY |

Both plans are in Wave 1 and touch strictly disjoint files, enabling parallel execution.

## Success Criteria Mapping

| # | Criterion | Covered By | Status |
|---|-----------|-----------|--------|
| 1 | GitHub Actions PR runs new project on ubuntu-latest / net10.0, passes | PLAN-1.1 task 1 (ci.yml step append) | **READY** |
| 2 | Jenkins master build runs new stage on Docker agent, passes (no Codecov upload) | PLAN-1.2 task 1 (Jenkinsfile stage append, no stash/coverage) | **READY** |
| 3 | Jenkins total stages +1 (14 total); 5s stagger pattern preserved | PLAN-1.2 task 1 (`sleep(time: 65, ...)` formula verified) | **READY** |
| 4 | No existing CI stage regresses | Implicit in both plans (no edits to existing stages/steps) | **READY** |
| 5 | Beacon-skip decision tracked in issue link (if needed) | Out-of-scope per CONTEXT-4: addressed in follow-up PR if feature-branch run fails | **N/A** |

**Verdict:** All 5 success criteria are either directly covered by plan tasks or explicitly marked out-of-scope with follow-up path approved.

## CONTEXT-4 Decision Coverage

| Decision | Locked Statement | Honored by Plans? | Evidence |
|----------|------------------|-------------------|----------|
| **#1: Optimistic UDP** | No skip mechanism, no `[TestCategory]`, no `--network=host` | YES | PLAN-1.1 task: no coverage/filter flags. PLAN-1.2 task: no `--filter`, no `--network=host`, no `[TestCategory]` — literal block has none of these. |
| **#2: Feature branch first** | No commit to master; all changes on branch | YES | Both plans' commit messages are descriptive (no branch name hardcoding). Build-time flow creates branch per CONTEXT-4. |
| **#3: No Codecov from new stage** | No `--collect`, `--settings`, `--results-directory`, `stash`, or `.trx` upload | YES | PLAN-1.1: ci.yml step has `--no-build -c Debug` only (no coverage flags). PLAN-1.2: literal stage body omits all coverage lines; task instruction explicitly forbids `--settings`, `--collect`, `--results-directory`, `stash`. |
| **#3 (Correction note)** | Use `-c Debug`, NOT `-c Release -p:CI=true` | YES | PLAN-1.2 task: literal block uses `-f net10.0 -c Debug` (no `-p:CI=true`). Task instruction (line 87) explicitly says "use `-c Debug` to match the 13 existing stages and avoid a redundant double-build" and forbids `-c Release` and `-p:CI=true`. |
| **#4: Append at end, preserve stagger** | New stage 14th, `sleep(time: 65, ...)`, don't renumber stages 1–13 | YES | PLAN-1.2 task: insert between lines 297–298 (after Dashboard closing brace, before parallel closing brace). Stagger formula verified: `(14-1)*5 = 65`. Task forbids touching any existing `sleep` value. |
| **#5: GitHub Actions — same job, no new job** | Add to existing `build-and-test` job, append one step | YES | PLAN-1.1 task: "Append a new step to the `build-and-test` job ... after the existing 'Unit Tests - Memory' step (currently ends at line 65)." No new job, no matrix changes. |

**Verdict:** All 5 locked decisions are honored. The -c Debug correction is explicitly stated in PLAN-1.2 task instruction (line 87).

## Structural Rules

**Task Count:**
- PLAN-1.1: 1 task (within ≤3 limit)
- PLAN-1.2: 1 task (within ≤3 limit)
- **PASS**

**File Disjointness (Wave 1 parallel execution):**
- PLAN-1.1 touches: `.github/workflows/ci.yml` only
- PLAN-1.2 touches: `Jenkinsfile` only
- No overlap; no task ordering dependency
- **PASS**

**Wave Dependencies:**
- Both plans in Wave 1, no inter-plan dependencies declared
- No circular dependencies
- **PASS**

**Acceptance Criteria (testability):**

PLAN-1.1 `<done>` block (lines 87–93):
- ✓ "`.github/workflows/ci.yml` contains exactly one new `- name: Integration Tests - TaskScheduler Distributed` step" — verifiable via `grep -c`
- ✓ "new step is the last step in the `build-and-test` job and appears after 'Unit Tests - Memory'" — verifiable via `grep -n` ordering
- ✓ "YAML parses cleanly with `python3 -c \"import yaml; yaml.safe_load(...)\"`" — runnable command provided
- ✓ "No coverage-collection flags were introduced" — verifiable via grep for absence of patterns
- ✓ "Single commit on working branch with message specified" — git-verifiable

PLAN-1.2 `<done>` block (lines 150–158):
- ✓ "exactly one new `stage('TaskScheduler Distributed')` block, placed as last stage" — verifiable via `grep -c` + line ordering
- ✓ "first `steps` action is `sleep(time: 65, ...)`" — verifiable via grep + awk range extraction
- ✓ "total `sleep(time: ` count is 14" — verifiable via `grep -c "sleep(time: "`
- ✓ "All 13 existing stagger offsets unchanged" — verifiable via exact `grep -n "sleep(time: "` output comparison
- ✓ "New stage body is exact literal block with NO coverage/Release/filter/retry-failed-tests" — verifiable via awk range + grep absence
- ✓ "Coverage Report stage unchanged (still unstashes 13)" — verifiable via `grep -c "^                unstash "`
- ✓ "Single commit on working branch with message specified" — git-verifiable

All acceptance criteria are **objectively measurable** via bash commands, not subjective.

**Verdict: PASS**

---

## Risk Assessment

| Risk | Rating | Mitigation in Plans |
|------|--------|---------------------|
| UDP multicast blocked in Docker bridge | Medium | CONTEXT-4 decision #1 (optimistic approach): no pre-emptive skip. Feature-branch run will surface failure; follow-up PR for skip mechanism pre-approved. |
| CONTEXT-4 `-c Debug` correction misunderstood | Low | PLAN-1.2 task instruction (line 87) explicitly states the correction and forbids `-c Release` / `-p:CI=true`. Literal block snippet uses `-c Debug`. |
| Jenkinsfile structure mismatch (e.g., stagger is a loop, not inline sleeps) | Low | RESEARCH.md confirmed stagger is inline `sleep(time: N, unit: 'SECONDS')` calls; exact line table provided (lines 70–86). No hidden formula. |
| ci.yml YAML indentation wrong | Low | PLAN-1.1 task specifies "6-space indentation under `steps:`" and "blank line before `- name:`". Verify command includes YAML parse check. |
| Stagger formula off by one | Low | RESEARCH.md verified: stages 1–13 are 0s, 5s, ..., 60s. Formula `(n-1)*5` confirmed. Stage 14 = 65s. Exact grep output listed in PLAN-1.2 verify block. |

**Verdict: Medium risk is inherent to Phase 4 (UDP unknown) and pre-approved. All other risks mitigated by explicit instructions.**

---

## Cross-Plan Integrity

**Forward References:** None. PLAN-1.1 and PLAN-1.2 are independent edits to disjoint files. The GitHub Actions step runs in `build-and-test` job on `ubuntu-latest`. The Jenkins stage runs in `Integration Tests` parallel block on Docker agent. No shared resources, no output of one used as input to the other.

**File Conflicts:** None. `.github/workflows/ci.yml` and `Jenkinsfile` are separate files.

**Hidden Ordering Dependencies:** None. Both plans can execute in parallel.

**Verdict: READY for parallel execution**

---

## Overall Verdict: **PASS**

### Summary
Phase 4 plans are well-structured, complete, and ready for execution:

1. **Coverage:** All 5 ROADMAP success criteria addressed; no requirements orphaned.
2. **CONTEXT-4 adherence:** All 5 locked decisions honored in plan tasks.
3. **Structural soundness:** ≤1 task per plan, disjoint files, no circular dependencies, testable acceptance criteria.
4. **Risk:** Medium UDP risk is pre-approved with follow-up path. Other risks are low and mitigated by explicit instructions.
5. **Feasibility:** File paths, line numbers, and stagger formula verified against live repo state.

**Next Step:** Builder executes both tasks in parallel (Wave 1), then verifies acceptance criteria per the `<done>` blocks. Push to feature branch and confirm both CI surfaces (GitHub Actions + Jenkins) run green before merging to master.
