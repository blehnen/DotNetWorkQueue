# Roadmap: Inbox Pattern Support for Relational Transports (+ SQLite-Outbox Sweep)

Source spec: `.shipyard/PROJECT.md`

This roadmap decomposes the inbox milestone into **eight phases** ordered to fail-fast on the highest-risk unknowns (heartbeat audit + command-timeout audit + SQLite DB-name comparison semantics) before any production seam is added. Foundation plumbing is shared across the three relational transports, so it precedes per-transport wiring. SqlServer ships first as the reference inbox transport; PostgreSQL mirrors its shape; SQLite combines inbox wiring with the SQLite-outbox sweep into a single phase because both halves touch the same `SqLiteMessageQueueInit` registration block. Negative-path tests on Memory/Redis/LiteDb confirm the capability cast cleanly fails. Integration tests pool into one phase across all three relational transports (24 inbox + 12 SQLite-outbox = 36 tests). Documentation lands last as the ship-blocker per PROJECT.md Risk #6.

---

## Phase 1 — Discovery Spike (Heartbeat Audit + Timeout Audit + SQLite DB-Name Decision) ✅ complete

**Description.** Discovery spike, no production code shipped to master. Three independent investigations land as a single `.shipyard/notes/inbox-spike.md` memo on the feature branch:

1. **Heartbeat audit (Risk #1).** Confirm that on each of the three relational transports (SqlServer, PostgreSQL, SQLite), heartbeats are *not* fired against the held connection when `EnableHoldTransactionUntilMessageCommitted = true`. Use Grep + targeted code reading to trace the heartbeat scheduler's connection acquisition path on each transport. Confirm-or-deny per transport; if any transport unexpectedly fires heartbeats during a held tx, file an ISSUE and document the limitation — do NOT redesign heartbeats in this milestone (PROJECT.md Risk #1).
2. **Timeout audit (Risk #2).** Enumerate every `IDbCommand` issued by the library on the held connection between handler dispatch and `RemoveMessage` completion. For each, confirm its `CommandTimeout` is configurable and compatible with a slow user handler. Output: a per-command table (command name × transport × current timeout × configurable yes/no) + a sizing-recommendation paragraph for the docs phase.
3. **SQLite DB-name comparison decision (Risk #3).** SQLite "DB names" are file paths. Decide the comparison strategy applied uniformly across platforms: `Path.GetFullPath()` + `OrdinalIgnoreCase` (Windows-precedent, permissive on Linux) vs `Path.GetFullPath()` + `Ordinal` (strict, matches Linux fs). Make the call; document the rationale; lock the choice for Phase 5 implementation. The chosen semantics MUST be platform-uniform (PROJECT.md Constraints §Technical).

Output is a single memo committed to `.shipyard/notes/inbox-spike.md`; a throwaway proof-of-concept may live transiently on the branch but is deleted before Phase 2 starts.

**Success criteria.**
- Memo lands at `.shipyard/notes/inbox-spike.md` with the three audits answered explicitly.
- Heartbeat audit closes Risk #1 per transport: each of SqlServer/PostgreSQL/SQLite has a documented yes/no answer with file-line citations.
- Timeout audit produces the per-command table; any "not configurable + tight" command is flagged for follow-up (file ISSUE if remediation is out of scope).
- SQLite DB-name comparison choice + canonicalization strategy locked; rationale documented.
- Risks #1, #2, #3 in PROJECT.md "Risk Inventory" are each closed or downgraded with a concrete path forward.

**Dependencies.** None.
**Risk.** Mid → resolves the three audit risks. This is the single most likely source of late-phase rework, so it goes first.
**Size.** S (2–4 hours wall time).

---

## Phase 2 — Foundation Layer (`IRelationalWorkerNotification` + `SqliteExternalDbNameExtractor`) ✅ complete (extractor deferred to Phase 5)

**Description.** Pure additive plumbing in `Transport.RelationalDatabase` with no transport-specific behavior. Introduces:

- `IRelationalWorkerNotification : IWorkerNotification` interface (`public`) in `DotNetWorkQueue.Transport.RelationalDatabase` with a single member `DbTransaction Transaction { get; }`. XML doc establishes the contract: when implemented, `Transaction` is non-null; presence of the interface IS the capability assertion (PROJECT.md §Functional New Public API).
- `SqliteExternalDbNameExtractor : IExternalDbNameExtractor` lands in `Transport.RelationalDatabase` *only if* the spike's path-normalization decision makes it transport-shared; if the chosen semantics are SQLite-specific, it lives in `Transport.SQLite` and Phase 2 stops at the interface alone. The architect's plan author for this phase reads the spike memo and places the extractor accordingly.
- XML doc comments on every new public type/member.

**Notes for plan authors.**
- The inbox foundation is **much lighter** than the outbox foundation — no validator analog is needed (library owns the tx by construction), no new command property (`ConnectionHolder` already exposes the tx internally), and no producer-queue type. Resist the urge to over-scope to match outbox Phase 2.
- ADO.NET types stay out of the root `DotNetWorkQueue` assembly — `DbTransaction` is in `System.Data.Common` and may appear in `Transport.RelationalDatabase` (the project already references it via the existing relational handler types).

**Success criteria.**
- `Transport.RelationalDatabase` builds clean (net10.0 + net8.0) with `TreatWarningsAsErrors` and `-p:CI=true`.
- `IRelationalWorkerNotification` is `public` with full XML doc.
- If the extractor lives in this phase: unit-test coverage on path normalization + DB-name comparison semantics chosen in spike (PROJECT.md §Success Criteria #7).
- No reference to `Microsoft.Data.SqlClient`, `Npgsql`, or `Microsoft.Data.Sqlite` introduced in this project.
- Existing SqlServer/PostgreSQL/SQLite/LiteDb/Memory/Redis unit tests still pass unmodified.

**Dependencies.** Phase 1 (spike conclusions drive extractor placement + comparison semantics).
**Risk.** Low — pure additive shared code; no behavior change to any existing transport.
**Size.** S (2–3 hours).

---

## Phase 3 — SqlServer Inbox Wiring + Unit Tests ✅ complete

**Description.** Implements the SqlServer half of the inbox feature end-to-end. Full vertical slice: per-transport relational notification impl, factory branch, DI wiring, unit tests in the same phase.

- New `internal` notification class in `Transport.SqlServer` implementing `IRelationalWorkerNotification`. The class pulls the `DbTransaction` from the per-message `ConnectionHolder` (existing infrastructure — no change to `ConnectionHolder` public surface per PROJECT.md §Functional Internal Implementation).
- The class declares only `: IRelationalWorkerNotification` — interface inheritance covers `IWorkerNotification` (PROJECT.md §Functional New Public API).
- SqlServer's `WorkerNotification` factory branches on `EnableHoldTransactionUntilMessageCommitted`: option `true` → construct the new relational impl; option `false` → construct the existing (non-relational) impl, leaving the capability cast to cleanly fail.
- DI wiring through `SqlServerMessageQueueInit` registers the factory branch.
- Unit tests (~6–8): factory returns correct impl per option value; relational impl exposes the holder's `DbTransaction`; existing notification path unchanged when option is false; capability cast succeeds against the relational impl, fails against the existing impl. Use `IDbConnectionFactory` injection per CLAUDE.md lesson; for any async-handler-related mocking use `DbConnection`/`DbCommand` abstract bases (not interfaces) per CLAUDE.md lesson.

**Success criteria.**
- `Transport.SqlServer` builds clean (net10.0 + net8.0).
- All new SqlServer unit tests pass; existing SqlServer unit tests still pass.
- SimpleInjector resolution smoke test: with `EnableHoldTransactionUntilMessageCommitted = true`, the consumer's notification `is IRelationalWorkerNotification` and `r.Transaction` is non-null. With the option false, the cast fails (PROJECT.md §Success Criteria #2).
- The new notification impl class is `internal` (PROJECT.md §Constraints Technical).
- No new `SqlConnection` sealed-type casts introduced.

**Dependencies.** Phase 2 (interface must exist).
**Risk.** Mid — first transport, sets the pattern. The seam (`ConnectionHolder`-exposure) is established but its sync/async path interactions need careful tracing.
**Size.** M (5–8 hours).

---

## Phase 4 — PostgreSQL Inbox Wiring + Unit Tests ✅ complete

**Description.** Mirrors Phase 3 for PostgreSQL. Mechanically a copy with `NpgsqlConnection`/`NpgsqlTransaction` substituted (but, per CLAUDE.md hard rule, no sealed-type casts in handler code — operate on `IDbConnection` / `DbTransaction` only).

- New `internal` notification class in `Transport.PostgreSQL` implementing `IRelationalWorkerNotification`, pulling the tx from the PG `ConnectionHolder`.
- PostgreSQL's `WorkerNotification` factory branches on `EnableHoldTransactionUntilMessageCommitted`.
- DI wiring through `PostgreSQLMessageQueueInit`.
- Unit tests (~6–8) mirroring Phase 3.

**Success criteria.**
- `Transport.PostgreSQL` builds clean (net10.0 + net8.0).
- All new PostgreSQL unit tests pass; existing tests still pass.
- Capability cast smoke test green for PostgreSQL.
- No `NpgsqlConnection` casts anywhere in PostgreSQL handlers (grep check — `rg "NpgsqlConnection" Source/DotNetWorkQueue.Transport.PostgreSQL --type cs` finds only test-fixture or DI-registration usages, not handler casts).

**Dependencies.** Phase 2. Does **not** depend on Phase 3 — same-wave possible, but ordered sequentially here to absorb any pattern adjustments from Phase 3 review (matches outbox milestone's ordering rationale, and respects CLAUDE.md "builder agents stall on bulk edits" by keeping each transport a focused phase).
**Risk.** Low — structurally identical to Phase 3, no fresh `NpgsqlBatch` investigation needed (that risk was burned down in the outbox milestone Phase 4).
**Size.** M (4–6 hours).

---

## Phase 5 — SQLite Inbox Wiring + SQLite-Outbox Sweep + Unit Tests (Combined) ✅ complete (EXPANDED scope: included hold-tx implementation; fork unit tests deferred to Phase 7)

**Description.** SQLite vertical slice combining BOTH directions in one phase. Rationale for combining: both halves touch the same `SqLiteMessageQueueInit` registration block; splitting them across two phases would cause the second phase to immediately re-edit files the first phase just landed, with little testability win. Inbox path adds the notification impl; outbox path adds `IRelationalProducerQueue<T>` registration + per-handler `HandleExternalTx` forks.

**Inbox half (mirrors Phases 3+4):**
- New `internal` notification class in `Transport.SQLite` implementing `IRelationalWorkerNotification`, pulling the tx from the SQLite `ConnectionHolder`.
- SQLite's `WorkerNotification` factory branches on `EnableHoldTransactionUntilMessageCommitted`.

**Outbox half (mirrors outbox milestone Phases 3+4):**
- `SqliteExternalDbNameExtractor` lands here if not already placed by Phase 2 (per spike decision). Comparison semantics per spike §3.
- `SqLiteMessageQueueInit` registers `IRelationalProducerQueue<T>` → `RelationalProducerQueue<T>` and the SQLite extractor.
- `SendMessageCommandHandler` and `SendMessageCommandHandlerAsync` in `Transport.SQLite` gain the same `HandleExternalTx` fork as SqlServer + PostgreSQL — when `command.ExternalTransaction != null`, route through caller's connection + tx, never `Commit`/`Rollback`/`Dispose`/`Close`. Batch path same fork.

**Unit tests (~10–14 combined):**
- Inbox: factory branch + capability cast (~5–7 tests, mirroring Phase 3).
- Outbox: handler fork branch selection, no `Commit/Rollback/Dispose/Close` on caller tx, extractor round-trip + comparison semantics, retry decorator NOT invoked on caller-tx path (~5–7 tests, mirroring outbox milestone Phase 3).
- Use `IDbConnectionFactory` injection; mock `DbConnection`/`DbCommand` abstract bases for async; no `SqliteConnection` sealed-type casts.

**Notes for plan authors.**
- This phase is the largest in the milestone — plan authors should split into separate PLAN-W.P files: one for inbox wiring, one for outbox wiring, one for the test suite. Single-builder-agent-per-plan discipline per CLAUDE.md "builder agents stall on bulk edits."
- Per CLAUDE.md, `LiteDbConnectionManager`-style lessons do NOT apply here — SQLite has full ADO.NET seams via `IDbConnectionFactory`. Don't reach for reflection-based testability workarounds.

**Success criteria.**
- `Transport.SQLite` builds clean (net10.0 + net8.0).
- All new SQLite unit tests pass; existing tests still pass.
- Inbox capability cast smoke test green for SQLite (PROJECT.md §Success Criteria #2).
- Outbox capability cast smoke test green for SQLite: `container.GetInstance<IProducerQueue<MyEvent>>() is IRelationalProducerQueue<MyEvent>` is `true`.
- Mocked unit test confirms zero `Commit`/`Rollback`/`Dispose`/`Close` calls on the caller's `DbTransaction` or connection in SQLite-outbox path (PROJECT.md §Success Criteria #8).
- SQLite extractor unit test covers path round-trip + the comparison semantics from spike §3 (PROJECT.md §Success Criteria #7).
- No `SqliteConnection` sealed-type casts in handlers (grep check).

**Dependencies.** Phase 2 (interface + extractor placement).
**Risk.** Mid — largest phase in the milestone; the combination is structurally simple per-half but the merged surface increases edit volume. Mitigated by splitting into per-half plan files.
**Size.** L (10–14 hours).

---

## Phase 6 — Negative-Path Coverage: Non-Relational Transports ✅ complete

**Description.** Defensive phase: confirm the capability-cast pattern correctly **fails** on transports that should not implement `IRelationalWorkerNotification`. Mirrors the outbox milestone's Phase 5 in shape. No production code change expected — pure verification that Phase 2's design didn't accidentally leak into Memory/Redis/LiteDb.

- Unit tests in `Transport.Memory.Tests`, `Transport.Redis.Tests`, `Transport.LiteDb.Tests`: resolve the consumer queue's notification; assert `notification is IRelationalWorkerNotification` is `false`.
- Confirm the four non-relational/quasi-relational transport assemblies (Memory, Redis, LiteDb) do not reference `IRelationalWorkerNotification` — grep check or build-time invariant.
- Note: SQLite is now a positive case (Phase 5 wires it up), so it is NOT in this phase's negative-path list.

**Success criteria.**
- 3 negative-path unit tests pass (one per non-relational transport).
- Build still green on net10.0 + net8.0 across all transports.
- PROJECT.md §Success Criteria #3 satisfied.
- Grep check confirms no `IRelationalWorkerNotification` references in Memory/Redis/LiteDb assembly source.

**Dependencies.** Phases 3 + 4 + 5 (need the interface registered on the right transports first to verify it's absent from the wrong ones).
**Risk.** Low — pure assertion of an invariant established in Phase 2.
**Size.** S (1–2 hours).

---

## Phase 7 — Integration Tests (SqlServer + PostgreSQL + SQLite, Inbox + SQLite-Outbox)

**Description.** 36 integration tests total — 24 inbox (8 per transport × 3 transports) + 12 SQLite-outbox (mirroring outbox milestone Phase 6's per-transport matrix). Slots into the existing Jenkins SqlServer + PostgreSQL + SQLite stages. No Jenkinsfile changes.

### Inbox Matrix (24 tests, 8 per transport)

Per relational transport (SqlServer, PostgreSQL, SQLite), 8 tests:

| Handler | Path | Commit test | Rollback test |
|---|---|---|---|
| Sync `IReceivedMessage<T>` | Inbox | ✓ business row + queue row visible | ✓ neither visible |
| Async `IReceivedMessage<T>` | Inbox | ✓ business row + queue row visible | ✓ neither visible |

Each pair confirms atomic dequeue+business-write semantics: the handler casts notification to `IRelationalWorkerNotification`, writes to a business table on `r.Transaction.Connection`, returns normally (commit case) or throws (rollback case). Verification is from a separate connection.

Two of the 8 per transport are dedicated to "option false" negative paths — with `EnableHoldTransactionUntilMessageCommitted = false`, the cast fails and handler code paths that depend on it must surface a discoverable error (not a NullReferenceException).

### SQLite-Outbox Matrix (12 tests)

Mirrors outbox milestone Phase 6's per-transport coverage:

**A. Method × outcome (8 tests):**

| Method | Commit | Rollback |
|---|---|---|
| `Send(T, DbTransaction)` | ✓ | ✓ |
| `SendAsync(T, DbTransaction)` | ✓ | ✓ |
| `Send(IEnumerable<...>, DbTransaction)` (batch) | ✓ | ✓ |
| `SendAsync(IEnumerable<...>, DbTransaction)` (batch) | ✓ | ✓ |

**B. `IAdditionalMessageData` round-trip (1 test):** custom headers/correlation ID round-trip through the caller-tx path.

**C. Validation (2 tests):** cross-database (file-path mismatch per spike §3 semantics) + connection-state (closed connection).

**D. Retry bypass (1 test):** transient error → propagates on first attempt, retry decorator not invoked.

### Implementation notes

- Use existing integration-test scaffolding (`connectionstring.txt`, queue-per-test isolation, Coverlet via `dotnet test`).
- Queue names use `Guid.NewGuid().ToString("N")` — CLAUDE.md lesson on DNQ hyphen rejection.
- Tests must be wave-isolated (own queue + own connection per test) for Jenkins parallel execution.
- Each inbox test runs the business INSERT through a second simple table created at test setup, so atomic semantics are directly verifiable.
- Metrics listener for retry-bypass test must use polling, not snapshot, per CLAUDE.md "integration test metrics assertions can race."
- SQLite tests must explicitly exercise the file-path canonicalization decided in spike §3.
- For the trace-decorator coverage path, register an `ActivityListener` for the `ActivitySource` per CLAUDE.md lesson — otherwise trace-decorator code shows 0% coverage in tests.

**Success criteria.**
- 36 new integration tests pass locally against real SqlServer, real PostgreSQL, and real SQLite.
- Jenkins SqlServer + PostgreSQL + SQLite integration stages green on the milestone PR (PROJECT.md §Success Criteria #11).
- PROJECT.md §Success Criteria #4, #5, #6 satisfied with explicit test names mapped to each.
- Coverlet line coverage on the new SQLite `HandleExternalTx` forks shows ≥1 hit per branch.
- No new flakiness on retries.
- SQLite single-writer concurrency characteristic (Risk #4) observed and documented for Phase 8.

**Dependencies.** Phases 3 + 4 + 5.
**Risk.** Mid — largest phase by test count (36); SQLite single-writer behavior under hold-tx is a real-DB unknown (Risk #4) that surfaces here for the first time. Mitigated by strict wave-isolation per test and by treating SQLite concurrency observations as documentation input rather than blockers.
**Size.** XL (16–22 hours) — exceeds the L threshold; plan authors should split into per-transport plan files (Inbox SqlServer / Inbox PG / Inbox SQLite / SQLite-outbox) for parallel builder execution where independent.

---

## Phase 8 — Documentation (`docs/inbox-pattern.md` + Outbox Page Update + README Pointer)

**Description.** Final deliverable, ship-blocker per PROJECT.md Risk #6.

- New page at `docs/inbox-pattern.md`:
  - Library-owned-transaction lifecycle contract (PROJECT.md §Ownership & Threading Inbox).
  - "Heartbeats disabled in hold-tx mode" explanation, citing the spike §1 audit results per transport.
  - Worked example: handler casts notification to `IRelationalWorkerNotification`, writes business row inside the exposed tx, returns to commit (or throws to roll back).
  - Per-provider DB-name comparison semantics for outbox (now including SQLite per spike §3).
  - SQLite single-writer concurrency callout (Risk #4) as a user-visible characteristic, with sizing recommendation from spike §2 (timeout audit).
  - Explicit "not supported on Memory/Redis/LiteDb" callout.
- Update `docs/outbox-pattern.md`:
  - Add SQLite to the supported-transports list.
  - Add SQLite to the per-provider DB-name semantics table.
  - Preserve existing SqlServer + PostgreSQL content verbatim where unchanged.
  - REVIEWER must trace any new tutorial code block against the actual API surface to catch missing `using` statements (CLAUDE.md lesson from outbox milestone — `using DotNetWorkQueue.Configuration;` was missing from a tutorial code block).
- Update `README.md` with a one-paragraph pointer to the new docs alongside the existing outbox pointer.
- XML doc comments verified on every public type/member added in Phases 2–5; build with XML doc generation enabled — any missing-doc warning is a `TreatWarningsAsErrors` build break.
- Verify Source Link works on Release build with `-p:CI=true` (PROJECT.md §Non-Functional Determinism).

**Success criteria.**
- `dotnet build -c Release -p:CI=true` of `DotNetWorkQueueNoTests.sln` produces no XML-doc warnings (NU1902 `<WarningsNotAsErrors>` carry-forward remains acceptable per repo Phase-7 lesson).
- `docs/inbox-pattern.md` exists with all six listed sections.
- `docs/outbox-pattern.md` updated with SQLite addition; existing SqlServer/PG content untouched verbatim.
- README updated with new pointer.
- PROJECT.md §Success Criteria #9, #10, #12 satisfied.

**Dependencies.** Phases 3 + 4 + 5 (final API shape) + Phase 7 (any docs-discoveries from integration, particularly SQLite single-writer observations). Phase 6 is independent.
**Risk.** Low (mechanically simple) but **ship-blocking** per PROJECT.md Risk #6.
**Size.** M (3–5 hours).

---

## Ordering Summary

```text
Phase 1 (Spike) ──> Phase 2 (Foundation) ──┬──> Phase 3 (SqlServer Inbox)            ──┐
                                           ├──> Phase 4 (PostgreSQL Inbox)           ──┤
                                           └──> Phase 5 (SQLite Inbox + Outbox)      ──┤
                                                                                       ├──> Phase 6 (Negative-path)    ──┐
                                                                                       └──> Phase 7 (Integration ×36)  ──┤
                                                                                                                          └──> Phase 8 (Docs)
```

Phases 3, 4, and 5 could run in parallel after Phase 2 lands, but ordering them sequentially captures pattern-refinement from earlier transports and avoids the CLAUDE.md "builder agents stall on bulk edits" failure mode by keeping each transport its own focused milestone. Phase 6 and Phase 7 can run in parallel after Phases 3+4+5 land. Phase 8 always last (ship-blocker, depends on Phase 7's real-DB observations).

## Cross-Cutting Notes (for plan authors)

- **`IDbConnection` discipline:** No sealed-type casts (`SqlConnection`, `NpgsqlConnection`, `SqliteConnection`) in handler code. Use `IDbConnectionFactory` injection for testability. CLAUDE.md hard rule; PROJECT.md §Non-Functional explicit constraint.
- **Async handler mocking:** Mock `DbConnection`/`DbCommand`/`DbDataReader` abstract bases, not interfaces — async methods (`OpenAsync`/`ExecuteReaderAsync`/`ReadAsync`) live on the bases. CLAUDE.md lesson.
- **ADO.NET types out of the root assembly:** `IRelationalWorkerNotification` carries `DbTransaction` and MUST live in `Transport.RelationalDatabase`, never the root `DotNetWorkQueue` project. CLAUDE.md + PROJECT.md hard rule.
- **No `Tx` abbreviation:** Use full word "transaction" in identifiers and prose. The outbox milestone caught this drift in Phase 6 review; pre-empt it here. Naming: `ExternalTransaction`, `Transaction`, `RelationalWorkerNotification` — never `*Tx*`. CLAUDE.md feedback.
- **Jenkins PR-trigger:** Open a draft PR before expecting feature-branch CI; `git push` alone won't trigger. `gh pr create --draft --base master --head <branch>`.
- **Queue naming in tests:** `Guid.NewGuid().ToString("N")` to avoid DNQ's hyphen rejection.
- **Builder-agent stall avoidance:** Plans should size individual tasks to single-file or single-handler scope. Per CLAUDE.md, multi-file bulk edits hit stalls — split aggressively. Phase 5 and Phase 7 in particular must split into multiple plan files.
- **Trace-decorator coverage:** Integration tests that exercise trace decorators must register an `ActivityListener` for the `ActivitySource` — otherwise `ActivitySource.StartActivity()` returns null and the decorator chain short-circuits silently with 0% coverage. CLAUDE.md lesson.
- **MSTest 3.x assertions:** Use `Assert.ThrowsExactly<T>` (not `Assert.ThrowsException<T>` from MSTest 2.x). After multi-file concurrent test edits, `rm -rf obj bin` before chasing phantom compile errors. CLAUDE.md lesson.
- **SQLite path normalization is platform-uniform:** Decision locked in spike §3; do NOT reintroduce platform-conditional logic in Phase 5 implementation. PROJECT.md §Constraints Technical.
- **Symmetric string normalization:** If Phase 5 introduces `.ToUpperInvariant()` (or any case-fold) on the SQLite extractor side, the validator's comparator on the other side MUST apply identical normalization, or both sides apply none. CLAUDE.md "string-comparator drift" lesson from outbox milestone.
- **CVE/advisory hygiene:** No new package bumps in this milestone; if any are needed, cite the advisory's "Patched versions" field verbatim per CLAUDE.md lesson.
