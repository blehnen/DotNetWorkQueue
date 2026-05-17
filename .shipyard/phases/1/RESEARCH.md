# Phase 1 RESEARCH — Discovery Spike Findings

**Date:** 2026-05-17
**Researcher driver:** main session (subagent dispatches stalled mid-investigation, per CLAUDE.md "Agent lockup awareness" lesson — falling back to direct investigation).
**Source memo:** `.shipyard/notes/inbox-spike.md` (full detail).

This RESEARCH doc is the architect's working summary for Phase 2 planning. The three audits closed three of PROJECT.md's risks.

---

## §1 SQLite DB-name comparison — RECOMMENDATION + RATIONALE

**Locked strategy:** `Path.GetFullPath()` + `StringComparer.OrdinalIgnoreCase` for `SqliteExternalDbNameExtractor`.

**Key implementation notes for Phase 2 / Phase 5:**
- Both sides of the comparator (the extractor AND the validator's expected value from `IConnectionInformation.Container`) MUST apply `Path.GetFullPath()` first. Mirror the outbox milestone's `NormalizedConnectionInformation` wrapper pattern (`Source/DotNetWorkQueue.Transport.SqlServer/Basic/NormalizedConnectionInformation.cs`, commit `fb8e7af5`).
- Short-circuit on the literal string `:memory:` (case-sensitive — SQLite keyword) before calling `GetFullPath()`. Both the extractor and the connection-info wrapper need this guard.
- Document in XML doc on the extractor class: "Comparison is case-insensitive after path canonicalization, matching SqlServer's `OrdinalIgnoreCase` precedent."

**Why not `Ordinal` (PG precedent):**
- SqlServer also uses `OrdinalIgnoreCase`. SQLite-file-paths and SqlServer-DB-names are both Windows-friendly identifiers; PG database names are Unix-friendly. Aligning SQLite with SqlServer is the better precedent match.
- `Ordinal` would surprise Windows developers with case-sensitive path matching on a case-insensitive filesystem.

**Why not OS-conditional:**
- Violates PROJECT.md §Constraints Technical "platform-uniform" rule.

---

## §2 Heartbeat Audit Summary

| Transport | Heartbeat connection | Verdict |
|---|---|---|
| SqlServer | Separate (`new SqlConnection(...)` per call) | ✓ No collision with held tx connection. |
| PostgreSQL | Separate (`_connectionFactory.Create()` per call, shared handler) | ✓ No collision. |
| SQLite | Separate (`_connectionFactory.Create()` per call, shared handler) | ✓ No collision. |

**Sub-finding (row-lock blocking):** Even on separate connections, the heartbeat UPDATE targets the same queue row the held tx has row-locked. The heartbeat command will block on the row lock and eventually hit a 30-second lock-wait timeout (driver default). Operational guidance: users running `EnableHoldTransactionUntilMessageCommitted=true` should set `EnableHeartBeat=false` — the held tx itself serves as the lease, making heartbeats redundant AND problematic.

**No code change required in this milestone.** Documentation only (Phase 8).

---

## §3 Timeout Audit Summary

`grep -rln "CommandTimeout" Source/` returns **zero matches**. No library code sets `CommandTimeout` anywhere. All commands use ADO.NET driver default (30s for SqlServer / Npgsql / Microsoft.Data.Sqlite).

| Command | Connection | During held tx? | Default | Risk |
|---|---|---|---|---|
| `SendHeartBeat` | SEPARATE | Yes — blocks on row lock | 30s | Mitigated by user disabling heartbeats. |
| `RemoveMessage` (commit) | HELD | After handler | 30s | Low. |
| Dequeue `SELECT...FOR UPDATE` | HELD | Pre-handler | 30s | None. |
| `ResetHeartBeat` (stuck records) | SEPARATE | Background | 30s | None. |

**Sizing recommendation paragraph (for Phase 8 docs draft):**

> When `EnableHoldTransactionUntilMessageCommitted = true`, set `EnableHeartBeat = false`. The held transaction serves as the message lease — no other worker can dequeue the row while the transaction is open — so heartbeats are redundant. Additionally, heartbeats run on a separate connection and will block on the held tx's row lock; they will throw `lock-wait timeout` errors if the handler runs for more than 30 seconds (the ADO.NET driver default). DNQ does not currently expose a `CommandTimeout` knob; the 30-second default is sufficient for all other library commands during normal operation.

**No ISSUEs filed.** No remediation needed in this milestone. No configurable timeouts to add.

---

## §4 Implementation Notes for Phases 2–5

**Heartbeat handler architecture (for the Phase 5 SQLite plan author):**
- PG and SQLite share `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/CommandHandler/SendHeartBeatCommandHandler.cs` (parametrized by `IDbConnectionFactory`).
- SqlServer has its own `SendHeartBeatCommandHandler` that opens `new SqlConnection(...)` directly — different shape, same separate-connection invariant.
- No Phase 5 changes needed to any heartbeat code.

**`ConnectionHolder` pattern (for Phases 3-5 inbox plan authors):**
- Each relational transport has its own `ConnectionHolder` class that owns the per-message connection + transaction across `Receive → handler dispatch → RemoveMessage`. The new `IRelationalWorkerNotification` impl pulls the `DbTransaction` from the holder.
- DO NOT modify the public surface of `ConnectionHolder` — it's an internal type already exposing what we need. Plan authors thread the existing accessor into the new notification factory.

**`IDbConnectionFactory` discipline (cross-cutting):**
- Heartbeat handlers already follow the `IDbConnectionFactory` pattern (PG + SQLite). SqlServer's heartbeat handler is older and uses `new SqlConnection(...)` directly — not a problem for the inbox milestone, but worth noting if a future refactor cleans it up.
- The new SQLite-outbox handler forks (Phase 5) MUST use `IDbConnectionFactory.Create()` for the dequeue/send-message path (this is the existing pattern from outbox milestone for SqlServer + PG).

**Symmetric normalization (for Phase 5 builder):**
- The outbox milestone's `NormalizedConnectionInformation` wrapper (`Source/DotNetWorkQueue.Transport.SqlServer/Basic/NormalizedConnectionInformation.cs`, introduced commit `fb8e7af5`) is the template. SQLite needs an analogous wrapper applying `Path.GetFullPath()` (with `:memory:` short-circuit) on the SQLite-side `IConnectionInformation`.

---

## §5 PROJECT.md Risk Inventory — Status After Phase 1

| # | Risk | Pre-Phase-1 status | Post-Phase-1 status |
|---|---|---|---|
| 1 | Heartbeats during hold-tx (audit risk) | Open | **Downgraded** — separate connections confirmed; row-lock blocking documented as user guidance, not a code defect. |
| 2 | Library-issued command timeouts during a slow handler | Open | **Downgraded** — no library code sets `CommandTimeout`; 30s driver default is acceptable with heartbeat-disable user guidance. |
| 3 | SQLite DB-name comparison semantics | Open | **Closed** — `Path.GetFullPath()` + `OrdinalIgnoreCase`; `:memory:` short-circuit; symmetric normalization both sides. |
| 4 | SQLite single-writer concurrency under hold-tx | Open | Unchanged — Phase 7 integration tests will observe behavior; Phase 8 documents the constraint. |
| 5 | `NpgsqlBatch` transaction binding | Closed (carry-forward) | Unchanged — already resolved in outbox Phase 4. |
| 6 | Documentation completeness (ship-blocker) | Open | Unchanged — Phase 8 deliverable. |

**Net effect:** Phase 1 closed 1 risk outright (#3) and downgraded 2 to documentation-only (#1, #2). No new risks discovered. The architect can plan Phase 2 against a fully-known foundation.
