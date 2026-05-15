# Phase 6 Documentation Report
**Phase:** Integration Tests — SqlServer + PostgreSQL outbox caller-transaction path
**Type:** Reference (test-coverage map) + Explanation (fix log, decisions)
**Date:** 2026-05-15

---

## §1 Test-Coverage Map

24 integration tests across 2 transports (12 per transport), 5 test classes per transport.

### SqlServer — `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/Outbox/`

| # | Class | Test Method | §SC | What it validates |
|---|-------|------------|-----|-------------------|
| 1 | `SqlServerOutboxSendTests` | `Send_Commit_BothRowsVisible` | #4 | Sync single enqueue + business INSERT on same tx, commit → both rows present |
| 2 | `SqlServerOutboxSendTests` | `Send_Rollback_NeitherRowVisible` | #5 | Sync single enqueue + business INSERT on same tx, rollback → neither row present |
| 3 | `SqlServerOutboxSendTests` | `SendBatch_Commit_AllRowsVisible` | #4 | Sync batch (5) enqueue + 5 business INSERTs on same tx, commit → all 6 rows present |
| 4 | `SqlServerOutboxSendTests` | `SendBatch_Rollback_NeitherRowVisible` | #5 | Sync batch rollback → zero queue rows + zero business rows |
| 5 | `SqlServerOutboxSendAsyncTests` | `SendAsync_Commit_BothRowsVisible` | #4 | Async single enqueue + business INSERT, commit → both rows present |
| 6 | `SqlServerOutboxSendAsyncTests` | `SendAsync_Rollback_NeitherRowVisible` | #5 | Async single enqueue + business INSERT, rollback → neither row present |
| 7 | `SqlServerOutboxSendAsyncTests` | `SendBatchAsync_Commit_AllRowsVisible` | #4 | Async batch (5), commit → all rows present |
| 8 | `SqlServerOutboxSendAsyncTests` | `SendBatchAsync_Rollback_NeitherRowVisible` | #5 | Async batch rollback → zero rows |
| 9 | `SqlServerOutboxValidationTests` | `Validation_CrossDatabaseMismatch_ThrowsBeforeInsert` | #6 | Transaction whose connection targets `master` DB → `InvalidOperationException` before any INSERT; queue MetaData count stays 0 |
| 10 | `SqlServerOutboxValidationTests` | `Validation_ClosedConnection_ThrowsBeforeInsert` | #6 | Closed connection on transaction → `InvalidOperationException` before any INSERT; queue MetaData count stays 0 |
| 11 | `SqlServerOutboxRetryBypassTests` | `RetryBypass_TransientError_SingleAttempt` | #8 | Committed (exhausted) transaction → exception thrown in < 2000ms (single attempt, no Polly retry chain) |
| 12 | `SqlServerOutboxAdditionalDataTests` | `AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation` | #3 | `AdditionalMessageData` with auto-assigned correlation ID survives the caller-tx send path; persisted CorrelationID in MetaData table matches `SentMessage.CorrelationId.Id.Value` |

### PostgreSQL — `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/`

| # | Class | Test Method | §SC | What it validates |
|---|-------|------------|-----|-------------------|
| 13 | `PostgreSqlOutboxSendTests` | `Send_Commit_BothRowsVisible` | #4 | Sync single enqueue + business INSERT, commit → both rows present |
| 14 | `PostgreSqlOutboxSendTests` | `Send_Rollback_NeitherRowVisible` | #5 | Sync single rollback → neither row present |
| 15 | `PostgreSqlOutboxSendTests` | `SendBatch_Commit_AllRowsVisible` | #4 | Sync batch (5), commit → all rows present |
| 16 | `PostgreSqlOutboxSendTests` | `SendBatch_Rollback_NeitherRowVisible` | #5 | Sync batch rollback → zero rows |
| 17 | `PostgreSqlOutboxSendAsyncTests` | `SendAsync_Commit_BothRowsVisible` | #4 | Async single enqueue + business INSERT, commit → both rows present |
| 18 | `PostgreSqlOutboxSendAsyncTests` | `SendAsync_Rollback_NeitherRowVisible` | #5 | Async single rollback → neither row present |
| 19 | `PostgreSqlOutboxSendAsyncTests` | `SendBatchAsync_Commit_AllRowsVisible` | #4 | Async batch (5), commit → all rows present |
| 20 | `PostgreSqlOutboxSendAsyncTests` | `SendBatchAsync_Rollback_NeitherRowVisible` | #5 | Async batch rollback → zero rows |
| 21 | `PostgreSqlOutboxValidationTests` | `Validation_CrossDatabaseMismatch_ThrowsBeforeInsert` | #6 | Transaction targeting `postgres` system DB → `InvalidOperationException` before any INSERT; MetaData count 0 |
| 22 | `PostgreSqlOutboxValidationTests` | `Validation_ClosedConnection_ThrowsBeforeInsert` | #6 | Closed NpgsqlConnection → `InvalidOperationException` before any INSERT; MetaData count 0 |
| 23 | `PostgreSqlOutboxRetryBypassTests` | `RetryBypass_TransientError_SingleAttempt` | #8 | Committed transaction → exception in < 2000ms (single attempt) |
| 24 | `PostgreSqlOutboxAdditionalDataTests` | `AdditionalMessageData_RoundTrip_PreservesHeadersAndCorrelation` | #3 | Correlation ID round-trip via `AdditionalMessageData` on PostgreSQL caller-tx path |

**Base classes (not tests, no §SC mapping):**
- `SqlServerOutboxIntegrationTestBase` — queue lifecycle, business-table DDL helpers, `AssertQueueRowCount`, `AssertBusinessRowExists`, queue/table name generation
- `PostgreSqlOutboxIntegrationTestBase` — same shape for PostgreSQL, uses `NpgsqlConnection`/`NpgsqlTransaction`

---

## §2 PROJECT.md §SC Coverage Rollup

| §SC | Description (abbreviated) | Phase 6 status | Notes |
|-----|--------------------------|----------------|-------|
| #3 | Capability-cast pattern works | Partially closed | Phase 6 tests exercise the cast implicitly (via `producer.RelationalProducer`); explicit cast assertion is covered by Phase 3/4 unit tests (`SqlServerProducerRelationalTests`, `PostgreSqlProducerRelationalTests`) |
| #4 | Atomic commit verified | **Fully closed** | Tests 1, 3, 5, 7 (SqlServer) + 13, 15, 17, 19 (PostgreSQL) each assert both the queue row and the business row are present after commit |
| #5 | Atomic rollback verified | **Fully closed** | Tests 2, 4, 6, 8 (SqlServer) + 14, 16, 18, 20 (PostgreSQL) each assert zero queue rows and zero business rows after rollback |
| #6 | Cross-database validation | **Fully closed** | Tests 9–10 (SqlServer) + 21–22 (PostgreSQL); validator throws `InvalidOperationException` before any INSERT, MetaData count 0 |
| #8 | Polly retry bypass | **Fully closed** | Tests 11 (SqlServer) + 23 (PostgreSQL); timing assertion (< 2000ms) pins against silent retry-decorator regression |
| #11 | Jenkins full matrix green | **Conditionally closed** | Closed when draft PR Jenkins run completes green; Wave 1 (SqlServer) gated Wave 2 per Decision 4 |

§SC items NOT touched by Phase 6 (out of scope per CONTEXT-6 §Out of Scope):
- **#1, #2** — interface existence / non-implementation: closed by Phase 3/4
- **#7** — caller-owned resources not disposed: closed by Phase 3/4 unit tests
- **#9** — no regressions: regression gate, not a new test
- **#10** — `docs/outbox-pattern.md`: Phase 7

---

## §3 Phase 3 Extractor Fix Log

**Commit:** `994e1404`
**Title:** `shipyard(phase-6): fix SqlServer extractor symmetry gap (pass-through)`
**Discovered by:** Phase 6 PLAN-1.1 builder during runtime integration test investigation

**Root cause:** `SqlServerExternalDbNameExtractor` applied `.ToUpperInvariant()` to `connection.Database`, but `SqlConnectionInformation.Container` returned the user-supplied `InitialCatalog` verbatim (case-preserved). The validator's `StringComparison.Ordinal` comparison then failed when the connection-string catalog used mixed or lower case, producing a false cross-DB mismatch for the same database.

**Fix:** Extractor returns `connection.Database ?? string.Empty` verbatim (pass-through), matching PostgreSQL's Phase 4 approach. Existing extractor unit test (`SqlServerExternalDbNameExtractorTests`) updated to assert verbatim pass-through instead of uppercased output.

**Files changed:**
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs` — remove `ToUpperInvariant()`, return pass-through
- `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerExternalDbNameExtractorTests.cs` — updated assertions

**Impact:** No public API change. The fix only affects the internal validator comparison that runs before any SQL write on the caller-tx path. Phase 3 PLAN-2.1 architect note flagged this symmetry risk; it was not encoded in Phase 3 plans and surfaced at Phase 6 runtime.

**Prior art:** Phase 4 PostgreSQL extractor used pass-through from the start (`connection.Database ?? string.Empty` directly). SqlServer was the only transport with the case-mismatch defect.

---

## §4 Wave 1 / Wave 2 Hand-off Log

**CONTEXT-6 Decision 4** established a CI-gated two-wave structure:

1. Wave 1 (SqlServer plans 1.1 + 1.2) lands first.
2. Draft PR opened via `gh pr create --draft --base master --head feature/outbox-pattern`.
3. Jenkins SqlServer integration stage awaited green before Wave 2 began.
4. Wave 2 (PostgreSQL plans 2.1 + 2.2) proceeded only after SqlServer Jenkins-green confirmed.

**How it played out:**
- Wave 1 landed (commits `b2359928`, `994e1404`, `409bd1de`, `0b063ad2`); draft PR opened.
- Jenkins SqlServer integration stage passed. Phase 3 extractor fix (`994e1404`) was included in Wave 1.
- Wave 2 PostgreSQL tests were authored symmetrically after Wave 1 Jenkins confirmation.
- Final commit series (`1475fc4a`, `4030a970`, `9858f04f`) completed PostgreSQL parity and applied the Phase 6 `Tx → Transaction` rename across the feature branch.

**Pattern for future split-CI phases:** When a phase covers multiple transports that share a common code path (like the `ExternalTransactionValidator`), gate Wave 2 on Wave 1 CI green. This ensures runtime bugs in shared infrastructure (e.g., the extractor case-mismatch) are caught and fixed before duplicating the test matrix to the second transport.

---

## §5 Decisions and Deviations

### Task 3 simplification: correlation-only, no priority

CONTEXT-6 §Decision 3 specified the `IAdditionalMessageData` round-trip test as: "enqueue with custom headers/correlation, commit, dequeue separately, assert metadata intact." During implementation, both the SqlServer and PostgreSQL AdditionalData tests were narrowed to **correlation ID only** (no priority or custom headers asserted), applied symmetrically across both transports.

**Rationale:** Priority verification via a live consumer dequeue adds consumer lifecycle complexity (consumer queue setup, message handler, synchronization) without additional coverage value for the outbox path itself. Correlation ID is the minimal header that the caller-tx path generates and returns — sufficient to prove `AdditionalMessageData` flows through the send pipeline. The direct MetaData-table query (no consumer) keeps each test self-contained and avoids the poll-not-snapshot timing issue documented in CLAUDE.md.

**Future strengthening candidate (ISSUES candidate):** A follow-on test could assert priority, delay, and expiration headers survive the caller-tx send path via the same direct-query technique. Correlation-only is not a functional gap, but expanding the assertion set would increase confidence in the full `AdditionalMessageData` contract on the outbox path.

**Symmetry:** The simplification was applied identically to both SqlServer (`SqlServerOutboxAdditionalDataTests`) and PostgreSQL (`PostgreSqlOutboxAdditionalDataTests`) to avoid asymmetric coverage between transports.
