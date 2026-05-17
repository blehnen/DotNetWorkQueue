---
phase: documentation
plan: 1.3
wave: 1
dependencies: []
must_haves:
  - Add a single bullet to README's "High-level features" list pointing at docs/outbox-pattern.md
  - Bullet inserts after the "Re-occurring job scheduler" line, before the blank line that precedes the Wiki pointer
  - No new H2 section, no other README changes, no formatting churn on installation tables or badges
files_touched:
  - README.md
tdd: false
risk: low
---

# Plan 1.3: README pointer bullet

## Context

CONTEXT-7 Decision 3 locks the README integration shape: a single bullet under the
existing "High-level features" list, NOT a new section. RESEARCH.md §4 supplies the
exact surrounding 3-line context (README lines 11–13) and the exact Edit replacement
text.

The README's "High-level features" section currently lists three bullets:

```
- Queue / de-queue POCOs for distributed processing
- Queue / process compiled LINQ expressions
- Re-occurring job scheduler
```

The new fourth bullet sits after "Re-occurring job scheduler" and before the blank line
that precedes "See the [Wiki]..." (README line 15). The bullet text is:

```
- Transactional outbox pattern on SqlServer / PostgreSQL (see [`docs/outbox-pattern.md`](docs/outbox-pattern.md))
```

## Dependencies

None. PLAN-1.3 touches only `README.md` — no overlap with PLAN-1.1 (csprojs) or PLAN-1.2
(`docs/outbox-pattern.md`). All three Wave 1 plans can run in parallel.

Note: the README link target (`docs/outbox-pattern.md`) is created by PLAN-1.2. The
README bullet may land before the doc file lands; markdown link targets are resolved at
view time, not at commit time. Wave 2's verification (PLAN-2.1) will confirm the link
resolves once both files are present.

## Tasks

### Task 1: Insert outbox bullet into README's High-level features list

**Files:** `README.md`
**Action:** modify
**Description:** Use a single Edit call with the following inputs (exact text — preserve
the literal backticks and brackets, do not URL-encode the link):

`old_string`:
```
- Re-occurring job scheduler

See the [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki) for in-depth documentation.
```

`new_string`:
```
- Re-occurring job scheduler
- Transactional outbox pattern on SqlServer / PostgreSQL (see [`docs/outbox-pattern.md`](docs/outbox-pattern.md))

See the [Wiki](https://github.com/blehnen/DotNetWorkQueue/wiki) for in-depth documentation.
```

This single Edit covers both the bullet insertion and verifies the surrounding blank line
+ Wiki link remain intact. Do not modify any other line of `README.md`. Do not change
heading levels, badge URLs, the installation tables, or the feature bullet wording above.

**Acceptance Criteria:**
- README's "High-level features" list now has four bullets (the original three plus the
  new outbox bullet at position 4).
- The blank line between the bullet list and the "See the [Wiki]..." line is preserved.
- The link is a relative markdown link to `docs/outbox-pattern.md` (no `./` prefix, no
  absolute URL).
- `git diff README.md` shows a +1/-0 line diff (one bullet added, nothing removed); the
  Edit's `new_string` replaces only the bullet+blank+Wiki three-line block to anchor the
  insertion, so the displayed diff is effectively a single line addition.

## Verification

```bash
# Bullet present, exactly once
grep -c "Transactional outbox pattern on SqlServer / PostgreSQL" README.md
# Expect: 1

# Bullet sits immediately after the job scheduler bullet
grep -n -A1 "Re-occurring job scheduler" README.md | head -3
# Expected output (line numbers may vary by one):
# 13:- Re-occurring job scheduler
# 14:- Transactional outbox pattern on SqlServer / PostgreSQL (see [`docs/outbox-pattern.md`](docs/outbox-pattern.md))

# Blank line + Wiki line still present after the new bullet
grep -n "See the \[Wiki\]" README.md
# Expect: a single match; line number should be (previous_wiki_line + 1)

# No churn on the installation table or badges
git diff --stat README.md
# Expect: 1 file changed, 1 insertion(+)

# The relative link target will exist once PLAN-1.2 lands
test -f docs/outbox-pattern.md && echo "doc target present" || echo "doc target missing (acceptable until PLAN-1.2 lands)"
```

## PROJECT.md Success Criteria coverage

| Plan element | §SC |
|---|---|
| README pointer to outbox doc | §SC #10 (final requirement: "README points at the new page" per ROADMAP §Phase 7 success criteria) |
