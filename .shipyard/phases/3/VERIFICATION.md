# Phase 3 Plan Verification (Coverage)

**Phase:** 3 — SqlServer Inbox Wiring + Unit Tests
**Date:** 2026-05-18
**Type:** plan-review
**Verifier:** Claude (Senior QA)

---

## A. Roadmap Success Criteria Coverage

| # | Criterion | Plan/Task | Status |
|---|-----------|-----------|--------|
| 1 | Transport.SqlServer builds clean (net10.0 + net8.0) | PLAN-1.1 Task 1 + Task 2; PLAN-2.1 Task 1; PLAN-2.2 Task 1 + Task 2 (verification commands cover Release build) | **COVERED** |
| 2 | All new SqlServer unit tests pass; existing pass | PLAN-2.2 Task 1 + Task 2 (seven unit tests + smoke test specified; verification command 2 confirms baseline + new) | **COVERED** |
| 3 | SimpleInjector smoke test: option=true → cast succeeds; option=false → cast fails | PLAN-2.2 Task 2 (Container_ResolvesRelationalNotification smoke test) + Task 1 (PlainWorkerNotification_DoesNotImplementIRelationalWorkerNotification for negative case) | **COVERED** |
| 4 | New notification impl class is `internal` | PLAN-1.1 Task 1 (class declaration acceptance criterion: `internal class SqlServerRelationalWorkerNotification`) | **COVERED** |
| 5 | No new `SqlConnection` sealed-type casts | PLAN-1.1 Task 1, Task 2 (grep forbidden acceptance criteria); PLAN-2.1 Task 1 (forbidden: no sealed cast on connection); PLAN-2.2 Task 1 (forbidden + acceptance grep for casts); PLAN-2.2 verification command 6 (grep check) | **COVERED** |

---

## B. CONTEXT-3 Decision Enforcement

| Decision | Plan Enforcement | Status |
|----------|------------------|--------|
| Class name: `SqlServerRelationalWorkerNotification` (CONTEXT-3 §1) | PLAN-1.1 Task 1: "Class declaration (exact spelling — CONTEXT-3 §1 user lock)" + acceptance criterion specifies exact name | **LOCKED** |
| Subclass `WorkerNotification` (CONTEXT-3 §3) | PLAN-1.1 Task 1: class declaration `: WorkerNotification, IRelationalWorkerNotification` + acceptance criterion forbids re-implementation of base members | **LOCKED** |
| Unit tests + SimpleInjector container smoke, no live-DB integration (CONTEXT-3 §2) | PLAN-2.2 Task 1 + Task 2: all tests unit-level with NSubstitute mocks + container smoke via QueueContainer (not Integration.Tests); acceptance criterion specifies six + seven test methods | **LOCKED** |
| `ConnectionHolder` property for post-construction injection (RESEARCH §4 Option A) | PLAN-1.1 Task 1: public mutable property `ConnectionHolder`; PLAN-2.1 Task 1: `if (context.WorkerNotification is SqlServerRelationalWorkerNotification relationalNotification)` post-ctor assignment | **LOCKED** |
| No public-surface change to `ConnectionHolder` (PROJECT.md §Functional Internal Implementation) | PLAN-2.1 Task 1: "This wire-up is invisible at the public API surface" + receive-path internal-only modification | **LOCKED** |

---

## C. Plan Structure

| Aspect | Finding |
|--------|---------|
| **Naming format** | ✓ PLAN-1.1, PLAN-2.1, PLAN-2.2 — correct format |
| **Task counts** | ✓ PLAN-1.1: 2 tasks; PLAN-2.1: 1 task; PLAN-2.2: 2 tasks (all ≤3) |
| **Wave 1 (PLAN-1.1)** | ✓ Foundation: creates class + registers in DI. "None — this plan is the foundation. PLAN-2.1 and PLAN-2.2 depend on this plan." |
| **Wave 2 (PLAN-2.1, PLAN-2.2)** | ✓ Both depend on PLAN-1.1 (receive-path wiring + unit tests). PLAN-2.1: "requires `SqlServerRelationalWorkerNotification` class to exist." PLAN-2.2: "requires `SqlServerRelationalWorkerNotification` class to exist"; PLAN-2.1 NOT required for PLAN-2.2 (unit tests construct directly). Parallel-safe: PLAN-2.1 modifies SQLServerMessageQueueReceive.cs; PLAN-2.2 creates + modifies SqlServerRelationalWorkerNotificationTests.cs — no conflict. |
| **File conflicts** | ✓ No cross-plan file edits: PLAN-1.1 creates new file + modifies Init; PLAN-2.1 modifies Receive; PLAN-2.2 creates + modifies Tests. Clean separation. |

---

## D. Acceptance Criteria Quality

| Plan | Criterion | Testability | Roadmap Mapping |
|------|-----------|-------------|-----------------|
| PLAN-1.1 Task 1 | File exists at exact path; LGPL header byte-identical; class declaration exact; constructor signature and forwarding; properties present and typed; no `Tx` tokens; no sealed casts; XML docs present | **Binary (file inspection + grep)** | Success #4 (internal), Success #5 (no sealed casts) |
| PLAN-1.1 Task 2 | Single Register line present in Init; placement (after line 73, before comment); no other lines modified; exactly one match via grep | **Binary (file inspection + grep)** | Success #1 (clean build validates registration) |
| PLAN-2.1 Task 1 | If-pattern block placed correctly; no other line edits; exactly one is-pattern match; no `Tx` tokens; release build passes | **Binary (file inspection + grep + test)** | Success #1 (clean build), Success #2 (existing tests pass) |
| PLAN-2.2 Task 1 | File exists; six test methods by exact name; correct MSTest 3.x API; all tests pass; no `Tx` tokens; no sealed casts | **Binary (test execution + grep)** | Success #2 (all new tests pass), Success #3 (negative cast case) |
| PLAN-2.2 Task 2 | Seventh test method exists; uses Assert.ThrowsExactly exactly; passes; no container.Verify() direct call | **Binary (test execution)** | Success #3 (SimpleInjector smoke test with option-true case) |

All criteria are measurable and objective — no subjective language ("clean", "looks good", etc.).

---

## E. Scope Guard (CONTEXT-3 + PROJECT.md)

| Guard | Plan Language | Status |
|-------|---------------|--------|
| **No public-surface change to `ConnectionHolder`/`IConnectionHolder`** | PLAN-2.1: "This wire-up is invisible at the public API surface — `IReceiveMessages`, `IMessageContext`, and `IConnectionHolder` are untouched." PROJECT.md §Functional Internal Implementation satisfied explicitly. | ✓ **ENFORCED** |
| **No `Tx` abbreviation** | PLAN-1.1 Task 1: "Forbidden: No `Tx`/`TX` token"; Task 2 comment uses full words. PLAN-2.1 Task 1: "Forbidden: Do NOT use `Tx` token"; comment uses `relationalNotification`. PLAN-2.2 Task 1: "Forbidden: Do NOT introduce `Tx`/`TX` tokens". Verification commands on all three plans grep for `\b(Tx|TX)\b`. | ✓ **ENFORCED** |
| **No live-DB integration test** | PLAN-2.2 Task 1: "Create a new `[TestClass]` file using MSTest 3.x conventions"; context: "NSubstitute unit tests ... smoke test" (no Integration.Tests directory mentioned). CONTEXT-3 §2: "unit-test suite" only. | ✓ **ENFORCED** |
| **New notification impl is `internal`** | PLAN-1.1 Task 1: "Class declaration (exact spelling — CONTEXT-3 §1 user lock)" with example `internal class SqlServerRelationalWorkerNotification`. Acceptance criterion: "Class is declared `internal class SqlServerRelationalWorkerNotification`". | ✓ **ENFORCED** |
| **`Transaction` property typed as `DbTransaction`, not `IDbTransaction` or `SqlTransaction`** | PLAN-1.1 Task 1: "The `Transaction` getter returns the underlying `SqlTransaction` typed as `DbTransaction` via implicit upcast — no explicit cast" + code example `public DbTransaction Transaction => ConnectionHolder?.Transaction;`. Task 1 forbidden: "Never write `(SqlTransaction)` anywhere". PLAN-2.2 Task 1: mock setup uses `Substitute.For<DbTransaction>()` (abstract base), not sealed `SqlTransaction`. | ✓ **ENFORCED** |

---

## F. Cross-Plan Coherence

| Check | Evidence | Status |
|-------|----------|--------|
| **PLAN-2.1 references correct class name** | "if (context.WorkerNotification is `SqlServerRelationalWorkerNotification` relationalNotification)" — matches PLAN-1.1 class name exactly. | ✓ **CORRECT** |
| **PLAN-2.2 references correct class name** | Factory helper constructs `new SqlServerRelationalWorkerNotification(...)` (Task 1); container smoke test resolves through DI (Task 2). Both reference the correct class from PLAN-1.1. | ✓ **CORRECT** |
| **Verification commands non-conflicting** | PLAN-1.1: dotnet build on Transport.SqlServer; grep on Notification + Init files. PLAN-2.1: dotnet build on Transport.SqlServer; grep on Receive file; dotnet test on Transport.SqlServer.Tests. PLAN-2.2: dotnet build on Transport.SqlServer; dotnet test on Transport.SqlServer.Tests with two filter variants; grep on Tests file. All test commands are idempotent. No step builds/tests in sequence that would conflict. | ✓ **NO CONFLICTS** |
| **Interdependency order (PLAN-1.1 → PLAN-2.1 & PLAN-2.2)** | PLAN-1.1 creates the class + registers it. PLAN-2.1 wires the holder on the class (requires PLAN-1.1 for compile-time reference). PLAN-2.2 tests the class directly (requires PLAN-1.1 for import). Both Wave 2 plans explicitly declare "PLAN-1.1 — requires `SqlServerRelationalWorkerNotification` class to exist." | ✓ **CORRECT** |

---

## G. Verification Command Quality

| Plan | Command | Type | Runnable? | Output Checkable? |
|------|---------|------|-----------|-------------------|
| PLAN-1.1 Task 1 | `dotnet build ... Transport.SqlServer.csproj -c Release -p:CI=true` | Deterministic build | ✓ Yes, produces "Build succeeded." or error | ✓ Exit code + message |
| PLAN-1.1 Task 1 | `grep -nE "\b(Tx\|TX)\b" ... SqlServerRelationalWorkerNotification.cs` | Negative assertion | ✓ Yes, expect exit code 1 (no matches) | ✓ Exit code |
| PLAN-1.1 Task 1 | `grep -nE "\(SqlConnection\)\|\(SqlTransaction\)" ... SqlServerRelationalWorkerNotification.cs` | Negative assertion | ✓ Yes, expect exit code 1 | ✓ Exit code |
| PLAN-1.1 Task 2 | `grep -n "container.Register<IWorkerNotification, SqlServerRelationalWorkerNotification>" ... SQLServerMessageQueueInit.cs` | Positive assertion | ✓ Yes, expect exactly 1 match | ✓ Exit code + line count |
| PLAN-2.1 Task 1 | `dotnet build ... Transport.SqlServer.csproj -c Release -p:CI=true` | Deterministic build | ✓ Yes | ✓ Exit code + message |
| PLAN-2.1 Task 1 | `grep -n "is SqlServerRelationalWorkerNotification" ... SQLServerMessageQueueReceive.cs` | Positive assertion | ✓ Yes, expect exactly 1 match | ✓ Exit code + line count |
| PLAN-2.1 Task 1 | `grep -nE "\b(Tx\|TX)\b" ... SQLServerMessageQueueReceive.cs` | Negative assertion | ✓ Yes, expect exit code 1 | ✓ Exit code |
| PLAN-2.1 Task 1 | `dotnet test ... Transport.SqlServer.Tests.csproj` | Regression test | ✓ Yes, compares baseline to current | ✓ Pass/fail count |
| PLAN-2.2 Task 1 | `dotnet build ... Transport.SqlServer.csproj -c Release -p:CI=true` | Deterministic build | ✓ Yes | ✓ Exit code + message |
| PLAN-2.2 Task 1 | `dotnet test ... Transport.SqlServer.Tests.csproj` | Full suite | ✓ Yes, includes new + existing | ✓ Pass/fail count |
| PLAN-2.2 Task 1 | `dotnet test ... --filter "FullyQualifiedName~SqlServerRelationalWorkerNotificationTests"` | Targeted new tests | ✓ Yes, isolates seven new | ✓ Pass count (expect 7) |
| PLAN-2.2 Task 1 | `grep -nE "\b(Tx\|TX)\b" ... SqlServerRelationalWorkerNotificationTests.cs` | Negative assertion | ✓ Yes, expect exit code 1 | ✓ Exit code |
| PLAN-2.2 Task 1 | `grep -n "Assert.ThrowsException<" ... SqlServerRelationalWorkerNotificationTests.cs` | Negative assertion (MSTest 2.x API) | ✓ Yes, expect exit code 1 | ✓ Exit code |
| PLAN-2.2 Task 1 | `grep -nE "\(SqlConnection\)\|\(SqlTransaction\)" ... SqlServerRelationalWorkerNotificationTests.cs` | Negative assertion (sealed casts) | ✓ Yes, expect exit code 1 | ✓ Exit code |

All commands are concrete, executable, and produce binary (pass/fail) outputs. No vague commands like "check that it works."

---

## Findings

### Critical (Blocking)

**None.** Plans are structurally sound and coverage is complete.

### Minor (Non-blocking)

1. **PLAN-2.2 Task 1 — SqlTransaction mocking caveat.** The plan acknowledges (lines 138–154) that `Microsoft.Data.SqlClient.SqlTransaction` is sealed and NSubstitute cannot mock it. The recommendation to use abstract `DbTransaction` base instead is correct per CLAUDE.md lesson. The test design pivots to `Transaction_DelegatesToConnectionHolder` (assert the holder is dereferenced exactly once) rather than constructing a non-null mock. This is a pragmatic trade-off and the integration tests in Phase 7 will cover the non-null path on live SqlServer. No action needed; plan is aware and has mitigated.

2. **PLAN-1.1 Task 1 — XML documentation requirement clarity.** Task 1 specifies XML docs on "the class, ctor, and `ConnectionHolder` property; `<inheritdoc/>` on `Transaction`" but also notes this is an `internal` class. The codebase does not enforce CS1591 on internal types (only on `public`). The plan language is correct ("XML docs are still required by project convention") but a builder reading this might wonder if docs are mandatory for internal. Recommend clarifying in acceptance criterion: "XML doc present on class (optional per CS1591 but required by project convention), ctor (optional), and `ConnectionHolder` property (optional); `<inheritdoc/>` on `Transaction` (recommended but not enforced)." This does not block — the plan is clear enough and the builder will follow project convention. **No action required.**

3. **PLAN-2.1 Task 1 — Namespace inference.** Task 1 states "The class `SqlServerRelationalWorkerNotification` lives in the same namespace (`DotNetWorkQueue.Transport.SqlServer.Basic`) as the receive class, so no explicit using is required — the type binds via the file's own namespace." This is correct for types in the same namespace, but receives a caveat: "Verify by inspecting the top-of-file `using` block (current lines 19–27); add only what is actually missing after the edit." This is a safe, correct instruction. No action needed.

### Gaps

**None.** Roadmap success criteria are fully addressed. CONTEXT-3 decisions are locked and enforced. Scope guards are explicit. No requirements left uncovered.

---

## Verdict

**PASS**

All three plans collectively address the Phase 3 roadmap success criteria (buildability, new unit tests, SimpleInjector smoke, internal visibility, no sealed casts). Plan structure is correct (PLAN-1.1 is foundation; PLAN-2.1 and PLAN-2.2 depend on PLAN-1.1 and are parallel-safe). CONTEXT-3 decisions are locked in plan language and acceptance criteria. Verification commands are concrete and executable. No blocking issues.

---

## Verdict Rationale

**Completeness:** ROADMAP success criteria #1–5 map directly to PLAN tasks:
- Buildability (#1) is verified by strict Release builds in PLAN-1.1, PLAN-2.1, PLAN-2.2.
- New unit tests (#2) are authored in PLAN-2.2 Task 1 (six methods) and Task 2 (one smoke test).
- SimpleInjector smoke (#3) is the Container_ResolvesRelationalNotification test (PLAN-2.2 Task 2) for the positive case and PlainWorkerNotification_DoesNotImplementIRelationalWorkerNotification (PLAN-2.2 Task 1) for the negative case.
- Internal visibility (#4) is specified in PLAN-1.1 Task 1 class declaration and acceptance criterion.
- No sealed-type casts (#5) are guarded by explicit "forbidden" language and grep commands in all three plans.

**Coherence:** Wave 1 (PLAN-1.1) establishes the foundation class and DI registration. Wave 2 plans (PLAN-2.1 and PLAN-2.2) both depend on PLAN-1.1 but do not depend on each other — they can execute in parallel without conflict. Cross-references to class names and interfaces are exact.

**Rigor:** Acceptance criteria are measurable (file paths, line counts, grep patterns, test pass/fail counts). Verification commands are executable from a known worktree root and produce deterministic output. Scope guards explicitly prevent public-surface changes, abbreviations, sealed-type casts, and live-DB tests.

**Risk:** Mid (per ROADMAP) — first transport, sets the pattern. Plans mitigate via detailed CLAUDE.md lessons on mocking strategy, sealed-type discipline, and namespace shadowing. SimpleInjector verification is lightweight (no live DB) but sufficient to catch DI registration errors before Phase 7 integration testing.

---

### Summary

- **3 plans (PLAN-1.1, PLAN-2.1, PLAN-2.2), 5 total tasks, all ≤3 tasks per plan.** Wave structure is correct (Wave 1 foundation, Wave 2 parallel).
- **All 5 ROADMAP success criteria fully covered** by corresponding tasks with measurable acceptance criteria.
- **All CONTEXT-3 decisions locked** in plan language and acceptance criteria (class name, subclass inheritance, unit-test scope, property injection, internal visibility).
- **All scope guards enforced** (no public API change, no `Tx` abbreviation, no sealed casts, no live DB).
- **Verification commands are concrete and executable** (grepping, building, testing) — no vague assertions.
- **No file conflicts** between plans — parallel execution is safe.
- **No critical gaps or blocking issues.**

**Verdict: PASS — Plans are ready for builder execution.**
