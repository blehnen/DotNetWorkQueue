# CRITIQUE: Phase 4 Plan Feasibility

**Verdict:** READY

---

## Per-plan findings

### PLAN-1.1 — Class + factory-delegate DI registration

**File paths exist:** PASS
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/ConnectionHolder.cs` exists; line 32 confirms `internal class ConnectionHolder : IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>` (researcher §1 verified).
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` exists; outbox `RegisterConditional` block ends at line 74, `//**all` general registrations start at line 76 (researcher §4 verified). Insertion point is precise.
- License header source `ConnectionHolder.cs:1-18` exists.

**API surface matches:** PASS
- `WorkerNotification` ctor signature (6 args) matches Phase 2/3 contract: `IHeaders`, `IQueueCancelWork`, `TransportConfigurationReceive`, `ILogger`, `IMetrics`, `ActivitySource` (per `Source/DotNetWorkQueue/Queue/WorkerNotifications.cs:41-46`).
- `IRelationalWorkerNotification.Transaction` is `DbTransaction` (Phase 2 interface, non-nullable per XML doc).
- `IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>` is the resolvable interface; `NpgsqlTransaction Transaction { get; set; }` at `ConnectionHolder.cs:89` is the property exposed via the interface.
- `ITransportOptionsFactory` (shared abstraction) is registered at PG init line 87 — same as SqlServer.
- `PostgreSqlMessageQueueTransportOptions` is the concrete options type (researcher §2 verified via `find Source/DotNetWorkQueue.Transport.PostgreSQL -iname "*MessageQueueTransportOptions*"`).

**Verify commands runnable:** PASS — all 5 gates use standard `dotnet build`/`dotnet test`/`grep` commands portable to the worktree's bash environment.

### PLAN-2.1 — Receive-path wiring

**File paths exist:** PASS
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueReceive.cs` exists; `GetConnectionAndSetOnContext(IMessageContext)` at line 153 (researcher §3 verified).
- `return connection;` is at line ~170 (the last line of the method body before the trailing `}`).
- `IMessageContext.WorkerNotification` is the seam (from `Source/DotNetWorkQueue/IMessageContext.cs:119` — verified by Phase 3 critique).

**API surface matches:** PASS
- Pattern-match form `is X variable` is C# 7+ syntax; the repo targets net8.0+ — supported.
- `PostgreSqlRelationalWorkerNotification` from PLAN-1.1 lives in the same namespace as the receive class; no using-directive change needed.

**Verify commands runnable:** PASS.

### PLAN-2.2 — Contract tests + SimpleInjector smoke tests

**File paths exist:** PASS
- `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/` directory exists with siblings `PostgreSqlExternalDbNameExtractorTests.cs`, `PostgreSqlRelationalProducerQueueTests.cs` etc. confirming the convention.

**API surface matches:** PASS
- MSTest 4.2.1 pinned (verified during Phase 3 critique against `Source/Directory.Packages.props`).
- `QueueContainer<TTransportInit>(Action<IContainer> registerService, Action<IContainer> setOptions = null)` constructor at `Source/DotNetWorkQueue/QueueContainer.cs:65` is the seam (verified during Phase 3 build).
- `IContainer.Register<TService>(Func<TService>, LifeStyles)` at `Source/DotNetWorkQueue/IContainer.cs:88` is the API for the mock factory registration.
- NSubstitute mocks `ITransportOptionsFactory` (interface — safe to mock).
- `PostgreSqlMessageQueueTransportOptions` (default ctor exists, mutable properties).

**Verify commands runnable:** PASS — same pattern as Phase 3 PLAN-2.2 which executed successfully.

## Cross-cutting

**Forward references:** PASS. PLAN-2.1 and PLAN-2.2 both depend on PLAN-1.1's new class; PLAN-1.1 dependencies = none. PLAN-2.1 and PLAN-2.2 touch different files (different production file vs new test files) — parallel-safe.

**Hidden dependencies:** PASS. PLAN-2.1's receive-path edit is exercised only by existing PG receive-path tests — none modify shared `Transport.RelationalDatabase` code. PLAN-2.2's smoke tests build a full PG container (touching the registration block from PLAN-1.1) but the try/catch fallback (lesson 1, baked into PLAN-1.1 Task 2) handles container.Verify-time failures.

**Complexity flags:** Each plan touches ≤2 files. ≤3 tasks per plan. Total 5 tasks across 3 plans. Phase 4 size = M (4-6h per ROADMAP), matches the scope.

**Most load-bearing assumption:** All five Phase 3 lessons are pre-baked into the plans, so the builder shouldn't re-discover them mid-build. If a Phase-4-specific surprise emerges (e.g., Npgsql-specific options-load timing differs from SqlClient), the try/catch fallback (lesson 1) provides graceful degradation.

## Verdict rationale

READY. All file paths verified against the live PG codebase. All API surface references match. All verification commands are runnable. All five Phase 3 lessons are baked into the plans (no mid-build self-discovery expected). PostgreSQL transport is structurally identical to SqlServer per researcher §1-§5 and per the visible code (`ConnectionHolder`, `MessageQueueInit`, `MessageQueueReceive` all mirror their SqlServer counterparts). Plans are READY for the builder.
