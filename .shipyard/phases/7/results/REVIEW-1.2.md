# Review: Plan 1.2 (Phase 7 Wave 1 — docs/outbox-pattern.md)

## Verdict: PASS (after fix)

Initial verdict was REQUEST_CHANGES (1 Important + 3 Suggestions). All findings addressed in commit `9156ad25`.

---

## Stage 1: Spec Compliance

**Verdict: PASS**

### Task 1 — `docs/outbox-pattern.md`

- **Status: PASS**
- **Evidence:**
  - File exists at `docs/outbox-pattern.md`, 205 lines (within 200-400 range; +1 from the `using` add).
  - All 8 required headings present at correct levels: `# Transactional Outbox Pattern`, `## Overview`, `## Quick Start`, `### Prerequisites`, `### Example: SqlServer`, `#### PostgreSQL note`, `## Reference`, plus reference subsections `### Lifecycle Contract`, `### Retry Contract`, `### Schema Deployment Prerequisite`, `### Database-Name Comparison Semantics`, `### Supported Transports`.
  - `IRelationalProducerQueue` appears 6 times (overview, prerequisites, prose around code block, code block, Supported Transports section heading + bullet).
  - `SqlServerExternalDbNameExtractor` + `PostgreSqlExternalDbNameExtractor` both cited in DB-name semantics section.
  - Exactly one ` ```csharp ` fence (the tutorial). ASCII retry diagram now uses ` ```text `.
  - PostgreSQL variation in inline prose (no duplicate code block).
  - No emojis; ASCII-only.
  - Style matches `docs/jenkins-setup.md`: imperative voice, H2 sections, fenced code with language tag.

---

## Stage 2: Code Quality

### Critical
None.

### Important (resolved)

1. **Tutorial code block missing `using DotNetWorkQueue.Configuration;`** — `QueueConnection` lives in `DotNetWorkQueue.Configuration`; the tutorial's existing `using DotNetWorkQueue;` does NOT re-export it. Reader copy-paste would fail with `CS0246`.
   - **Resolved in commit `9156ad25`:** added `using DotNetWorkQueue.Configuration;` as the fourth using directive.

### Suggestions (resolved + 2 deferred)

1. **Untagged fence in Retry Contract section.** ASCII retry-hierarchy diagram used ` ``` ` with no language tag.
   - **Resolved in commit `9156ad25`:** changed to ` ```text `.

2. **`tx` abbreviation usage in tutorial code + reference prose** (8 occurrences total) — inconsistent with user's stored preference (no `Tx` abbreviation) and the post-9858f04f / post-ef848165 codebase convention.
   - **Resolved in commit `9156ad25`:** renamed all `tx` → `transaction` via whole-word replace (verified via `grep -nP "\btx\b"` returning zero matches post-edit).

3. **Lifecycle Contract references `IConnectionHolder`/`IConnectionHolderFactory` without namespace** (lines 113-115) — internal types not part of public API. Adds confusion for user-facing audience.
   - **Status:** Deferred — non-blocking suggestion; can be polished in a future docs pass.

4. **PROJECT.md cross-references use plain text, not Markdown anchor links** (`§Ownership & Threading Contract` etc.). Heading changes would silently break the citations.
   - **Status:** Deferred — low-priority suggestion; PROJECT.md is in-repo and not currently anchor-linked elsewhere.

### Follow-up ISSUE candidate (non-blocking)

- **PROJECT.md may still describe "OrdinalIgnoreCase vs Ordinal"** for the DB-name comparison — outdated after Phase 3 pass-through fix (`994e1404`). Both extractors now use `Ordinal` symmetrically. The doc correctly reflects the implementation; PROJECT.md needs a separate update. Tracked as ISSUE-039 candidate (see SUMMARY-1.2).

---

## Summary

**Verdict: APPROVE (after fix)**

Document is well-structured, spec-compliant, and the Ordinal-on-both-sides claim is confirmed accurate against the extractor source code. Initial REQUEST_CHANGES verdict was driven by the missing `using` in the tutorial (Important — copy-paste regression). Resolved in `9156ad25` along with two opportunistic improvements (text fence tag + `tx → transaction` rename for codebase consistency). Two non-blocking suggestions deferred.

Critical: 0 | Important: 0 (1 resolved) | Suggestions: 2 deferred (2 resolved)
