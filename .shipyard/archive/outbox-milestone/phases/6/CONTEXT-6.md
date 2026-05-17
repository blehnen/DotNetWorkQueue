# Phase 6 Context: Integration Tests (SqlServer + PostgreSQL)

Source phase description: `.shipyard/ROADMAP.md` §Phase 6.
Source project: `.shipyard/PROJECT.md`.
Prior phases: Phase 3 (SqlServer unit) + Phase 4 (PostgreSQL unit) + Phase 5 (negative-path) — all complete. Phase 6 is the runtime/integration counterpart of the unit-test coverage.

## Phase Scope (from ROADMAP.md + user decisions below)

Phase 6 ships **22 integration tests** (11 per transport) against real SqlServer + PostgreSQL instances, slotting into the existing 14-stage Jenkins integration test matrix without Jenkinsfile changes. Coverage is **method-matrix driven** per ROADMAP §Phase 6 (every public method on `IRelationalProducerQueue<T>` gets its own test exercising the caller-tx path, because async branches don't infer from sync coverage per the existing codecov collection model).

Per-transport matrix:
- **A. Method × outcome (8 tests):** `Send(T, tx)` / `SendAsync(T, tx)` / `Send(List<...>, tx)` (batch) / `SendAsync(List<...>, tx)` (batch async) × commit + rollback = 8 tests
- **B. `IAdditionalMessageData` round-trip (1 test):** enqueue with custom headers/correlation, commit, dequeue separately, assert metadata intact
- **C. Validation (2 tests):** cross-database mismatch + closed-connection
- **D. Retry bypass (1 test):** force transient SQL error mid-send, assert single-attempt failure

Total per-transport: 8 + 1 + 2 + 1 = 12. Wait — ROADMAP says 11. Let me recount: ROADMAP says "8 tests per transport" for A, "1 test per transport" for B, "2 tests per transport" for C, "1 test per transport" for D. That's 12 per transport = 24 total, not 22. **Architect note: clarify discrepancy with ROADMAP's "22" total (8+1+2+1=12 per transport, 24 total). If architect resolves to 11/transport, drop one batch-rollback test (most likely candidate: the SendAsync batch rollback if duplicate with sync batch rollback). Default to ROADMAP's 22-total; flag the discrepancy.**

Total scope estimate: 22-24 integration tests + 2 shared test base classes (one per transport).

**Risk classification:** Mid per ROADMAP (first time the full path runs against real DBs).

**Phase size:** L per ROADMAP (14-18 hours).

## User Decisions

### Decision 1: Test project location — EXISTING TRANSPORT INTEGRATION TEST PROJECTS

Phase 6 tests live in:
- `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/` (or equivalent subfolder; researcher confirms exact naming)
- `Source/DotNetWorkQueue.Transport.PostgreSQL.IntegrationTests/Outbox/`

Uses existing scaffolding: `connectionstring.txt`, Coverlet, queue-per-test isolation. **Slots into existing Jenkins SqlServer + PostgreSQL integration stages without Jenkinsfile changes** (ROADMAP invariant).

**Rejected: new dedicated outbox integration test projects.** Would require Jenkinsfile changes (new stages), violating ROADMAP's "No Jenkinsfile changes" invariant.

**Rejected: shared cross-transport project.** Violates DNQ convention of per-transport integration test projects; harder to slot into parallel Jenkins stages.

### Decision 2: Test harness — SHARED TEST BASE CLASSES PER TRANSPORT

Two new base classes:
- `SqlServerOutboxIntegrationTestBase` in `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/`
- `PostgreSqlOutboxIntegrationTestBase` in `Source/DotNetWorkQueue.Transport.PostgreSQL.IntegrationTests/Outbox/`

Each base class provides:
- **`CreateBusinessTable(connection)`** setup helper — creates a simple parallel business table for atomic-commit verification (e.g., `CREATE TABLE OutboxBusiness_<random> (Id INT, Value NVARCHAR(100))`)
- **`InsertBusinessRow(tx, value)`** helper — INSERT into the business table within the caller's transaction
- **`AssertMessageInQueue(connection, queueName, expectedCount)`** — POLLING assertion (NOT snapshot; CLAUDE.md lesson on metrics races) — queries the queue's status table for count of un-dequeued messages
- **`AssertBusinessRowExists(connection, value)`** — assertion that the business INSERT committed (or did not, for rollback tests)
- **Queue name generation** — `"q" + Guid.NewGuid().ToString("N")` (CLAUDE.md lesson — DNQ rejects hyphens)
- **Wave-isolated setup/cleanup** — each test owns its queue + business table + caller-tx connection; tear-down drops both. Allows Jenkins parallel execution.

Tests inherit and call helpers. Cross-transport shape similarity is high — but two separate base classes avoid layering complexity from a shared helper project (Rejected option B per Decision 2 prompt).

**Rejected: cross-transport shared test base.** Would need a new helper project; deviates from per-transport DNQ convention.

**Rejected: inline helpers in each test class.** 22-24 tests with significant duplication — much harder to maintain.

### Decision 3: Plan structure — 4 PLANS ACROSS 2 WAVES (reconciles CI strategy)

The user's CI strategy (Decision 4) implies wave ordering: SqlServer first, then PostgreSQL after Jenkins green. So Phase 6 is structured as:

- **Wave 1 (SqlServer side, plans parallel-safe):**
  - **PLAN-1.1: SqlServer method-matrix tests (3 tasks).** 8 tests covering Send/SendAsync × single/batch × commit/rollback. Split across 3 tasks: (a) Send (sync, both batch + single, both commit + rollback = 4 tests), (b) SendAsync (async, both batch + single, both commit + rollback = 4 tests). Wait — that's 8 tests in 2 tasks but the plan needs ≤3 tasks total. Better split: (a) Send single + batch × commit/rollback (4 tests), (b) SendAsync single + batch × commit/rollback (4 tests), (c) shared `SqlServerOutboxIntegrationTestBase` setup. Total 3 tasks.
  - **PLAN-1.2: SqlServer validation + retry-bypass + AdditionalMessageData (3 tasks).** Task (a) 2 validation tests (cross-DB mismatch + closed-conn), task (b) 1 retry-bypass test (force transient error → assert single attempt), task (c) 1 `IAdditionalMessageData` round-trip test. Total 4 tests across 3 tasks.

- **Wave 2 (PostgreSQL side, plans parallel-safe, depends on Wave 1):**
  - **PLAN-2.1: PostgreSQL method-matrix tests (3 tasks).** Mirrors PLAN-1.1 shape.
  - **PLAN-2.2: PostgreSQL validation + retry-bypass + AdditionalMessageData (3 tasks).** Mirrors PLAN-1.2 shape.

**Why Wave 2 depends on Wave 1:** The Jenkins CI strategy (Decision 4) requires SqlServer Jenkins-green before PG-side plans land. Wave dependency captures this.

**Rejected: all 4 plans in Wave 1 parallel.** Doesn't reflect the CI gating between SqlServer-ready and PostgreSQL-ready states.

**Rejected: 1 plan per transport (2 plans total).** 11 tasks per plan blatantly violates ≤3 tasks/plan rule.

### Decision 4: CI strategy — OPEN DRAFT PR AFTER WAVE 1 LANDS, AWAIT JENKINS GREEN BEFORE WAVE 2

After Wave 1 (SqlServer-side plans) lands and local SqlServer integration tests pass:

1. Push the feature branch (commits land on `master` per current branch model OR on a feature branch — branch strategy TBD with user)
2. Open a draft PR via `gh pr create --draft --base master --head <branch>` per CLAUDE.md "Jenkins is PR-triggered, not branch-triggered" rule
3. Await Jenkins SqlServer integration stage green
4. If Jenkins SqlServer is green → proceed to Wave 2 (PostgreSQL plans)
5. If Jenkins SqlServer fails → revisit PLAN-1.1/PLAN-1.2 before Wave 2

Phase 6 verifier should explicitly check the Jenkins SqlServer stage status before declaring PASS.

**Rejected: open draft PR after ALL Phase 6 plans land.** Late CI feedback; transport-specific flakes would force revisit during ship gate.

**Rejected: skip Jenkins draft PR until ship.** CLAUDE.md explicitly warns against this for feature-branch CI validation.

## Hard Rules / Cross-Cutting Constraints

These come from PROJECT.md, CLAUDE.md, and Phase 1–5 lessons. The architect MUST encode them in plan task acceptance criteria:

- **Queue name format:** `"q" + Guid.NewGuid().ToString("N")` (CLAUDE.md lesson — DNQ rejects hyphenated GUIDs).
- **Metrics polling not snapshot** (for the retry-bypass test): use the live `IMetrics` object polled in a loop with a reasonable timeout, NOT a single snapshot. CLAUDE.md lesson.
- **Wave-isolated tests:** each test owns its own queue + own caller-tx connection. Allows Jenkins parallel execution.
- **Atomic-commit harness:** each method-matrix test runs the caller's business INSERT through a second simple table created at test setup. Atomic semantics are directly verifiable (both rows present after commit OR neither row present after rollback).
- **`-c Debug` not `-c Release`:** integration tests run in Debug. `-p:CI=true` is ONLY for the pre-publish Release build (CLAUDE.md lesson).
- **No new NuGet dependencies.** Existing integration-test scaffolding (MSTest 4.x, NSubstitute, AutoFixture, FluentAssertions 6.12.2 per memory) is sufficient.
- **LGPL-2.1 license header** on every new `.cs` file.
- **No regressions:** existing SqlServer + PostgreSQL integration test suites must still pass.
- **Jenkins PR-trigger:** the draft PR step in Decision 4 is mandatory between waves.

## Exit Criteria for Phase 6

1. **22 new integration tests pass locally** against real SqlServer + real PostgreSQL.
2. **Jenkins SqlServer integration stage green** on draft PR (between Wave 1 and Wave 2). PROJECT.md §Success Criteria #11.
3. **Jenkins PostgreSQL integration stage green** on draft PR (after Wave 2 lands).
4. **PROJECT.md §Success Criteria #4, #5, #6 satisfied** — test names explicitly mapped to each in plan task descriptions.
5. **Coverlet line coverage on the new `HandleExternalTx` (sync + async) and batch external-tx forks** shows ≥1 hit per branch in BOTH transports.
6. **No new flakiness** on retries (CLAUDE.md lesson: poll, don't snapshot).
7. **All existing integration tests still pass** (regression gate).

## Out of Scope (Phase 6)

- `docs/outbox-pattern.md` user-facing documentation (Phase 7).
- README.md update (Phase 7).
- Wiki draft (Phase 7).
- CLAUDE.md "Lessons Learned" additions from Phases 3-5 (deferred to ship time).
- Performance benchmarking (not in scope for any phase of this milestone).
- NpgsqlBatch spike (Risk #2 deferred per Phase 4 CONTEXT-4).

## Dependencies

- Phase 3 (SqlServer unit) — complete
- Phase 4 (PostgreSQL unit) — complete
- Phase 5 (negative-path) — complete (independent; not strictly a Phase 6 dependency)
- Live SqlServer instance + connection string in `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/connectionstring.txt`
- Live PostgreSQL instance + connection string in `Source/DotNetWorkQueue.Transport.PostgreSQL.IntegrationTests/connectionstring.txt`

## Notes for Researcher / Architect

### Researcher questions to answer:

1. **Exact integration test project paths** — does the repo use `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/` or `Source/DotNetWorkQueue.Transport.SqlServer.Integration.Tests/`? Confirm both transports.
2. **Existing test class patterns** — find 1-2 representative existing integration tests (e.g., basic send/receive) per transport. The Phase 6 test bodies should match those patterns (queue setup, schema creation, teardown).
3. **`IAdditionalMessageData` round-trip pattern** — how do existing integration tests assert metadata round-trip? Find a reference implementation.
4. **Retry-bypass test mechanism** — how to "force a transient SQL error mid-send"? SqlServer: short query timeout + lock conflict; PostgreSQL: equivalent (advisory lock conflict?). Capture the existing patterns or note that this needs a Phase 6 invention.
5. **Jenkins stage names** — confirm the SqlServer and PostgreSQL integration test stages exist in `Jenkinsfile`. Phase 6 should NOT modify the Jenkinsfile; tests slot into existing stages by virtue of being in the right .csproj.
6. **Discrepancy resolution: 22 vs 24 total tests.** ROADMAP says 22 total; math says 8+1+2+1=12/transport×2=24. Architect to resolve: either drop 1 test per transport (most likely candidate: one of the batch tests) or accept 24 as the actual count and update ROADMAP at ship time.

### Architect to encode in plan tasks:

- The 22 (or 24) test names mapped to PROJECT.md §Success Criteria #4, #5, #6 explicitly.
- Each plan task has a single-test or small-cluster-of-tests scope (≤3 tasks per plan, each task may bundle 2-4 closely-related tests for the method-matrix plans).
- Acceptance criteria include: build clean, test pass count, regression check, and (for Wave 1 PLAN-1.2's retry-bypass test) metrics-polling pattern verification.

### Phase 6 builders may need multiple SendMessage resumes:

Based on Phase 1-5 patterns, builders consistently hit the turn budget on large plans. Phase 6 has high test-code volume (~22 tests with helpers). Architect should write self-contained plan task bodies with explicit code blocks so builders can copy verbatim, minimizing investigation time.
