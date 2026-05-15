# Phase 7 Discussion Capture

**Date:** 2026-05-15
**Phase:** 7 — Documentation + Wiki Draft

## Decisions captured

### Decision 1: Doc target scope — `docs/` only; Wiki deferred

**Decision:** Phase 7 ships `docs/outbox-pattern.md` as the in-repo source of truth. README points to it. **Wiki update is deferred to a manual post-ship task** (matches ROADMAP §Phase 7 "Wiki draft reviewed and approved (manual gate)" wording — the gate is manual, not automated).

**Rationale:**
- Existing precedent: `docs/jenkins-setup.md` is the only in-repo doc; README already says "See the [Wiki] for in-depth documentation."
- Wiki content is typically written by the author against the published API surface, not generated upfront in a planning phase.
- Keeps Phase 7 scope tight and within-repo (no GitHub Wiki API access needed in the build flow).

**Implication for plans:** No plan should attempt to push to the GitHub Wiki. Plans target `docs/outbox-pattern.md`, `README.md`, and XML doc comments on production code.

### Decision 2: `docs/outbox-pattern.md` style — tutorial + reference hybrid

**Decision:** The doc opens with a **runnable end-to-end code example** (caller-owned `SqlTransaction`, business INSERT + queue `Send`, atomic commit; capability cast from `IProducerQueue<T>` to `IRelationalProducerQueue<T>`). After the example, **reference sections** cover:

1. **Lifecycle contract** — caller owns connection + transaction + retry; producer never commits/rolls-back/disposes; PROJECT.md §Ownership & Threading.
2. **Retry contract** — `IRetrySkippable` short-circuits the producer's Polly chain on the caller-tx path; the caller owns retry semantics. PROJECT.md §Functional Implementation last bullet.
3. **Schema deploy prerequisite** — `QueueCreationContainer<T>` + `CreateQueue()` is a one-time deploy-time operation; the outbox path requires the schema already exists.
4. **Per-provider DB-name comparison semantics** — SqlServer uses `OrdinalIgnoreCase`, PostgreSQL uses `Ordinal` (per Phase 3/4 extractor implementation). Cross-DB validator behavior differs accordingly.
5. **Supported transports** — explicit "supported on SqlServer + PostgreSQL only; not supported on Memory / Redis / LiteDb / SQLite" callout.

**Rationale:**
- Tutorial-first is more discoverable for developers landing on the doc from a Google search.
- Reference sections match PROJECT.md's structure and serve as the long-term lookup surface.
- Two heavy tutorials (e.g., EF Core integration sketch) would push scope beyond ROADMAP M-size.

**Implication for plans:** Plan for ONE worked example (SqlServer commit path is the canonical case; PG variation noted inline as a "PostgreSQL note" callout, not a duplicate code block).

### Decision 3: README pointer placement — under "High-level features" bullet list

**Decision:** Add one bullet to the existing "High-level features" list immediately after the cron job-scheduler bullet:
> - Transactional outbox pattern on SqlServer / PostgreSQL (see [`docs/outbox-pattern.md`](docs/outbox-pattern.md))

**Rationale:**
- README's existing top section is feature-driven; outbox is a feature.
- A dedicated H2 section would crowd the README which is already long with installation tables.
- The bullet keeps the README's information density consistent.

**Implication for plans:** README edit is a single-line addition. NOT a new section. NOT a multi-paragraph block.

## Non-decisions / locked from ROADMAP

These are locked by ROADMAP §Phase 7 itself, not by this discussion:

- **XML doc comments** on every public type/member added in phases 2–4. Required to clear `TreatWarningsAsErrors` on Release build.
- **Source Link verification** via `dotnet build -c Release -p:CI=true` of `DotNetWorkQueueNoTests.sln`.
- **Phase 7 success criteria** (ROADMAP):
  - Release `-c Release -p:CI=true` Debug build produces zero XML-doc warnings.
  - Wiki draft reviewed and approved (manual gate — see Decision 1; deferred to post-ship).
  - README points at the new page.
  - PROJECT.md §SC #10 satisfied.

## Open questions for plan authors

- **Which exact public types/members from Phases 2–4 need XML docs?** RESEARCH agent must enumerate by walking `IRelationalProducerQueue<T>`, the `Send`/`SendAsync` overloads + their batch variants, `RelationalProducerQueue<T>` base + transport subclasses (`SqlServerRelationalProducerQueue<T>`, `PostgreSqlRelationalProducerQueue<T>`), `ExternalTransactionValidator`, `IExternalDbNameExtractor`, and the SqlServer + PostgreSQL extractor implementations. Phase 5's `IRelationalProducerQueue<T>` check tests in non-relational transports DO NOT add public types — they are test-only.
- **README's existing badge/installation table format must be preserved** — no formatting churn.
- **`docs/outbox-pattern.md` should follow `docs/jenkins-setup.md`'s heading/voice/code-fence conventions.** Architect should ensure plans direct authors to read that file as the style reference.
