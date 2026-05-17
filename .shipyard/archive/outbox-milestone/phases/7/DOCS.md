# Documentation Close-Out Audit — Phase 7

**Phase:** 7 — Transactional Outbox Pattern documentation
**Date:** 2026-05-15
**Auditor:** Documentation Engineer (claude-sonnet-4-6)

---

## §1 Phase 7 Deliverable Adequacy

**File:** `docs/outbox-pattern.md` (204 lines)

### Coverage against CONTEXT-7 Decision 2 requirements

| Required topic | Present | Notes |
|---|---|---|
| Tutorial — runnable end-to-end example | Yes | SqlServer sync path, complete vertical slice |
| Reference 1 — Lifecycle contract | Yes | Never-commit/never-dispose invariants explicit |
| Reference 2 — Retry contract | Yes | `IRetrySkippable` bypass explained, retry wrapping diagram included |
| Reference 3 — Schema deployment prerequisite | Yes | Prose steps for `QueueCreationContainer` + `CreateQueue` |
| Reference 4 — DB-name comparison semantics | Yes | Table + per-transport extractor named; Ordinal-on-both confirmed |
| Reference 5 — Supported transports | Yes | Positive list + negative list; capability-cast failure behavior noted |

### Quality observations

**Tutorial (§Quick Start)**

- Example is self-contained and covers the complete lifecycle from container setup through `transaction.Commit()`. Actionable without prior reading of PROJECT.md.
- `SendAsync` mentioned explicitly as available; sync form chosen for clarity. Appropriate.
- PostgreSQL variation covered as inline prose note, not a duplicate code block — correct per Decision 2.
- Schema deployment is deferred to the reference section, which is the right split. A tutorial reader does not need that detail mid-example.

**Reference sections**

- Lifecycle contract: the bullet "The producer performs its queue INSERTs ... using `transaction.Connection` and the `transaction` reference directly" is precise and accurate against implementation.
- The note "See PROJECT.md §Ownership & Threading Contract" at the end of the lifecycle section is an internal-reference-only pointer. If `docs/outbox-pattern.md` ever moves to the Wiki, that pointer will be dangling. Minor future-polish item only.
- Retry contract: the ASCII tree diagram is clear.
- The line "See PROJECT.md §Functional Implementation and `IRetrySkippable.cs` for implementation details" is an implementer-facing pointer — acceptable in a reference section.
- DB-name semantics table: correctly shows `Ordinal` for both transports (matches Phase 3 commit `994e1404` pass-through implementation), not the original PROJECT.md asymmetric design. This is the load-bearing correction from SUMMARY-1.2.
- Supported transports: "the interface is simply absent, so misconfigured callers fail at the cast rather than at the first `Send`" — accurate and useful.

**Gaps (non-blocking, future polish)**

1. **No `SendAsync` code example.** The doc states async overloads exist but shows only sync. For async-first codebases this is a friction point. Low priority since the sync example is structurally identical to async.
2. **Batch overload not shown or explained.** `Send(IEnumerable<QueueMessage<T, IAdditionalMessageData>>, DbTransaction)` is a public API on `IRelationalProducerQueue<T>` that callers may reach for. It merits at minimum a "see also" note in the reference section. Low priority.
3. **No DI-injection pattern for `IRelationalProducerQueue<T>`.** The example resolves the producer directly from `QueueContainer`. Production code will typically inject it via the DI container. A short note on how to register + inject would reduce friction for users whose first instinct is constructor injection. Low priority.
4. **`IAdditionalMessageData` in send signatures mentioned only implicitly.** Callers who want correlation IDs or delay scheduling alongside the outbox path have no example. Low priority; existing producer docs cover `IAdditionalMessageData` independently.

Overall: the deliverable satisfies CONTEXT-7 Decision 2 and §SC #10 fully. The gaps above are §5 ISSUE candidates, not blockers.

---

## §2 Pointer Audit

**Existing pointers:**

- `README.md` line 14: bullet under "High-level features" → `docs/outbox-pattern.md`. Present, link syntax correct, target exists. Verified by PLAN-2.1 Task 3.
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` line 37: XML doc `<c>docs/outbox-pattern.md</c>`. Present. This is a plain-text reference (not a hyperlink) which is correct for XML doc comments — XML docs do not resolve file paths as links.

**Spot-check — other locations:**

| Location | Needs pointer? | Finding |
|---|---|---|
| `docs/jenkins-setup.md` | No | Jenkins CI doc; no outbox surface |
| `CLAUDE.md` | No | Mentions no outbox-specific lesson. No link needed — CLAUDE.md captures lessons-learned, not feature cross-references |
| `IRetrySkippable.cs` XML doc | No | Already mentions "outbox-pattern feature" inline (line 28 observed); no doc link needed on an internal type |
| Non-relational test files (`MemoryProducerDoesNotImplementRelationalTests`, `LiteDbProducerDoesNotImplementRelationalTests`, `RedisProducerDoesNotImplementRelationalTests`) | No | Test-only files; doc links in test XML comments are not convention on this repo |

**Verdict: no additional pointers needed.** The README bullet and the `IRelationalProducerQueue<T>` XML doc reference are sufficient for discoverability.

---

## §3 Wiki Future-Task Brief

**Background:** CONTEXT-7 Decision 1 deferred the Wiki update. The brief below is source material for a human writer post-ship.

### Recommended Wiki page structure

```
Title: Transactional Outbox Pattern

1. What is the outbox pattern? (1–2 paragraphs — adapt §Overview from docs/outbox-pattern.md)
2. When to use it (supported transports table — same as docs/outbox-pattern.md §Supported Transports)
3. Quick start (code example from docs/outbox-pattern.md §Quick Start — copy verbatim, update prose framing for Wiki voice)
4. How it works (capability-cast explanation, IRelationalProducerQueue<T> relationship to IProducerQueue<T>)
5. Lifecycle contract (adapt docs/outbox-pattern.md §Lifecycle Contract)
6. Retry contract (adapt docs/outbox-pattern.md §Retry Contract)
7. Schema setup (adapt docs/outbox-pattern.md §Schema Deployment Prerequisite — add QueueCreationContainer code example that was omitted from docs/ for conciseness)
8. DB-name comparison semantics (adapt table from docs/outbox-pattern.md §Database-Name Comparison Semantics)
9. EF Core integration sketch (NOT in docs/ — a good Wiki addition; show wrapping `DbContext.SaveChangesAsync` + relationalProducer.SendAsync inside the same `BeginTransactionAsync` block)
10. FAQ / troubleshooting (InvalidOperationException: names don't match; what happens on rollback; can I use this with MSDTC)
```

### Source material pointers

| Material | Path |
|---|---|
| In-repo doc (primary) | `docs/outbox-pattern.md` |
| Feature requirements + contract language | `.shipyard/PROJECT.md` §Goals, §Functional, §Ownership & Threading Contract |
| Design decisions | `.shipyard/phases/7/CONTEXT-7.md` |
| DB-name comparison correction | `.shipyard/phases/7/results/SUMMARY-1.2.md` §Issues Encountered |
| Public API surface | `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` |
| Polly bypass mechanism | `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs`, `.shipyard/notes/phase-1-polly-bypass-spike.md` |

### Recommended sequence

1. Merge `feature/outbox-pattern` to master and publish the release.
2. Allow 2–4 weeks for early users to read `docs/outbox-pattern.md` and raise questions via GitHub issues.
3. Incorporate any FAQ items from issues into the Wiki draft before publishing.
4. Expand the §Schema setup section with a complete `QueueCreationContainer` code example (omitted from docs/ for scope).
5. Add the EF Core integration sketch (section 9 above) — this is the most-requested integration style for outbox and is not covered in the in-repo doc.
6. Publish Wiki page.

---

## §4 PROJECT.md §SC Closure Mapping

Phase 7 closes §SC #10 (the documentation criterion). The earlier phases 1–6 closed §SC #1–9 and #11.

| §SC | Text (abbreviated) | Closed by |
|---|---|---|
| 1 | `IRelationalProducerQueue<T>` exists in Transport.RelationalDatabase, implemented by SqlServer + PostgreSQL | Phases 2–4 |
| 2 | Memory/Redis/LiteDb/SQLite `is` check fails | Phase 5 |
| 3 | Capability-cast pattern works | Phases 2–4 |
| 4 | Atomic commit verified (integration test) | Phase 5/6 |
| 5 | Atomic rollback verified | Phase 5/6 |
| 6 | Cross-DB validation throws | Phase 5/6 |
| 7 | Caller-owned resources not disposed (unit test) | Phase 5/6 |
| 8 | Polly retry bypass verified | Phase 5/6 |
| 9 | Existing tests pass; no regressions | Phase 5/6 |
| **10** | **`docs/outbox-pattern.md` drafted covering all five reference topics** | **Phase 7 (PLAN-1.2)** |
| 11 | Jenkins 14-stage matrix passes on feature branch via draft PR | Phase 6 / PR gate |

Phase 7 also satisfies the three ROADMAP §Phase 7 success criteria recorded in SUMMARY-2.1:

| Criterion | Evidence |
|---|---|
| Release `-p:CI=true` build — 0 XML-doc warnings | PLAN-2.1 Task 1: 0 errors, 0 CS1591 |
| README points at new page | PLAN-1.3 commit `af4fee60`; PLAN-2.1 Task 3 link-resolution check |
| PROJECT.md §SC #10 satisfied | This audit + PLAN-1.2 |

---

## §5 ISSUE Candidates Surfaced

### ISSUE-039 (carried from SUMMARY-1.2) — PROJECT.md §Validation asymmetric-comparison language is stale

**Location:** `.shipyard/PROJECT.md` lines 76–77
**Current text:** "SqlServer: `conn.Database`, compared case-insensitively (`StringComparer.OrdinalIgnoreCase`). PostgreSQL: `conn.Database`, compared case-sensitively (`StringComparer.Ordinal`)."
**Actual implementation:** Both transports use `StringComparer.Ordinal` (Phase 3 commit `994e1404` pass-through fix made comparison symmetric).
**Impact:** PROJECT.md is internal reference, not user-facing. Low priority. Misleads future plan authors if the comparison is ever revisited.
**Suggested fix:** Update lines 75–77 of PROJECT.md to read "Both transports: `conn.Database`, compared byte-for-byte (`StringComparer.Ordinal`). See `docs/outbox-pattern.md` §Database-Name Comparison Semantics for the rationale."
**Classification:** Documentation maintenance. Not blocking for ship.

### ISSUE-040 (new) — `docs/outbox-pattern.md` has no `SendAsync` example

**Location:** `docs/outbox-pattern.md`
**Gap:** Six overloads on `IRelationalProducerQueue<T>` include async variants and batch variants. Only the sync single-message `Send` is shown. Async-first callers have no cut-paste starting point.
**Suggested fix:** Add a compact `SendAsync` code snippet (5–6 lines, `await relationalProducer.SendAsync(msg, transaction)`) as a follow-on to the existing example. Batch variant can remain prose-only.
**Classification:** Documentation polish. Not blocking for ship.

### ISSUE-041 (new) — `IRelationalProducerQueue<T>` XML doc reference to `docs/outbox-pattern.md` is a plain-text path, not a hyperlink

**Location:** `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` line 37
**Current:** `See <c>docs/outbox-pattern.md</c>` — renders as code-literal in IDE tooltips, not a clickable link.
**Suggested fix:** Change to `<see href="https://github.com/blehnen/DotNetWorkQueue/blob/master/docs/outbox-pattern.md">docs/outbox-pattern.md</see>` once the branch ships to master and the URL is stable.
**Classification:** Documentation polish. Not blocking for ship; requires the branch to be merged first so the URL is valid.
