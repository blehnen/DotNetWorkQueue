# Phase 6 Simplification Review

**Date:** 2026-05-15
**Scope:** Test-only phase; symmetric mirror tests across SqlServer + PostgreSQL transports (8 test files + 2 base classes + 1 production-code fix)
**Verdict:** LOW (one cross-transport naming inconsistency found and fixed in-flight; otherwise clean)

---

## §1. Cross-file duplication (expected by design)

High structural duplication between `SqlServerOutbox*Tests.cs` and `PostgreSqlOutbox*Tests.cs` is INTENTIONAL. Per CONTEXT-6 Decision 1 + RESEARCH §6, each transport's tests must be self-contained for codecov visibility (`SendMessageCommandHandlerAsync` is a separate class from `SendMessageCommandHandler`; async branches are not inferred from sync coverage). The two base classes (`SqlServerOutboxIntegrationTestBase`, `PostgreSqlOutboxIntegrationTestBase`) intentionally use different ADO.NET types (`SqlConnection`/`SqlTransaction` vs `NpgsqlConnection`/`NpgsqlTransaction`); consolidating into a shared generic would couple transport test suites in a way the project explicitly avoids.

**No consolidation recommended.** Duplication is design intent.

---

## §2. Within-file opportunities

None found. The 6 test files plus 2 base classes were already reviewed PASS by per-plan reviewer agents. Helper coverage in each base class is complete; no missed extraction candidates within a single file.

---

## §3. Dead code / AI bloat

None found. No unused imports, no leftover commented-out code, no orphan helpers. Comment quality is appropriate — only one comment block was load-bearing (the `RetryBypass_TransientError_SingleAttempt` "do not remove this assertion" warning at PostgreSqlOutboxRetryBypassTests.cs:71-76 and the SqlServer equivalent). Both correctly preserved.

---

## §4. Symmetric-but-divergent inconsistencies (FOUND + FIXED)

**Issue:** `tx` vs `transaction` variable naming inconsistency across transports.

- SqlServer Wave 1 files (post-commit `9858f04f`): used `transaction` per ISSUE-036 rename.
- PostgreSQL Wave 2 files (PLAN-2.1 commits `c64562a7`, `e35b8a06`, `3c0b8017`): used `tx` because the PLAN-2.1 + PLAN-2.2 code shapes were authored BEFORE commit `9858f04f` did the rename. Builder followed plan verbatim without noticing the post-rename inconsistency in the freshly-modified SqlServer reference files.
- User's stored feedback (`feedback_no_tx_abbreviation.md`): "never use `Tx` for transaction; use `Trans` or `Transaction`."

**Resolution:** commit `ef848165` renamed `tx` → `transaction` across 3 PG outbox files (`PostgreSqlOutboxIntegrationTestBase.cs`, `PostgreSqlOutboxSendTests.cs`, `PostgreSqlOutboxSendAsyncTests.cs`). 34 substitutions. `wrongTx` left as-is — that compound name is the same on both transports (per SqlServer file inspection) and is not the bare `tx` variable the convention targets. Build clean. Pushed to PR-138 (commit `ef848165` is now the HEAD).

---

## §5. Recommendations (with severity)

| Severity | Finding | Resolution |
|---|---|---|
| Low | `tx` → `transaction` naming inconsistency between Wave 1 (post-rename) and Wave 2 (pre-rename plan shapes) | **Resolved in commit `ef848165`** |
| Info | Plans 2.1 and 2.2 still contain `tx` in their inline code shapes (in `.shipyard/phases/6/plans/`) — historical document, not source code. Optional: update plan files to match the post-rename naming for future-reference accuracy. | Not changed — plans are historical artifacts |

---

## Lessons-learned candidate

When a mid-build production rename happens (here, commit `9858f04f`'s `Tx → Transaction` across the outbox feature), in-flight plan code shapes authored against the pre-rename state are at risk. The PostgreSQL Wave 2 plans were finalized hours before the rename and not re-checked when the rename landed. Two follow-up patterns to consider:

1. **Plan refresh on rename**: when a phase-wide rename lands, run a quick `grep` over outstanding plan files for the old token before kicking off subsequent waves.
2. **Reviewer mandate**: reviewer agents could be primed to explicitly cross-check naming conventions against the most recent sibling implementation (here, SqlServer Wave 1) — the Wave 2 reviewers passed both plans without flagging the `tx` divergence.

Both patterns are noted for `.shipyard/LESSONS.md` capture during ship.
