# Phase 5 Documentation Review

## Status: SUFFICIENT

Phase 5 ships 4 negative-path unit tests + 2 `.csproj` `<ProjectReference>` additions across `Transport.Memory.Tests`, `Transport.LiteDb.Tests`, `Transport.Redis.Tests`, `Transport.SQLite.Tests`. **Zero production-code changes.** No public-API surface added. The phase is pure defensive verification of a Phase 2 design invariant.

## XML Doc Coverage

- **N/A — test-only phase.** Test methods are not part of the public API surface; `TreatWarningsAsErrors` + XML doc generation only applies to packable production projects in `DotNetWorkQueueNoTests.sln`. CONTEXT-5.md §Hard Rules explicitly waives XML doc requirement: "No new XML doc required (test methods aren't part of the public API surface)."
- Build cleanliness confirmed: 0 errors on net10.0 + net8.0 for all 4 test projects per VERIFICATION-equivalent in SUMMARY-1.1 / SUMMARY-1.2.

## Architecture Documentation

- **Deferred to Phase 7 (consistent with Phase 4 documenter precedent).** Phase 4's documenter verdict explicitly recommended deferring `.shipyard/codebase/ARCHITECTURE.md` updates until the multi-transport picture is complete. Phase 5 — which only proves the *absence* of relational surface on 4 transports — adds no architectural shape worth documenting standalone. The right time to write up the relational/non-relational split is Phase 7, when SqlServer + PostgreSQL outbox + the negative-path invariants can be presented as a coherent system view.
- No diagrams, no component-interaction docs, no data-flow updates needed in Phase 5.

## User-Facing Documentation

- **N/A.** No README, migration guide, or how-to changes warranted. The outbox surface remains invisible to non-relational transport consumers — by design. Phase 7 will document the relational outbox feature end-to-end.

## CLAUDE.md "Lessons Learned" Candidates

Two candidates worth capturing at **ship time** (not now — phase-local lessons land in the ship-level documenter pass):

1. **Reflection-based assembly assertion for type-system invariants.** Pattern: load the transport assembly via `typeof(<TransportInit>).Assembly`, iterate `GetTypes()`, walk `GetInterfaces()` and compare `i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)`. Catches both closed-generic (`class Foo : IRelationalProducerQueue<Bar>`) and open-generic (`class Foo<T> : IRelationalProducerQueue<T>`) implementations. Robust replacement for source-file grep gates (which suffer from the relative-path-walk fragility documented in ISSUE-033/034/035). Anchor the assembly lookup on a stable transport type (e.g., `MemoryDashboardInit`, `LiteDbMessageQueueInit`) — not on a producer-queue type that might itself be the regression target.

2. **Type-system check beats runtime DI resolution for "transport must not implement X" invariants.** Phase 3 Wave 1 hit `SimpleInjector.EnableAutoVerification` triggering ActivationException for unrelated pre-existing transient-disposable diagnostic warnings when resolving `container.GetInstance<IProducerQueue<T>>()`. The type-system assertion (`Assert.IsFalse(typeof(IFoo).IsAssignableFrom(typeof(Bar)))`) sidesteps the entire DI verification surface — precise, deterministic, no flakiness across transports with different pre-existing warning baselines.

These are general-purpose patterns useful beyond the outbox feature. Recommend the Phase 7 ship-level documenter promote them into `CLAUDE.md` §Lessons Learned alongside the rest of the outbox-feature lessons.

## Recommendations

1. **Accept Phase 5 as documentation-SUFFICIENT.** Match Phase 3/4 documenter precedent.
2. **At ship time (Phase 7), capture the two lessons above** in `CLAUDE.md` §Lessons Learned. Keep them short and pattern-focused, not phase-narrative.
3. **At ship time (Phase 7), update `.shipyard/codebase/ARCHITECTURE.md`** with the relational/non-relational outbox split — the 4 negative tests from Phase 5 are the empirical proof that supports the architectural claim. Cross-reference the test file paths from the architecture doc.
4. **No action required for Phase 5 itself** beyond this review.

## Convention Deviation (non-doc, surfaced for traceability)

Both PLAN-1.1 and PLAN-1.2 builders used `test(memory):` / `test(litedb):` / `test(redis):` / `test(sqlite):` commit prefixes instead of the `shipyard(phase-5):` convention used in Phases 1–4. Non-blocking; flagged in both SUMMARYs. Not a documentation concern but worth noting if a future grep over commit history needs to find Phase 5 work — `git log --grep "test(memory)\|test(litedb)\|test(redis)\|test(sqlite)"` rather than the usual `--grep "shipyard(phase-5)"`.
