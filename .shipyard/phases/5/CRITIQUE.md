# CRITIQUE: Phase 5 Plan Feasibility

**Verdict:** CAUTION (proceed with awareness of PLAN-1.1's architectural complexity and Wave 2 init-file overlap)

---

## Per-plan findings

### PLAN-1.1 — SQLite hold-transaction implementation

**File paths exist:** PASS
- `SqLiteMessageQueueReceive.cs`, `Message/ReceiveMessage.cs`, `QueryHandler/ReceiveMessageQueryHandler.cs`, `SqLiteMessageQueueSharedInit.cs` all verified during research.
- New files (`SqLiteConnectionState.cs`, `SqLiteHeaders.cs`) are creates — directories exist.
- License header source `SqLiteMessageQueueInit.cs:1-18` verified.

**API surface matches:** PARTIAL
- `IMessageContext.Set<T>(IMessageContextData<T>, T)` confirmed at `IMessageContext.cs:51`.
- `IDbFactory.CreateConnection` + `IDbFactory.CreateTransaction(connection).BeginTransaction()` confirmed in current `ReceiveMessageQueryHandler.cs:94-97`.
- `ReceiveMessageQuery<IDbConnection, IDbTransaction>` constructor takes `(connection, transaction, ...)` per `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Query/ReceiveMessageQuery.cs` — currently called with `(null, null, ...)` so the params are already there; just need to populate them. **Architect should verify the params are public/settable and not internal.**
- The plan's Task 2 pseudo-code shape uses `_options.Value.EnableHoldTransactionUntilMessageCommitted` — confirm this property name on `SqLiteMessageQueueTransportOptions.cs:278`.
- The `SqLiteHeaders` pattern needs to mirror SqlServer's `IConnectionHeader<,,>` — but SQLite doesn't have an equivalent typed-header. The plan suggests `IMessageContextData<SqLiteConnectionState>` direct construction or a SqLite-specific headers class. **Builder will need to choose at execution time.**

**Verify commands runnable:** PASS — all 5 gates are standard `dotnet build`/`dotnet test`/`grep`.

**Architectural concerns:**
- **Task 2's API signature change** (mutating `ReceiveMessageQueryHandler.Handle` behavior based on query.Connection/Transaction nullability) is a behavior change on a shared `IQueryHandler<ReceiveMessageQuery<,>, ...>` interface. If the shared interface contract documents the params as non-null inputs (vs caller-supplied), this change could be ambiguous. Builder must verify the existing contract.
- **Task 3's connection-creation lift** moves connection creation FROM the query handler INTO the receive layer — significant ownership shift. Cleanup/Commit/Rollback handlers must now read state from context, which means the existing fire-and-forget delegate pattern (`ContextOnCommit` just calling `_handleMessage.CommitMessage.Commit(context)`) needs to be wrapped with the new tx-disposal logic.

**Mitigation:** PLAN-1.1's complexity is the dominant Phase 5 risk. Builder should:
1. Make Task 1 atomic (just the state types) — easy commit.
2. Task 2 (query handler change) atomic — testable in isolation if existing tests cover the option-false path.
3. Task 3 (receive-path wire-up) atomic — most complex; may surface mid-build questions.

### PLAN-2.1 — SQLite inbox notification + DI + receive-path
**File paths exist:** PASS.
**API surface matches:** PASS — Phase 3/4 patterns transfer directly.
**Verify commands runnable:** PASS.

The plan correctly defers the `IMessageContext` access pattern to the builder ("Builder to confirm at execution time and pick whichever resolution works without breaking the `IWorkerNotification` Transient lifecycle"). This is appropriate given the unknowns from PLAN-1.1's execution.

### PLAN-2.2 — SQLite outbox wiring
**File paths exist:** PASS — `SendMessageCommandHandler.cs`, `SendMessageCommandHandlerAsync.cs`, `SendMessage.cs` (batch) all verified.
**API surface matches:** PASS — SqlServer outbox-milestone pattern transfers cleanly to SQLite (already uses `IDbConnection`/`IDbTransaction` interface-level access).
**Verify commands runnable:** PASS.

**Note:** Plan correctly identifies the need to READ `ExternalTransactionValidator` before authoring the wrapper to confirm comparator semantics. Builder must do this before committing the wrapper file.

### PLAN-3.1 — Inbox tests
**File paths exist:** PASS — `Source/DotNetWorkQueue.Transport.SQLite.Tests/Basic/` confirmed.
**API surface matches:** PASS — MSTest 3.x + NSubstitute conventions verified in PG/SqlServer test files.
**Verify commands runnable:** PASS.

### PLAN-3.2 — Outbox tests
**File paths exist:** PASS.
**API surface matches:** PASS.
**Verify commands runnable:** PASS.

## Cross-cutting

**Forward references:** PASS. Wave 2 (PLAN-2.1, PLAN-2.2) depend on PLAN-1.1; Wave 3 depends on Wave 2. Declared correctly.

**Hidden dependencies:**
- **Wave 2 init-file overlap (flagged):** PLAN-2.1 Task 2 and PLAN-2.2 Task 2 both modify `SqLiteMessageQueueSharedInit.cs`. Different insertion locations (inbox block vs outbox `RegisterConditional` block) but same file. Parallel-safe in spirit (additive non-overlapping regions) but git auto-merge needs both builders to fetch before commit. **Mitigation:** if running plans in parallel, execute as a sequential pair instead — losses minimal because both plans are <3 tasks each.

**Complexity flags:**
- PLAN-1.1 touches 5 files across `Basic/`, `Basic/Message/`, `Basic/QueryHandler/`. Crosses 3 directories. Marginal "high-risk" per critique rubric (>3 directories) but below the 10-file threshold. Verdict: CAUTION not REVISE.
- PLAN-2.2 touches 5 files (extractor, wrapper, producer queue, init, 3 send handlers). Crosses 3 directories (Basic/, root, Basic/CommandHandler/). Same CAUTION shape.
- Other plans within thresholds.

**Most load-bearing assumption:** PLAN-1.1's Approach B (context-state-based) is the right call. If the builder discovers that `IMessageContext` lifecycle doesn't support the per-message state pattern cleanly (e.g., context is shared across messages or disposed too early), pivot to Approach A (typed `ConnectionHolder`-style wrapper) is required. The plan acknowledges this latent option but doesn't pre-author Approach A — accepting the risk per CONTEXT-5 §3a.

## Verdict rationale

**CAUTION.** All file paths exist, API surface matches the live codebase, verification commands are runnable. The two Minor findings (PLAN-1.1 architectural size + Wave 2 init-file overlap) are real but manageable:

1. **PLAN-1.1 size:** mitigated by atomic task splits (state model → query handler → receive-path); builder will surface mid-build questions; expect 1-2 retry cycles vs Phase 3/4's zero.
2. **Wave 2 init-file overlap:** mitigated by running PLAN-2.1 and PLAN-2.2 sequentially instead of parallel if concern arises; minimal time cost.

Plans READY for execution. Builder should plan for higher-than-usual iteration on PLAN-1.1; PLAN-2.1, PLAN-2.2, PLAN-3.1, PLAN-3.2 should track the proven Phase 3/4 patterns closely.
