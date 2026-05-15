# Phase 3 Documentation Review

## Status: SUFFICIENT

Phase 3 ships SqlServer-specific implementation built on Phase 2's foundation. Per `ROADMAP.md`, user-facing documentation (`docs/outbox-pattern.md`) is Phase 7's responsibility — Phase 3's only documentation requirement is XML doc coverage on new public types/members, gated by `TreatWarningsAsErrors` + XML doc generation in the Release build. All three SUMMARYs report 0 errors / 0 warnings on `dotnet build -c Release`, so the gate is already enforced.

## XML Doc Coverage

7 files spot-checked:

| File | Public surface added | XML docs |
|---|---|---|
| `SqlServerExternalDbNameExtractor.cs` (NEW) | 1 sealed class + 1 method `Extract(DbConnection)` | `<summary>` on class (cross-references `IExternalDbNameExtractor`, explains case-normalization rationale + Phase 2 PLAN-2.1 architect note); `<summary>` + `<param>` + `<returns>` on method. **Complete.** |
| `SqlServerRelationalProducerQueue.cs` (NEW) | 1 sealed generic class + 1 ctor (11 params) + 4 `protected override` hooks + 2 private helpers + 1 private static helper | `<summary>` + `<typeparam>` on class (explains SqlServer specialization + lifecycle-ownership invariant); `<summary>` on ctor; all 11 ctor params have `<param>` docs (incl. explicit note on `ownMessageFactory` re-injection rationale — answers the Phase 4 mirror question raised in the review); `<inheritdoc />` on all 4 protected override hooks (correctly inherits from base contract). Private helpers `SendOne`, `SendOneAsync`, `GuardSqlTransaction` are private — XML doc not required. **Complete.** |
| `SQLServerMessageQueueInit.cs` (MODIFIED) | No new public surface — only 5 DI registrations added inside existing public `RegisterImplementations()` method | Inline comment `// Phase 3: outbox-pattern producer wiring (SqlServer side)` at line 63 documents the registration block. Class/method already had complete XML docs from prior phases. **Complete.** |
| `SendMessageCommandHandler.cs` (MODIFIED) | No new public surface — `private long HandleExternalTx(SendMessageCommand)` added | Private member — XML doc not required by the Release gate. Plan opted to include `<summary>` + `<param>` (lines 186, 196) anyway. **Complete (exceeds requirement).** |
| `SendMessageCommandHandlerAsync.cs` (MODIFIED) | No new public surface — `private async Task<long> HandleExternalTxAsync(SendMessageCommand)` added | Same as sync handler. **Complete (exceeds requirement).** |
| `SqlServerExternalDbNameExtractorTests.cs` (NEW) | Test class — no XML doc requirement | n/a |
| `SqlServerRelationalProducerQueueTests.cs` + 2 fork smoke test files (NEW) | Test classes — no XML doc requirement | n/a |

**Verdict:** XML doc coverage is complete on every new public type and member. The Release build gates (0 errors / 0 warnings across all 3 SUMMARYs) already proved this; spot check confirms.

## Architecture / Codebase Docs

`.shipyard/codebase/ARCHITECTURE.md` mentions SqlServer init wiring at lines 89, 98, 155, 423, 456, 474 but does NOT yet describe:
- The outbox / external-transaction producer pattern (introduced in Phase 2, specialized in Phase 3)
- `SqlServerRelationalProducerQueue<T>` as the first concrete transport-specific producer subclass
- The `RegisterConditional` pattern used to chain-of-responsibility the per-transport producer override with the base `RelationalProducerQueue<T>` fallback

This is an **information-oriented gap, not a Phase 3 blocker.** Phase 7 (user-facing `docs/outbox-pattern.md`) is the natural place to introduce the user-side narrative. The architecture-doc update for the new producer-subclass pattern is best deferred to Phase 7 ship (or Phase 4 ship if PostgreSQL mirror lands first), where it can be written once against the full multi-transport picture.

No update recommended in Phase 3.

## CLAUDE.md "Lessons Learned" Candidates

Three lessons surfaced during Phase 3 build that are worth capturing in CLAUDE.md "Lessons Learned" at ship time (not now — that section is curated at milestone close, not per-phase):

1. **`RegisterConditional` preserves SimpleInjector lazy verification semantics over plain `Register`.** When a transport-specific producer subclass needs to claim `IProducerQueue<>` ahead of the base fallback, use `container.RegisterConditional(typeof(IProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton)` (predicate defaults to `c => !c.Handled` per `ContainerWrapper.cs`). Plain `Register` on the same open-generic triggers eager-verification diagnostics that surface unrelated pre-existing transient-IDisposable warnings (`IMessageContext`, `IWorker`, `IPrimaryWorker`) and breaks any `Assert.ThrowsExactly<SqlException>` tests by throwing `ActivationException` first. Phase 3 Wave 1 lost time discovering this; documenting it saves Phase 4 PostgreSQL the same trip.

2. **Test grep guards must strip line comments before scanning, OR the source comment must not contain the forbidden substrings.** Both Wave 2 builders (sync + async handler fork) independently tripped on a load-bearing source comment `// Deliberately NO trans.Commit() / Rollback() / Dispose() / sqlConn.Close().` which contained every forbidden substring the lifecycle-ownership smoke test was checking for. Two valid resolutions (one strips comments in the test, the other rephrases the source comment); either is fine, but the gotcha needs to be in the playbook for any future "structural grep over source body" test pattern.

3. **`SqlServerRelationalProducerQueue<T>` re-injects `IMessageFactory` twice because Phase 2's `RelationalProducerQueue<T>` seals its copy as private.** The 11-param constructor takes `messageFactory` (forwarded to `base()`) plus `ownMessageFactory` (kept for the caller-tx dispatch path) — same instance, both names. If Phase 4 mirrors this for PostgreSQL, either accept the same shape, or refactor Phase 2's base to expose `_messageFactory` as `protected` first. Documenting this avoids a "why is this injected twice" question on every transport mirror.

These are not blockers and not Phase 3's deliverable — they're ship-time candidates. Flagging here for the orchestrator to roll into the CLAUDE.md update at Phase 7 ship.

## User-facing Docs Status

- `docs/outbox-pattern.md` — **scoped to Phase 7**, not Phase 3. Confirmed against `ROADMAP.md` §Phase 7 and `PROJECT.md` per-phase scope table. Phase 3 does NOT need to create this file.
- `README.md` — **N/A for Phase 3**. Phase 7 will add the README pointer to the new outbox doc when the user-facing surface is complete (handler fork + producer subclass + integration tests + reference docs all landed across Phases 2–6).
- No other user-facing docs touched or expected in this phase.

## Recommendations

1. **No documentation changes required in Phase 3.** XML doc gate already enforced by the Release build and confirmed by spot check.
2. **Defer** the three "Lessons Learned" candidates above to ship time (Phase 7 close, or earlier if convenient at a milestone boundary). They are valuable but not phase-scoped.
3. **Defer** the architecture-doc note on transport-specific producer subclassing to Phase 4 or Phase 7 ship — write it once against the multi-transport picture, not once per transport.
4. **Phase 7 reminder**: when `docs/outbox-pattern.md` is authored, include the `GuardSqlTransaction` runtime contract (caller must hand in a `Microsoft.Data.SqlClient.SqlTransaction`, not a generic `DbTransaction`) and the lifecycle-ownership invariant (caller commits / rolls back / disposes — handler never does). Both are already pinned by unit tests; the doc needs to surface them to users.
