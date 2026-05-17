# Phase 1 — Documentation Generation

## Status: DEFERRED to Phase 8

## Rationale

Phase 1 is a research-only discovery spike. Its deliverables (`.shipyard/notes/inbox-spike.md` + `.shipyard/phases/1/RESEARCH.md`) are themselves the documentation for the spike — internal engineering memos that drive subsequent phases' design.

Public-facing / user-facing documentation is explicitly Phase 8's deliverable per ROADMAP.md and PROJECT.md Risk #6 (ship-blocker):

- `docs/inbox-pattern.md` — the new user-facing page covering the lifecycle contract, the "heartbeats disabled in hold-tx mode" guidance from Phase 1's audit, the SQLite single-writer concurrency callout, and a worked example.
- `docs/outbox-pattern.md` — to be updated with SQLite addition (Phase 5 + Phase 8).
- `README.md` — to be updated with a pointer to the new inbox docs page (Phase 8).
- XML doc comments on every new public type from Phases 2-5 (architect's plans for those phases enforce this; Phase 8 verifies).

## Inputs Phase 8 will consume from Phase 1

Phase 8's plan author should incorporate the following Phase 1 findings:

1. **From spike memo §1 / RESEARCH.md §2 — Heartbeat guidance.** User must disable `EnableHeartBeat` when `EnableHoldTransactionUntilMessageCommitted = true`. The held tx already serves as the message lease (no other worker can dequeue the row), and heartbeats running on a separate connection will block on the row lock and hit the 30s ADO.NET driver default → throw on every slow handler. This is a docs-only fix; no code change in the milestone.

2. **From spike memo §3 / RESEARCH.md §1 — SQLite DB-name comparison rationale.** The per-provider DB-name semantics table in `docs/outbox-pattern.md` adds a SQLite row: `Path.GetFullPath()` + `OrdinalIgnoreCase` + `:memory:` short-circuit. Rationale (precedent alignment with SqlServer; benign failure mode for SQLite specifically) goes into the table footnote or accompanying paragraph.

3. **From spike memo §2 / RESEARCH.md §3 — No new options surface.** `CommandTimeout` is not exposed by DNQ; the 30s driver defaults stand. Phase 8 docs should not claim a tunable knob exists.

## Findings: None requiring immediate action.

## Disposition

Phase 1 documentation surface is fully captured for Phase 8 consumption. No `docs/` writes in this phase. No CHANGELOG entry needed (research-only). Resume documentation review at Phase 2 when production code begins to land XML doc comments.
