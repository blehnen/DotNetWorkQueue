# Build Summary: Plan 2.1 (Phase 6 Wave 2 — PostgreSQL Method-Matrix + Base Class)

## Status: complete (runtime verification deferred to Jenkins)

## Tasks Completed

- Task 1: Created `PostgreSqlOutboxIntegrationTestBase` in `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxIntegrationTestBase.cs`. Helpers: `CreateBusinessTable`, `InsertBusinessRow`, `DropBusinessTable`, `CountQueueMessages` (using `TableNameHelper`/`SqlConnectionInformation` from PG namespace), `AssertQueueRowCount` (polling), `AssertBusinessRowExists`, `BuildBatch`. Uses `NpgsqlConnection`/`NpgsqlTransaction`; `COUNT(*)` cast to `long` (PG returns `long`, not `int`). Added `using DotNetWorkQueue.Messages` (missing from plan shape — required for `QueueMessage<,>` resolution).
- Task 2: Created `PostgreSqlOutboxSendTests.cs` — 4 sync tests covering Send single/batch × commit/rollback.
- Task 3: Created `PostgreSqlOutboxSendAsyncTests.cs` — 4 async tests using `await using`/`BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` with cast to `NpgsqlTransaction` for `InsertBusinessRow`.

## Commits

| SHA | Task | Subject |
|-----|------|---------|
| `c64562a7` | 1 | `test(postgresql): add PostgreSqlOutboxIntegrationTestBase with atomic-commit harness` |
| `e35b8a06` | 2 | `test(postgresql): add PostgreSqlOutboxSendTests (4 sync outbox integration tests)` |
| `3c0b8017` | 3 | `test(postgresql): add PostgreSqlOutboxSendAsyncTests (8 async/sync outbox integration tests)` |

## Files Modified

- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxIntegrationTestBase.cs` — NEW
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxSendTests.cs` — NEW (4 [TestMethod])
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/Outbox/PostgreSqlOutboxSendAsyncTests.cs` — NEW (4 [TestMethod])

## Decisions Made

- Builder added `using DotNetWorkQueue.Messages;` to the base class (plan shape omitted it; `QueueMessage<FakeMessage, IAdditionalMessageData>` in `BuildBatch` required it). Build confirmed the fix.
- LGPL-2.1 license header applied to all 3 files, matching Wave 1 (SqlServer) convention. Existing PG integration test files have no license header; the plan explicitly required LGPL on all 3 new files, so Wave 1's header pattern was used.
- Business table name uses all-lowercase (`outboxbusiness_` prefix) per plan note on PG identifier case-folding.
- `COUNT(*)` return cast to `long` throughout (PG `NpgsqlCommand.ExecuteScalar()` returns `long` for `SELECT COUNT(*)`, unlike SqlServer's `int`). `AssertQueueRowCount` signature uses `long expected`; `AssertBusinessRowExists` uses `long expectedCount`.

## Issues Encountered

- **Missing `using DotNetWorkQueue.Messages`:** Plan's code shape omitted this import. First build produced `CS0246: QueueMessage<,>`. Fixed inline before commit.
- **No local PostgreSQL connection:** `connectionstring.txt` is intentionally empty. Runtime verification deferred to Jenkins.
- Pre-existing NU1902 OpenTelemetry advisory warnings (20 per Debug build) — expected baseline, same as Wave 1.

## Verification Results

| Gate | Expected | Actual |
|---|---|---|
| 3 source files exist (base + sync tests + async tests) | present | OK |
| `DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj` Debug build | 0 errors | 0 errors, 20 pre-existing NU1902 warns |
| Filtered Outbox tests against live PostgreSQL | 8 pass | DEFERRED TO JENKINS — empty `connectionstring.txt` on local machine (by design; Jenkins agent injects the connection string) |

## Path Convention Note

PostgreSQL integration tests live in `Source/DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests/` (dot before "Integration"). The csproj is `DotNetWorkQueue.Transport.PostgreSQL.Integration.Tests.csproj`. This differs from SqlServer's `Source/DotNetWorkQueue.Transport.SqlServer.IntegrationTests/` (no dot).

## Hand-off to PLAN-2.2

- `PostgreSqlOutboxIntegrationTestBase` is in place and the 8 method-matrix tests compile cleanly.
- PLAN-2.2 additional tests (validation, retry-bypass, AdditionalMessageData) can inherit from `PostgreSqlOutboxIntegrationTestBase`.
- PG extractor (Phase 4) already uses pass-through — no symmetry fix needed (unlike Wave 1 Phase 3 extractor bug).
