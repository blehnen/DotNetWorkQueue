# Documentation & Status Updates Plan

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Close 4 concerns as "Accepted Risk / Will Not Fix" with documented rationale, and create the missing Phase 7 SUMMARY file.

**Architecture:** Pure documentation changes — only `.shipyard/` markdown files are modified. No code changes.

**Tech Stack:** Markdown

---

## Task 1: Mark Accepted Risk Concerns in CONCERNS.md

**Files:**
- Modify: `.shipyard/codebase/CONCERNS.md`

**Step 1: Update C-1 status**

Change the C-1 section status and add resolution:

```markdown
- **Status**: Accepted Risk (2026-03-30)
```

Add after the existing "Recommendation" line:

```markdown
- **Resolution**: Transport security is the user's responsibility. The DenyList binder provides defense-in-depth by default. Users handling untrusted messages should switch to the AllowList binder (documented in README). The deny list should be periodically reviewed against new ysoserial.net gadget discoveries. Source code exists for 2 of 3 vendored libraries (Schyntax, ExpressionJsonSerializer); JpLabs.DynamicCode is net48-only and will be removed when net48 support is dropped.
```

**Step 2: Update C-2 status**

Change C-2 status:

```markdown
- **Status**: Accepted Risk (2026-03-30)
```

Add resolution:

```markdown
- **Resolution**: Dynamic LINQ compilation is by-design functionality that enables the method-based queue pattern. Transport security (authentication, encryption, network isolation) is the user's responsibility. The library's README should document that queue transports carrying LINQ expressions must be secured against untrusted message injection.
```

**Step 3: Update H-1 status**

Change H-1 status:

```markdown
- **Status**: Accepted Risk (Partial) (2026-03-30)
```

Add resolution:

```markdown
- **Resolution**: Source code exists for Schyntax (F:\Git\cs-schyntax) and Aq.ExpressionJsonSerializer (F:\Git\expression-json-serializer). JpLabs.DynamicCode is net48-only and will be removed when .NET Framework 4.8 support is dropped. The supply-chain risk is limited to net48 builds only.
```

**Step 4: Update L-3 status**

Change L-3 status:

```markdown
- **Status**: Will Not Fix (2026-03-30)
```

Add resolution:

```markdown
- **Resolution**: .NET Framework 4.8 support is required by employer until their .NET 10 migration completes. Timeline is unknown. When net48 is eventually dropped, this will also resolve the JpLabs.DynamicCode vendored binary concern (H-1).
```

**Step 5: Update Summary Table**

Update the summary table rows for C-1, C-2, H-1, L-3 to reflect the new statuses:
- C-1: `Accepted Risk (2026-03-30)`
- C-2: `Accepted Risk (2026-03-30)`
- H-1: `Accepted Risk (Partial) (2026-03-30)`
- L-3: `Will Not Fix (2026-03-30)`

**Step 6: Update Open Questions**

Remove or mark as answered the open questions that are now resolved:
- "What is the intended security model..." → Answered: transport security is user's responsibility
- "Are the vendored DLLs maintained forks?" → Answered: yes for 2/3, DynamicCode is net48-only
- "Is .NET Framework 4.8 support contractually required?" → Answered: yes, employer requirement

**Verification:**

```bash
grep -c "Accepted Risk" .shipyard/codebase/CONCERNS.md
# Expected: 3 (C-1, C-2, H-1)
grep -c "Will Not Fix" .shipyard/codebase/CONCERNS.md
# Expected: 1 (L-3)
```

**Commit:**

```bash
git add .shipyard/codebase/CONCERNS.md
git commit -m "docs: mark C-1, C-2, H-1, L-3 as accepted risk / will not fix"
```

---

## Task 2: Create Missing Phase 7 Plan 01 SUMMARY (ISSUE-012)

**Files:**
- Create: `.shipyard/phases/7/SUMMARY-plan01.md`

**Note:** Check if this file already exists first. The directory listing showed `SUMMARY-plan02.md` exists but the filename `SUMMARY-plan01.md` also appeared in the listing — verify before creating. If it exists, mark ISSUE-012 as already resolved.

**Step 1: Check existing files**

```bash
ls -la .shipyard/phases/7/
```

If `SUMMARY-plan01.md` already exists, skip to Step 3.

**Step 2: Create SUMMARY file**

Create `.shipyard/phases/7/SUMMARY-plan01.md` with a summary of Phase 7 Plan 01 (BaseMonitor modernization). Read `.shipyard/phases/7/plans/01-PLAN.md` for context on what the plan covered.

**Step 3: Commit**

```bash
git add .shipyard/phases/7/SUMMARY-plan01.md
git commit -m "docs: add missing SUMMARY for Phase 7 Plan 01 (ISSUE-012)"
```

**Verification:**

```bash
test -f .shipyard/phases/7/SUMMARY-plan01.md && echo "EXISTS" || echo "MISSING"
# Expected: EXISTS
```
