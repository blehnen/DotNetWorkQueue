# Roadmap: Outbox Pattern Support for Relational Transports

Source spec: `.shipyard/PROJECT.md`

This roadmap decomposes the outbox feature into seven phases ordered to **fail-fast on the highest-risk unknown** (the Polly decorator bypass) before any production code is built on top of it. Foundation plumbing is shared between transports, so it precedes both transport implementations. SqlServer ships first as the reference transport; PostgreSQL mirrors its shape with one mid-risk discovery point (`NpgsqlBatch` transaction binding). Unit-test scaffolding rides alongside each implementation phase; integration tests are pooled into one phase that runs after both transports compile so the shared infra (`AppendableConnectionStrings`, atomic-commit harness) is written once. Documentation lands last to capture final API shape.

---

## Phase 1 — Polly Decorator Bypass Spike ✅ complete

**Description.** Discovery spike, no production code shipped to master. Stand up a throwaway branch test harness that resolves `SendMessageCommandHandler` (sync + async) for SqlServer and PostgreSQL **without** the `BeginTransactionRetryDecorator` and any related Polly retry wrappers in the chain. Confirm whether a bare-handler resolution is reachable via existing SimpleInjector seams, a keyed registration, or whether the producer must construct the handler directly. Catalog every decorator in the current `Send` chain (trace, retry, metrics) and decide which must be preserved and which must be skipped on the caller-tx path. Output is a one-page memo committed to the feature branch under `.shipyard/notes/` plus a proof-of-concept test (deleted before phase 2 starts) showing the resolution works.

**Success criteria.**
- A documented resolution strategy for bare-handler access on both transports (one of: keyed registration, producer-side `new`, container child scope).
- An enumerated list of every decorator currently wrapping `SendMessageCommandHandler` and `SendMessageCommandHandlerAsync` on SqlServer + PostgreSQL, with a keep/skip decision per entry.
- Proof-of-concept passes locally (in-memory SimpleInjector resolution test).
- Risk #1 in PROJECT.md "Risk Inventory" is closed or downgraded with a concrete remediation path.

**Dependencies.** None.
**Risk.** Mid → resolves Risk #1 from the inventory. This is the single most likely source of late-phase rework, so it goes first.
**Size.** S (1–3 hours wall time).

---

## Phase 2 — Foundation Layer (RelationalDatabase + Shared Plumbing)

**Description.** Pure additive plumbing in `Transport.RelationalDatabase` with no transport-specific behavior. Introduces:
- `IRelationalProducerQueue<TMessage> : IProducerQueue<TMessage>` interface with the six tx-aware overloads enumerated in PROJECT.md §Functional.
- `RelationalProducerQueue<TMessage>` concrete inheriting the existing producer; tx-aware overloads delegate to a producer-side hook that will be wired in phases 3–4. Stubbed `NotImplementedException` paths are NOT acceptable per CLAUDE.md "compile errors over runtime errors" guidance — overloads either route to the real handler or throw a documented `InvalidOperationException("transport not configured")` only when the foundation interface is somehow registered without a transport binding.
- `SendMessageCommand.ExternalTransaction { get; }` optional property (defaults to null, preserving existing path).
- `IExternalDbNameExtractor` interface in `Transport.RelationalDatabase` with the validation contract from PROJECT.md §Validation. No implementations yet (those live with each transport in phases 3–4).
- `ExternalTransactionValidator` (or equivalent name) running the four checks from PROJECT.md §Functional Validation: non-null tx, non-null connection, `Open` state, database-name match via injected `IExternalDbNameExtractor`.
- XML doc comments on every new public type.

**Success criteria.**
- `Transport.RelationalDatabase` builds clean (net10.0 + net8.0) with `TreatWarningsAsErrors` and CI=true.
- `IRelationalProducerQueue<T>` is `public`; the concrete `RelationalProducerQueue<T>` is `public` so transport DI can register it.
- `SendMessageCommand.ExternalTransaction` is optional and defaults to null; existing SQLite/LiteDB handler tests still pass unmodified.
- `ExternalTransactionValidator` is unit-test-covered with the four validation paths plus the happy path (5 tests).
- No reference to `Microsoft.Data.SqlClient` or `Npgsql` in this project (purity invariant — RelationalDatabase must not depend on a specific transport).

**Dependencies.** Phase 1 (its conclusions inform whether `RelationalProducerQueue<T>` needs a bare-handler delegate hook in its constructor).
**Risk.** Low — pure additive shared code, no behavior change to existing transports.
**Size.** M (4–6 hours).

---

## Phase 3 — SqlServer Implementation + Unit Tests

**Description.** Implements the SqlServer half of the feature end-to-end. The full vertical slice: handler fork, validator, DI wiring, and unit tests in the same phase to keep the seam definitions and the tests they exercise co-located.

- `SendMessageCommandHandler` and `SendMessageCommandHandlerAsync` in `Transport.SqlServer` get a `HandleExternalTx(...)` fork called when `command.ExternalTransaction != null`. The fork: uses `tx.Connection` as the connection, sets `cmd.Connection` and `cmd.Transaction` on every command, never calls `Commit`/`Rollback`/`Dispose`/`Close`, reuses the existing static SQL builders.
- Batch `Send` handler (`SendMessageCommandBatch*` or equivalent — confirm name during spike) gets the same fork.
- `SqlServerExternalDbNameExtractor : IExternalDbNameExtractor` using `conn.Database` with `StringComparer.OrdinalIgnoreCase`.
- `SqlServerMessageQueueInit` registers `IRelationalProducerQueue<T>` → `RelationalProducerQueue<T>` and the per-provider extractor. The producer factory is updated to return the concrete `RelationalProducerQueue<T>` so callers can capability-cast.
- Polly bypass implemented per Phase 1 resolution strategy.
- Unit tests (~7–8) targeting: handler-fork branch selection on null vs non-null `ExternalTransaction`, validator wired into both `Send` and `SendAsync`, no `Commit/Rollback/Dispose/Close` calls on mocked `DbTransaction`+`DbConnection`, OrdinalIgnoreCase comparison correctness, retry decorator NOT invoked on the caller-tx path. Use `IDbConnectionFactory` injection per CLAUDE.md lesson; mock `DbConnection`/`DbCommand`/`DbDataReader` (not interfaces) for async handlers per CLAUDE.md lesson.

**Success criteria.**
- `Transport.SqlServer` builds clean (net10.0 + net8.0).
- All new SqlServer unit tests pass; existing SqlServer unit tests still pass.
- Capability cast works in a SimpleInjector resolution smoke test: `container.GetInstance<IProducerQueue<MyEvent>>() is IRelationalProducerQueue<MyEvent>` is `true` for SqlServer.
- Mock-based unit test confirms zero `Commit`/`Rollback`/`Dispose`/`Close` calls on the caller's `DbTransaction` or its connection (PROJECT.md §Success Criteria #7).
- Polly retry bypass unit test confirms a single attempt on transient failure (PROJECT.md §Success Criteria #8).

**Dependencies.** Phase 2 (interface + validator + `ExternalTransaction` command property must exist).
**Risk.** Mid — primary transport, sets the pattern for PostgreSQL. Some chance the spike conclusions need adjustment when applied to a real handler.
**Size.** L (8–12 hours).

---

## Phase 4 — PostgreSQL Implementation + Unit Tests

**Description.** Mirrors phase 3 for PostgreSQL. The batch path is the unique discovery point: confirm during this phase whether the PostgreSQL batch send uses `NpgsqlBatch` (Risk #2) and verify it correctly inherits the active transaction from its associated connection. If `NpgsqlBatch` doesn't bind cleanly, fall back to looping single inserts inside the caller's tx — note the deviation in the wiki draft.

- `SendMessageCommandHandler` + `SendMessageCommandHandlerAsync` in `Transport.PostgreSQL` get the same `HandleExternalTx(...)` fork as SqlServer.
- Batch path: verify `NpgsqlBatch` transaction binding via integration test scaffolding before relying on it; spike a single-loop fallback if it doesn't. Document the fallback decision in `docs/outbox-pattern.md` (Phase 7).
- `PostgreSqlExternalDbNameExtractor : IExternalDbNameExtractor` using `conn.Database` with `StringComparer.Ordinal` (case-sensitive per PostgreSQL semantics).
- `PostgreSQLMessageQueueInit` registers `IRelationalProducerQueue<T>` and the per-provider extractor; producer factory returns the concrete relational producer.
- Unit tests (~5–7) mirroring phase 3, plus the quoted-identifier edge case test (Risk #3): a queue name with mixed case must round-trip through the validator correctly given PostgreSQL's case-sensitive semantics.
- All transport handlers continue to operate on `IDbConnection` (no `NpgsqlConnection` casts inside handlers — CLAUDE.md hard rule).

**Success criteria.**
- `Transport.PostgreSQL` builds clean (net10.0 + net8.0).
- All new PostgreSQL unit tests pass; existing tests still pass.
- Capability cast works for PostgreSQL via SimpleInjector smoke test.
- Risk #2 closed: `NpgsqlBatch` transaction-binding decision documented (works → use it; doesn't → fallback loop).
- Risk #3 closed: case-sensitive validator unit test green.
- No `NpgsqlConnection` casts anywhere in PostgreSQL handlers (grep check).

**Dependencies.** Phase 2 (foundation). Does **not** depend on Phase 3 — same-wave with SqlServer if we want parallelism, but ordered sequentially here to capture pattern adjustments from phase 3's review before duplicating mistakes.
**Risk.** Mid — `NpgsqlBatch` is the open question. Mitigated by handling it inside this phase rather than carrying it into integration.
**Size.** L (8–12 hours).

---

## Phase 5 — Negative-Path Coverage: Non-Relational Transports

**Description.** Defensive phase: confirm the capability-cast pattern correctly **fails** on transports that should not implement the new interface. No production code change expected here — this is verification that Phase 2's design didn't accidentally leak into transports that don't need it.

- Unit tests in `Transport.Memory.Tests`, `Transport.Redis.Tests`, `Transport.LiteDb.Tests`, `Transport.SQLite.Tests`: resolve `IProducerQueue<T>` from the transport container and assert `producer is IRelationalProducerQueue<T>` is `false`.
- Grep-style assertion in CI (or a build-time check) that the four non-relational transport assemblies do not reference `IRelationalProducerQueue<T>`.
- Confirm SQLite is explicitly deferred (negative test that SQLite producer does NOT implement `IRelationalProducerQueue<T>` even though it's the closest transport in shape).

**Success criteria.**
- 4 negative-path unit tests pass (one per non-relational transport).
- Build still green on net10.0 + net8.0 across all transports.
- PROJECT.md §Success Criteria #2 satisfied.

**Dependencies.** Phases 3 + 4 (need the interface registered on the right transports first to verify it's absent from the wrong ones).
**Risk.** Low — pure assertion of an invariant Phase 2 established.
**Size.** S (1–2 hours).

---

## Phase 6 — Integration Tests (SqlServer + PostgreSQL)

**Description.** ~22 integration tests, 11 per transport, slotting into the existing Jenkins SqlServer + PostgreSQL stages. No Jenkinsfile changes. Coverage is **method-matrix driven**, not scenario-driven — every public method on `IRelationalProducerQueue<T>` must have its own integration test exercising the caller-tx path. This is required because codecov coverage on this repo is driven primarily by integration tests, and `SendMessageCommandHandlerAsync` is a separate class from `SendMessageCommandHandler`, so async branches are not inferred from sync coverage.

Per transport, the test matrix:

**A. Method × outcome coverage (8 tests per transport):**

| Method | Commit test | Rollback test |
|---|---|---|
| `Send(T, DbTransaction)` | ✓ both rows visible | ✓ neither row visible |
| `SendAsync(T, DbTransaction)` | ✓ both rows visible | ✓ neither row visible |
| `Send(IEnumerable<...>, DbTransaction)` (batch) | ✓ all N + business row | ✓ neither |
| `SendAsync(IEnumerable<...>, DbTransaction)` (batch) | ✓ all N + business row | ✓ neither |

Each pair (commit + rollback) confirms atomic semantics with a parallel business write inside the same caller tx.

**B. `IAdditionalMessageData` round-trip (1 test per transport):**

- Enqueue via `Send(msg, additionalData, tx)` with custom headers/correlation ID → commit caller tx → dequeue with a consumer in a separate connection → assert all `additionalData` values round-tripped intact.
- Exercises the `IAdditionalMessageData` overloads' code path and confirms metadata table writes flow through the caller tx.

**C. Validation (2 tests per transport):**

- **Cross-database:** caller tx connection points to database A, queue configured for database B → `InvalidOperationException` before any DB write; queue tables remain unchanged.
- **Connection-state:** caller passes a closed connection (or a disposed-tx-with-null-connection) → `InvalidOperationException`; queue tables remain unchanged.

**D. Retry bypass (1 test per transport):**

- Force a transient SQL error mid-send (e.g., short timeout + lock conflict on SqlServer, equivalent on PostgreSQL) → exception propagates to caller on the first attempt; attempt count (via metrics listener) = 1, not 3.

**Implementation notes:**
- Use existing integration-test scaffolding (`connectionstring.txt`, queue-per-test isolation, Coverlet via `dotnet test`).
- Queue names use `Guid.NewGuid().ToString("N")` (CLAUDE.md lesson — DNQ rejects hyphens).
- Tests must be wave-isolated (own queue + own caller-tx connection per test) to allow Jenkins parallel execution.
- Each test runs the caller's business INSERT through a second simple table created at test setup, so atomic semantics are directly verifiable.
- Metrics listener for retry-bypass test must use polling, not snapshot, to avoid the race documented in CLAUDE.md ("integration test metrics assertions can race").

**Success criteria.**
- 22 new integration tests pass locally against a real SqlServer and a real PostgreSQL.
- Jenkins SqlServer + PostgreSQL integration stages green on a draft PR (PROJECT.md §Success Criteria #11).
- PROJECT.md §Success Criteria #4, #5, #6 satisfied with explicit test names mapped to each.
- Coverlet line coverage on the new `HandleExternalTx` (sync + async) and batch external-tx forks shows ≥1 hit per branch in both transports.
- No new flakiness on retries (CLAUDE.md lesson on metrics-snapshot races: poll, don't snapshot).

**Dependencies.** Phases 3 + 4.
**Risk.** Mid — first time the full path runs against real DBs; transactional semantics across Microsoft.Data.SqlClient and Npgsql can surprise. Doubled test count vs. prior draft increases the surface for flakiness, mitigated by strict wave-isolation per test.
**Size.** L (14–18 hours).

---

## Phase 7 — Documentation + Wiki Draft

**Description.** Final deliverable: docs and wiki page closing PROJECT.md §Success Criteria #10 (ship-blocking per Risk #4).

- XML doc comments on every public type/member added in phases 2–4 (interface overloads, validator, extractors, producer). Build with XML doc generation enabled — any missing-doc warning is a `TreatWarningsAsErrors` build break.
- In-repo documentation page at `docs/outbox-pattern.md` (location locked at brainstorm time, matches the existing `docs/jenkins-setup.md` convention):
  - Caller-owned-transaction lifecycle contract (PROJECT.md §Ownership & Threading).
  - Caller-owned-retry contract (PROJECT.md §Functional Implementation last bullet).
  - Capability-cast usage example from PROJECT.md §Functional New Public API.
  - Schema-deployment prerequisite (`CreateQueue` once at deploy time).
  - Per-provider DB-name comparison semantics (OrdinalIgnoreCase vs Ordinal).
  - Explicit "not supported on Memory/Redis/LiteDb/SQLite" callout.
- Update `README.md` with a one-paragraph pointer to the new docs.
- Verify Source Link works on Release build with `-p:CI=true` (PROJECT.md §Non-Functional Determinism).

**Success criteria.**
- `dotnet build -c Release -p:CI=true` of `DotNetWorkQueueNoTests.sln` produces no XML-doc warnings.
- Wiki draft reviewed and approved (manual gate).
- README points at the new page.
- PROJECT.md §Success Criteria #10 satisfied.

**Dependencies.** Phases 3 + 4 (final API shape) + Phase 6 (any docs-discoveries from integration). Phase 5 is independent.
**Risk.** Low (mechanically simple) but **ship-blocking** per PROJECT.md Risk #4.
**Size.** M (3–4 hours).

---

## Ordering Summary

```
Phase 1 (Spike)        ──┐
                         ├──> Phase 2 (Foundation) ──┬──> Phase 3 (SqlServer)     ──┐
                                                    └──> Phase 4 (PostgreSQL)    ──┤
                                                                                    ├──> Phase 5 (Negative-path) ──┐
                                                                                    └──> Phase 6 (Integration)   ──┤
                                                                                                                    └──> Phase 7 (Docs)
```

Phases 3 and 4 could run in parallel after Phase 2 lands, but ordering them sequentially captures pattern-refinement from phase 3 review and avoids the CLAUDE.md "builder agents stall on bulk edits" failure mode by keeping each transport its own focused milestone. Phase 5 and Phase 6 can run in parallel after both transport phases land.

## Cross-Cutting Notes (for plan authors)

- **`IDbConnection` discipline:** No sealed-type casts in handlers. Use `IDbConnectionFactory` injection for testability. CLAUDE.md lesson, repeated here so plan authors see it.
- **Async handler mocking:** Mock `DbConnection`/`DbCommand`/`DbDataReader` abstract bases, not interfaces. CLAUDE.md lesson.
- **Jenkins PR-trigger:** Open a draft PR before expecting feature-branch CI; `git push` alone won't trigger.
- **Queue naming in tests:** `Guid.NewGuid().ToString("N")` to avoid hyphen rejection.
- **Builder-agent stall avoidance:** Plans should size individual tasks to single-file or single-handler scope. Per CLAUDE.md, multi-file bulk edits hit stalls — split aggressively.
