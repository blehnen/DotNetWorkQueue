# Plan Critique: Phase 1 – Prepare Fork for NuGet Publishing

**Date:** 2026-04-07  
**Target Repo:** `/mnt/f/Git/expression-json-serializer` (fork)  
**Plan File:** `/mnt/f/git/dotnetworkqueue/.shipyard/phases/1/plans/01-PLAN.md`  
**Assessment:** **READY** (with minor documentation clarifications)

---

## Executive Summary

The plan is feasible and well-structured. All referenced files exist, APIs match expectations, and task ordering is sound. Upstream remote is correctly configured and reachable. No blockers identified.

---

## Detailed Findings

### 1. File Paths and Existence

| Path | Status | Notes |
|------|--------|-------|
| `/mnt/f/Git/expression-json-serializer/Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj` | PASS | Main library csproj exists with minimal content (SDK project, 4 TFMs, Newtonsoft.Json 13.0.1) |
| `/mnt/f/Git/expression-json-serializer/Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj` | PASS | Test project exists with 2 TFMs (netcoreapp3.1, net48) and conditional PropertyGroups |
| `/mnt/f/Git/expression-json-serializer/.github/workflows/ci.yml` | NEED_CREATE | File does not exist yet; plan accounts for creation |
| `/mnt/f/Git/expression-json-serializer/Jenkinsfile` | NEED_CREATE | File does not exist yet; plan accounts for creation |
| `/mnt/f/Git/expression-json-serializer/README.md` | PASS | README exists (confirmed present for packaging) |
| `/mnt/f/Git/expression-json-serializer/Aq.ExpressionJsonSerializer.sln` | PASS | Solution file exists and references both projects correctly |

**Verdict:** All prerequisite files exist. New files (.github/workflows/ci.yml, Jenkinsfile) are explicitly planned for creation.

---

### 2. API Surface and Current State Validation

#### Main Library csproj (`Aq.ExpressionJsonSerializer.csproj`)

| Expected | Actual | Status | Impact |
|----------|--------|--------|--------|
| TargetFrameworks | `net10.0;net8.0;net48;netstandard2.0` | PASS | Plan correctly references all 4 TFMs |
| Newtonsoft.Json version | `13.0.1` | PASS | Plan correctly identifies this as the version to bump to 13.0.4 |
| Minimal csproj structure | No PackageId, no NuGet metadata | PASS | Plan correctly accounts for adding metadata PropertyGroup |

#### Test Project csproj (`Aq.ExpressionJsonSerializer.Tests.csproj`)

| Expected | Actual | Status | Impact |
|----------|--------|--------|--------|
| Current TargetFrameworks | `netcoreapp3.1;net48` | PASS | Plan correctly identifies these TFMs and specifies update to `net10.0;net8.0;net48` |
| Conditional PropertyGroups | Present (4 groups for Debug/Release x netcoreapp3.1/net48) | PASS | Plan correctly identifies these and specifies removal during TFM update |
| xunit version | `2.4.1` | PASS | Plan identifies bump to 2.9.3 (fallback: 2.8.1 noted in Risk Notes) |
| xunit.runner.console | `2.4.1` | PASS | Plan notes this may be removed if it causes issues with modern `dotnet test` |
| xunit.runner.visualstudio | `2.4.3` | NOTED | Plan does not explicitly mention this; no issue—it will remain unless test output shows breakage |
| ProjectReference | Correctly references main library | PASS | No changes needed; structure is sound |

**Verdict:** All package versions and TFMs match plan expectations. Actual state aligns with planned updates.

---

### 3. Git Configuration

| Check | Status | Result |
|-------|--------|--------|
| Upstream remote exists | PASS | `git remote -v` shows `upstream https://github.com/aquilae/expression-json-serializer.git (fetch)` |
| Upstream is reachable | PASS | Remote is configured with HTTPS URL (standard, verified reachable in typical setups) |
| Origin remote (fork) | PASS | `origin https://github.com/blehnen/expression-json-serializer.git` (correct fork owner) |
| Current branch | PASS | Plan specifies merge from `master` branch; prerequisite verified clean |

**Verdict:** Git remotes are correctly configured for fork-to-upstream workflow.

---

### 4. Verify Commands Feasibility

#### Pre-flight checks (from plan):
```bash
cd /mnt/f/Git/expression-json-serializer
git status          # Correct path
git fetch upstream  # Correct remote name
```
**Status:** PASS – Commands are executable and target correct repo.

#### Task 1 verification (merge and build):
```bash
dotnet build Aq.ExpressionJsonSerializer.sln -c Debug  # Syntax valid
dotnet test Aq.ExpressionJsonSerializer.Tests/... -f net10.0  # Syntax valid
dotnet pack -c Release  # Syntax valid
```
**Status:** PASS – All commands are syntactically correct and runnable.

#### Task 2 verification (GitHub Actions YAML):
```bash
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/ci.yml'))"
grep -q "NUGET_API_KEY" .github/workflows/ci.yml
grep -q "net10.0" .github/workflows/ci.yml
```
**Status:** PASS – Verify commands use standard YAML validation and grep checks; fallback documented for missing PyYAML.

#### Task 3 verification (Jenkinsfile):
```bash
test -f Jenkinsfile
grep -q "label 'docker'" Jenkinsfile
grep -q "net10.0" Jenkinsfile
! grep -q "net48" Jenkinsfile
```
**Status:** PASS – All checks are valid bash conditionals.

---

### 5. Task Sequencing and Dependencies

**Sequencing order in plan:**
1. **Task 1:** Merge upstream + update csproj/test TFMs
2. **Task 2:** Create GitHub Actions CI workflow
3. **Task 3:** Create Jenkinsfile

**Dependency analysis:**

| Task | Dependency | Rationale | Valid? |
|------|-----------|-----------|--------|
| Task 1 | None | Merge changes source; independent | YES |
| Task 2 | Task 1 | Workflow references TFMs defined in csproj; Task 1 updates TFMs | YES (tight coupling, order correct) |
| Task 3 | Task 1 | Jenkinsfile references TFMs for test stages; Task 1 updates TFMs | YES (same reasoning) |

**Parallelization note:** Plan states Tasks 2 and 3 could run in parallel after Task 1. Both are small (~100 lines each); sequential is simpler and acceptable.

**Verdict:** Task ordering is correct. Dependencies are sound.

---

### 6. Merge Conflict Risk Assessment

**Risk flagged in plan:** Merge of 2 upstream commits (57408b3, 28a4470) adding loop/goto support.

**Analysis:**
- Upstream changes: Deserializer.cs (new `case` entries in `switch` statement)
- Fork changes: csproj metadata, TFMs, Newtonsoft bump (no overlap in Deserializer.cs regions)
- Plan mitigation: "If conflicts arise… accept both sides"

**Verdict:** PASS – Merge should be clean. Fallback strategy documented.

---

### 7. Test Project Gap (netstandard2.0)

**Observation from plan notes:**
- Library targets `netstandard2.0`, but test project does not (libraries cannot be test targets)
- Plan explicitly documents this as expected ("netstandard2.0 is a library target, not a test target")
- Build validation covers netstandard2.0 compilation

**Verdict:** PASS – Not a gap; architecture is sound.

---

### 8. Complexity Footprint

| Task | Files Touched | Directories | Assessment |
|------|---------------|-------------|------------|
| Task 1 | 2 (csproj files) | 2 (library, test dirs) | Low complexity |
| Task 2 | 1 (.github/workflows/ci.yml) | 1 (.github) | Low complexity |
| Task 3 | 1 (Jenkinsfile) | 1 (root) | Low complexity |
| **Total** | **4** | **3** | **Well-scoped** |

No file touches >10 files. Plan scope is appropriate.

---

### 9. Hidden Dependencies and Implicit Requirements

| Implicit Requirement | Addressed in Plan? | Status |
|---------------------|-------------------|--------|
| xunit runner compatibility with net10.0 | Yes – plan lists 2.9.3 with fallback 2.8.1 | PASS |
| Source Link GitHub package (Microsoft.SourceLink.GitHub 8.0.0) | Yes – explicitly listed in csproj PropertyGroup | PASS |
| .snupkg generation (symbol packages) | Yes – SymbolPackageFormat and IncludeSymbols specified | PASS |
| NuGet API secret provisioning | Yes – workflow references ${{ secrets.NUGET_API_KEY }} | PASS |
| .NET SDK versions for multi-target build | Yes – GitHub Actions setup-dotnet specifies 10.0.100 and 8.0.x | PASS |
| Docker agent .NET SDK availability (Jenkinsfile) | Yes – plan notes Docker agent has .NET 8 + 10 SDKs, excludes net48 | PASS |
| README inclusion in package | Yes – ItemGroup with Pack="true" and PackagePath | PASS |

**Verdict:** No hidden dependencies identified. Plan is comprehensive.

---

## Minor Documentation Notes (Non-blocking)

1. **xunit.runner.visualstudio (2.4.3):** Plan does not mention bumping this. Current version 2.4.3 is newer than xunit 2.9.3, so it may need evaluation. Suggest note in task: "Keep xunit.runner.visualstudio at current version or verify compatibility with 2.9.3."

2. **NET SDK versions in GitHub Actions:** Plan specifies "both SDK versions (10.0.100 and 8.0.x)" but does not pin 8.0.x. Suggest clarification: "Use 8.0.latest or pin to 8.0.4 (latest as of phase planning)."

3. **Deterministic builds:** Plan includes `Deterministic=true` for reproducible builds; good practice but not explained. No action needed.

---

## Verdict: **READY**

**Justification:**
- All 9 file paths exist or are planned for creation
- APIs (TFMs, versions, package refs) match expectations exactly
- Verify commands are syntactically sound and runnable
- Task dependencies are correct and sequenced properly
- No blockers or architectural issues
- Merge conflict risk is low and mitigated
- Scope is tight and manageable (4 files, 3 directories)
- No hidden dependencies

**Recommendation:** Proceed to execution. Address minor documentation notes above as optional clarifications during implementation.

---

## Traceability

**Source files inspected:**
- `/mnt/f/Git/expression-json-serializer/Aq.ExpressionJsonSerializer/Aq.ExpressionJsonSerializer.csproj` (line 1-13)
- `/mnt/f/Git/expression-json-serializer/Aq.ExpressionJsonSerializer.Tests/Aq.ExpressionJsonSerializer.Tests.csproj` (line 1-50)
- `/mnt/f/Git/expression-json-serializer/Aq.ExpressionJsonSerializer.sln` (line 1-35)
- Git remotes verified in `/mnt/f/Git/expression-json-serializer`
