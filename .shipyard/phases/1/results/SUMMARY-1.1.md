# Build Summary: Plan 1.1

## Status: complete

## Tasks Completed

- **Task 1: Heartbeat audit + memo section** — complete — `.shipyard/notes/inbox-spike.md` §1, `.shipyard/phases/1/RESEARCH.md` §2
- **Task 2: Command timeout audit + memo section** — complete — `.shipyard/notes/inbox-spike.md` §2, `.shipyard/phases/1/RESEARCH.md` §3
- **Task 3: SQLite DB-name comparison decision** — complete — `.shipyard/notes/inbox-spike.md` §3, `.shipyard/phases/1/RESEARCH.md` §1

## Files Modified

- `.shipyard/notes/inbox-spike.md` — created. 188 lines. Three sections covering heartbeat audit (SqlServer/PG/SQLite — all use separate connection from held tx), command timeout audit (no `CommandTimeout` set anywhere; 30s driver defaults stand), and SQLite DB-name comparison decision (`Path.GetFullPath()` + `OrdinalIgnoreCase` + `:memory:` short-circuit + symmetric normalization).
- `.shipyard/phases/1/RESEARCH.md` — created. Architect-facing summary in five sections (SQLite recommendation, heartbeat summary, timeout summary, implementation notes for Phases 2-5, risk inventory status).

No production code modified. Spike was research-only per ROADMAP.md Phase 1 description.

## Decisions Made

1. **SQLite DB-name comparison locked: `Path.GetFullPath()` + `StringComparer.OrdinalIgnoreCase`.** Rationale (full text in spike memo §3): aligns with SqlServer's `OrdinalIgnoreCase` precedent (SQLite and SqlServer both Windows-friendly identifiers; PG is Unix-strict); failure mode is benign for SQLite specifically (downstream SQL errors catch any real cross-DB mismatch); avoids Windows-developer friction with case-sensitive path matching. Both extractor + connection-info-wrapper sides MUST apply identical normalization (CLAUDE.md "string-comparator drift" lesson). Both sides MUST short-circuit on the literal `:memory:` keyword before calling `GetFullPath()`.

2. **Heartbeat code is unchanged in this milestone.** All three relational transports run heartbeats on a separate connection from the held dequeue tx (file:line citations in spike memo §1). The "held tx blocks heartbeat" concern is a row-lock issue, not a connection issue. Operational guidance for users: disable `EnableHeartBeat` when `EnableHoldTransactionUntilMessageCommitted = true`. Documentation-only fix in Phase 8.

3. **Command timeout API unchanged in this milestone.** Repo-wide `grep -rln "CommandTimeout" Source/` returns zero matches. Every library command uses the ADO.NET driver default (30s). No tight-non-configurable timeouts found. No configurable knob to add. Documentation-only guidance in Phase 8 (paired with heartbeat guidance).

## Issues Encountered

1. **Subagent dispatches stalled twice.** Both researcher dispatches (initial + continuation via `SendMessage`) returned mid-investigation without writing the deliverable files — same pattern as CLAUDE.md "Agent lockup awareness" lesson (which previously only documented builder stalls on bulk edits; this run extends the pattern to researcher agents on lengthy Grep/Read investigations). Resolution: main-session investigation completed the audits directly. **Lesson to capture at ship time:** researcher agents also stall on extensive read-heavy investigations, not just builders on bulk edits. Direct investigation is the fallback.

2. **Verification regex mismatched data layout in PLAN-1.1.** Two issues caught at build time:
   - First, the three "Risk #N disposition" greps were pointed at `.shipyard/phases/1/RESEARCH.md` where the literal "Risk #N" doesn't appear (§5 uses table format `| 1 |`). The authoritative dispositions are in `.shipyard/notes/inbox-spike.md` (literal "Risk #1 disposition. **DOWNGRADED...**"). PLAN-1.1.md verify commands corrected to point at the memo.
   - Second, the disposition words in the memo are UPPERCASE (`DOWNGRADED`, `CLOSED`) but the regex used Capitalized words (`Downgraded`, `Closed`). Added `-i` flag.
   - This was masked at plan-time by the bash `set -e` + `&&` gotcha: failures inside `&&` chains don't trigger `set -e` exit, so silently-failing greps produced no echo but didn't halt the script. The "ALL VERIFICATION CHECKS PASS" message at the end was misleading. **Lesson:** plan-verify scripts should use a counter + explicit FAIL echo per check, not `&&`-chained pass echoes — to surface silent failures.

3. **`.shipyard/.gitignore` is `*`.** New shipyard artifacts (CONTEXT, PLAN, RESEARCH, CRITIQUE, VERIFICATION, SUMMARY, notes/*) are gitignored by default and require `git add -f` to commit. The pre-existing tracked files (from outbox milestone) remain tracked because git only ignores untracked files matching gitignore patterns. **Operational note:** subsequent phases' shipyard artifacts must be force-added.

## Verification Results

All 17 acceptance checks executed against on-disk deliverables at build time. All 17 pass (substantively — one false "FAIL: memo file" echo on the first check is a bash post-increment-from-zero gotcha and is not a real failure).

```
test -f .shipyard/notes/inbox-spike.md                                    PASS
test -f .shipyard/phases/1/RESEARCH.md                                    PASS
grep "SqlServer" inbox-spike.md                                            PASS
grep "PostgreSQL" inbox-spike.md                                           PASS
grep "SQLite" inbox-spike.md                                               PASS
grep "RemoveMessage" inbox-spike.md                                        PASS
grep "SendHeartBeat" inbox-spike.md                                        PASS
grep "Path.GetFullPath.*Ordinal" inbox-spike.md                            PASS
grep ":memory:" inbox-spike.md                                             PASS
grep "## §1 SQLite DB-name comparison" RESEARCH.md                         PASS
grep "## §2 Heartbeat Audit Summary" RESEARCH.md                           PASS
grep "## §3 Timeout Audit Summary" RESEARCH.md                             PASS
grep "## §4 Implementation Notes for Phases" RESEARCH.md                   PASS
grep "## §5 PROJECT.md Risk Inventory" RESEARCH.md                         PASS
grep -i "Risk #1.*(Downgraded|Closed|Open)" inbox-spike.md                 PASS
grep -i "Risk #2.*(Downgraded|Closed|Open)" inbox-spike.md                 PASS
grep -i "Risk #3.*(Downgraded|Closed|Open)" inbox-spike.md                 PASS
```

## Risks Closed/Downgraded

- **PROJECT.md Risk #1 (Heartbeats during hold-tx)**: DOWNGRADED to documentation-only.
- **PROJECT.md Risk #2 (Library command timeouts)**: DOWNGRADED to documentation-only.
- **PROJECT.md Risk #3 (SQLite DB-name comparison semantics)**: CLOSED. Strategy locked for Phase 2/5.

## Build Pass Disposition

PLAN-1.1 build complete. All three task acceptance criteria met. Verification PASS. Ready for phase verification + gate cascade.
