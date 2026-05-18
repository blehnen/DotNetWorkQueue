# CONTEXT-5: User Decisions for Phase 5 (SQLite Inbox + SQLite-Outbox Sweep)

Captured during `/shipyard:plan 5` discussion-capture step on 2026-05-18.

## Phase 5 framing

Per ROADMAP.md lines 101-134, Phase 5 is the largest in the milestone — SQLite vertical slice combining BOTH inbox and outbox-sweep work in a single phase. Rationale: both halves touch `SqLiteMessageQueueInit` registration; splitting across two phases would force the second phase to immediately re-edit files the first phase just landed.

**Structural divergence from SqlServer/PG (Phase 3/4):** Initial scout revealed SQLite transport does NOT have:
- A typed `ConnectionHolder<SqliteConnection, SqliteTransaction, SqliteCommand>` wrapper.
- A `GetConnectionAndSetOnContext(IMessageContext)` method in the receive class.

SQLite uses `IDbFactory` + `IDbConnection`/`IDbTransaction` interface-level access throughout. This means the Phase 3/4 inbox pattern (settable typed `ConnectionHolder` property + receive-path pattern-match setter) does NOT mechanically transfer. The researcher must map the actual SQLite seam and confirm an internal-only injection point exists.

## Decisions

### 1. Plan layout: 4 plans across 2 waves

- **Wave 1 (parallel, 2 plans):**
  - **PLAN-1.1** — Inbox wiring: new `SqliteRelationalWorkerNotification` class + factory-delegate DI registration in `SqLiteMessageQueueInit` + receive-path wire-up (whatever seam the researcher identifies).
  - **PLAN-1.2** — Outbox wiring: `SqliteExternalDbNameExtractor` + `SqliteNormalizedConnectionInformation` wrapper + `IRelationalProducerQueue<T>` registration + `HandleExternalTx` forks in `SendMessageCommandHandler` and `SendMessageCommandHandlerAsync` (+ batch path).

- **Wave 2 (parallel, 2 plans, both depend on Wave 1):**
  - **PLAN-2.1** — Inbox tests (~5-7).
  - **PLAN-2.2** — Outbox tests (~5-7).

**Why this layout:**
- Best matches ROADMAP guidance ("split into per-half plan files").
- Parallel-safe within each wave (different file sets between inbox and outbox).
- Honors CLAUDE.md "builder agents stall on bulk edits" by keeping each plan ≤3 tasks.
- Wave 2 parallel saves wall-clock vs sequential tests.

### 2. NormalizedConnectionInformation wrapper bundled with extractor (PLAN-1.2)

The `SqliteExternalDbNameExtractor` + `SqliteNormalizedConnectionInformation` wrapper are a matched pair per Phase 1 spike §3 (symmetric `Path.GetFullPath()` + `OrdinalIgnoreCase` on BOTH sides of the validator comparator, plus `:memory:` short-circuit). Both land in PLAN-1.2; their tests go in PLAN-2.2.

**Why bundled, not split:** They form a single semantic unit. Splitting would force PLAN-1.2's verification to wait for a separate wrapper plan, and a future reader debugging DB-name comparison failures wants one place to grok the whole story.

### 3a. AMENDMENT (2026-05-18 post-research): Inbox gate fired REFACTOR REQUIRED — user opted to expand scope

After RESEARCH.md surfaced the §2 verdict (SQLite has `EnableHoldTransactionUntilMessageCommitted` declared but never read; substantive refactor required to implement hold-transaction semantics), the user **chose to expand Phase 5 scope to include both the hold-tx precursor AND the inbox wiring**, rather than the recommended scope-reduce-to-outbox-only path.

**New Phase 5 scope:** XL+ (20-30h estimated)
- Hold-tx implementation (new — was never built for SQLite)
- Inbox notification wiring (mirrors Phase 3/4 but on the new hold-tx infrastructure)
- Outbox sweep (per original Phase 5 plan)
- All combined tests

**Revised plan layout (5 plans, 3 waves):**

- **Wave 1 (sequential, 1 plan — foundational):**
  - **PLAN-1.1** — SQLite hold-transaction implementation. Restructure `SqLiteMessageQueueReceive` + `Message/ReceiveMessage` + `QueryHandler/ReceiveMessageQueryHandler` to keep the dequeue tx alive across the user-handler invocation. Add per-`IMessageContext` state to hold the tx. Add `EnableHoldTransactionUntilMessageCommitted` branches. ~3 tasks. Architect chooses pattern (likely mirrors SqlServer/PG `ConnectionHolder`-style approach OR uses context-set-based seam — pick whichever is most consistent with existing SQLite transport idioms).

- **Wave 2 (parallel, 2 plans; both depend on PLAN-1.1):**
  - **PLAN-2.1** — Inbox notification class + factory-delegate DI + receive-path setter (mirrors Phase 3/4; reads from PLAN-1.1's new tx state). ~3 tasks.
  - **PLAN-2.2** — Outbox wiring: `SqLiteExternalDbNameExtractor`, `SqliteNormalizedConnectionInformation` wrapper, `SqliteRelationalProducerQueue<T>`, `IRelationalProducerQueue<T>` registration, `HandleExternalTx` forks in send handlers. ~3 tasks.

- **Wave 3 (parallel, 2 plans; both depend on Wave 2):**
  - **PLAN-3.1** — Inbox tests (~5-7). Same shape as Phase 3/4 PLAN-2.2.
  - **PLAN-3.2** — Outbox tests (~5-7).

**Total: 5 plans, ~15 tasks, 3 waves.**

**Risk acknowledgment:** Most of PLAN-1.1's work is architectural (new hold-tx implementation on SQLite). Mid-build surprises could push effort further. Phase 4's inline-build posture may not scale to PLAN-1.1's complexity — architect should consider dispatching the builder agent for PLAN-1.1 specifically (where the architectural reasoning is most valuable) and using inline builds for PLAN-2.x and PLAN-3.x (which are pattern-mirrors of Phase 3/4).

### 3b. ORIGINAL Inbox feasibility gate (now superseded by §3a)

The researcher's first job is to map SQLite's transaction-creation point in the receive path and confirm an `internal`-only injection seam exists or can be added without a substantive refactor.

**Two outcomes:**

- **If seam exists or trivial addition** → proceed with the full 4-plan Phase 5 as scoped above. PLAN-1.1 includes the specific seam implementation.

- **If substantive refactor needed** (e.g., the only seam requires reshaping `IQueryHandler<ReceiveMessageQuery<,>, ...>` which has reachthrough impacts on `Transport.RelationalDatabase.Basic.Query`) → **stop the architect dispatch, surface the ISSUE to the user, and let the user decide**:
  - File `ISSUE-NEW` capturing the architectural blocker.
  - Reduce Phase 5 scope to outbox-only (PLAN-1.2 + PLAN-2.2).
  - SQLite inbox addressed in a follow-up phase or future milestone.
  - PROJECT.md §Scope lists "Three relational transports" — partial ship-with-issue is preferable to scope creep that pushes Phase 5 from L (10-14h) to XL+.

**Why this gate:** ROADMAP estimates Phase 5 at L (10-14h) on the assumption SQLite mirrors SqlServer/PG structurally. Initial scout proves it does not. Confirming feasibility before committing to plans prevents late-discovered scope blowup.

### 4. Naming convention: `Sqlite*` (verify via researcher)

Phase 4 surfaced a PG-specific gotcha: filename `PostgreSQL` (all-caps) but type `PostgreSql` (lowercase q). For SQLite, the existing convention appears uniform:
- File: `SqLiteMessageQueueInit.cs` (note the `SqLite` casing — capital L)
- Class: `SqLiteMessageQueueInit`
- Existing related types follow the same `SqLite`/`Sqlite` patterns

The new classes should be:
- `SqliteRelationalWorkerNotification` (inbox)
- `SqliteExternalDbNameExtractor` (outbox)
- `SqliteNormalizedConnectionInformation` (outbox wrapper)
- `SqliteRelationalProducerQueue<T>` (outbox, mirrors `SqlServerRelationalProducerQueue<T>`)

Researcher §X to verify the convention is `Sqlite` (lowercase) vs `SqLite` (capital L) by surveying existing related types.

## Phase 3/4 lessons to apply verbatim

All 5 Phase 3 lessons (re-validated by Phase 4 as transferable) apply to PLAN-1.1's inbox half:

1. **Factory-delegate try/catch fallback** to `false` from the first commit — mirrors `IBaseTransportOptions` precedent at the SQLite init's options-loading point.
2. **No `Register<WorkerNotification>` self-registration** — core already binds it.
3. **Receive-path setter via pattern-match (not cast)** — clean no-op on option-false path. (If the SQLite seam differs structurally, adapt the principle: never throw `InvalidCastException` on the option-false path.)
4. **Contract Test naming: `ConnectionHolder_PropertySet_Does_Not_Throw`-style** for any test where the underlying transaction-equivalence cannot be NSubstitute-mocked due to sealed-type limitations.
5. **Smoke-test seam:** `QueueContainer<SqLiteMessageQueueInit>(registerService, setOptions)` with mocked `ITransportOptionsFactory` returning a stub `SqLiteMessageQueueTransportOptions`.

Plus Phase 4 carry-over: **check SQLite init for `using DotNetWorkQueue.Queue;` gap** (Phase 4 had to add this to PG init; PLAN-1.1 should pre-emptively verify SQLite init).

## Outbox milestone lessons to apply (PLAN-1.2)

Outbox milestone Phase 3 + Phase 4 patterns transfer to PLAN-1.2 (verbatim where possible):

- **`HandleExternalTx` fork in send handlers:** when `command.ExternalTransaction != null`, route through caller's connection + tx; never `Commit`/`Rollback`/`Dispose`/`Close` the caller's resources.
- **`IRelationalProducerQueue<T>` registration via `RegisterConditional`** (open generic, matches existing pattern in SqlServer/PG init).
- **No `Tx` abbreviation** in any of the new code (outbox milestone caught this drift in Phase 6 review; pre-empt here).
- **`-using DotNetWorkQueue.Configuration;` for tutorial code blocks** — outbox milestone caught this in REVIEW-1.2; PLAN-1.2 reviewer must trace any example code against actual API surface.

## Non-decisions (settled upstream)

- **`SqliteExternalDbNameExtractor` lives in `Transport.SQLite/Basic/`** per CONTEXT-2 (SQLite-specific path-normalization semantics decided in spike §3). Not in shared `Transport.RelationalDatabase`.
- **DB-name comparison semantics:** `Path.GetFullPath()` + `StringComparer.OrdinalIgnoreCase` + `:memory:` short-circuit (spike §3).
- **No platform-conditional code** in extractor or wrapper (PROJECT.md §Constraints Technical "platform-uniform").
- **Class visibility:** all new transport-specific classes are `internal` unless interface contracts require otherwise.
- **`Transaction` property type:** `System.Data.Common.DbTransaction` (Phase 2 interface contract).

## Open issues review (`.shipyard/ISSUES.md`)

Three open issues at planning time (ISSUE-033/-034/-035) — all PG smoke-test residue. **None material to Phase 5.**

## Scope reminders for plan authors

- **Phase 5 is the milestone's largest phase. Resist scope creep beyond what's listed here.**
- The 4-plan layout assumes the inbox seam is feasible per the researcher's investigation. If the gate (Decision §3) fires, planning re-scopes to outbox-only and the user reviews.
- Plan-1.1's seam implementation is researcher-driven — architect should not commit to a specific seam shape before reading RESEARCH.md.
- All five Phase 3 + outbox-milestone lessons MUST be baked into the plans from the outset. The Phase 4 build proved this approach yields zero mid-build self-fixes.
- Inline build/plan authoring posture (no subagent dispatches) is validated for structural-mirror phases — but Phase 5's structural divergence from Phase 3/4 may warrant returning to dispatched agents for the architecturally novel parts (researcher in particular).
