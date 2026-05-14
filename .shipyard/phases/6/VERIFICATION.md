# Phase 6 Plan Verification

**Phase:** Phase 6 — Integration Tests (SqlServer + PostgreSQL)
**Date:** 2026-05-14
**Type:** plan-review (Step 6 of /shipyard:plan)
**Plans reviewed:** PLAN-1.1, PLAN-1.2, PLAN-2.1, PLAN-2.2

## Verdict: PASS

All ROADMAP §Phase 6 categories are covered, all PROJECT.md Success Criteria #4/#5/#6 are explicitly mapped to test names, all 7 hard rules from CONTEXT-6 are encoded as either base-class behavior or per-test acceptance criteria, structural constraints (≤3 tasks/plan, 2-wave ordering, file disjointness) hold, and test count reconciles to 24 (12/transport) per RESEARCH §6.

Six minor recommendations (none ship-blocking, all addressable by the builder at copy-time).

---

## §1. Requirement Coverage Matrix

### ROADMAP §Phase 6 categories

| Category | ROADMAP requirement | Plan coverage | Status |
|---|---|---|---|
| A | 8 method-matrix tests/transport (Send/SendAsync × single/batch × commit/rollback) | PLAN-1.1 Task 2+3 (4+4=8 SqlServer); PLAN-2.1 Task 2+3 (4+4=8 PG) | PASS |
| B | 1 IAdditionalMessageData round-trip/transport | PLAN-1.2 Task 3 (SqlServer); PLAN-2.2 Task 3 (PG) | PASS |
| C | 2 validation tests/transport (cross-DB + closed-conn) | PLAN-1.2 Task 1 (2 tests SqlServer); PLAN-2.2 Task 1 (2 tests PG) | PASS |
| D | 1 retry-bypass test/transport | PLAN-1.2 Task 2 (SqlServer); PLAN-2.2 Task 2 (PG) | PASS |

### PROJECT.md §Success Criteria

| #SC | Criterion | Mapped test(s) | Status |
|---|---|---|---|
| #3 | Capability-cast pattern works at runtime | `Send_Commit_BothRowsVisible` (both transports) via `Assert.IsInstanceOfType` inside `CreateRelationalProducer`. PLAN-1.1 Task 1 acceptance criterion line 178 cites #SC #3 explicitly. | PASS |
| #4 | Atomic commit: queue row + business row visible | `Send_Commit_BothRowsVisible`, `SendBatch_Commit_AllRowsVisible`, `SendAsync_Commit_BothRowsVisible`, `SendBatchAsync_Commit_AllRowsVisible` (both transports). PLAN-1.1 §SC coverage table maps all four. | PASS |
| #5 | Atomic rollback: neither row visible | `Send_Rollback_NeitherRowVisible`, `SendBatch_Rollback_NeitherRowVisible`, `SendAsync_Rollback_NeitherRowVisible`, `SendBatchAsync_Rollback_NeitherRowVisible` (both transports). | PASS |
| #6 | Cross-DB validation throws before write | `Validation_CrossDatabaseMismatch_ThrowsBeforeInsert` (both transports). PLAN-1.2 + PLAN-2.2 §SC table. | PASS |
| #8 | Retry decorator bypass | `RetryBypass_TransientError_SingleAttempt` (both transports). PLAN-1.2 §SC table. NOTE: structural unit pin is in Phase 3/4; Phase 6 covers integration level only (wall-clock + no-write assertions). | PASS |
| #11 | Jenkins green on draft PR | CI gating captured in PLAN-2.1 dependencies (`[1.1, 1.2]`) and PLAN-2.2 "Phase 6 ship gate" section steps 1-5. | PASS |

---

## §2. Structural Verification

### Task counts (≤3 per plan)

| Plan | Tasks | Test count |
|---|---|---|
| PLAN-1.1 | 3 (Base + Send sync 4 + SendAsync 4) | 8 |
| PLAN-1.2 | 3 (Validation 2 + RetryBypass 1 + AdditionalData 1) | 4 |
| PLAN-2.1 | 3 (Base + Send sync 4 + SendAsync 4) | 8 |
| PLAN-2.2 | 3 (Validation 2 + RetryBypass 1 + AdditionalData 1) | 4 |
| **Total** | **12 tasks** | **24 tests** |

PASS — 12/12 tasks within the ≤3-per-plan ceiling.

### 24 total test count breakdown (RESEARCH §6 resolution of ROADMAP "22" typo)

| Transport | Method-matrix (A) | AddlData (B) | Validation (C) | RetryBypass (D) | Total |
|---|---|---|---|---|---|
| SqlServer | 8 | 1 | 2 | 1 | 12 |
| PostgreSQL | 8 | 1 | 2 | 1 | 12 |
| **TOTAL** | **16** | **2** | **4** | **2** | **24** |

PASS — RESEARCH §6 explicitly recommends "Accept 24 (12/transport); update ROADMAP at ship time. Do not cut batch tests." All four plans implement the recommended count.

### Wave ordering / dependencies

| Plan | Wave | Dependencies declared in YAML frontmatter | Verified |
|---|---|---|---|
| PLAN-1.1 | 1 | `[]` | PASS — no deps |
| PLAN-1.2 | 1 | `[]` (per frontmatter) — body declares "PLAN-1.1 base class — same wave; Task 1 lands first" | PASS conditionally (see §5 gap #1) |
| PLAN-2.1 | 2 | `[1.1, 1.2]` | PASS — depends on Wave 1 |
| PLAN-2.2 | 2 | `[1.1, 1.2]` (per frontmatter) — body declares "PLAN-2.1 base class — same wave; Task 1 lands first" | PASS conditionally (see §5 gap #1) |

PASS for wave structure (1 → 2 with CI gate). The within-wave Task-1-first ordering for the base class is documented in plan bodies and is naturally enforced by C# compilation (the test classes won't compile until the base class compiles).

### File disjointness

**Wave 1 (SqlServer) — `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/`:**

| Plan | File | Disjoint? |
|---|---|---|
| PLAN-1.1 T1 | `SqlServerOutboxIntegrationTestBase.cs` | unique |
| PLAN-1.1 T2 | `SqlServerOutboxSendTests.cs` | unique |
| PLAN-1.1 T3 | `SqlServerOutboxSendAsyncTests.cs` | unique |
| PLAN-1.2 T1 | `SqlServerOutboxValidationTests.cs` | unique |
| PLAN-1.2 T2 | `SqlServerOutboxRetryBypassTests.cs` | unique |
| PLAN-1.2 T3 | `SqlServerOutboxAdditionalDataTests.cs` | unique |

**Wave 2 (PostgreSQL) — `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/`:**

| Plan | File | Disjoint? |
|---|---|---|
| PLAN-2.1 T1 | `PostgreSqlOutboxIntegrationTestBase.cs` | unique |
| PLAN-2.1 T2 | `PostgreSqlOutboxSendTests.cs` | unique |
| PLAN-2.1 T3 | `PostgreSqlOutboxSendAsyncTests.cs` | unique |
| PLAN-2.2 T1 | `PostgreSqlOutboxValidationTests.cs` | unique |
| PLAN-2.2 T2 | `PostgreSqlOutboxRetryBypassTests.cs` | unique |
| PLAN-2.2 T3 | `PostgreSqlOutboxAdditionalDataTests.cs` | unique |

PASS — 12 distinct file paths. Within-wave PLAN-X.1/X.2 share a base class file but PLAN-X.2 only READS the base class (via inheritance), and PLAN-X.1 Task 1 is the sole writer. No write conflicts.

Cross-wave: Wave 1 and Wave 2 touch completely different project directories. Zero overlap.

PASS.

---

## §3. CONTEXT-6 Decision Encoding

### Decision 1: Existing transport integration test projects

| Plan | Project path used | Matches Decision 1? |
|---|---|---|
| PLAN-1.1 | `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/` | PASS — matches RESEARCH §1 confirmed path |
| PLAN-1.2 | same | PASS |
| PLAN-2.1 | `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/` | PASS — note "DOT before Integration" called out in PLAN-2.1 line 37 and PLAN-2.2 line 34 |
| PLAN-2.2 | same | PASS |

PASS. The PG path asymmetry (DOT vs. no DOT) is explicitly flagged in both PG plans, mitigating the foot-gun.

No new test projects. No Jenkinsfile changes. PASS.

### Decision 2: Shared test base class per transport

| Base class | Plan creating it | Plan(s) inheriting it |
|---|---|---|
| `SqlServerOutboxIntegrationTestBase` | PLAN-1.1 Task 1 | PLAN-1.1 T2/T3, PLAN-1.2 T1/T2/T3 |
| `PostgreSqlOutboxIntegrationTestBase` | PLAN-2.1 Task 1 | PLAN-2.1 T2/T3, PLAN-2.2 T1/T2/T3 |

PASS. Both base classes provide the harness helpers from CONTEXT-6 lines 46-51 (CreateBusinessTable, InsertBusinessRow, AssertMessageInQueue [as AssertQueueRowCount], AssertBusinessRowExists, queue-name generation, wave-isolated setup/cleanup via `QueueScope`/`ProducerScope` IDisposables).

### Decision 3: 4 plans across 2 waves

PASS. PLAN-1.1, PLAN-1.2, PLAN-2.1, PLAN-2.2 — exactly 4 plans. Wave 1 = SqlServer (PLAN-1.x). Wave 2 = PostgreSQL (PLAN-2.x) depends on Wave 1. Matches Decision 3 structure verbatim.

### Decision 4: CI strategy (draft PR after Wave 1; Jenkins green before Wave 2)

| Captured where? | Status |
|---|---|
| PLAN-2.1 dependencies `[1.1, 1.2]` (frontmatter line 5) | PASS |
| PLAN-2.1 Context line 34-35: "Phase 6 Wave 2 ships only after the Wave-1 (SqlServer) draft PR achieves Jenkins-green on the `SqlServer` stage" | PASS |
| PLAN-2.1 Dependencies section line 43: "Wave 1 (PLAN-1.1 + PLAN-1.2) merged and Jenkins SqlServer stage green on draft PR" | PASS |
| PLAN-2.2 Dependencies section line 40: "PLAN-1.1 + PLAN-1.2 merged with Jenkins SqlServer stage green (CI gating per Decision 4)" | PASS |
| PLAN-2.2 "Phase 6 ship gate" section (lines 429-439) with 5-step procedure including `gh pr create --draft` | PASS |

PASS — Decision 4 is encoded in multiple locations across both Wave 2 plans.

---

## §4. Hard Rules

| Rule (from CONTEXT-6 lines 95-105) | Where encoded | Status |
|---|---|---|
| Queue name `"q" + Guid.NewGuid().ToString("N")` (DNQ rejects hyphens) | PLAN-1.1 base class line 112: `protected static string NewQueueName() => "q" + Guid.NewGuid().ToString("N");` Identical pattern in PLAN-2.1 line 100. Every test calls `NewQueueName()`. | PASS — encoded in base class so all 24 tests inherit |
| `oCreation.RemoveQueue()` in `finally` block on every test | PLAN-1.1 base `QueueScope.Dispose()` line 126: `try { OCreation?.RemoveQueue(); } catch { /* swallow */ }`. Every test uses `using var queue = CreateQueue(qc);` — disposal runs at scope exit. PLAN-1.2 Task 3 AND PLAN-2.2 Task 3 (which bypass the base class for the priority-enabled queue) include explicit `try { oCreation?.RemoveQueue(); } catch { /* ignore */ }` in their own finally blocks (PLAN-1.2 line 406, PLAN-2.2 line 335). | PASS |
| Metrics polling pattern (not snapshot) | PLAN-1.1 base class `AssertQueueRowCount` lines 227-239: polls in 100ms intervals up to 5000ms timeout. Used by every method-matrix test. RetryBypass test (PLAN-1.2 T2 + PLAN-2.2 T2) uses wall-clock timing rather than metrics polling — per RESEARCH §4 and PLAN-1.2 line 211, the producer-side retry-attempt metric is not exposed by `IMetrics`, so wall-clock `Stopwatch` is the architect-selected substitute. The plan acknowledges this trade-off and acknowledges the unit-level structural pin in Phase 3 covers the gap. | PASS (with documented architect substitution) |
| `Assert.ThrowsExactly<T>` (MSTest 4.x) — NOT `Assert.ThrowsException<T>` (MSTest 2.x) | PLAN-1.2 line 125 + line 153 (SqlServer validation). PLAN-2.2 line 103 + line 130 (PG validation). Explicit note in PLAN-1.2 line 173: "Assert.ThrowsExactly<T> is MSTest 4.x (CLAUDE.md lesson)". | PASS |
| LGPL-2.1 header on every new .cs file | PLAN-1.1 Task 1 line 92: "LGPL-2.1 license header (copy from ... SharedClasses.cs lines 1-18)". Every subsequent task body in all 4 plans opens with `// LGPL-2.1 license header`. Every Acceptance Criteria block contains the phrase "with LGPL-2.1 header". | PASS |
| Cross-DB validation uses `master` (Sql) / `postgres` (PG) | PLAN-1.2 Task 1 line 118: `builder.InitialCatalog = "master"`. PLAN-2.2 Task 1 line 96: `builder.Database = "postgres"`. Both validated against RESEARCH §10 comparison matrix. | PASS |
| Retry-bypass uses committed-tx technique | PLAN-1.2 Task 2 lines 246-251: `conn.Open(); var tx = conn.BeginTransaction(); tx.Commit();` — exactly the technique from RESEARCH §4 / RESEARCH lines 502-512. Identical in PLAN-2.2 Task 2 lines 203-206. | PASS |

PASS — all 7 hard rules from CONTEXT-6 are present, either in the base class (so every test inherits them) or per-test where transport-specific syntax differs.

### Additional CLAUDE.md / PROJECT.md rules

| Rule | Status |
|---|---|
| `-c Debug` not `-c Release`; no `-p:CI=true` on tests (CLAUDE.md) | PASS — every verification block in all 4 plans uses `-c Debug` and no `-p:CI=true`. |
| No new NuGet dependencies (CONTEXT-6 line 102) | PASS — plans use only existing transport NuGets (`Microsoft.Data.SqlClient`, `Npgsql`) and `Microsoft.VisualStudio.TestTools.UnitTesting`. |
| `using` `Microsoft.VisualStudio.TestTools.UnitTesting` (MSTest namespace) | PASS — present in every test file shape. |

---

## §5. Gaps / Recommendations

### Gap #1 (Minor — frontmatter consistency)

**Issue:** PLAN-1.2 and PLAN-2.2 frontmatter declare `dependencies: []` (PLAN-1.2 line 5) and `dependencies: [1.1, 1.2]` (PLAN-2.2 line 5), but their bodies declare an in-wave ordering requirement: "PLAN-1.1 base class — same wave; Task 1 lands first" (PLAN-1.2 line 44) and equivalent (PLAN-2.2 line 39). The within-wave Task-1-first order is enforced by C# compilation (the inherited symbol must exist), so this is a soft dependency rather than a build-blocker. Recommend adding `intra_wave_depends_on: [PLAN-1.1#Task1]` (or equivalent annotation) to make the implicit ordering explicit for the builder's queue.

**Severity:** Minor — does not block plan execution; the builder will hit a compile error if order is wrong and re-order naturally.

**Recommendation:** Builder should land PLAN-1.1 Task 1 before any other Wave-1 task. Architect may optionally extend the YAML schema to encode this; not blocking.

### Gap #2 (Minor — likely-broken inline `CorrelationIdContainer` shells)

**Issue:** PLAN-1.2 Task 3 (lines 432-444) and PLAN-2.2 Task 3 (lines 361-373) include inline `CorrelationIdContainer` / `MessageCorrelationId` shells that implement `ICorrelationId` / `IMessageId`. Both plans explicitly flag this as likely-non-compiling: PLAN-1.2 line 449-457 says "The exact ctor shape may differ. The builder should resolve via existing CorrelationId helpers in DotNetWorkQueue.Messages" and provides a fallback ("drop the assertion if brittle"). PLAN-2.2 line 384-390 mirrors this.

**Severity:** Minor — the plan explicitly anticipates the failure and provides two fallbacks (substitute the existing public helper; or drop the assertion entirely and rely on priority round-trip).

**Recommendation:** Builder should investigate `DotNetWorkQueue.Messages.CorrelationId` (or similar canonical helper) before copy-pasting the inline shell. Single grep call: `grep -rn "data.CorrelationId =" Source/`.

### Gap #3 (Minor — RemoveQueue ordering in Task 3 (AdditionalData) test)

**Issue:** PLAN-1.2 Task 3 (line 372) and PLAN-2.2 Task 3 (line 302) construct `QueueCreationContainer` directly (not via the base class's `CreateQueue` helper) because the base class hardcodes `EnablePriority = false`. The custom `finally` block at PLAN-1.2 line 405-409 and PLAN-2.2 line 334-338 disposes correctly, but DROPS the test pattern of using a `using var queue = CreateQueue(qc)` scope. The note at PLAN-1.2 line 466-469 even suggests "Refactor option: add an optional `Action<SqlServerMessageQueueCreation>` parameter to base `CreateQueue` — defer to builder."

**Severity:** Minor — both finally blocks are correct; the in-task cleanup is sound. The refactor would be a polish improvement.

**Recommendation:** Builder may optionally extend the base class signature `CreateQueue(QueueConnection, Action<SqlServerMessageQueueCreation> configure = null)` for cleaner Task 3 code. Not required for correctness.

### Gap #4 (Minor — MSTest namespace `Microsoft.VisualStudio.TestTools.UnitTesting` vs MSTest 4.x recommended)

**Issue:** Plans use `using Microsoft.VisualStudio.TestTools.UnitTesting;` which works with MSTest 2.x AND 4.x. Other projects in this repo (per CLAUDE.md "MSTest 3.x uses `Assert.ThrowsExactly<T>`") are on MSTest 4.x.

**Severity:** None — this is the correct namespace for both versions. `Assert.ThrowsExactly<T>` ships in MSTest 3.x+ regardless of `using` line.

**Recommendation:** No action.

### Gap #5 (Minor — retry-bypass wall-clock 2000ms cap is heuristic)

**Issue:** PLAN-1.2 Task 2 (line 277) and PLAN-2.2 Task 2 (line 226) assert `sw.ElapsedMilliseconds < 2000` as the single-attempt-vs-3-retry-chain discriminator. The plan author notes (PLAN-1.2 line 297-303) that "if the test proves flaky on slow CI hosts, raise the cap to 3000ms — DO NOT remove the timing assertion entirely; it is the only integration-level pin against the retry decorator silently regressed failure mode." This is a sound defensive position.

**Severity:** Minor — flake mitigation is documented; threshold is generous (3x retry with seconds-long Polly backoff would massively exceed 2s).

**Recommendation:** Builder should observe actual wall-clock on first local run. If <100ms, the threshold has 20x headroom and is safe. If approaching 1000ms, raise to 3000ms per plan guidance. The unit-level Phase 3 IRetrySkippable pin remains the primary regression guard.

### Gap #6 (Minor — `ConnectionString.Trim()` applied at use site, not in shared infra)

**Issue:** PLAN-1.2 Task 1 line 118 and PLAN-2.2 Task 1 line 95 apply `ConnectionInfo.ConnectionString.Trim()` when feeding `SqlConnectionStringBuilder` / `NpgsqlConnectionStringBuilder`. RESEARCH §7 / "Uncertainty Flag #3" notes that the existing `ConnectionString.cs` does NOT trim, and trailing-newline injection from Jenkins `echo` is a known risk. PLAN-1.2 line 169-172 explicitly justifies "Trim at the use site, not in `ConnectionInfo` (avoid touching shared infra)."

**Severity:** None — the chosen approach is correct (root-cause-isolated, minimum-blast-radius).

**Recommendation:** No action. If a future test fails on trailing-newline elsewhere, fix the root cause in `ConnectionString.cs` per CLAUDE.md ("fix the root cause, not the symptom") — but Phase 6 should NOT take that on.

---

## §6. Verification Method

The following items remain MANUAL-or-deferred-to-build-time (cannot be plan-verified):

| Item | When verifiable |
|---|---|
| Tests actually pass against real SqlServer | Build verification (post-execution) |
| Tests actually pass against real PostgreSQL | Build verification (post-execution) |
| Jenkins SqlServer stage green on draft PR | Wave-1-merge gate (per Decision 4) |
| Jenkins PostgreSQL stage green on draft PR | Wave-2-merge gate (per Decision 4) |
| Coverlet ≥1 hit per branch on `HandleExternalTx`/`HandleExternalTxAsync` | Ship verification (after Phase 6 merge) |
| No regressions in existing transport integration suites | Build verification (every plan's acceptance criteria includes the no-regression assert) |

These are not gaps in the plans themselves; they are correctly deferred to the right verification gate.

---

## §7. Cross-References to Prior Phases

No prior Phase 6 VERIFICATION.md exists (this is the first verification for Phase 6). The plans were authored after Phases 1-5 completed; Phase 5 was scoped down to a SimpleInjector smoke test per CLAUDE.md ("Memory transport storage is per-`QueueContainer` instance" lesson) so there is no Phase 6 dependency on Phase 5 paths.

ISSUE-033/-034/-035 in `.shipyard/ISSUES.md` are Phase 3/4 fork-smoke-test minor remediations; they are unrelated to Phase 6 integration tests (different test category, different files). No Phase 6 action required.

---

## §8. Files Reviewed

- `/mnt/f/git/dotnetworkqueue/.shipyard/phases/6/plans/PLAN-1.1.md` (701 lines)
- `/mnt/f/git/dotnetworkqueue/.shipyard/phases/6/plans/PLAN-1.2.md` (507 lines)
- `/mnt/f/git/dotnetworkqueue/.shipyard/phases/6/plans/PLAN-2.1.md` (655 lines)
- `/mnt/f/git/dotnetworkqueue/.shipyard/phases/6/plans/PLAN-2.2.md` (440 lines)
- `/mnt/f/git/dotnetworkqueue/.shipyard/phases/6/CONTEXT-6.md` (154 lines)
- `/mnt/f/git/dotnetworkqueue/.shipyard/phases/6/RESEARCH.md` (591 lines)
- `/mnt/f/git/dotnetworkqueue/.shipyard/ROADMAP.md` (Phase 6 section, lines 116-164)
- `/mnt/f/git/dotnetworkqueue/.shipyard/PROJECT.md` (Success Criteria section, lines 95-108)
- `/mnt/f/git/dotnetworkqueue/CLAUDE.md` (project conventions)
- `/mnt/f/git/dotnetworkqueue/.shipyard/ISSUES.md` (no Phase 6-related open issues)
