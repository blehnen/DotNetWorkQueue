# Phase 4 Plan Critique — Feasibility Stress Test

**Mode:** B (Plan Critique / Feasibility)  
**Date:** 2026-04-15  
**Status:** Pre-execution stress test

## Per-Plan Findings

### PLAN-1.1 (GitHub Actions — ci.yml)

**File existence:** PASS  
- `.github/workflows/ci.yml` exists at `/mnt/f/git/dotnetworkqueue/.github/workflows/ci.yml`
- File is 66 lines, well-formed YAML (`python3 -c "import yaml; yaml.safe_load(...)"` → `YAML OK`)

**Line reference accuracy:** PASS  
- Plan states "currently ends at line 65" for "Unit Tests - Memory" step
- Actual live file: `- name: Unit Tests - Memory` at line 64, `run: dotnet test ...` at line 65
- **Correct:** insertion point is after line 65 ✓

**Snippet accuracy:** PASS  
- Plan shows:
  ```yaml
      - name: Integration Tests - TaskScheduler Distributed
        run: dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" --no-build -c Debug
  ```
- Indentation: 6 spaces under `steps:` (matching lines 34, 40, etc.) ✓
- Flags: `--no-build -c Debug` (matches all other test steps, no coverage flags) ✓
- Project path: `DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests` exists (Phase 3 shipped it) ✓

**Verify command runnability:** PASS  
All 5 verify commands in PLAN-1.1 `<verify>` block (lines 66–86) are executable:

1. `grep -c "Integration Tests - TaskScheduler Distributed" .github/workflows/ci.yml` — standard grep, runnable ✓
2. `grep -c "TaskScheduling.Distributed.TaskScheduler.Integration.Tests" .github/workflows/ci.yml` — standard grep ✓
3. `grep -cE "(XPlat Code Coverage|coverage.runsettings|--collect|--results-directory|\.trx)" .github/workflows/ci.yml` — regex grep ✓
4. `python3 -c "import yaml, sys; yaml.safe_load(open('.github/workflows/ci.yml')); print('YAML OK')"` — python3 available in repo ✓
5. `grep -n "Unit Tests - Memory\|Integration Tests - TaskScheduler Distributed" .github/workflows/ci.yml` — line number check ✓

All commands are dry-runnable against the current repo state.

**YAML indentation:** PASS  
- Existing steps (lines 34–65) use consistent 6-space indentation: `      - name:` (6 spaces)
- Plan's snippet matches this exactly ✓
- Blank line pattern: all steps are separated by one blank line (e.g., line 33 is blank, line 34 is step; line 36 is blank, line 37 is step)
- Plan specifies "one blank line before `- name:`" ✓

---

### PLAN-1.2 (Jenkinsfile — new Jenkins stage)

**File existence:** PASS  
- `Jenkinsfile` exists at `/mnt/f/git/dotnetworkqueue/Jenkinsfile`
- File is 363 lines, well-formed Groovy

**Line reference accuracy:** PASS  
- Plan states "`stage('Dashboard')` spans lines 273–297. Its closing `}` is on line 297. The `parallel` block's closing `}` is on line 298."
- Actual live file:
  - `stage('Dashboard') {` at line 273 ✓
  - Closing `}` of Dashboard stage at line 297 ✓
  - Closing `}` of parallel block at line 298 ✓
- **Correct:** insertion point is between lines 297 and 298 ✓

**Stagger formula check (stage 13 = sleep 60s, stage 14 = sleep 65s):** PASS  
- Plan task specifies: `sleep(time: 65, unit: 'SECONDS')` for the new stage
- Verify block (line 127) expects: `sleep(time: 65, unit: 'SECONDS')` as the 14th entry
- Formula: stage n = `(n-1)*5` seconds
  - Stage 13 (Dashboard): `(13-1)*5 = 60` ✓
  - Stage 14 (new): `(14-1)*5 = 65` ✓
- Live file confirms stage 13 sleep is 60s at line 276 ✓
- **Formula verified correct**

**Stagger preservation check:** PASS  
- Plan's verify block (lines 107–127) expects exactly 14 `sleep(time: ` entries with offsets 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65
- Current live file has exactly 13 entries (0–60) at lines 72, 91, 110, 129, 148, 167, 186, 201, 216, 231, 246, 261, 276
- New plan adds one entry at 65s, bringing total to 14 ✓
- No existing entries are to be modified (plan forbids this at lines 91–92) ✓

**Indentation check:** PASS  
- Plan specifies exact indentation (lines 53–59):
  - Stage declaration: 16 spaces (4 levels)
  - `agent { label 'docker' }`: 20 spaces (5 levels)
  - `steps {`: 20 spaces
  - Sleep/sh calls: 24 spaces (6 levels)
  - Triple-quoted block body: 28 spaces
- Live file Memory stage (lines 243–256) uses identical indentation ✓
- Plan's literal snippet (lines 64–75) has matching indentation ✓

**No-coverage-flag awk guardrail:** PASS  
- Plan's verify command (line 131):
  ```bash
  awk "/stage\('TaskScheduler Distributed'\)/,/^                \}$/" Jenkinsfile | grep -cE "(XPlat Code Coverage|coverage.runsettings|--collect|--results-directory|coverage/int-|stash )"
  ```
- Awk range pattern: `/stage\('TaskScheduler Distributed'\)/` to `/^                \}$/ (closing brace at 16-space indent + literal `\}`)
  - Start anchor: matches opening of stage declaration ✓
  - End anchor: matches closing `}` at column 17 (16 spaces + `}`) ✓
  - Range correctly bounds the new stage body ✓
- Grep pattern: `(XPlat Code Coverage|coverage.runsettings|--collect|--results-directory|coverage/int-|stash )` covers all coverage-related tokens ✓
- Expected result: 0 matches (coverage flags must not appear) ✓
- **Guardrail is correct and precise**

**Release/CI=true belt-and-braces check:** PASS  
- Plan's verify command (line 135):
  ```bash
  awk "/stage\('TaskScheduler Distributed'\)/,/^                \}$/" Jenkinsfile | grep -cE "(-c Release|-p:CI=true)"
  ```
- Same awk range pattern (correct) ✓
- Grep pattern checks for `-c Release` or `-p:CI=true` ✓
- Expected result: 0 matches ✓
- This confirms the CONTEXT-4 correction note is enforced ✓

**Beacon-skip guardrail:** PASS  
- Plan's verify command (line 139):
  ```bash
  awk "/stage\('TaskScheduler Distributed'\)/,/^                \}$/" Jenkinsfile | grep -cE "(--network=host|BEACON_SKIP|TestCategory|FullyQualifiedName!~NodeDiscovery)"
  ```
- Awk range pattern (correct) ✓
- Grep pattern covers decision #1 requirements (no network=host, no env var skip, no TestCategory, no NodeDiscovery filter) ✓
- Expected result: 0 matches ✓

**Coverage Report stage unchanged:** PASS  
- Plan's verify command (line 143):
  ```bash
  grep -c "^                unstash " Jenkinsfile
  ```
- Checks for unstash count remaining at 13 (unchanged from before) ✓
- Current live file has 13 unstash lines (304–317) ✓

**Dashboard stage sleep unchanged:** PASS  
- Plan's verify command (line 147):
  ```bash
  grep -B1 "stage('Dashboard')" Jenkinsfile >/dev/null && grep -A3 "stage('Dashboard')" Jenkinsfile | grep "sleep(time: 60"
  ```
- Finds Dashboard stage and checks that its sleep is 60s ✓
- Current live file confirms: line 276 is `sleep(time: 60, unit: 'SECONDS')` within the Dashboard stage ✓

**Literal snippet accuracy:** PASS  
- Plan's literal block (lines 64–75) is exact groovy syntax:
  ```groovy
  stage('TaskScheduler Distributed') {
      agent { label 'docker' }
      steps {
          sleep(time: 65, unit: 'SECONDS')
          sh 'dotnet build "Source/DotNetWorkQueue.sln" -c Debug'
          sh '''
              dotnet test "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.Integration.Tests.csproj" \
                  -f net10.0 -c Debug
          '''
      }
  }
  ```
- Matches existing stage structure (Memory stage at lines 243–256) ✓
- No coverage flags, no stash, no withCredentials ✓
- Uses `-c Debug` (not Release), no `-p:CI=true` ✓
- Uses `-f net10.0` (matches other integration stages) ✓
- No `--filter`, no `--retry-failed-tests` (matches Memory/SQLite/LiteDB pattern) ✓

**Verify command runnability:** PASS  
All 9 verify commands in PLAN-1.2 `<verify>` block (lines 99–149) are executable:

1. `grep -c "stage('TaskScheduler Distributed')" Jenkinsfile` ✓
2. `grep -c "TaskScheduling.Distributed.TaskScheduler.Integration.Tests" Jenkinsfile` ✓
3. `grep -c "sleep(time: " Jenkinsfile` ✓
4. `grep -n "sleep(time: " Jenkinsfile` — returns 13 entries (will be 14 after plan execution) ✓
5. Awk + grep for coverage flags (lines 131) ✓
6. Awk + grep for Release/CI=true (line 135) ✓
7. Awk + grep for beacon-skip (line 139) ✓
8. `grep -c "^                unstash " Jenkinsfile` ✓
9. `grep -B1 "stage('Dashboard')"` + grep for sleep 60 (line 147) ✓

All commands are dry-runnable and will work after plan execution.

---

## Cross-Plan Integrity

**Forward References:** NONE
- PLAN-1.1 produces a step in ci.yml; PLAN-1.2 produces a stage in Jenkinsfile
- No task in PLAN-1.1 reads the output of PLAN-1.2 or vice versa
- GitHub Actions runs on ubuntu-latest; Jenkins runs on Docker agent — separate CI surfaces
- **Result:** Plans are truly independent; parallel execution is safe ✓

**File Conflicts:** NONE
- PLAN-1.1: `.github/workflows/ci.yml`
- PLAN-1.2: `Jenkinsfile`
- Different files, no overlap ✓

**Hidden Ordering Dependencies:** NONE
- Both plans append to their respective files (insertion points are disjoint)
- No plan modifies a resource that the other consumes
- Wave 1 parallel execution is valid ✓

---

## Complexity Flags

**File count touched:** 2 total
- PLAN-1.1: 1 file
- PLAN-1.2: 1 file
- **Below threshold:** Both plans touch ≤1 file each ✓

**Line count modified:**
- PLAN-1.1: +2 lines (`- name:` + `run: ...`) + 1 blank line = 3 lines added to ci.yml
- PLAN-1.2: +12 lines (stage body with leading blank line) added to Jenkinsfile
- **Below threshold:** Single edits, no refactoring ✓

**Directory count:** 2 total
- `/.github/workflows/` (ci.yml)
- `/` (Jenkinsfile at root)
- **Below threshold:** Span ≤3 directories ✓

**Groovy syntax validity:** PLAN-1.2's snippet is valid Groovy:
- Stage declaration, agent block, steps block, sleep/sh calls all standard Jenkins Groovy ✓
- No syntax errors detected by visual inspection ✓
- Indentation matches live file exactly ✓

---

## Identified Issues and Observations

### Issue 1: RESEARCH.md assumes Memory integration tests exist in ci.yml (non-blocking)
**Severity:** LOW (INFO)  
**Finding:** RESEARCH.md decision #5 says "add to the existing unit+integration job that runs Memory-transport-only integration tests." RESEARCH.md itself discovered (line 31) that NO Memory integration tests currently exist in ci.yml — only unit tests. The Phase 3 project will be the FIRST integration test in ci.yml.

**Impact:** Does not affect plan correctness. PLAN-1.1's action correctly appends the new step after the last unit test step. The done block doesn't assume a prior Memory integration test exists; it just checks that the new step was added.

**Recommendation:** Clarify the RESEARCH.md assumption for future readers, but no change to plan needed.

---

### Issue 2: docs/jenkins-setup.md will be stale after Phase 4 ships (non-blocking)
**Severity:** LOW (DOC HYGIENE)  
**Finding:** RESEARCH.md notes (line 157) that `docs/jenkins-setup.md` says "13 parallel stages" at lines 66 and 193–196. After Phase 4, this will be 14. CONTEXT-4 marks doc updates as out-of-scope.

**Impact:** None on Phase 4 execution. Follow-up issue should be opened to update the doc.

**Recommendation:** Flagged in RESEARCH.md already; no change to plan needed.

---

### Issue 3: CONTEXT-4 decision #3 was corrected post-research, but change is clear (non-blocking)
**Severity:** LOW (CLARIFICATION)  
**Finding:** CONTEXT-4 line 42–46 contains a **corrected note** (dated 2026-04-15) stating that an earlier draft specified `-c Release -p:CI=true`, but that is wrong. The correction says use `-c Debug` to match the 13 existing stages. PLAN-1.2 task instruction (line 87) explicitly enforces this, and the literal block uses `-c Debug`.

**Impact:** None. The correction is clearly stated and both CONTEXT-4 and PLAN-1.2 align on `-c Debug`.

**Recommendation:** No action. Plans are correct.

---

## Overall Verdict: **READY**

### Summary

**PLAN-1.1 (ci.yml):**
- ✓ File exists, line references accurate
- ✓ Insertion point correct (after line 65)
- ✓ Snippet matches existing indentation and style
- ✓ All verify commands are runnable and correct
- ✓ YAML will parse cleanly

**PLAN-1.2 (Jenkinsfile):**
- ✓ File exists, line references accurate
- ✓ Insertion point correct (between lines 297–298)
- ✓ Stagger formula verified (stage 14 = 65s)
- ✓ Indentation matches live file exactly
- ✓ All awk/grep guardrails are precise and correct
- ✓ All verify commands are runnable and correct
- ✓ Literal stage body is syntactically valid Groovy

**Cross-Plan:**
- ✓ No forward references or file conflicts
- ✓ No hidden ordering dependencies
- ✓ Safe for parallel execution

**Complexity:**
- ✓ ≤1 file per plan
- ✓ ≤12 lines added per plan
- ✓ ≤3 directories touched

**Known Risks (pre-approved in CONTEXT-4):**
- UDP multicast may fail on Docker bridge → optimistic approach, feature-branch run will surface, follow-up PR for skip mechanism approved
- CONTEXT-4 says `-c Release` but correction updated to `-c Debug` → both CONTEXT-4 and PLAN-1.2 now aligned

### Recommendation: **READY FOR BUILDER**

Both plans are structurally sound, feasible, and verifiable. No blocking issues found. Execute both tasks in parallel (Wave 1), then verify using the exact `<verify>` commands and `<done>` criteria. Push to feature branch for CI validation (GitHub Actions + Jenkins) before merging to master.
