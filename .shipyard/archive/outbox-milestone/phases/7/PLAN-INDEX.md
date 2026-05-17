# Phase 7 Plan Index — Documentation + Wiki Draft

**Branch:** `feature/outbox-pattern`
**Phase target size:** M (3–4 hours per ROADMAP)
**Plans:** 4 total / 2 waves / 8 tasks
**Locked decisions:** see `.shipyard/phases/7/CONTEXT-7.md`
**Research:** see `.shipyard/phases/7/RESEARCH.md`

## Wave 1 — independent, parallelizable (no file overlap, no logical ordering)

| Plan | Title | Files touched | Tasks |
|------|-------|---------------|-------|
| [PLAN-1.1](plans/PLAN-1.1.md) | csproj XML-doc gate fix + per-project verification | `Transport.RelationalDatabase.csproj`, `Transport.SQLite.csproj` | 3 |
| [PLAN-1.2](plans/PLAN-1.2.md) | Author `docs/outbox-pattern.md` (tutorial + reference hybrid) | `docs/outbox-pattern.md` (new) | 1 |
| [PLAN-1.3](plans/PLAN-1.3.md) | README pointer bullet under "High-level features" | `README.md` | 1 |

## Wave 2 — depends on Wave 1

| Plan | Title | Depends on | Tasks |
|------|-------|------------|-------|
| [PLAN-2.1](plans/PLAN-2.1.md) | Full-solution Release+CI=true verification + Source Link spot-check | 1.1, 1.2, 1.3 | 3 |

## Scope notes for the builder agent

- **PLAN-1.1 closes ISSUE-032 inline.** Adding `<WarningsNotAsErrors>NU1902</WarningsNotAsErrors>` to `Transport.SQLite.csproj` is the architect-chosen option (B) over per-project verification (option A). Rationale recorded in PLAN-1.1 Context section: ROADMAP's strict success criterion requires the full-solution build to be clean, and the SQLite NU1902 escalation predates Phase 1 and would otherwise re-block the gate later anyway.
- **The XML-doc plan is VERIFICATION-first, not writing-from-scratch.** RESEARCH.md §1 confirmed all 9 new public types carry XML docs; PLAN-1.1 Task 3 only runs `dotnet build` against each of the four outbox projects and confirms zero CS1591. If CS1591 fires, the builder surfaces the member back rather than patching silently — the architect's expectation is zero.
- **PLAN-1.2 doc-writer reference:** read `docs/jenkins-setup.md` first for voice/style/heading conventions. One C# fenced block for SqlServer; PostgreSQL is a prose callout, not a duplicate code block.
- **PLAN-1.3 README edit shape:** exact `old_string` / `new_string` Edit pair given in the plan; one bullet inserted, no other README change.
- **Wave 2 cannot start until all three Wave 1 plans report green** — Wave 2's full-solution build depends on the csproj edits, and the link-resolution check depends on both PLAN-1.2 and PLAN-1.3 having landed.
