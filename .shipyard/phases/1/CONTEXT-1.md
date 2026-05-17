# CONTEXT-1: User Decisions for Phase 1 (Discovery Spike)

Captured during `/shipyard:plan 1` discussion-capture step.

## Decisions

### 1. SQLite DB-name (file path) comparison semantics — DEFERRED to researcher

User has no upfront preference. The researcher investigates platform behavior (Windows case-insensitive vs Linux case-sensitive filesystem semantics), common-case deployment shapes (containers, dev boxes), and the CLAUDE.md "string-comparator drift" lesson from outbox milestone, then recommends one of:

- `Path.GetFullPath()` + `OrdinalIgnoreCase` (matches SqlServer precedent, permissive on Linux)
- `Path.GetFullPath()` + `Ordinal` (matches PostgreSQL precedent + Linux fs, strict)

The chosen semantics MUST be platform-uniform (PROJECT.md §Constraints Technical — no platform-conditional logic).

User reviews the recommendation when RESEARCH.md lands; final lock happens at that review point, not here.

## Non-Decisions (deferred per PROJECT.md)

- **Heartbeat audit disposition.** PROJECT.md Risk #1 is explicit: if a transport unexpectedly fires heartbeats during a held tx, file an ISSUE and document the limitation — do NOT redesign the heartbeat system in this milestone. No additional user decision required.
- **Timeout audit remediation.** PROJECT.md Risk #2 is explicit: produce a per-command table of timeouts; flag any "not configurable + tight" command for follow-up (file ISSUE if remediation is out of scope). No additional user decision required.

## Scope reminders for the spike

- Output is a single memo at `.shipyard/notes/inbox-spike.md`.
- A throwaway proof-of-concept may live transiently on the branch but is deleted before Phase 2 starts.
- No production code is committed to master from Phase 1.

## Archive note (2026-05-17)

Outbox milestone artifacts archived under `.shipyard/archive/outbox-milestone/` (phases/1-7, AUDIT-SHIP.md, MILESTONE-REPORT.md) before Phase 1 planning. The new `.shipyard/phases/1/` is a clean slate for this milestone.
