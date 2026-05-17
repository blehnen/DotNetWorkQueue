# Inbox Pattern Spike — Phase 1 Discovery Memo

**Date:** 2026-05-17
**Phase:** 1 (Discovery Spike)
**Status:** Closed — three risks downgraded.

---

## 1. Heartbeat Audit (closes PROJECT.md Risk #1)

**Question.** When `EnableHoldTransactionUntilMessageCommitted = true`, does the heartbeat scheduler fire commands against the held connection (which would deadlock the dequeue tx)?

**Investigation.** Located the heartbeat command handler per transport:

| Transport | Handler file | Connection ownership |
|---|---|---|
| SqlServer | `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendHeartBeatCommandHandler.cs` (lines 22-77) | Opens a brand-new `SqlConnection(_connectionInformation.ConnectionString)` per call. |
| PostgreSQL | `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/CommandHandler/SendHeartBeatCommandHandler.cs` (lines 30-66) — shared handler, parametrized by PG's `IDbConnectionFactory` registration. | Calls `_connectionFactory.Create()` per invocation. |
| SQLite | Same shared handler as PG, parametrized by SQLite's `IDbConnectionFactory` registration. | Calls `_connectionFactory.Create()` per invocation. |

**Verdict.** All three relational transports run heartbeats on a SEPARATE connection from the held dequeue tx. **Heartbeats do NOT use the held connection.** Risk #1's "heartbeat connection collision" scenario does not exist.

**Sub-finding (lock blocking, not connection blocking).** Even with separate connections, the heartbeat UPDATE statement targets the SAME queue row that the held tx has row-locked via the dequeue `SELECT ... FOR UPDATE` (or equivalent). The heartbeat command will block on the row lock until the held tx completes. If the user's handler runs for >30 seconds (default `CommandTimeout` on SqlServer + Npgsql), the heartbeat command will hit its lock-wait timeout and throw.

**Operational guidance.** In `EnableHoldTransactionUntilMessageCommitted = true` mode, users should disable `EnableHeartBeat` because:
1. The held tx itself serves as the message lease (no other worker can dequeue the row).
2. The heartbeat command is redundant AND will fail with lock-wait timeouts on slow handlers.

DNQ's existing integration tests already pair these flags as independent — the user is responsible for the configuration combination. No code change in this milestone; **documented as user guidance in Phase 8 docs**.

**Risk #1 disposition.** **DOWNGRADED to documentation-only.** No ISSUE filed.

---

## 2. Command Timeout Audit (closes PROJECT.md Risk #2)

**Question.** What `CommandTimeout` values do library commands use during a held tx? Tight timeouts could fire on internal `RemoveMessage` / heartbeat operations while user code is slow.

**Investigation.** A repo-wide `grep -rln "CommandTimeout"` against `Source/` returns **zero matches**. No library code anywhere in DNQ sets `IDbCommand.CommandTimeout` explicitly. Every library-issued command relies on the ADO.NET driver default:

| Driver | Default CommandTimeout |
|---|---|
| `Microsoft.Data.SqlClient` 6.x | 30 seconds |
| `Npgsql` 10.x | 30 seconds |
| `Microsoft.Data.Sqlite` 9.x | 30 seconds (changed from `infinite` in earlier `System.Data.SQLite` era) |

**Per-command exposure during a held tx (sync + async paths):**

| Command | Connection | Runs during held tx? | Default timeout | Risk |
|---|---|---|---|---|
| `SendHeartBeat` (heartbeat) | SEPARATE | Yes — blocks on row lock | 30s | Will throw lock-wait timeout if handler >30s. Mitigated by user disabling heartbeats in hold-tx mode (see §1). |
| `RemoveMessage` (commit-side delete) | HELD | After handler returns | 30s | Only an issue if the held tx has accrued enough lock work to make the cleanup delete itself slow. Low risk for typical use. |
| Dequeue `SELECT ... FOR UPDATE` | HELD | At dequeue (before handler) | 30s | Independent of handler latency. No risk. |
| Reset stuck records (`ResetHeartBeat`) | SEPARATE | Background | 30s | Independent of handler latency. No risk. |

**Sizing recommendation (for Phase 8 docs).** None of the library-issued commands are tight relative to handler latency, EXCEPT for the heartbeat lock-wait. The recommended user guidance is to disable `EnableHeartBeat` when `EnableHoldTransactionUntilMessageCommitted = true` (per §1). With heartbeats off, all remaining library commands run on independent timing windows from the user's handler. No configurable knob exposure needed in this milestone.

**Risk #2 disposition.** **DOWNGRADED to documentation-only.** No ISSUE filed. No configurable timeouts to add. The 30s default across all drivers is acceptable given the heartbeat-disable guidance.

---

## 3. SQLite DB-Name Comparison Decision (closes PROJECT.md Risk #3)

**Question.** SQLite's "DB name" is a file path. How should `SqliteExternalDbNameExtractor` compare it against the validator's expected value, given that paths are case-insensitive on Windows but case-sensitive on Linux?

**Investigation.**

- **`Microsoft.Data.Sqlite.SqliteConnection.DataSource`** returns the connection string's `Data Source=` value verbatim. Relative paths are preserved as-relative; absolute paths as-absolute; the special form `:memory:` is preserved.
- **`Path.GetFullPath()`** canonicalizes: resolves relative-to-cwd, normalizes separators, collapses `..`/`.`. It does NOT normalize case (preserves what's on disk on Windows; passes through verbatim on Linux). On `:memory:` it would interpret as a relative file path and fail — needs a `:memory:` short-circuit.
- **DNQ's `IConnectionInformation.Container` for SQLite** is populated from the connection string by the SQLite-specific `IConnectionInformation` implementation, also verbatim.
- **CLAUDE.md "string-comparator drift" lesson** (from outbox milestone): if one side normalizes and the other doesn't, the validator produces false mismatches. Both sides must apply identical normalization OR both must apply none. The outbox milestone solved this for SqlServer via a `NormalizedConnectionInformation` wrapper applied symmetrically.

**Candidate strategies:**

| Strategy | Pros | Cons |
|---|---|---|
| `Path.GetFullPath()` + `OrdinalIgnoreCase` | Matches SqlServer precedent. Permissive on Linux but failure mode is benign (cross-DB writes are blocked by SQL-level row constraints elsewhere). Tolerates Windows case variation (`c:\data\queue.db` vs `C:\Data\Queue.db`). | A deliberately case-variant Linux deployment could accept a path mismatch. Hypothetical, not a security boundary. |
| `Path.GetFullPath()` + `Ordinal` | Matches PG precedent. Strict; matches Linux filesystem reality. | Surprises Windows users — they'd have to match case in connection strings exactly. Real-world friction. |
| OS-conditional comparison | Matches actual fs semantics per platform. | **Violates PROJECT.md §Constraints Technical "platform-uniform" rule.** Not allowed. |

**Recommendation: `Path.GetFullPath()` + `StringComparer.OrdinalIgnoreCase`.**

Rationale:
1. **Aligns with SqlServer precedent** — the outbox milestone already chose `OrdinalIgnoreCase` for SqlServer's `Database` comparison; SQLite + SqlServer being case-insensitive vs PG being case-sensitive matches each engine's native semantics.
2. **Permissive failure mode** — false positives (validator accepting a mismatch that should be rejected) are benign for SQLite specifically: the user's caller-tx must already point at SOME SQLite file; if it's a different file than the queue's, downstream SQL errors will catch the mismatch even if the validator lets the path-string-compare slide. The validator's job is fast-fail, not security.
3. **Real-world friction** — Windows developers routinely mix case in paths. `Ordinal` strictness would cause confusing validation failures during dev/test that don't reflect actual cross-DB risk.
4. **`:memory:` short-circuit** — both `SqliteExternalDbNameExtractor` and the connection-info-side normalization must short-circuit on the literal string `:memory:` (case-sensitive — it's a SQLite keyword, not a path). Phase 2 architect note: add a `:memory:` early-return in both paths.

**Symmetric normalization requirement.** Per CLAUDE.md lesson, BOTH sides of the comparator MUST apply the same `Path.GetFullPath()` + `OrdinalIgnoreCase`. If the `IConnectionInformation` path doesn't already canonicalize, Phase 2 / Phase 5 must add a `NormalizedConnectionInformation`-style wrapper on the SQLite side (mirror the outbox SqlServer pattern from commit `fb8e7af5` — `shipyard(phase-3): close validator normalization asymmetry via NormalizedConnectionInformation wrapper`).

**Risk #3 disposition.** **CLOSED.** Comparison strategy locked. Phase 2 plan author and Phase 5 builder implement per this spec.

---

## Summary

| Risk | Status | Action |
|---|---|---|
| #1 Heartbeat collision | Downgraded | None — heartbeats already use separate connections. Document user guidance to disable heartbeats in hold-tx mode. |
| #2 Command timeout starvation | Downgraded | None — no library code sets `CommandTimeout`; driver defaults (30s) are acceptable given heartbeat-disable guidance. |
| #3 SQLite DB-name comparison | Closed | `Path.GetFullPath()` + `OrdinalIgnoreCase`; symmetric normalization on both validator sides; `:memory:` short-circuit. |

No ISSUEs filed. No throwaway proof-of-concept code committed. Ready for Phase 2 planning.
