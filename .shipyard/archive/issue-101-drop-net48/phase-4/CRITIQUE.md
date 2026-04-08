# Plan Critique: Phase 4
**Date:** 2026-04-07
**Type:** feasibility stress test

---

## PLAN-1.1 -- Code Cleanup: Unstaged Sweep + ISSUE-021 + ISSUE-022 + ISSUE-023

### File Paths Exist
All 7 ISSUE-021 files exist (6-9 lines each). All 8 ISSUE-022 files exist. Memory csproj exists. All 10 unstaged files confirmed in `git status --porcelain`. **PASS.**

### API Surface Matches
- Shared `JobSchedulerTests.Run()` signature has `bool dynamic` at line 11. Confirmed.
- `if (!dynamic)` guard at line 32 with body lines 33-38. Confirmed.
- All 6 transport caller DataRow/parameter patterns match plan descriptions exactly. Confirmed for PostgreSQL (`DataRow(true, false), DataRow(true, true)` with `bool interceptors, bool dynamic`), LiteDb (`DataRow(false), DataRow(true)` with `bool dynamic`), SqlServer (`DataRow(true, false)` with `bool interceptors, bool dynamic`), SQLite (`DataRow(false, false), DataRow(false, true)` with `bool dynamic, bool inMemoryDb`), Redis (`DataRow(false)` with `bool dynamic`), Memory (`DataRow(false)` with `bool dynamic`). **PASS.**

### Verification Commands Runnable
- `git log -1 --stat | head -15` -- runnable. **PASS.**
- `ls ... 2>&1 | grep -c "No such file"` -- runnable. **PASS.**
- `dotnet build Source/DotNetWorkQueue.sln -c Debug --verbosity quiet 2>&1 | tail -5` -- runnable. **PASS.**

### Forward References
Task 1 (commit) and task 2 (delete) are independent. Task 3 (edit) is independent of tasks 1-2 at the file level. Plan correctly notes "Tasks 1 and 2 are independent. Task 3 should be last." **PASS.**

### Complexity Flags
Task 3 touches 8 files across 7 directories. Each edit is small (parameter removal) but the variety of per-transport signatures means 6 slightly different edits. **CAUTION -- medium complexity, but well-documented in builder notes.**

### Feasibility Issues
1. **SQLite parameter order.** Plan says SQLite has `(bool dynamic, bool inMemoryDb)` -- dynamic is FIRST, unlike all others where it is LAST. The plan correctly notes this: "`DataRow(false, false)` becomes `[DataRow(false)]`, `DataRow(false, true)` becomes `[DataRow(true)]`" -- after removing the first `dynamic` param, the remaining `inMemoryDb` values shift position. Verified against actual code: lines 14-15 show `DataRow(false, false), DataRow(false, true)` and lines 17-18 show `bool dynamic, bool inMemoryDb`. **Plan is correct.**

### Verdict: **READY**

---

## PLAN-1.2 -- GitHub Actions CI Update

### File Paths Exist
`.github/workflows/ci.yml` exists (63 lines). **PASS.**

### API Surface Matches
- `windows-latest` at line 15. Confirmed.
- `10.0.100` at line 24. Confirmed.
- 8 test steps with `-f net48`. Confirmed (lines 35, 38, 41, 44, 47, 50, 53, 62).
- Dashboard.Api (line 56) and Dashboard.Client (line 59) do NOT have `-f net48`. Confirmed.
- Backslash paths in all `dotnet` commands. Confirmed. **PASS.**

### Verification Commands Runnable
- `grep -c 'net48\|windows-latest\|\\\\' .github/workflows/ci.yml` -- runnable. Note: the quadruple backslash is grep escaping for literal backslash. **PASS.**

### Forward References
None -- single-file, single-task plan. **PASS.**

### Complexity Flags
Single file, 63 lines. Plan suggests full rewrite if easier than 15+ edits. **LOW risk.**

### Feasibility Issues
None found. The plan is thorough and the file is small enough for a complete rewrite.

### Verdict: **READY**

---

## PLAN-1.3 -- Documentation Updates (README + CLAUDE.md)

### File Paths Exist
`README.md` and `CLAUDE.md` both exist. **PASS.**

### API Surface Matches
- README line 8 targets: `Targets .NET 4.8, .NET 8.0, .NET 10.0, and .NET Standard 2.0.` -- Confirmed.
- README line 12 dynamic mention: `Queue / process LINQ statements (compiled or dynamic, expressed as a string)` -- need to verify.
- README lines 61-67 "Differences Between Versions" section: Confirmed at lines 61-67.
- README line 85-86 producer note with dynamic reference: Confirmed at line 85.
- README lines 87-95 producer subsection with dynamic casting: Confirmed at lines 87-95.
- README lines 96-107 dynamic arguments section: Confirmed at lines 99-107.
- README lines 109-114 consumer with AppDomain.AssemblyResolve: Confirmed at lines 109-114.
- README lines 116-129 security considerations: Confirmed at lines 116-128 (plan says 14 lines including the `---` at line 130).
- README line 187 JpLabs.DynamicCode: Confirmed at line 187.
- CLAUDE.md line 43 AppMetrics.Tests: Confirmed. Project does NOT exist (correctly being removed).
- CLAUDE.md conventions NETFULL reference: Confirmed in CLAUDE.md. **PASS.**

### Verification Commands Runnable
- `grep -c "dynamic LINQ\|JpLabs\|DynamicCode\|net48\|netstandard2.0\|AppDomain.AssemblyResolve\|application domain sandbox" README.md` -- runnable. **PASS.**
- `grep -c "net48\|netstandard2.0\|NETFULL.*4.8\|AppMetrics.Tests\|DynamicCode\|dynamic LINQ" CLAUDE.md` -- runnable. **PASS.**

### Forward References
None between tasks (README is task 1, CLAUDE.md is task 2, different files).

### Hidden Dependencies
**FOUND:** CLAUDE.md is listed in PLAN-1.1 task 1 (commit as-is). PLAN-1.3 task 2 edits CLAUDE.md. Both are wave 1. If PLAN-1.3 edits CLAUDE.md before PLAN-1.1 commits it, the commit in PLAN-1.1 would capture unintended changes. **Fix: add `"1.1"` to PLAN-1.3 dependencies.**

### Complexity Flags
README task 1 makes 7+ surgical edits to a single file with line-number references. **CAUTION -- line numbers will drift as deletions are applied.** Builder must apply edits bottom-up (highest line numbers first) or use content-based matching rather than line numbers.

### Feasibility Issues
1. **Line drift in README edits.** The plan references lines 61-67 (delete 7 lines), then 85-86, 87-95, 96-107, 109-114, 116-129. After deleting lines 61-67 (7 lines), all subsequent line numbers shift down by 7. The plan does not warn about this. If the builder applies edits in the listed order (top-down), the second edit targeting "line 85" would actually be at line 78. **Recommendation: add builder note to apply bottom-up or use string matching.**

2. **README lines 96-107 description mismatch.** The plan labels this section "Dynamic Arguments" and shows content starting with "Dynamic arguments like `Guid` and `int`...". The actual content at lines 99-107 starts with "When passing value types, you will need to parse them inline." The description is close enough but the plan's quoted text does not exactly match the file. The builder should match on actual file content, not the plan's paraphrase.

3. **Security section line count.** Plan says "Delete all 14 lines" for lines 116-129, but the actual section runs from line 116 to line 128 (13 lines of content) plus the `---` separator at line 130. The builder should verify the exact boundaries.

### Verdict: **CAUTION** -- The CLAUDE.md ordering dependency and README line-drift risk need attention. Neither is blocking, but the builder must be aware.

---

## PLAN-2.1 -- Version Bump, CHANGELOG, and Final Verification

### File Paths Exist
`Source/DotNetWorkQueue/DotNetWorkQueue.csproj` exists (version 0.9.18 at line 7). `CHANGELOG.md` exists (current top entry is 0.9.18). `.shipyard/ISSUES.md` exists. **PASS.**

### API Surface Matches
- `<Version>0.9.18</Version>` at csproj line 7. Confirmed.
- CHANGELOG top entry is `### 0.9.18`. Confirmed. **PASS.**

### Verification Commands Runnable
- `grep '<Version>0.9.19</Version>' Source/DotNetWorkQueue/DotNetWorkQueue.csproj && head -3 CHANGELOG.md` -- runnable. **PASS.**
- `grep -c "Status.*Open" .shipyard/ISSUES.md` -- runnable. **PASS.**
- Full verification sweep (10 commands) -- all runnable. **PASS.**

### Forward References
Task 3 (verification) correctly depends on all prior tasks and all wave-1 plans. **PASS.**

### Complexity Flags
3 tasks, 3 files. Low complexity. **PASS.**

### Feasibility Issues
1. **Test command config mismatch.** Task 3 action says `dotnet test ... --no-build -c Debug` but the preceding build is `dotnet build ... -c Release`. The `--no-build` flag would look for Debug build artifacts that don't exist. Builder notes acknowledge this, but the task action text is misleading. **Minor -- builder should adjust.**

2. **CHANGELOG date.** Plan hardcodes `2026-04-07` as the release date. If execution slips to another day, the date will be wrong. **Trivial -- builder can adjust.**

### Verdict: **READY**

---

## Cross-Plan Analysis

### File Conflict Matrix (Wave 1)

| File | PLAN-1.1 | PLAN-1.2 | PLAN-1.3 | Conflict? |
|------|----------|----------|----------|-----------|
| CLAUDE.md | Task 1: commit | -- | Task 2: edit | YES (ordering) |
| .github/workflows/ci.yml | -- | Task 1: edit | -- | No |
| README.md | -- | -- | Task 1: edit | No |
| 7 shell files | Task 2: delete | -- | -- | No |
| 8 JobSchedulerTests + csproj | Task 3: edit | -- | -- | No |

### Dependency Graph

```
Wave 1:  PLAN-1.1 ---> PLAN-1.3 (hidden dep on CLAUDE.md)
         PLAN-1.2 (independent)
Wave 2:  PLAN-2.1 (depends on 1.1, 1.2, 1.3)
```

### Task Count Compliance
- PLAN-1.1: 3 tasks (at limit)
- PLAN-1.2: 1 task
- PLAN-1.3: 2 tasks
- PLAN-2.1: 3 tasks (at limit)
- Total: 9 tasks across 4 plans. All within 3-task limit.

---

## Overall Verdict: **CAUTION**

Plans are thorough, well-researched, and cover all phase requirements. Two issues need attention before execution:

1. **CLAUDE.md ordering dependency (must fix):** Add `"1.1"` to PLAN-1.3's dependencies to prevent parallel conflict.
2. **README line-drift awareness (should document):** Add builder note to PLAN-1.3 task 1 to apply edits bottom-up or use content matching.

With these two adjustments, the plans are ready to execute.
