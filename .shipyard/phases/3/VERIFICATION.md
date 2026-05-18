# Phase 3 Verification (Post-Build)

**Phase:** 3 — SqlServer Inbox Wiring + Unit Tests
**Date:** 2026-05-18
**Type:** post-build
**Worktree:** `/mnt/f/git/dotnetworkqueue/.worktrees/phase-2-inbox-foundation`
**Branch:** `phase-2-inbox-foundation`
**Commit range:** `05325c50..151fbda8` (6 commits)
**Verdict:** COMPLETE

---

## Coverage (ROADMAP.md Phase 3 success criteria, lines 67-72)

| # | Criterion | Status | Evidence |
|---|---|---|---|
| 1 | `Transport.SqlServer` builds clean (net10.0 + net8.0) with `TreatWarningsAsErrors` + `-p:CI=true` | PASS | Final Release build: `Build succeeded. 14 Warning(s) [all NU1902 pre-existing OpenTelemetry advisory carry-forward] 0 Error(s)`. Both targets clean. No CS1591. |
| 2 | All new SqlServer unit tests pass; existing pass | PASS | `Transport.SqlServer.Tests` 164/164 (baseline 156 + 6 contract + 2 smoke). Zero regressions. |
| 3 | SimpleInjector smoke: with `EnableHoldTransactionUntilMessageCommitted = true`, notification `is IRelationalWorkerNotification`. With option false, the cast fails. | PASS | `SqlServerRelationalWorkerNotificationRegistrationTests.cs` — `Resolves_Relational_When_HoldTransaction_Enabled` (cast succeeds) and `Resolves_NonRelational_When_HoldTransaction_Disabled` (cast fails). Both pass. Directly satisfies PROJECT.md §Success Criteria #2. |
| 4 | Notification impl class is `internal` | PASS | `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs:50` — `internal class SqlServerRelationalWorkerNotification`. |
| 5 | No new `SqlConnection` sealed-type casts in handlers | PASS | Grep guard from PLAN-1.1 Gate 3: zero `(SqlConnection)`/`(SqlTransaction)` matches. PLAN-2.1's receive-path edit uses pattern-match on `IWorkerNotification`, not a cast. |

---

## Re-run gate evidence (executed in worktree)

### Gate 1 — Release build (`Transport.SqlServer`, both TFMs)
```
Build succeeded.
   14 Warning(s)  [all NU1902 pre-existing OpenTelemetry advisory carry-forward]
    0 Error(s)
Time Elapsed 00:00:16.41
```

### Gate 2 — `Transport.SqlServer.Tests` full run
```
Passed!  - Failed: 0, Passed: 164, Skipped: 0, Total: 164, Duration: 16 s
  - DotNetWorkQueue.Transport.SqlServer.Tests.dll (net10.0)
```

### Gate 3 — Core unit-test regression smoke (`DotNetWorkQueue.Tests`)
```
Passed!  - Failed: 0, Passed: 905, Skipped: 0, Total: 905, Duration: 1 m 4 s
  - DotNetWorkQueue.Tests.dll (net10.0)
```
Zero regressions in the core library tests downstream of `IWorkerNotification`.

### Gate 4 — Scope confirmation (`git diff --name-only 05325c50..HEAD -- Source/`)
```
Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationRegistrationTests.cs
Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalWorkerNotificationTests.cs
Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs
Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueReceive.cs
Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs
```
Exactly 5 files (3 production code + 2 test files). All within planned directories. No drift into `Transport.RelationalDatabase`, `Transport.PostgreSQL`, `Transport.SQLite`, or shared/Core.

### Gate 5 — Diff statistics
```
 .../Basic/SQLServerMessageQueueInit.cs                          |  30 +++++
 .../Basic/SQLServerMessageQueueReceive.cs                       |  12 ++
 .../Basic/SqlServerRelationalWorkerNotification.cs              |  90 +++++++++++++
 .../Basic/SqlServerRelationalWorkerNotificationRegistrationTests.cs | 100 +++++++++++++++
 .../Basic/SqlServerRelationalWorkerNotificationTests.cs         | 140 +++++++++++++++++++++
 5 files changed, 372 insertions(+)
```

---

## Integration soundness

- REVIEW-1.1: PASS (with two minor non-blocking findings — bare catch + null-when-unset comment).
- REVIEW-2.1: PASS.
- REVIEW-2.2: PASS (with one minor non-blocking finding — Test 4 weaker than name suggests due to NSubstitute sealed-type limitation; covered by Phase 7 integration).
- No critical findings outstanding.

## CLAUDE.md compliance

| Lesson | Verification | Status |
|---|---|---|
| "No `Tx` abbreviation for transaction" | Grep guards across all 5 new/modified files | PASS |
| "Async-handler abstract-base mocking" — `DbTransaction` not `IDbTransaction` | Interface contract from Phase 2 preserved; new class delegates to `IConnectionHolder.Transaction` which is `SqlTransaction` (upcasts to `DbTransaction` implicitly) | PASS |
| "ADO.NET types out of root assembly" | All ADO.NET-touching code stays in `Transport.SqlServer.Basic` | PASS |
| "`IDbConnection` discipline / no sealed-type casts" | Grep guard confirms zero `(SqlConnection)`/`(SqlTransaction)` casts | PASS |
| "Casting `IDbConnection` to sealed transport-specific type breaks NSubstitute" | New class uses `IConnectionHolder<...>` interface as the test seam; mocks the interface directly | PASS |
| "Sync vs async handler mocking split" | N/A — notification is sync-only (property getter); contract tests mock interfaces | PASS |
| "MSTest 3.x `Assert.ThrowsExactly`" | New tests use `Assert.IsTrue/IsFalse/IsNull/AreSame/IsInstanceOfType<T>`; no `Assert.ThrowsException<T>` | PASS |

---

## Infrastructure validation

**N/A.** Phase 3 changes no Terraform, Ansible, Docker, Kubernetes, GitHub Actions, Jenkinsfile, or other infrastructure-as-code files.

---

## Phase-3-specific lessons captured (for ship-time and roadmap reuse on Phase 4/5)

1. **Factory-delegate registrations must include try/catch fallback** against options-load failures during container.Verify / early resolution. The pattern from `IBaseTransportOptions` (SQLServerMessageQueueInit.cs:140-144) is the existing codebase invariant — Phase 4 (PostgreSQL) and Phase 5 (SQLite) must apply the same try/catch shape to their own `IWorkerNotification` factory delegates.

2. **`Register<WorkerNotification>(LifeStyles.Transient)` self-registration is redundant** when the core already registers `WorkerNotification` as the default `IWorkerNotification` implementation. SimpleInjector auto-resolves concrete types that are registered as implementation targets. Pre-registering just the relational concrete + the factory delegate is sufficient.

3. **Receive-path setter via pattern-match (not cast)** gives a clean no-op on the option-false path. Cast would throw `InvalidCastException` when the factory returned a plain `WorkerNotification`. Pattern-match elegantly handles both paths in one statement.

4. **NSubstitute cannot proxy `SqlTransaction` (sealed).** Unit tests can mock `IConnectionHolder<...>` (the interface) and assert delegation paths, but reference-equality assertions on the underlying `SqlTransaction` instance need a real `SqlTransaction` from a live DB (Phase 7 integration territory). Test 4 of the contract suite documents this with an inline comment.

5. **Test seam for option-driven smoke tests:** `QueueContainer<T>(registerService, setOptions)` with a mocked `ITransportOptionsFactory` returning a stub options object is fast (~700ms for both tests), self-contained, and avoids live-DB requirements. Pattern reusable for Phase 4 and Phase 5 smoke tests.

---

## Gaps identified

**None.** All Phase 3 ROADMAP success criteria satisfied. Both Phase 1 spike lessons (heartbeat audit, command-timeout audit) and Phase 2 deliverables (interface contract) carry forward intact.

---

## Recommendations

- Proceed to **Step 5a** (security audit), **Step 5b** (simplification review), **Step 5c** (documentation generation).
- After those gates: mark Phase 3 complete in `ROADMAP.md`, commit artifacts, tag `post-build-phase-3-inbox`.
- Next phase: **Phase 4 — PostgreSQL Inbox Wiring + Unit Tests**. Apply Phase-3 lessons (especially #1 factory-delegate try/catch + #2 no redundant self-registration) verbatim to the PostgreSQL transport.
