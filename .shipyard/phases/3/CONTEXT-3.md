# CONTEXT-3: User Decisions for Phase 3 (SqlServer Inbox Wiring)

Captured during `/shipyard:plan 3` discussion-capture step on 2026-05-18.

## Phase 3 framing

Per ROADMAP.md lines 57-77, Phase 3 implements the SqlServer half of the inbox feature end-to-end:
- New `internal` notification class in `Transport.SqlServer` implementing `IRelationalWorkerNotification`.
- The class pulls `DbTransaction` from the per-message `ConnectionHolder` (existing infrastructure — no change to public surface per PROJECT.md §Functional Internal Implementation).
- SqlServer's DI registration branches on `EnableHoldTransactionUntilMessageCommitted`.
- ~6–8 unit tests covering factory branch + capability cast in both option states.

Phase 3 depends only on Phase 2 (interface) and is the reference transport for the per-transport pattern that Phases 4 (PostgreSQL) and 5 (SQLite) will mirror.

## Decisions

### 1. Class naming: `SqlServerRelationalWorkerNotification`

The new internal class uses the transport-prefixed name `SqlServerRelationalWorkerNotification`.

**Why:** Matches the existing per-transport type-naming convention (`SqlServerExternalDbNameExtractor`, `PostgreSqlExternalDbNameExtractor`, `SQLServerMessageQueueInit`). Phase 4 + Phase 5 cousins will be `PostgreSqlRelationalWorkerNotification` and `SqliteRelationalWorkerNotification` — the prefix keeps stack traces, DI dumps, and grep results unambiguous across the three relational transports.

**How to apply:** Use this exact spelling — `SqlServerRelationalWorkerNotification` — in the new file's class declaration. The file path is `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalWorkerNotification.cs` (mirrors the `/Basic/` subdirectory convention of every other SqlServer internal type).

### 2. Test scope: unit tests + SimpleInjector container smoke

Phase 3 ships:
- ~6–8 NSubstitute-based unit tests on the new class + registration-branch behavior.
- A single SimpleInjector container `Verify()` smoke test that resolves `IWorkerNotification` with `EnableHoldTransactionUntilMessageCommitted = true` and confirms `is IRelationalWorkerNotification` succeeds. A second case with the option `false` confirms the cast returns `null` cleanly.

Both unit tests and the smoke test live in `Source/DotNetWorkQueue.Transport.SqlServer.Tests/` (no live database; no Integration.Tests scope).

**Why:** ROADMAP success criterion #2 explicitly requires the capability-cast smoke test to be green at Phase 3 sign-off, not deferred to Phase 7. Without it, broken DI wiring would surface only during integration testing — much later and at higher cost. The cast is a single resolution call against a built container; no I/O or live DB required, so it belongs in the fast unit-test suite.

**How to apply:** Write the smoke test as a stand-alone `[TestMethod]` (or as an inline assertion inside the registration-branch test, whichever the architect prefers). Use SimpleInjector's `container.Verify()` to catch upstream registration mistakes; then call `container.GetInstance<IWorkerNotification>()` and `Assert.IsTrue(instance is IRelationalWorkerNotification)` for the positive case, `Assert.IsFalse(...)` for the negative case.

Do NOT introduce a live-DB integration test in Phase 3 — Phase 7 owns the 24-test inbox matrix (8/transport × 3) per ROADMAP lines 162-174.

### 3. Inheritance shape: subclass `WorkerNotification`

`SqlServerRelationalWorkerNotification` derives from `DotNetWorkQueue.Queue.WorkerNotification` and additionally implements `IRelationalWorkerNotification`:

```csharp
internal class SqlServerRelationalWorkerNotification : WorkerNotification, IRelationalWorkerNotification
```

The new class only declares the `Transaction` property (and any constructor plumbing required to receive the SqlServer `ConnectionHolder`). All eight base members (`WorkerStopping`, `HeaderNames`, `HeartBeat`, `TransportSupportsRollback`, `Log`, `Metrics`, `Tracer`, `MessageCancellation`) inherit unchanged from `WorkerNotification`.

**Why:** `Source/DotNetWorkQueue/Queue/WorkerNotifications.cs` exposes `WorkerNotification` as `public class` with virtual-eligible setters and no `sealed` modifier — subclassing is the intended extension point. Composition would require 30+ lines of forwarding boilerplate that drifts every time a member is added to `IWorkerNotification`. Subclassing is the lower-LOC, lower-drift choice and matches PROJECT.md §Functional New Public API: "The class declares only `: IRelationalWorkerNotification` — interface inheritance covers `IWorkerNotification`."

**How to apply:** The class must declare ONLY `: WorkerNotification, IRelationalWorkerNotification` in the inheritance list. The interface inheritance chain (`IRelationalWorkerNotification : IWorkerNotification`) takes care of the contract; `WorkerNotification` provides the implementation. Do not re-implement any inherited member.

## Non-decisions (settled upstream)

- **Branch trigger.** DI registration branches on `EnableHoldTransactionUntilMessageCommitted` at queue-init time (not per-message). Researcher to confirm exact registration seam.
- **`ConnectionHolder` access.** Per PROJECT.md §Functional Internal Implementation and ROADMAP line 41, `ConnectionHolder` already exposes `DbTransaction` internally — no public surface change. Researcher to map exact path.
- **Class visibility.** `internal`, per ROADMAP line 72 and PROJECT.md §Constraints Technical.
- **No new `SqlConnection` sealed-type casts.** CLAUDE.md hard rule; ROADMAP line 72. Architect must enforce in plan task wording.
- **No `Tx` abbreviation.** CLAUDE.md + outbox-milestone lesson. ROADMAP line 269.
- **Heartbeat behavior.** Phase 1 spike confirmed heartbeats use a separate connection and the user is expected to disable `EnableHeartBeat` in hold-tx mode — documented in Phase 8 docs, not changed here.
- **`IDbConnectionFactory` injection for tests.** CLAUDE.md lesson. Sync mocks via `IDbConnection`/`IDbCommand`; async mocks via `DbConnection`/`DbCommand` abstract bases.

## Open issues review (`.shipyard/ISSUES.md`)

Three open issues at planning time, all PG smoke-test residue from the outbox milestone:
- ISSUE-033 (fork-body end-bound overreach in PG sync smoke test)
- ISSUE-034 (fragile relative source-file path in fork smoke tests)
- ISSUE-035 (path-resolution block duplicated across smoke tests)

**None material to Phase 3.** No carry-in tasks.

## Scope reminders for plan authors

- Phase 3 is the reference transport for the inbox pattern. Patterns established here will be mirrored verbatim by Phase 4 (PG) and Phase 5 (SQLite, combined with outbox sweep). Resist transport-specific shortcuts that don't generalize.
- `ConnectionHolder`'s public surface MUST NOT change. If the existing internal `DbTransaction` accessor isn't reachable from where the new class needs it, the architect must propose an internal-only seam, not a public method.
- All new files include the standard 18-line LGPL-2.1 header.
- Builds clean on both net10.0 and net8.0 with `TreatWarningsAsErrors` and `-p:CI=true`.
