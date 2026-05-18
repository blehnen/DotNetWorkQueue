# CONTEXT-2: User Decisions for Phase 2 (Foundation Layer)

Captured during `/shipyard:plan 2` discussion-capture step on 2026-05-18.

## Phase 2 framing

Phase 2 is "Foundation Layer (`IRelationalWorkerNotification` + `SqliteExternalDbNameExtractor`)" per `ROADMAP.md` lines 32-54. The roadmap left two placement decisions open pending the Phase 1 spike outcome (`.shipyard/notes/inbox-spike.md`):

1. Whether `SqliteExternalDbNameExtractor` lives in `Transport.RelationalDatabase` (shared) or `Transport.SQLite` (transport-specific).
2. Whether the symmetric-normalization wrapper (`NormalizedConnectionInformation`-style, per spike §3) lands in Phase 2 or Phase 5.

Spike §3 chose `Path.GetFullPath()` + `OrdinalIgnoreCase` with a `:memory:` short-circuit — semantics that are SQLite-specific. Existing precedent (`SqlServerExternalDbNameExtractor`, `PostgreSqlExternalDbNameExtractor`) is also per-transport placement, not shared.

## Decisions

### 1. Phase 2 scope: interface-only

Phase 2 ships ONLY the `IRelationalWorkerNotification` interface in `Transport.RelationalDatabase`. `SqliteExternalDbNameExtractor` (and any symmetric-normalization wrapper) defers to Phase 5.

**Rationale.**
- The `:memory:` short-circuit and path-canonicalization rules are SQLite-specific — placing them in `Transport.RelationalDatabase` would force a `Microsoft.Data.Sqlite` reference into shared code (constraint violation per ROADMAP line 48).
- Per-transport extractor placement matches the existing SqlServer / PostgreSQL convention.
- Roadmap line 37 explicitly covers this branch: "if the chosen semantics are SQLite-specific, it lives in `Transport.SQLite` and Phase 2 stops at the interface alone."
- Keeps Phase 2 at the roadmap-estimated S size (2–3 hours) and truly additive — zero behavior change.

### 2. NormalizedConnectionInformation wrapper: defer to Phase 5

The symmetric-normalization requirement from spike §3 ("BOTH sides of the comparator MUST apply the same `Path.GetFullPath()` + `OrdinalIgnoreCase`") lands together with the SQLite extractor in Phase 5.

**Rationale.**
- Spike §3 wording is "Phase 2 / Phase 5 must add" — the wrapper belongs where the symmetry is actually exercised.
- Nothing in Phase 2 consumes the wrapper; landing it early risks design drift if Phase 5 discovers a different normalization shape.
- Keeps Phase 2 strictly additive: interface + XML docs, no consumers.

## Non-decisions (settled upstream)

- **Interface namespace.** `DotNetWorkQueue.Transport.RelationalDatabase` per PROJECT.md §Functional New Public API and ROADMAP line 36. Not in the root `DotNetWorkQueue` assembly (ADO.NET types stay out of root per ROADMAP line 268).
- **Interface shape.** `public interface IRelationalWorkerNotification : IWorkerNotification` with single member `DbTransaction Transaction { get; }`. Capability-cast pattern (presence of interface = capability assertion). Per ROADMAP line 36 and PROJECT.md.
- **Naming.** No `Tx` abbreviation — `Transaction` spelled in full (CLAUDE.md feedback, ROADMAP line 269).
- **XML docs.** Required on every new public type/member; build with XML doc generation enabled (`TreatWarningsAsErrors` + `-p:CI=true`).

## Scope reminders for plan authors

- Pure additive plumbing. No transport-specific code in this phase.
- No reference to `Microsoft.Data.SqlClient`, `Npgsql`, or `Microsoft.Data.Sqlite` may be introduced in `Transport.RelationalDatabase`.
- Existing SqlServer / PostgreSQL / SQLite / LiteDb / Memory / Redis unit tests must pass unmodified.
- Build clean (net10.0 + net8.0) with `TreatWarningsAsErrors` and `-p:CI=true`.
- No `Tx` token in identifiers, prose, or commits.
