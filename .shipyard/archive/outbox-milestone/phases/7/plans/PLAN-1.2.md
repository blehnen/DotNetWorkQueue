---
phase: documentation
plan: 1.2
wave: 1
dependencies: []
must_haves:
  - Create docs/outbox-pattern.md as tutorial+reference hybrid per CONTEXT-7 Decision 2
  - Single SqlServer worked example (one fenced C# block); PostgreSQL noted inline as a callout, not a duplicate code block
  - Reference sections cover the 5 topics from RESEARCH.md §3 (lifecycle, retry, schema prerequisite, DB-name semantics, supported transports)
  - Style/voice/heading conventions match docs/jenkins-setup.md
files_touched:
  - docs/outbox-pattern.md
tdd: false
risk: low
---

# Plan 1.2: Author `docs/outbox-pattern.md` (tutorial + reference hybrid)

## Context

CONTEXT-7 Decision 1 locks the doc target: `docs/outbox-pattern.md` is the in-repo source
of truth; Wiki is deferred. CONTEXT-7 Decision 2 locks the doc shape: one runnable
end-to-end SqlServer example, then reference sections matching PROJECT.md's structure.

RESEARCH.md §3 provides the full content blueprint (section structure, source-material
map per section, exact callout for PostgreSQL note). RESEARCH.md §7 calls out the style
reference: `docs/jenkins-setup.md` — H2 sections, fenced code blocks, no emojis,
imperative voice.

This is the only doc file written in Phase 7. PROJECT.md §SC #10 is satisfied by this
file landing with the six required content elements: caller-owned-tx lifecycle contract,
caller-owned-retry contract, capability-cast example, schema-deployment prerequisite,
per-provider DB-name comparison semantics, and the "not supported on Memory/Redis/LiteDb/SQLite"
callout.

## Dependencies

None. PLAN-1.2 touches a single new file (`docs/outbox-pattern.md`) that no other Phase 7
plan modifies. Can run in parallel with PLAN-1.1 (csproj edits) and PLAN-1.3 (README bullet).

## Tasks

### Task 1: Write `docs/outbox-pattern.md`

**Files:** `docs/outbox-pattern.md` (new file)
**Action:** create
**Description:** Author a single markdown file with the section structure below. Before
writing, **read `docs/jenkins-setup.md`** to internalize voice, heading style, and code-fence
conventions (imperative voice, H2 section headers, ` ```bash ` / ` ```csharp ` fences, no
emojis).

**Required sections (in order):**

1. **`# Transactional Outbox Pattern`** — H1 title.
2. **`## Overview`** — 2–4 sentence summary: what the pattern solves (atomic business
   INSERT + queue Send), why DNQ's default producer cannot do it (it owns its own
   connection + transaction), how the capability cast is the opt-in surface
   (`IRelationalProducerQueue<T>` extends `IProducerQueue<T>`). Pull from PROJECT.md
   §Description + §Goals.
3. **`## Quick Start`**
   - **`### Prerequisites`** — bullet list: (a) SqlServer or PostgreSQL transport only;
     (b) queue tables must already exist (`QueueCreationContainer<T>` + `CreateQueue()`
     at deploy time).
   - **`### Example: SqlServer`** — one fenced ` ```csharp ` block, end-to-end vertical
     slice covering: resolve `IProducerQueue<MyEvent>`, capability-cast to
     `IRelationalProducerQueue<MyEvent>`, open `SqlConnection`, `BeginTransaction`,
     perform a business `INSERT` on the same connection inside the transaction, call
     `rp.Send(new MyEvent(...), tx)` (sync form is fine for the canonical example;
     mention `SendAsync` exists in the prose around the snippet), `tx.Commit()`. The
     example should compile against types that exist on this branch — do not invent
     types. `MyEvent` can be a sketched POCO with one or two properties.
   - **`#### PostgreSQL note`** — a 2–4 line callout (NOT a duplicate code block): same
     pattern works with `NpgsqlConnection` + `NpgsqlTransaction`; the capability cast
     succeeds identically; the only behavioral difference is the DB-name comparison
     semantics (covered in the reference section).
4. **`## Reference`**
   - **`### Lifecycle Contract`** — bullets per RESEARCH.md §3: caller owns the
     connection + transaction + their disposal; producer never calls
     `Commit`/`Rollback`/`Dispose`/`Close`; ADO.NET transactions are not thread-safe so
     do not call `Send(msg, tx)` from multiple threads on the same transaction
     concurrently. Cite PROJECT.md §Ownership & Threading.
   - **`### Retry Contract`** — the producer's Polly retry decorator is bypassed on the
     caller-tx path via `IRetrySkippable.SkipRetry = true`; to retry the whole business
     operation the caller wraps both writes + `Send` in their own retry policy. Cite
     PROJECT.md §Functional Implementation last bullet + `IRetrySkippable.cs`.
   - **`### Schema Deployment Prerequisite`** — `CreateQueue()` is a one-time deploy-time
     operation; the outbox path requires the schema to already exist. Cite PROJECT.md
     §Non-Goals ("Auto-creation... wrong contract for outbox").
   - **`### Database-Name Comparison Semantics`** — small markdown table with three
     columns (Transport, Extractor, Comparison). Rows: SqlServer +
     `SqlServerExternalDbNameExtractor` + pass-through Ordinal; PostgreSQL +
     `PostgreSqlExternalDbNameExtractor` + pass-through Ordinal. Add a one-line note
     below the table: both extractors are pass-through (no `ToUpperInvariant`), so users
     must configure `Database=<name>` in the connection string exactly as the catalog
     appears. Source: extractor file XML doc remarks.
   - **`### Supported Transports`** — two bullets: Supported (SqlServer, PostgreSQL);
     Not supported (Memory, Redis, LiteDb, SQLite — capability cast returns false for
     all four).

**Constraints:**
- No emojis.
- Imperative voice; second-person ("you") is fine; first-person ("I", "we") is not.
- Code fences use the explicit language tag (` ```csharp `, ` ```bash `).
- File ends with a single trailing newline.
- No images, no diagrams (matches `jenkins-setup.md`).
- File length target: 120–200 lines. If draft exceeds 250 lines, trim — the goal is a
  scannable reference page, not an essay.

**Acceptance Criteria:**
- File exists at `docs/outbox-pattern.md`.
- All six PROJECT.md §SC #10 content elements present (lifecycle, retry, capability-cast
  example, schema prerequisite, DB-name semantics, not-supported callout).
- Exactly one C# fenced code block (the SqlServer example). No second example block for
  PostgreSQL — the PG note is prose.
- Heading hierarchy: H1 once, H2 for top sections, H3 for sub-sections, H4 only for the
  PostgreSQL note inside the example.
- `grep -F "IRelationalProducerQueue" docs/outbox-pattern.md` returns at least three
  matches (overview, example, supported-transports section).
- `grep -F "SqlServerExternalDbNameExtractor" docs/outbox-pattern.md` returns at least
  one match (DB-name semantics table).
- `grep -F "PostgreSqlExternalDbNameExtractor" docs/outbox-pattern.md` returns at least
  one match.

## Verification

```bash
# File exists and has reasonable size
ls -la docs/outbox-pattern.md
wc -l docs/outbox-pattern.md

# All six required content anchors are present
grep -c "## Overview\|## Quick Start\|## Reference\|### Lifecycle Contract\|### Retry Contract\|### Schema Deployment Prerequisite\|### Database-Name Comparison Semantics\|### Supported Transports" docs/outbox-pattern.md
# Expect at least 8 matches (one per heading)

# Capability-cast and extractor type references present
grep -c "IRelationalProducerQueue" docs/outbox-pattern.md
grep -F "SqlServerExternalDbNameExtractor" docs/outbox-pattern.md
grep -F "PostgreSqlExternalDbNameExtractor" docs/outbox-pattern.md

# Exactly one C# fenced block
grep -c '^```csharp' docs/outbox-pattern.md
# Expect 1

# No emojis (heuristic: high-BMP unicode)
LC_ALL=C grep -P '[\x80-\xFF]' docs/outbox-pattern.md
# Expect: no output (or only non-emoji extended ASCII that matches jenkins-setup.md voice)
```

## PROJECT.md Success Criteria coverage

| Plan element | §SC |
|---|---|
| `docs/outbox-pattern.md` exists with lifecycle contract | §SC #10 |
| Retry contract documented | §SC #10 |
| Capability-cast usage example present | §SC #10 |
| Schema-deployment prerequisite documented | §SC #10 |
| Per-provider DB-name comparison semantics documented | §SC #10 |
| "Not supported on Memory/Redis/LiteDb/SQLite" callout present | §SC #10 |
