# Phase 5 Context: Dashboard.Api DashboardExtensions Coverage

**Captured:** 2026-04-13
**Branch:** `phase-5-dashboard-coverage` (feature branch, will merge back to master via PR)
**Risk:** LOW (per ROADMAP.md Phase 5)

## Phase Scope (from ROADMAP.md)

Improve `DotNetWorkQueue.Dashboard.Api.DashboardExtensions` line coverage from the current **33.3%** (244/366 lines uncovered) toward a target of **≥50%**. This is the final phase of the code-coverage milestone.

## User Decisions

### Decision 1 — Stale plan handling
**Q:** Phase 5 already had a stale `PLAN-1.1.md` from the old Schyntax/Lib cleanup work (committed 2026-04-09, unrelated to current roadmap).
**A:** Archived to `.shipyard/phases/5/plans/archived-schyntax-cleanup/PLAN-1.1.md` via `git mv` so history is preserved but doesn't interfere with new planning.

### Decision 2 — Coverage ambition: BALANCED (~60–70%)
**Q:** ROADMAP says "at least 50%." Minimum / balanced / aggressive?
**A:** **Balanced.** Aim for roughly 60–70% line coverage. Cover realistic configuration combinations that real users would hit. Accept that pure registration-overload branches with no behavioral difference remain uncovered. Don't chase edge cases that exist only for API-surface completeness.
**How to apply:**
- Plan tasks should target the most-used `DashboardExtensions` entry points first
- Stop adding tests once the target band is reached — don't over-invest in a low-priority phase
- If research identifies a cluster of dead/unused overloads (see Decision 4), prefer deleting those over writing tests for them

### Decision 3 — Test layer: DEFER TO RESEARCH
**Q:** Integration tests only, unit-level `IServiceCollection` tests + integration, or researcher's call?
**A:** **Researcher decides.** The researcher agent should examine the actual `DashboardExtensions` code and recommend the right mix based on what's there. Pure DI registration branches (no conditional logic) may be better as unit tests hitting `IServiceCollection` directly. Runtime-behavioral branches need integration tests via the existing `Dashboard.Api.Integration.Tests` project.
**How to apply:**
- Researcher must produce an explicit "test layer recommendation" section in `RESEARCH.md`
- Architect then builds plans around the researcher's recommendation
- Default test project for integration work: `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/`
- Default test project for unit work: `Source/DotNetWorkQueue.Dashboard.Api.Tests/`
- Use **Memory transport** for integration tests (no external services needed — already established convention in `Dashboard.Api.Integration.Tests`)

### Decision 4 — Dead-overload policy: DELETE THEM
**Q:** If research finds `DashboardExtensions` overloads with no callers in the repo, no tests, and no runtime coverage, what do we do with them?
**A:** **Delete them.**
**Why:** Consistent with the existing CLAUDE.md lesson: "Compile errors over runtime errors. Delete dead API surface; `NotSupportedException` stubs are hidden landmines." Compile errors force any consumers to update; silently-unused overloads rot and confuse future readers.
**How to apply:**
- Research phase must produce an explicit **dead-overload candidate list** in `RESEARCH.md` with callsite analysis (grep the entire `Source/` tree for each public overload signature)
- Architect should structure plans so that dead-overload deletion is a **separate plan** from test-writing work — this makes the delete reviewable independently and gives the user a single plan to approve/reject the deletions
- The deletion plan should include a verification step that the full solution still builds after the deletes (`dotnet build "Source/DotNetWorkQueue.sln" -c Debug`)
- Do NOT delete overloads that are part of the documented public API in `DashboardExtensions` if any exist — flag those for user review instead of auto-deleting (the roadmap explicitly says "Accept residual gaps — some DI registration overloads exist for API surface completeness but are never called in practice"; these get kept, not deleted)
- **User must approve the final deletion list before the builder executes the deletion plan.** This is a hard gate. Use the plan review gate for this.

### Decision 5 — Feature branch workflow
**Q:** Work on `master` or isolate on a branch?
**A:** **Feature branch.** Work happens on `phase-5-dashboard-coverage`. After Phase 5 completes and verifies cleanly, the user will push the branch and open a PR to merge back to `master` manually. This is a departure from the Phase 1–4 workflow (which committed directly to `master`) and is justified because Phase 5 includes **code deletions** (dead overloads) that should be reviewable as a single diff before merging.
**How to apply:**
- All Phase 5 commits land on `phase-5-dashboard-coverage`, not `master`
- Pre-build checkpoint tag (if created) should reference the branch name
- Post-build commit stays on the branch
- Ship-time workflow should route to "push and create PR" (option 2 in git-workflow skill), not "merge locally"

## Constraints (unchanged from ROADMAP)

1. No new external service dependencies — Memory transport only
2. Existing Dashboard API integration tests must continue to pass
3. `Dashboard.Api.Tests` and `Dashboard.Api.Integration.Tests` are the allowed test projects (no new test projects)
4. Accept that some overloads stay uncovered if they're legitimately part of the public API surface

## Out of Scope

- `DashboardExtensions` consumers (the Dashboard.Ui project, external callers) — only the API itself
- Changes to `Dashboard.Api` routing, controllers, or middleware beyond what's needed to exercise `DashboardExtensions` paths
- Changes to non-Dashboard transport projects
- Refactoring `DashboardExtensions` internal structure (beyond deletions of dead overloads) — any non-trivial refactor should be a separate phase

## Next Steps

1. Research phase → produces `RESEARCH.md` with:
   - Coverage gap analysis (which specific lines are uncovered and why)
   - Test layer recommendation per decision 3
   - Dead-overload candidate list per decision 4
2. Architect phase → produces plans respecting the balanced-coverage budget and the feature-branch policy
3. Plan verification + critique
4. Build phase (to follow via `/shipyard:build 5`)
