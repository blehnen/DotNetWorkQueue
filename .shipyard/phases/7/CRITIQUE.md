# CRITIQUE: Phase 7 Plan Feasibility

**Verdict:** CAUTION (live-DB requirement; build-session-only)

## Findings

### File paths exist
- All 4 integration test project directories confirmed (`SqlServer.IntegrationTests/`, `PostgreSQL.Integration.Tests/`, `SQLite.Integration.Tests/`).
- All 4 plans target NEW `Inbox/` or `Outbox/` subdirectories (don't exist yet — to be created during build).
- Reference template `SqlServerOutboxIntegrationTestBase.cs` exists and was read for shape confirmation.

### API surface
- `QueueContainer<SqLiteMessageQueueInit>`, `QueueCreationContainer<>`, `IRelationalProducerQueue<T>`, `IRelationalWorkerNotification` all exist (Phases 2-5 shipped).
- `IConsumerQueue` / `IConsumerQueueAsync` for the inbox tests are pre-existing core types.

### Verification commands runnable
- Standard `dotnet build` + `dotnet test --filter "FullyQualifiedName~Inbox"` / `~Outbox`.
- **All test runs require live SqlServer / PostgreSQL / SQLite + `connectionstring.txt` in each project**. This is the dominant Phase 7 build-time constraint — not feasible in any session without those services.

### Cross-plan coherence
- 4 plans target 4 different transport directories — zero file conflicts.
- Test classes in same plan use shared base class (1 file per plan) — base must be written first, but all 3 tasks within a plan are sequential within the plan anyway.

### Concerns / mitigations

**Live-DB requirement.** Cannot validate plans during planning session. Build session must have:
- Running SQL Server (`localhost` or configured connection)
- Running PostgreSQL (same)
- Writable filesystem for SQLite file-based DBs (also CI image's `/tmp` or similar)
- `connectionstring.txt` files in each integration test project (existing convention).

**SQLite single-writer (Risk #4).** First time SQLite hold-tx semantics hit a real DB. May surface unexpected deadlock or wait behavior. PLAN-1.3 Task 3 acceptance criterion explicitly captures these in SUMMARY-1.3 for Phase 8 docs (not blocker).

**Option-false test depends on user-handler-code shape.** PLAN-1.1/1.2/1.3 Task 3 tests rely on the test's handler implementation to detect the failed cast and throw `InvalidOperationException`. This is testing USER-CODE behavior — the inbox feature itself doesn't enforce the throw, but the test asserts the test's own handler shape. Reasonable for an integration test.

**§SC #8 zero-call assertion (PLAN-1.4 Task 1).** Requires wrapping the caller's connection + tx with a spy/counter. SqlServer outbox milestone has the same pattern — copy template from there.

## Verdict rationale
CAUTION (not blocking). Plans are detailed and the test patterns mirror established outbox-milestone templates. Build is deferred to a session with live DBs — the only blocker for Phase 7 progress.
