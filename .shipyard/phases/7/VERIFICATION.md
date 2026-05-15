# Verification Report: Phase 7 Plans
**Phase:** 7 — Documentation + Wiki Draft  
**Date:** 2026-05-15  
**Type:** plan-review  
**Verifier:** Senior Verification Engineer

---

## §1. Coverage Matrix

| Phase 7 requirement | Which plan covers it | Status |
|---|---|---|
| XML doc comments on Phases 2–4 public types | PLAN-1.1 Task 3 | PASS |
| `dotnet build -c Release -p:CI=true` of `DotNetWorkQueueNoTests.sln` produces zero XML-doc warnings | PLAN-2.1 Task 1 | PASS |
| `docs/outbox-pattern.md` exists with tutorial + 5 reference sections | PLAN-1.2 Task 1 | PASS |
| README points at the new page (under "High-level features" bullet per CONTEXT-7 Decision 3) | PLAN-1.3 Task 1 | PASS |
| Source Link verification on Release build | PLAN-2.1 Task 2 | PASS |
| PROJECT.md §SC #10 satisfied | PLAN-2.1 Task 3 + PLAN-1.2 Task 1 | PASS |
| ISSUE-032 inline closure (`WarningsNotAsErrors NU1902`) | PLAN-1.1 Task 2 | PASS |
| net8.0 Release XML-doc gate on Transport.RelationalDatabase | PLAN-1.1 Task 1 | PASS |

---

## §2. Structural Verification

### Task Count & Wave Organization ✓

- **Wave 1:** 3 plans (PLAN-1.1, 1.2, 1.3), 5 tasks, parallelizable (no file overlap).
  - PLAN-1.1: 3 tasks (add net8.0 block, add NU1902 exclusion, per-project verification).
  - PLAN-1.2: 1 task (author `docs/outbox-pattern.md`).
  - PLAN-1.3: 1 task (README bullet).
- **Wave 2:** 1 plan (PLAN-2.1), 3 tasks, depends on Wave 1 (PLAN-1.1, 1.2, 1.3).
  - PLAN-2.1: 3 tasks (full-solution Release build, Source Link pack verification, README link resolution).
- **Total:** 4 plans, 8 tasks, 2 waves. Matches PLAN-INDEX.md declared scope.

### YAML Frontmatter Compliance ✓

All four plans carry valid YAML frontmatter with required keys:
- `phase`, `plan`, `wave`, `dependencies`, `must_haves`, `files_touched`, `tdd`, `risk`.
- PLAN-1.1: `dependencies: []` (correct, independent).
- PLAN-1.2: `dependencies: []` (correct, independent).
- PLAN-1.3: `dependencies: []` (correct, independent).
- PLAN-2.1: `dependencies: [1.1, 1.2, 1.3]` (correct, depends on all Wave 1 plans).

### File Disjointness (Wave 1) ✓

- PLAN-1.1 touches: `Transport.RelationalDatabase.csproj`, `Transport.SQLite.csproj`.
- PLAN-1.2 touches: `docs/outbox-pattern.md` (new file, no overlap).
- PLAN-1.3 touches: `README.md` (single bullet insertion, no overlap with others).
- **Result:** No file conflicts within Wave 1. All three can run in parallel.

### Wave 2 Dependencies ✓

- PLAN-2.1 declares `dependencies: [1.1, 1.2, 1.3]`.
- Rationale verified in plan context (PLAN-2.1 Context §):
  - Task 1 (full-solution build) requires PLAN-1.1's csproj fixes to pass.
  - Task 3 (README link resolution) requires PLAN-1.2 (file existence) and PLAN-1.3 (bullet presence).
- **Result:** Dependency graph is correct; Wave 2 cannot start until Wave 1 is complete.

### Acceptance Criteria Testability ✓

| Plan | Task | Acceptance Criteria | Testable? |
|---|---|---|---|
| PLAN-1.1 | Task 1 | `dotnet build RelationalDatabase.csproj -c Release -p:CI=true` succeeds, zero CS1591, two Release blocks present | ✓ Concrete test commands |
| PLAN-1.1 | Task 2 | Three Release blocks in SQLite.csproj contain `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>`, NU1902 surfaces as warning not error | ✓ File inspection + build command |
| PLAN-1.1 | Task 3 | Four per-project Release builds succeed, zero CS1591 on each, 0 Warning(s) for CS1591 in output | ✓ Four concrete `dotnet build` commands with specific grep patterns |
| PLAN-1.2 | Task 1 | File exists, H1+H2+H3 structure present, 6 content elements (lifecycle, retry, example, schema, DB-name, not-supported), exactly 1 C# fenced block, grep anchors hit | ✓ File existence + line counts + grep patterns + wc |
| PLAN-1.3 | Task 1 | Bullet present once, positioned after job-scheduler line, blank line + Wiki line preserved, `git diff` shows +1/-0 | ✓ grep + git diff |
| PLAN-2.1 | Task 1 | `dotnet build DotNetWorkQueueNoTests.sln -c Release -p:CI=true` exits 0, zero CS1591 warnings, zero errors | ✓ Full build command with explicit grep for CS1591 count |
| PLAN-2.1 | Task 2 | `dotnet pack` succeeds, .nuspec extracted contains `<repository>` + `type="git"` + non-empty commit, no CS1591 | ✓ Concrete pack command, unzip, grep |
| PLAN-2.1 | Task 3 | File exists, bullet present once, grep exact link syntax matches, no accidental duplicates | ✓ test -f, grep patterns |

**Result:** All acceptance criteria are measurable and concrete. None rely on subjective judgment.

### Task Sizing & Complexity ✓

- **PLAN-1.1 Task 1:** single `<PropertyGroup>` block insertion into one csproj (indentation-aware). Low complexity.
- **PLAN-1.1 Task 2:** add `<WarningsNotAsErrors>` line to three existing `<PropertyGroup>` blocks. Low complexity.
- **PLAN-1.1 Task 3:** run four `dotnet build` commands, grep output. Mechanical verification, low complexity.
- **PLAN-1.2 Task 1:** write ~150 lines of markdown matching `docs/jenkins-setup.md` style. Moderate complexity; decision points documented in CONTEXT-7 Decision 2 and RESEARCH.md §3.
- **PLAN-1.3 Task 1:** single Edit call with exact old/new string provided. Low complexity.
- **PLAN-2.1 Task 1:** run full-solution Release build, grep for CS1591. Low complexity.
- **PLAN-2.1 Task 2:** `dotnet pack`, unzip, grep, cleanup. Low complexity.
- **PLAN-2.1 Task 3:** grep + test commands, no file changes. Low complexity.

**Result:** Each task targets single-file or single-command scope. Matches ROADMAP §Cross-Cutting Notes on agent-stall avoidance.

---

## §3. CONTEXT-7 Compliance

### Decision 1: Doc target scope (docs/ only; Wiki deferred) ✓

**Requirement:** No plan should push to GitHub Wiki API or write content to `wiki/*` paths.

**Verification:**
- PLAN-1.1: modifies csprojs only. ✓
- PLAN-1.2: creates `docs/outbox-pattern.md` (in-repo doc, not Wiki). ✓
- PLAN-1.3: modifies `README.md`. ✓
- PLAN-2.1: verification only, no Wiki API calls in tasks. ✓

**Result:** PASS. All plans target in-repo artifacts. Wiki is explicitly deferred per CONTEXT-7 Decision 1 wording ("manual post-ship task").

### Decision 2: Tutorial + reference hybrid, ONE worked example ✓

**Requirement:** PLAN-1.2 should specify ONE SqlServer worked example, not multiple. Reference sections follow.

**Verification from PLAN-1.2 Task 1:**
- "**Example: SqlServer**" — one fenced ` ```csharp ` block covering resolve + capability-cast + transaction + Send + Commit.
- "**#### PostgreSQL note**" — 2–4 line callout (NOT duplicate code block), mentions NpgsqlConnection/NpgsqlTransaction inline.
- Acceptance criterion explicit: "**Exactly one C# fenced block** (the SqlServer example). No second example block for PostgreSQL — the PG note is prose."
- Verification command: `grep -c '^```csharp' docs/outbox-pattern.md # Expect 1`

**Result:** PASS. Plan explicitly enforces single-block limit with grep verification.

### Decision 3: README pointer placement (under "High-level features" bullet, not new section) ✓

**Requirement:** Single-line bullet addition, not new H2 section, not multi-paragraph block.

**Verification from PLAN-1.3 Task 1:**
- Edit's `new_string` contains exact bullet text: `- Transactional outbox pattern on SqlServer / PostgreSQL (see [`docs/outbox-pattern.md`](docs/outbox-pattern.md))`
- Positioned after "- Re-occurring job scheduler" via the old_string context.
- Acceptance criterion: "README's 'High-level features' list now has four bullets (the original three plus the new outbox bullet at position 4)."
- `git diff` must show +1/-0 (one line added, nothing removed).
- Explicit constraint: "Do not change heading levels, badge URLs, the installation tables, or the feature bullet wording above."

**Result:** PASS. Plan enforces single-bullet scope and provides exact old/new strings for the Edit call, precluding accidental section creation.

---

## §4. Architectural Decision Validation

### ISSUE-032 Inline Closure (Option B) ✓

**Requirement from ROADMAP §Phase 7:** PLAN-1.1 should add `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to `Transport.SQLite.csproj`.

**Verification from PLAN-1.1:**
- **Task 2 title:** "Add WarningsNotAsErrors NU1902 to Transport.SQLite.csproj (closes ISSUE-032)".
- **Description:** "Add `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to each of the three Release-condition `<PropertyGroup>` blocks in the SQLite csproj."
- **Rationale in PLAN-1.1 Context:** "ROADMAP §Phase 7 success criterion requires the full-solution Release build to be clean for XML-doc warnings; ISSUE-032's pre-existing NU1902 error obscures that signal. Inline fix is cheaper than scoping verification to per-project builds..."
- **Acceptance criteria:** All three Release blocks contain the line; build succeeds; NU1902 appears as warning not error.

**Result:** PASS. PLAN-1.1 Task 2 matches the architect's chosen option B and cites explicit rationale.

### net8.0 XML-doc gate fix (Transport.RelationalDatabase) ✓

**Requirement from RESEARCH.md §2 finding:** `Transport.RelationalDatabase.csproj` has `Release|net10.0` block but is **missing** `Release|net8.0` condition block for `DocumentationFile` + `TreatWarningsAsErrors`.

**Verification from PLAN-1.1:**
- **Task 1 title:** "Add Release|net8.0 condition block to Transport.RelationalDatabase.csproj".
- **Description:** Insert new `<PropertyGroup>` after existing `Release|net10.0` block with three properties: `<DefineConstants></DefineConstants>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<DocumentationFile>DotNetWorkQueue.Transport.RelationalDatabase.xml</DocumentationFile>`.
- **Acceptance criteria:** Two Release blocks present (one per TFM), identical property contents, per-project build succeeds with zero CS1591, `git diff` shows only new block.
- **Constraints:** "Preserve the existing tab-vs-space indentation of the surrounding blocks (the file uses tab indentation on the `Release|net10.0` block at lines 22–27 — match that)."

**Result:** PASS. PLAN-1.1 Task 1 explicitly targets the exact gap identified by RESEARCH.md §2 with concrete csproj line numbers and indentation awareness.

---

## §5. Content Quality & Reference Completeness

### PLAN-1.2 (`docs/outbox-pattern.md`) Content Coverage ✓

**Required sections per ROADMAP §Phase 7 §SC #10:**
1. Caller-owned-transaction lifecycle contract.
2. Caller-owned-retry contract.
3. Capability-cast usage example from PROJECT.md §Functional New Public API.
4. Schema-deployment prerequisite (`CreateQueue` once at deploy time).
5. Per-provider DB-name comparison semantics (OrdinalIgnoreCase vs Ordinal).
6. Explicit "not supported on Memory/Redis/LiteDb/SQLite" callout.

**Plan-specified sections (PLAN-1.2 Task 1 Required sections):**
1. **`## Overview`** — feature summary.
2. **`## Quick Start`** with Prerequisites + Example: SqlServer (fenced C# block) + PostgreSQL note (callout).
3. **`## Reference`** with five subsections:
   - **`### Lifecycle Contract`** — bullets per RESEARCH.md §3 (caller owns connection/transaction/disposal; producer never calls Commit/Rollback/Dispose/Close; thread-safety warning). Cites PROJECT.md §Ownership & Threading.
   - **`### Retry Contract`** — `IRetrySkippable.SkipRetry = true` bypasses Polly; caller wraps whole operation in their retry policy. Cites PROJECT.md + `IRetrySkippable.cs`.
   - **`### Schema Deployment Prerequisite`** — `CreateQueue()` one-time deploy-time; outbox path requires pre-existing schema. Cites PROJECT.md §Non-Goals.
   - **`### Database-Name Comparison Semantics`** — markdown table (Transport | Extractor | Comparison); SqlServer + SqlServerExternalDbNameExtractor; PostgreSQL + PostgreSqlExternalDbNameExtractor; note on pass-through semantics.
   - **`### Supported Transports`** — two bullets (Supported: SqlServer, PostgreSQL; Not supported: Memory, Redis, LiteDb, SQLite with capability-cast explanation).

**Verification:**
- Acceptance criteria explicitly enumerate all six PROJECT.md §SC #10 content elements.
- Grep verification commands confirm anchors: `grep -c "## Overview\|## Quick Start\|## Reference\|### Lifecycle Contract\|### Retry Contract\|### Schema Deployment Prerequisite\|### Database-Name Comparison Semantics\|### Supported Transports"` (expect at least 8 matches).
- `grep -c "IRelationalProducerQueue"` (expect ≥3 matches).
- `grep -F "SqlServerExternalDbNameExtractor"` and `grep -F "PostgreSqlExternalDbNameExtractor"` (expect 1 match each).

**Result:** PASS. All six required content elements are explicitly planned as sections with citations to source material.

### PLAN-1.2 Style & Voice Compliance ✓

**Reference:** `docs/jenkins-setup.md` (per CONTEXT-7 Decision 2 and RESEARCH.md §7).

**Plan constraints (PLAN-1.2 Task 1):**
- No emojis.
- Imperative voice; second-person ("you") fine; first-person ("I", "we") not used.
- Code fences use explicit language tag (` ```csharp `, ` ```bash `).
- File ends with single trailing newline.
- No images, no diagrams.
- **File length target: 120–200 lines.** If draft exceeds 250 lines, trim.

**Verification:**
- `LC_ALL=C grep -P '[\x80-\xFF]' docs/outbox-pattern.md # Expect no output` (catches high-BMP unicode emojis).
- `wc -l docs/outbox-pattern.md` (verify line count within target range).

**Result:** PASS. Constraints are concrete and machine-verifiable.

---

## §6. Success Criteria Alignment

| ROADMAP Phase 7 success criterion | Plan coverage | Status |
|---|---|---|
| `dotnet build -c Release -p:CI=true` of `DotNetWorkQueueNoTests.sln` produces no XML-doc warnings | PLAN-1.1 (csproj fixes), PLAN-2.1 Task 1 (full-solution build verification) | PASS |
| Wiki draft reviewed and approved (manual gate) | Out of scope; deferred per CONTEXT-7 Decision 1 | N/A |
| README points at the new page | PLAN-1.3 Task 1 (bullet insertion), PLAN-2.1 Task 3 (link resolution verification) | PASS |
| PROJECT.md §SC #10 satisfied | PLAN-1.2 Task 1 (six content elements) + PLAN-2.1 (verification) | PASS |

| PROJECT.md §SC #10 sub-criterion | Plan coverage |
|---|---|
| Caller-owned-transaction lifecycle contract documented | PLAN-1.2 "Lifecycle Contract" section |
| Caller-owned-retry contract documented | PLAN-1.2 "Retry Contract" section |
| Capability-cast usage example present | PLAN-1.2 "Example: SqlServer" code block |
| Schema-deployment prerequisite documented | PLAN-1.2 "Schema Deployment Prerequisite" section |
| Per-provider DB-name comparison semantics documented | PLAN-1.2 "Database-Name Comparison Semantics" table |
| "Not supported on Memory/Redis/LiteDb/SQLite" callout | PLAN-1.2 "Supported Transports" section |

**Result:** PASS. All ROADMAP Phase 7 and PROJECT.md §SC #10 criteria are addressed by plans with concrete verification commands.

---

## §7. Risk Assessment

| Risk | Identified? | Mitigation in plans |
|---|---|---|
| Transport.RelationalDatabase net8.0 TFM has no Release XML-doc gate | ✓ Yes (RESEARCH.md §2) | PLAN-1.1 Task 1 explicitly adds the block with line-number context |
| ISSUE-032 NU1902 error blocks full-solution Release build | ✓ Yes (RESEARCH.md §6) | PLAN-1.1 Task 2 closes it inline; RESEARCH.md rationale cited in plan context |
| `-p:CI=true` misunderstood as suppressing XML-doc warnings | ✓ Yes (RESEARCH.md §5) | PLAN-2.1 context explicitly documents that `-p:CI=true` enables Source Link, not warning suppression |
| Doc style divergence from `docs/jenkins-setup.md` | ✓ Yes (RESEARCH.md §7) | PLAN-1.2 Task 1 directs builder to read jenkins-setup.md before writing; constraints enumerate voice/style expectations |
| `<inheritdoc />` on overrides fails CS1591 check | ✓ Yes (RESEARCH.md §7 uncertainty) | RESEARCH.md confirmed this is accepted; no plan change needed |
| PostgreSQL DB-name case-sensitivity doc gap | ✓ Yes (RESEARCH.md §3 table) | PLAN-1.2 "Database-Name Comparison Semantics" section explicitly documents SqlServer vs. PostgreSQL semantics |

**Result:** PASS. All significant risks from RESEARCH.md are explicitly addressed in plan tasks or context.

---

## §8. Recommendations

### No Revisions Required

All four plans are structured correctly and ready for execution. Specific observations:

1. **PLAN-1.1 Task 1 — indentation precision:** The constraint "Preserve the existing tab-vs-space indentation of the surrounding blocks (the file uses tab indentation on the `Release|net10.0` block at lines 22–27 — match that, do not convert to spaces)" is appropriate and will prevent cosmetic churn. Builder should follow this explicitly.

2. **PLAN-1.2 Task 1 — decision point:** The plan correctly directs the builder to read `docs/jenkins-setup.md` first and enforces ONE C# code block via grep verification. The PostgreSQL callout as prose (not code block) is locked by CONTEXT-7 Decision 2 and RESEARCH.md §3.

3. **PLAN-2.1 Task 1 — error vs. warning clarity:** The plan correctly distinguishes NU1902 (warning, pre-existing, acceptable) from CS1591 (warning, Phase 7 scope, must be zero). The full-solution build is the authoritative gate per ROADMAP §Phase 7 success criterion.

4. **PLAN-2.1 Task 2 — nuspec inspection:** The Source Link verification via `.nuspec` extraction is a lightweight spot-check. The plan acknowledges `ildasm`/`strings` inspection as "nice-to-have, not blocking," which is appropriate for a Release+CI=true smoke test.

5. **Wave 2 dependency chain:** PLAN-2.1's three tasks have no internal ordering constraints (all are verification-only), but the plan explicitly declares dependencies on Wave 1, which is correct.

### No Outstanding Issues

- No file conflicts between plans.
- No circular dependencies.
- No subjective acceptance criteria.
- No vague verification commands.
- All decisions from CONTEXT-7 are present and enforced in plan text.

---

## Verdict: PASS

**Summary:** Phase 7 plans are well-formed, comprehensive, and ready for execution. All four plans (PLAN-1.1, 1.2, 1.3, 2.1) collectively satisfy the Phase 7 success criteria and PROJECT.md §SC #10. Wave 1 is parallelizable; Wave 2 correctly depends on Wave 1 completion. Acceptance criteria are concrete and machine-verifiable. Architectural decisions (ISSUE-032 closure, net8.0 doc gate, single worked example, single README bullet) are all reflected in plan tasks with explicit line numbers and field names where applicable. Risk mitigation is thorough.

**Builder may proceed with execution.**

---

*Verification completed 2026-05-15 by Senior Verification Engineer*
