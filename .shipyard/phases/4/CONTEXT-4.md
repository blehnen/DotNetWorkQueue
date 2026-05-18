# CONTEXT-4: User Decisions for Phase 4 (PostgreSQL Inbox Wiring)

Captured during `/shipyard:plan 4` discussion-capture step on 2026-05-18.

## Phase 4 framing

Per ROADMAP.md lines 80-98, Phase 4 mirrors Phase 3 for PostgreSQL. Structurally a copy with `NpgsqlConnection`/`NpgsqlTransaction` substituted for `SqlConnection`/`SqlTransaction`. Phase 3's success (164/164 SqlServer tests green) validated the pattern; Phase 4 reapplies it verbatim.

## Decisions

### 1. Phase 4 mirrors Phase 3 exactly

Plan structure, file count, wave layout, and verification gates are byte-faithful to Phase 3 with Npgsql substituted for SqlClient.

**Plans:**
- **Wave 1 — PLAN-1.1** (2 tasks): Author `PostgreSqlRelationalWorkerNotification.cs` + factory-delegate registration in `PostgreSQLMessageQueueInit.cs`.
- **Wave 2 — PLAN-2.1** (1 task, depends on PLAN-1.1): Wire `ConnectionHolder` setter into `PostgreSQLMessageQueueReceive.GetConnectionAndSetOnContext`.
- **Wave 2 — PLAN-2.2** (2 tasks, depends on PLAN-1.1, parallel-safe with PLAN-2.1): 6 contract tests + 2 option-driven SimpleInjector smoke tests.

**Why mirror exactly:** Phase 3 worked first-try (with one mid-build self-fix on the factory-delegate try/catch). Replicating the exact wave layout, task split, and verification gates costs nothing and benefits from the proven pattern.

### 2. Class name: `PostgreSqlRelationalWorkerNotification`

The new internal class uses the existing PG transport prefix convention.

**Why:** Matches the existing per-transport type-naming convention. Look at:
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs` ↔ `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs`
- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs` ↔ `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs`

The naming pattern is `SqlServer` for SQL Server and `PostgreSql` (lowercase "q") for PostgreSQL. Phase 3 used `SqlServerRelationalWorkerNotification`; Phase 4 uses `PostgreSqlRelationalWorkerNotification`.

**File path:** `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalWorkerNotification.cs`.
**Namespace:** `DotNetWorkQueue.Transport.PostgreSQL.Basic`.

## Phase 3 lessons to apply verbatim (per VERIFICATION-3 §"Phase-3-specific lessons")

These five lessons drove the Phase 3 build either as designed or via in-build correction. Phase 4 plan authors MUST bake them into the plan tasks so the builder doesn't re-discover them mid-build:

1. **Factory-delegate registrations require try/catch fallback against options-load failures.** PLAN-1.1's DI block must include the try/catch from the outset — falling back to `EnableHoldTransactionUntilMessageCommitted = false` on any exception during options resolution. Mirrors `IBaseTransportOptions` precedent at `PostgreSQLMessageQueueInit.cs:99-103`. Phase 3 broke 6 existing tests by initially omitting this; Phase 4 ships it on the first commit.

2. **`Register<WorkerNotification>(LifeStyles.Transient)` self-registration is redundant.** Core already binds `WorkerNotification` via `ComponentRegistration.cs:217`. Pre-register only the new relational concrete + the factory delegate.

3. **Receive-path setter via pattern-match (not cast).** `if (context.WorkerNotification is PostgreSqlRelationalWorkerNotification relational) { relational.ConnectionHolder = connection; }` — clean no-op on option-false path; no `InvalidCastException` risk.

4. **NSubstitute cannot proxy sealed `NpgsqlTransaction`.** Same limitation as Phase 3's `SqlTransaction`. Contract test for the non-null-return path should be named for what it actually proves (`ConnectionHolder_PropertySet_Does_Not_Throw` style) rather than implying non-null-return coverage. Full reference-equality coverage lands in Phase 7 PG integration tests.

5. **Test seam for option-driven smoke tests:** `QueueContainer<PostgreSQLMessageQueueInit>(registerService, setOptions)` with a mocked `ITransportOptionsFactory` returning a stub `PostgreSqlMessageQueueTransportOptions`. The mock pattern is identical to Phase 3 — only the options type changes.

## Non-decisions (settled upstream)

- **Class visibility:** `internal`, per ROADMAP and Phase 3 precedent.
- **`Transaction` property type:** `System.Data.Common.DbTransaction` (the Phase 2 interface contract; `NpgsqlTransaction` is sealed-but-upcasts-implicitly to `DbTransaction`).
- **No new `NpgsqlConnection` sealed-type casts in handlers.** CLAUDE.md hard rule. ROADMAP success criterion #4 explicitly calls for `rg "NpgsqlConnection" Source/DotNetWorkQueue.Transport.PostgreSQL --type cs` to find only test-fixture/DI usage, not handler casts.
- **No `Tx` abbreviation.** Carry-forward CLAUDE.md feedback.
- **Receive-path edit location:** `GetConnectionAndSetOnContext` in `PostgreSQLMessageQueueReceive.cs`, just before `return connection;` — same shape and placement as Phase 3.

## Scope reminders for plan authors

- All five Phase 3 lessons MUST be designed into the plans (especially #1 try/catch and #5 test seam — both were discovered mid-build in Phase 3).
- File paths use `PostgreSQL` (all-caps SQL) in the project/folder name but `PostgreSql` (lowercase "q") in type names — match existing convention exactly.
- Plan authors confirm `PostgreSQLMessageQueueReceive.cs` has a `GetConnectionAndSetOnContext` (or equivalent) method as the receive-path edit point. Researcher §3 to verify.
- No `Npgsql`-specific behavior expected beyond name substitution; PG's `ConnectionHolder` already exposes `NpgsqlTransaction` via the same `IConnectionHolder<,,>` shape as SqlServer.
