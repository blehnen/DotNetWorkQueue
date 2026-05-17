# Plan 1.1: Discovery Spike Deliverables (Heartbeat + Timeout + SQLite DB-Name Audits)

## Context

Phase 1 is a discovery spike: three independent audits driven by PROJECT.md Risks #1, #2, #3. Output is two markdown deliverables — no production code. The research was conducted in the planning session itself (subagent dispatches stalled per CLAUDE.md "Agent lockup awareness" lesson; main-session investigation completed the audits). This plan documents what the deliverables contain and the criteria the builder confirms during `/shipyard:build 1`.

## Dependencies

None — first phase of the milestone.

## Tasks

### Task 1: Heartbeat audit + memo section

**Files:** `.shipyard/notes/inbox-spike.md` §1, `.shipyard/phases/1/RESEARCH.md` §2

**Action:** investigate + document

**Description:** Trace the heartbeat command handler for each of the three relational transports (SqlServer, PostgreSQL, SQLite). Determine whether heartbeats use the same connection as the held dequeue tx or a separate one. Capture file:line citations.

**Acceptance Criteria:**
- Per-transport verdict with file:line citations (SqlServer / PG / SQLite all confirmed)
- Sub-finding on row-lock blocking documented if it exists
- Operational guidance: when to disable `EnableHeartBeat` in hold-tx mode
- PROJECT.md Risk #1 disposition stated (Open / Downgraded / Closed)
- No ISSUE filed unless an anomaly was discovered

### Task 2: Command timeout audit + memo section

**Files:** `.shipyard/notes/inbox-spike.md` §2, `.shipyard/phases/1/RESEARCH.md` §3

**Action:** investigate + document

**Description:** Enumerate every library-issued `IDbCommand` that runs on (or against) the held connection during the dequeue → handler → `RemoveMessage` lifecycle. For each: file:line, purpose, `CommandTimeout` source, configurable y/n, tight-vs-slack relative to a 60+ second slow handler. Produce a per-command table and a sizing-recommendation paragraph for Phase 8 docs.

**Acceptance Criteria:**
- Repo-wide `grep -rln "CommandTimeout" Source/` result reported (zero or non-zero, with files if non-zero)
- Per-command table with: command, connection (HELD vs SEPARATE), during-held-tx?, default timeout, risk level
- Sizing-recommendation paragraph drafted for Phase 8 docs
- PROJECT.md Risk #2 disposition stated
- No ISSUE filed unless a tight non-configurable timeout was discovered

### Task 3: SQLite DB-name comparison decision

**Files:** `.shipyard/notes/inbox-spike.md` §3, `.shipyard/phases/1/RESEARCH.md` §1

**Action:** investigate + decide + document

**Description:** Investigate `Microsoft.Data.Sqlite.SqliteConnection.DataSource`, `Path.GetFullPath()` cross-platform behavior, and DNQ's existing `IConnectionInformation.Container` population for SQLite. Cross-reference CLAUDE.md "string-comparator drift" lesson. Recommend ONE strategy: `Path.GetFullPath()` + `OrdinalIgnoreCase` OR `Path.GetFullPath()` + `Ordinal`. Justify with deployment-shape reasoning and precedent alignment.

**Acceptance Criteria:**
- Recommendation locked: one strategy, named explicitly
- Justification includes: SqlServer/PG precedent alignment, real-world deployment shape (Windows/Linux), failure-mode asymmetry (false positive vs false negative)
- `:memory:` short-circuit requirement called out for both extractor + normalization-wrapper sides
- Symmetric normalization requirement stated (per CLAUDE.md lesson) — both sides of the comparator must apply identical `Path.GetFullPath()` canonicalization
- Implementation pointers for Phase 2 (extractor placement) and Phase 5 (`NormalizedConnectionInformation`-style wrapper) included in RESEARCH.md §4
- PROJECT.md Risk #3 disposition stated (Closed when decision locks)

## Verification

```bash
# Both deliverables exist
test -f .shipyard/notes/inbox-spike.md
test -f .shipyard/phases/1/RESEARCH.md

# Heartbeat audit covers all 3 transports
grep -q "SqlServer" .shipyard/notes/inbox-spike.md
grep -q "PostgreSQL" .shipyard/notes/inbox-spike.md
grep -q "SQLite" .shipyard/notes/inbox-spike.md

# Timeout audit has the per-command table
grep -q "RemoveMessage" .shipyard/notes/inbox-spike.md
grep -q "SendHeartBeat" .shipyard/notes/inbox-spike.md

# SQLite decision is locked and stated
grep -qE "Path\.GetFullPath.*Ordinal" .shipyard/notes/inbox-spike.md
grep -q ":memory:" .shipyard/notes/inbox-spike.md

# RESEARCH.md has all 5 sections
grep -q "## §1 SQLite DB-name comparison" .shipyard/phases/1/RESEARCH.md
grep -q "## §2 Heartbeat Audit Summary" .shipyard/phases/1/RESEARCH.md
grep -q "## §3 Timeout Audit Summary" .shipyard/phases/1/RESEARCH.md
grep -q "## §4 Implementation Notes for Phases 2" .shipyard/phases/1/RESEARCH.md
grep -q "## §5 PROJECT.md Risk Inventory" .shipyard/phases/1/RESEARCH.md

# All three risks have an explicit disposition line in the memo
# (RESEARCH.md §5 uses table format; authoritative dispositions live in inbox-spike.md)
# Case-insensitive: memo uses UPPERCASE disposition words (DOWNGRADED, CLOSED) by convention.
grep -qiE "Risk #1.*(Downgraded|Closed|Open)" .shipyard/notes/inbox-spike.md
grep -qiE "Risk #2.*(Downgraded|Closed|Open)" .shipyard/notes/inbox-spike.md
grep -qiE "Risk #3.*(Downgraded|Closed|Open)" .shipyard/notes/inbox-spike.md
```

All checks pass at plan-write time (deliverables already on disk). The builder re-runs these in `/shipyard:build 1`.
