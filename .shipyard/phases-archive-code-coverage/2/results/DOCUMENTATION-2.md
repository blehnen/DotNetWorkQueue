# Documentation Report
**Phase:** 2 -- RelationalDatabase handler test coverage

## Summary
- API/Code docs: 0 files (no production API changes)
- Architecture updates: 0
- User-facing docs: 0
- Proposed CLAUDE.md lessons learned: 3 (see P2 section)
- Proposed test convention doc: 1 (see P3 section)

## Scope Verification
Phase 2 is test-only. Diff touches only
`Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/Basic/**`:
- NEW: `CommandHandler/CreateJobTablesCommandHandlerTests.cs`
- NEW: `QueryHandler/GetJobIdQueryHandlerTests.cs`
- NEW: `QueryHandler/GetJobLastKnownEventQueryHandlerTests.cs`
- EXPANDED: `QueryHandler/GetDashboardJobsQueryHandlerTests.cs` (+5)
- EXPANDED: `QueryHandler/GetDashboardJobsQueryHandlerAsyncTests.cs` (+6)
- EXPANDED: `QueryHandler/GetDashboardErrorRetriesQueryHandlerTests.cs` (+5)
- EXPANDED: `QueryHandler/GetDashboardErrorRetriesQueryHandlerAsyncTests.cs` (+6)

No public interfaces were added, removed, or changed. **No XML doc
comment updates are required.** No README, architecture, or migration
doc updates needed.

---

## [P1 - None] Public API Documentation

None required. Phase 2 does not alter any production API surface.

---

## [P2 - Recommended] CLAUDE.md Lessons Learned (Proposed)

Three non-obvious discoveries from Phase 2 are worth promoting to the
"Lessons Learned" section. **Proposed wording only** — orchestrator to
review and add verbatim or edit as desired.

### Proposed Lesson 1 — Sync vs async handler mocking split

> Relational database handler tests need two different ADO.NET mock
> shapes. Sync handlers (`Handle`) use the `IDbConnection` /
> `IDbCommand` / `IDataReader` interfaces, but async handlers
> (`HandleAsync`) call `OpenAsync` / `ExecuteReaderAsync` which are
> defined only on the abstract `DbConnection` / `DbCommand` /
> `DbDataReader` base classes. Mocking the interfaces for an async
> handler will compile but the async methods go to default
> implementations and the test becomes a no-op. Use NSubstitute's
> `Substitute.For<DbConnection>()` etc. for any handler whose test
> targets `HandleAsync`.

*Source:* Plan 1.3 SUMMARY "Mocked dependencies" section; same split
is already applied in the existing `GetDashboardConfiguration*`
handler tests but was never written down.

### Proposed Lesson 2 — MSTest 3.x `Assert.ThrowsExactly<T>` (not `ThrowsException`)

> MSTest 3.x deprecated `Assert.ThrowsException<T>` in favor of
> `Assert.ThrowsExactly<T>` / `Assert.Throws<T>`. New tests must use
> the new API. A mix of old and new APIs in sibling files under
> `obj/` / `bin/` caches can surface phantom compile errors on the
> *correct* files when a stale build is reused — always clean
> `obj/bin` in the affected test project after concurrent plans
> touch multiple files.

*Source:* Plan 1.1/1.2 decisions + Plan 1.3 "Issues Encountered"
(stale obj/bin triggered phantom `ThrowsException` errors on
sibling files that were actually correct).

### Proposed Lesson 3 — Async dashboard handlers have no `CancellationToken`

> The `GetDashboard*QueryHandlerAsync` family does not accept a
> `CancellationToken` on `HandleAsync`. Do not add cancellation
> tests to these handler test files — the signature is fixed by
> `IQueryHandlerAsync<TQuery,TResult>` and the handlers cannot
> honor one without an interface change. Use
> `Task.CompletedTask`-style awaited-completion assertions instead.

*Source:* Plan 1.3 "Decisions Made" — explicit critique-fix removed
cancellation tests because the interface does not carry a token.

---

## [P3 - Nice-to-have] Test Convention Document (Proposed)

Phase 2 exposed that `DotNetWorkQueue.Transport.RelationalDatabase.Tests`
has **two coexisting fixture patterns** for handler tests:

1. **`CreateFixture()` factory** — used by `DoesJobExistQueryHandlerTests`
   and the new Phase 2 files (`CreateJobTablesCommandHandlerTests`,
   `GetJobIdQueryHandlerTests`, `GetJobLastKnownEventQueryHandlerTests`).
   Returns a tuple/holder of the handler + all mocks so each test
   arranges and asserts against named fields.

2. **`CreateHandler(int rowCount)` helper** — used by the existing
   `GetDashboard*QueryHandler[Async]Tests` dashboard suites. Takes a
   row count, produces a `bool[]` sequence for NSubstitute's
   `Returns(first, rest)` reader iteration, and returns the handler.

Plan 1.3 explicitly chose to *preserve* pattern (2) rather than
rewrite to pattern (1), which is the correct call — but the project
now has an undocumented convention that handler tests with reader
iteration use `CreateHandler(rowCount)` while one-shot command or
scalar-query handler tests use `CreateFixture()`.

### Recommendation

**Do not create a new documentation file for this.** Instead, the
orchestrator should decide one of:

- **Option A (lightest):** Add a one-line comment at the top of each
  helper method in the test files describing when each shape is
  preferred. No new markdown.
- **Option B:** Add a short "Test patterns" subsection to
  `CLAUDE.md` under `## Conventions` that codifies the two shapes in
  3-5 lines.
- **Option C (do nothing):** Both patterns are visible in-source
  and Phase 2 proved they coexist without friction. The cost of
  adding a convention doc nobody reads may exceed the value.

My recommendation: **Option C**. The split is organic (reader
iteration vs. scalar) and new contributors can copy the nearest
sibling file — the existing behavior already steers correctly.

---

## Gaps

None introduced by Phase 2. Pre-existing gap (not in Phase 2 scope):
`docs/` has no developer-facing guide on how to add a new
transport-independent handler test. Phase 2 did not make this gap
worse or better; flagging for future phases if coverage work
continues beyond Phase 2.

## Status by File
| File | Type | Status |
|---|---|---|
| `CreateJobTablesCommandHandlerTests.cs` | Test | No docs needed |
| `GetJobIdQueryHandlerTests.cs` | Test | No docs needed |
| `GetJobLastKnownEventQueryHandlerTests.cs` | Test | No docs needed |
| `GetDashboardJobsQueryHandlerTests.cs` (sync+async) | Test | No docs needed |
| `GetDashboardErrorRetriesQueryHandlerTests.cs` (sync+async) | Test | No docs needed |
| `CLAUDE.md` | Explanation | 3 proposed lessons (P2) — orchestrator to apply |

## Final Recommendation
Phase 2 documentation work is **complete with no file edits
required**. Orchestrator should review the three proposed
`CLAUDE.md` lessons in section P2 and decide whether to land them;
all three capture real, non-obvious friction that future builders
will otherwise rediscover.
