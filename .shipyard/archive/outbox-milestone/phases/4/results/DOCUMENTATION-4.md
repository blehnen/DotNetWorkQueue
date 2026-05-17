# Phase 4 Documentation Review

## Status: SUFFICIENT

Phase 4 ships PostgreSQL-specific implementation built on Phase 2's foundation, mirroring Phase 3 (SqlServer). Per `ROADMAP.md` and `.shipyard/PROJECT.md`, user-facing documentation (`docs/outbox-pattern.md`) is scoped to Phase 7. Phase 4's only documentation requirement is XML doc coverage on every new public type/member, gated by `TreatWarningsAsErrors` + XML doc generation in the Release build. All 3 SUMMARYs report 0 errors / 0 warnings on `dotnet build -c Release` for the PG main project, so the gate is already enforced.

## XML Doc Coverage

5 source files spot-checked:

| File | Public surface added | XML docs |
|---|---|---|
| `PostgreSqlExternalDbNameExtractor.cs` (NEW) | 1 sealed class + 1 method `Extract(DbConnection)` | `<summary>` on class (cross-references `IExternalDbNameExtractor`, explains pass-through rationale + case-sensitive PG semantics + how `NpgsqlConnectionStringBuilder.Database` keeps both sides of the validator byte-consistent); `<summary>` + `<param>` + `<returns>` on method. **Complete.** |
| `PostgreSqlRelationalProducerQueue.cs` (NEW) | 1 sealed generic class + 1 ctor (11 params) + 4 `protected override` hooks + 2 private helpers + 1 private static guard | `<summary>` + `<typeparam>` on class (calls out PG-specialization, fail-fast validator-before-cast ordering, batch sequential-iteration rationale); `<summary>` on ctor; all 11 ctor params have `<param>` docs incl. the `ownMessageFactory` re-injection note carried forward from Phase 3 SqlServer; `<inheritdoc />` on all 4 protected override hooks. Private helpers (`SendOne`, `SendOneAsync`, `GuardNpgsqlTransaction`) — XML doc not required. **Complete.** |
| `PostgreSQLMessageQueueInit.cs` (MODIFIED) | No new public surface — 5 DI registrations added inside existing public `RegisterImplementations()` | Inline comment block documents the registration block (CONTEXT-4 Rule A — `RegisterConditional`). Class/method XML docs already complete from prior phases. **Complete.** |
| `SendMessageCommandHandler.cs` (MODIFIED) | No new public surface — `private long HandleExternalTx(SendMessageCommand)` | Private — not required by Release gate. Plan opted to include `<summary>` + `<param>` + `<returns>` + `<exception>` at lines ~195-199. **Complete (exceeds requirement).** |
| `SendMessageCommandHandlerAsync.cs` (MODIFIED) | No new public surface — `private async Task<long> HandleExternalTxAsync(SendMessageCommand)` | Same as sync. `<summary>` + `<param>` + `<returns>` + `<exception>` at lines ~198-203. **Complete (exceeds requirement).** |

**Verdict:** XML doc coverage is complete on every new public type and member. Release build gate (0 errors / 0 warnings across all 3 SUMMARYs) confirms.

## Architecture / Codebase Docs

`.shipyard/codebase/ARCHITECTURE.md` mentions SqlServer + PostgreSQL transport init wiring (lines 33, 89, 91, 98, 155, 423, 456, 474) but still does NOT describe:

- The outbox / external-transaction producer pattern (introduced in Phase 2, now specialized in TWO transports as of Phase 4)
- `SqlServerRelationalProducerQueue<T>` + `PostgreSqlRelationalProducerQueue<T>` as the first two concrete transport-specific producer subclasses
- The `RegisterConditional` chain-of-responsibility pattern that pins per-transport producer overrides ahead of the base `RelationalProducerQueue<T>` fallback
- The `IExternalDbNameExtractor` extension point and its two implementations (SqlServer normalizes via `.ToUpperInvariant()` because of `MARS`/case-insensitive catalog semantics; PostgreSQL passes through verbatim because of case-sensitive catalog semantics)

Phase 3 documenter recommended deferring this update to Phase 4 or 7 "for the multi-transport picture". Phase 4 now completes the relational multi-transport picture (SqlServer + PostgreSQL both shipped; SQLite/LiteDb are non-relational outbox candidates handled separately). **Recommend continuing to defer to Phase 7 ship** — at that point the architecture-doc update can be written once against the FULL picture including the user-facing `docs/outbox-pattern.md` narrative, integration test evidence (Phase 6), and any final case-extractor variants. Writing it now risks a rewrite when Phase 7 reveals doc-shape constraints.

No update recommended in Phase 4.

## CLAUDE.md "Lessons Learned" candidates

Surfaced during Phase 4 build, ship-time candidates (not phase-scoped):

1. **Meta-lesson: encoding prior-phase lessons as CONTEXT hard rules cuts rediscovery to zero.** Phase 4 had THREE waves land clean first try (no reverts, no rediscovery cycles, no test churn) because CONTEXT-4 promoted three Phase 3 discoveries to upfront hard rules: Rule A (`RegisterConditional` over `Register` to preserve SimpleInjector lazy verification), Rule B (lifecycle-invariant source comment must NOT contain `.Commit()`/`.Rollback()`/`.Close()`/`.Dispose()` substrings so plain grep tests pass without comment-stripping), Rule C (11-param ctor mirroring Phase 3 exactly). Compare to Phase 3 where each of these surfaced as a mid-build issue (PLAN-1.1 lost time on `RegisterConditional`; PLAN-2.1/2.2 both tripped on the source-comment grep). Pattern: when phase N discovers an architecture/test invariant the hard way, promote it to CONTEXT for phase N+1 as a numbered hard rule with explicit failure mode + grep gate. The cost is one extra paragraph in CONTEXT; the saving is a full build cycle per discovery.

2. **PostgreSQL case-sensitivity inverts the SqlServer extractor contract — both are correct.** `SqlServerExternalDbNameExtractor.Extract()` applies `.ToUpperInvariant()` because SQL Server's catalog is case-insensitive and Npgsql/SqlClient surface different casing for the same DB. `PostgreSqlExternalDbNameExtractor.Extract()` does the opposite — returns `connection.Database ?? string.Empty` verbatim — because PG's catalog IS case-sensitive (quoted-identifier `"MyDb"` is a different database from `mydb`) AND because `NpgsqlConnectionStringBuilder.Database` and `NpgsqlConnection.Database` are byte-consistent. Any future transport's extractor must consult provider-specific casing rules; do NOT default to a single normalization strategy. Risk #3 in the Phase 4 plan specifically pinned this with a closure unit test (`connection.Database = "mydb"` + `connInfo.Container = "MyDb"` produces `InvalidOperationException` with both names in the message).

3. **NpgsqlBatch deferral is intentional — PG outbox uses per-message dispatch like SqlServer.** Phase 4 did NOT introduce `NpgsqlBatch`-based bulk send in `SendWithExternalTransactionBatch[Async]`. The override iterates sequentially because ADO.NET `DbTransaction` is not thread-safe and the existing handler signature accepts one `SendMessageCommand` at a time. A future perf phase could introduce a batch handler + `NpgsqlBatch`/`SqlBulkCopy` path, but it requires a new handler contract (`SendMessageBatchCommand`?) that's out of scope for the outbox pattern. The sequential-loop design is the correct shape for this phase; the optimization is a separate decision.

4. **`IDbConnection` discipline pays off again (Phase 4 corollary).** Phase 4's `GuardNpgsqlTransaction` is the ONLY place the PG producer subclass mentions `NpgsqlTransaction` by name. The rest of the producer subclass + the handler `HandleExternalTx[Async]` fork operate on `DbTransaction` / `DbConnection`. The cast is the fail-fast boundary, deliberately isolated to one private static method. This continues the codebase discipline (see CLAUDE.md "The `IDbConnection` abstraction pays off for transport major bumps" lesson) — when Npgsql jumps to v11+, only that one method changes.

These are ship-time candidates. Not blocking Phase 4.

## User-facing docs status

- `docs/outbox-pattern.md` — **scoped to Phase 7**, NOT Phase 4. Confirmed against `ROADMAP.md` §Phase 7 and `PROJECT.md` per-phase scope table. Phase 4 does NOT create this file.
- `README.md` — **N/A for Phase 4**. Phase 7 owns the README pointer to the new outbox doc.
- No other user-facing docs touched or expected in this phase.

## Recommendations

1. **No documentation changes required in Phase 4.** XML doc gate already enforced by the Release build and confirmed by spot check; user-facing docs scoped to Phase 7.
2. **Continue to defer** the `.shipyard/codebase/ARCHITECTURE.md` update for the outbox producer-subclass + `IExternalDbNameExtractor` pattern to Phase 7 ship, where it can be written once against the full multi-transport picture (Phase 7 will already touch user-facing outbox docs).
3. **Roll the 4 "Lessons Learned" candidates above into the CLAUDE.md update at Phase 7 ship** (or earlier milestone close if convenient). Especially #1 (the meta-lesson on CONTEXT-as-rule-bank) — it's a process improvement worth pinning across the project, not just for this milestone.
4. **Phase 7 reminder (additive to Phase 3's reminder)**: when `docs/outbox-pattern.md` is authored, the PG section must cover the case-sensitivity inversion vs SqlServer (Lesson #2 above), the `GuardNpgsqlTransaction` runtime contract (caller must hand in an `Npgsql.NpgsqlTransaction`, not a generic `DbTransaction`), and confirm that the lifecycle-ownership invariant is identical to SqlServer (caller commits / rolls back / disposes — handler never does). All three are pinned by unit tests; the doc needs to surface them to users.
