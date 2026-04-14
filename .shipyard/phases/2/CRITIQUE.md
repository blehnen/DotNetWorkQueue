# Plan Critique — Phase 2 (Mode B Feasibility Stress Test)

**Phase:** taskscheduler-nuget-0.4.0
**Date:** 2026-04-14
**Sibling repo under test:** `/mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler`
**Scope:** Verify every file path, line number, command, and string-match the plans assume against the CURRENT state of the sibling repo.

---

## 1. File paths and line numbers exist as assumed

| Plan | Assumption | Ground truth | Status |
|---|---|---|---|
| PLAN-3.1 Task 1 | `Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj` line 9 = `<Version>0.3.0</Version>` | Confirmed. File exists, line 9 is `    <Version>0.3.0</Version>`. | PASS |
| PLAN-3.1 Task 1 | `README.md` line 30 contains `<PackageReference Include="..." Version="0.3.0" />` | Confirmed. Line 30 reads `<PackageReference Include="DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler" Version="0.3.0" />`. Exactly one occurrence of `Version="0.3.0"` in README.md. | PASS |
| PLAN-3.1 Task 2 | `CHANGELOG.md` line 3 = `### 0.3.0 2026-04-10`, and the insertion point is between line 2 (blank) and line 3 | Confirmed. Line 1 `# Changelog`, line 2 blank, line 3 `### 0.3.0 2026-04-10`. Insertion point correct. | PASS |
| PLAN-3.1 Task 3 | `Source/ITaskSchedulerJobCountSync.cs` line 55 = `void Start();` with `<summary>` on 53 and `</summary>` on 54 | Confirmed on checked-out branch `phase-1-lock-fix`. Lines 52–56 read: `/// <summary>\n/// Starts this instance.\n/// </summary>\nvoid Start();\n}` (with leading indentation). | PASS |
| PLAN-3.1 Task 3 | `Source/TaskSchedulerJobCountSync.cs` line 106 = `public void Start()` with `<summary>` block above | Confirmed via `git show phase-1-lock-fix:Source/TaskSchedulerJobCountSync.cs`. Lines 103–106 read `/// <summary>\n/// Starts this instance.\n/// </summary>\npublic void Start()`. Note: file state on master is different (pre-refactor), but the plan correctly depends on PLAN-2.1 landing the merge first, so by the time PLAN-3.1 runs, master will have this exact shape. | PASS |
| PLAN-2.2 | `.github/workflows/ci.yml` publish job exists with two separate `dotnet nuget push` steps using `Source/bin/Release/*.nupkg` and `Source/bin/Release/*.snupkg` | Confirmed. Publish job starts at `publish:` with `if: startsWith(github.ref, 'refs/tags/v')`. Two push steps at the end of the file, both using `${{ secrets.NUGET_API_KEY }}`. Pack step: `dotnet pack ... -c Release --no-restore` (no `-p:CI=true`, no `-o deploy`). Plan-2.2's edits will apply cleanly. | PASS |
| PLAN-5.1 Task 1 | `v0.3.0` tag format is derivable | Confirmed annotated + unsigned: `git for-each-ref` returns `tag v0.3.0 Brian Lehnen Release 0.3.0 - first public NuGet release`. Taggername is populated (`Brian Lehnen`), confirming annotated. Plan's default `git tag -a v0.4.0 -m "..."` branch matches. | PASS |
| PLAN-1.1 Task 2 | Release commit `a392e62` exists with a usable subject line | Confirmed. `git show a392e62 --stat` returns commit `a392e62e9f45641a125bec0f7ffee99b243833a0`, author Brian Lehnen, subject `Release 0.3.0: modernization + first NuGet release`, with a multi-line body. PLAN-3.1 Task 3's default subject template `Release 0.4.0: lock-contention fix for TaskSchedulerJobCountSync` mirrors the `Release X.Y.Z: <topic>` convention exactly. | PASS |

---

## 2. API-surface / find-and-replace safety

### PLAN-3.1 Task 1 csproj edit
The plan says "Change line 9 `<Version>0.3.0</Version>` -> `<Version>0.4.0</Version>`." Confirmed: line 9 is the only line in the csproj matching `<Version>`. No ambiguity. **Safe.**

### PLAN-3.1 Task 1 README edit
The plan says find `Version="0.3.0"` and replace with `Version="0.4.0"`. Confirmed: exactly one match in README.md at line 30 inside the install-snippet code fence. The `.shipyard/` metadata is excluded by the plan's grep filter. **Safe.**

### PLAN-3.1 Task 2 CHANGELOG insertion
The plan inserts the new block between line 2 (blank after `# Changelog`) and line 3 (`### 0.3.0 2026-04-10`). Confirmed the current file structure matches exactly. The plan specifies `one blank line above the heading, one blank line below the final bullet`, matching existing visual spacing. The verbatim bullet content is a byte-for-byte copy of DOCUMENTATION-1.md lines 22–28. **Safe.**

### PLAN-3.1 Task 3 `<remarks>` insertion — PASS with one nit
The plan's target find-text is a 4-line `<summary>` block. Confirmed the current text (on `phase-1-lock-fix`, which will be on master post PLAN-2.1) is byte-identical to what the plan quotes. The Edit tool's find-and-replace will match.

**Nit:** The plan uses `<see cref="Start"/>` inside `<remarks>`. Under C# XML doc rules with `TreatWarningsAsErrors=true` + `<GenerateDocumentationFile>true</GenerateDocumentationFile>` (both are on for this csproj per research §1.3), a `cref` with no parameter list resolves to the method when unambiguous — but if a future overload is added, this becomes CS0419. For the current source (a single `Start()` with no overloads), this is safe. Note for future: `<see cref="Start()"/>` with parens is more robust. **Current: PASS.** Nit filed only as future-proofing.

### PLAN-2.2 workflow edit
The plan replaces two push steps with one, and modifies the Pack step to add `-p:CI=true -o deploy`. Confirmed the workflow file's current shape matches the find-text. Plan writes valid YAML. Plan's verify step includes a `python3 -c 'import yaml; yaml.safe_load(...)'` parse check, which is excellent. **Safe.**

---

## 3. Verify commands runnable

| Command | Reachability | Status |
|---|---|---|
| `dotnet build "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.sln" -c Release -p:CI=true` | Confirmed .sln exists (research §5.1). | PASS |
| `dotnet pack "Source/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler.csproj" -c Release -p:CI=true -o deploy --no-build` | Confirmed .csproj exists; `<IncludeSymbols>true</IncludeSymbols>` + `<SymbolPackageFormat>snupkg</SymbolPackageFormat>` on lines 24–25 guarantee both `.nupkg` and `.snupkg` are emitted. | PASS |
| `rm -rf Source/bin Source/obj deploy` | Standard. | PASS |
| `git merge --no-ff phase-1-lock-fix -m "Merge phase-1-lock-fix for 0.4.0 release"` | Runnable. Phase 1 branch exists (`git branch -a` confirmed `phase-1-lock-fix` local + `origin/master`). | PASS |
| `git tag -a v0.4.0 -m "..."` | Runnable. `v0.3.0` exists as annotated tag; format matches. | PASS |
| `git push origin v0.4.0` | Cannot dry-run; irreversible. Plan correctly gates this behind user confirmation in PLAN-5.1 Task 2. | PASS |
| `unzip -l deploy/*.0.4.0.nupkg` (PLAN-4.1 Task 2) | Requires `unzip` installed on the local host. Not verified, but standard on WSL/Linux. Note: PowerShell-only Windows environments don't ship `unzip` — flag for the builder. | PASS-with-note |
| `python3 -c 'import yaml; yaml.safe_load(...)'` (PLAN-2.2 verify) | Requires `python3` + `pyyaml`. WSL usually has python3; `pyyaml` may need `pip install`. Not blocking — optional verification. | PASS |

---

## 4. Forward references and hidden dependencies

**Wave 2 intra-wave risk.** PLAN-2.1 and PLAN-2.2 both live in Wave 2. PLAN-2.2's body says "parallelizable with PLAN-2.1 — disjoint branches", but the body ALSO says "this task STACKS on top of the merge commit" (Task 1 action). These are mutually inconsistent. In practice the builder MUST run PLAN-2.1 first (otherwise there's no merge commit to stack on), so PLAN-2.2 is really a serial Wave 2b. **Recommended fix:** change PLAN-2.2 front-matter dependency from `[1.1]` to `[1.1, 2.1]`. Non-blocking because runtime behavior is forced by `git`, but the plan text should not lie.

**Wave 3 → Wave 4 dependency.** PLAN-4.1 verifies the release commit via `git log -1 --format='%h %s'`. Depends on PLAN-3.1 having pushed master. Explicit in front-matter (`dependencies: [3.1]`). OK.

**Wave 5 manual-checklist dependency.** PLAN-5.1 Task 3 "nuget.org page loads and shows 0.4.0" depends on nuget.org indexing, which can take 5–15 minutes. Plan explicitly notes this and tells the user to retry. OK.

**CI race in Wave 3 push.** PLAN-3.1 Task 3 says `git push origin master` will push BOTH the PLAN-2.2 workflow-fix commit AND the release commit together. This is correct and intentional (avoids leaving master momentarily broken for CI). Note: the CI build job on that push will run on a pre-tag master — it will not fire `publish` because tag is not yet created. **Correct.**

---

## 5. Sibling repo current state — blocker check

Ran against live sibling repo at /mnt/f/Git/DotNetWorkQueue.TaskScheduling.Distributed.TaskScheduler:

- **Current branch: `phase-1-lock-fix` (NOT `master`).** PLAN-2.1 Task 1 does `git checkout master`, so this is handled.
- **Working tree is dirty:** `git status --short` shows ` M .gitignore`. PLAN-2.1 Task 1's first check is `git status --porcelain` expecting empty output. **This will STOP PLAN-2.1 on startup.** The user must decide what to do with the `.gitignore` modification (commit on phase-1-lock-fix, stash, or revert) before Wave 2 can begin. **Blocker for automated execution; not a plan defect. Flag this upfront.**
- `Source/bin/Debug/net472` / `net48` stale directories: not verified this pass, but research §5.5 + PLAN-4.1 Task 1's aggressive clean explicitly handles them. OK.

---

## 6. Complexity flags

- **PLAN-3.1 touches 5 files in one commit.** All cohesive (the release commit). OK.
- **Cross-repo plan (DNQ authoring repo + sibling target repo).** Plans correctly use absolute paths for the sibling repo throughout. OK.
- **No plan touches >5 files.** No plan crosses >2 directories in the sibling repo (`Source/` and repo root). Well below the 10-file/3-directory complexity threshold.

---

## 7. Top issues to see before execution

1. **Sibling repo is currently on `phase-1-lock-fix` branch with a dirty `.gitignore`.** PLAN-2.1 Task 1's `git status --porcelain` halt will fire on start. Builder will immediately pause and ask the user. Pre-resolve this: either commit the `.gitignore` change on phase-1-lock-fix (so it gets merged into master), stash it, or revert it. **Not a plan defect — but if not pre-resolved, Wave 2 stalls immediately.**
2. **PLAN-2.2 front-matter says `dependencies: [1.1]` but PLAN-2.2's body correctly serializes on top of PLAN-2.1's merge commit.** Cosmetic inconsistency. Recommended fix: `dependencies: [1.1, 2.1]`.
3. **Optional: confirm Jenkinsfile does NOT also publish on `v*` tag push** before the `v0.4.0` tag is pushed (research §8 flag #6). Suggest adding a single `grep` line to PLAN-1.1 Task 3.
4. **Optional: PLAN-4.1 `unzip` dependency.** On a Windows-native shell without WSL, `unzip` is not installed. If the user runs the Phase 2 builder from PowerShell, this step needs substitution (`Expand-Archive` or `tar -tf`). From WSL (the documented dev environment per CLAUDE.md), `unzip` is standard.

---

## 8. Verdict

**READY-WITH-CAUTIONS**

The 6 plans are collectively sound, fully cover all Phase 2 requirements, honor every CONTEXT-2 locked decision (including the two revised ones), and have concrete testable verify blocks. File paths, line numbers, and string-match targets have been verified against the live sibling repo and all match. No blockers; no plan defects serious enough to require a REVISE.

The single item that WILL affect the builder's first minute of execution is the dirty `.gitignore` on the sibling repo — this should be resolved by the user (or the architect amending PLAN-2.1 Task 1 to pre-stash/pre-revert it) before builder kickoff. Everything else on the gap list is a nice-to-have clarification.

**Recommendation:** proceed to task scaffolding + builder kickoff, BUT first either (a) resolve the sibling repo working-tree dirty state and switch to master manually, or (b) amend PLAN-2.1 Task 1 to explicitly handle the `.gitignore` modification and the branch switch. Option (a) is simpler and removes builder-time friction.
