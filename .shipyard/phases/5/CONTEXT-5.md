# Phase 5 Context: Negative-Path Coverage on Non-Relational Transports

Source phase description: `.shipyard/ROADMAP.md` §Phase 5.
Source project: `.shipyard/PROJECT.md`.
Prior phases: Phase 3 (SqlServer) + Phase 4 (PostgreSQL) — both shipped end-to-end at unit-test level. The 4 non-relational transports under audit here received NO Phase 3/4 changes; Phase 5 verifies that omission was intentional.

## Phase Scope (from ROADMAP.md + user decisions below)

Phase 5 is **defensive verification**, not production-code work. The phase ships:

1. **4 negative-path unit tests** (one per non-relational transport): Memory, Redis, LiteDb, SQLite. Each asserts that the transport's `IProducerQueue<T>` resolution does NOT satisfy `IRelationalProducerQueue<T>`. Test approach is **type-system check** mirroring Phase 3/4's capability-cast pattern (Decision 1).
2. **Reflection-based assembly assertion** in each negative-path test: confirms no type in the transport's main assembly implements `IRelationalProducerQueue<T>` (Decision 2). Strict invariant — even private internal types are caught.
3. **1 extra SQLite-specific assertion**: confirms SQLite's producer does NOT derive from `RelationalProducerQueue<T>` base class — defends against the "accidentally wired to the relational base" case ROADMAP flags as a real risk for SQLite specifically (Decision 4).

**Total scope:** 4 negative-path test methods + 1 extra SQLite-specific assertion = ~5 test methods across 4 test projects. Plus the reflection-based assembly assertion folded into each negative test.

**Risk classification:** Low per ROADMAP. Pure assertion of a Phase 2 design invariant.

**Phase size:** S per ROADMAP (1–2 hours).

## User Decisions

### Decision 1: Test approach — TYPE-SYSTEM CHECK (mirror Phase 3/4 capability cast pattern)

Each test asserts `Assert.IsFalse(typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(typeof(<TransportProducerQueue>)));` using a representative producer type from the transport. This bypasses the SimpleInjector `EnableAutoVerification` issue Phase 3 discovered (where the runtime DI test surfaces pre-existing transient-disposable diagnostic warnings).

**Why type-system not runtime:**
- Phase 3 Wave 1 proved that `container.GetInstance<IProducerQueue<T>>()` triggers eager verification → ActivationException for unrelated reasons. Same problem would apply to Phase 5 negative tests (worse: it'd be inconsistent — some transports might happen to verify clean, others not).
- The type-system check is precise: it answers "does this type implement this interface" yes/no, no DI machinery involved.
- Combined with the reflection-based assembly assertion (Decision 2), the invariant is fully proven at the type-system level.

**Rejected: runtime DI resolution.** Same EnableAutoVerification surface as Phase 3. Worse: non-relational transports may have different sets of pre-existing diagnostic warnings than the relational ones; tests would be flaky across transports.

**Rejected: source-code grep gate (no unit tests).** Less useful as a regression detector; tests are the right tool for type-system invariants.

### Decision 2: Grep gate — REFLECTION ASSERTION IN EACH NEGATIVE-PATH TEST

Each negative-path test ALSO performs:

```csharp
var transportAssembly = typeof(<TransportInit>).Assembly;
var allTypes = transportAssembly.GetTypes();
bool anyImplementsRelational = allTypes.Any(t =>
    t.GetInterfaces().Any(i =>
        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRelationalProducerQueue<>)));
Assert.IsFalse(anyImplementsRelational,
    $"Transport assembly '{transportAssembly.GetName().Name}' must not implement IRelationalProducerQueue<T>.");
```

This catches both:
- The closed-generic case (e.g., `class Foo : IRelationalProducerQueue<Bar>`).
- The open-generic case (e.g., `class Foo<T> : IRelationalProducerQueue<T>`).

Implementation note: `t.GetInterfaces()` returns the closed-generic forms. For the open-generic check we walk to `i.GetGenericTypeDefinition()`. Both forms collapse to `typeof(IRelationalProducerQueue<>)` for comparison.

**Rejected: source-grep test reading `.cs` files from disk.** Same brittleness pattern as ISSUE-033/034/035 (relative-path-walk fragility, comment/string false positives). Reflection-on-loaded-assembly is precise and robust.

**Rejected: skip the assembly check entirely.** The ROADMAP explicitly calls for a "grep-style assertion that non-relational transport assemblies do not reference `IRelationalProducerQueue<T>`". Reflection is the modern equivalent of grep on an assembly.

### Decision 3: Plan structure — 2 PLANS ACROSS WAVE 1, PARALLEL

The 4 negative-path tests split across 2 plans for the ≤3 tasks/plan rule:

- **PLAN-1.1** (Wave 1): Memory + LiteDb negative tests (2 tasks).
- **PLAN-1.2** (Wave 1): Redis + SQLite negative tests (2 tasks). SQLite includes the extra Decision-4 assertion.

Each plan touches 2 different test projects — no file conflicts between PLAN-1.1 and PLAN-1.2. Wave 1 parallel-safe.

**Rejected: 1 plan with 4 tasks.** Violates Shipyard ≤3 tasks/plan rule.

**Rejected: 4 plans, one per transport.** Excessive builder dispatch overhead for trivial mechanical work. The 2-plan structure pairs naturally (relational-like SQLite paired with non-relational Redis in PLAN-1.2 to keep Memory + LiteDb together for a "stores in memory or local file" semantic grouping).

### Decision 4: SQLite-specific extra assertion — ADD `RelationalProducerQueue<T>` BASE-CLASS CHECK

The SQLite test in PLAN-1.2 carries TWO assertions instead of one:

1. **Primary (Decision 1):** SQLite's producer type does NOT implement `IRelationalProducerQueue<T>`.
2. **Extra (Decision 4):** SQLite's producer type does NOT derive from `RelationalProducerQueue<T>` base class.

Rationale: SQLite is the closest transport in shape to the relational ones (per ROADMAP §Phase 5 description — it's the explicitly-deferred relational case). The extra assertion catches the "accidentally inherits from `RelationalProducerQueue<T>` even though it shouldn't implement the outbox surface" misconfiguration.

```csharp
Assert.IsFalse(
    typeof(RelationalProducerQueue<TestMessage>).IsAssignableFrom(typeof(<SqliteProducerQueue>)),
    "SQLite producer must not derive from RelationalProducerQueue<T> base — outbox surface is deferred for SQLite.");
```

**Rejected: treat SQLite identically to Memory/Redis/LiteDb.** ROADMAP explicitly flags SQLite's shape similarity as a risk; the extra assertion costs nothing and catches a documented threat model.

## Hard Rules / Cross-Cutting Constraints

- **No production code changes.** Phase 5 ships test code only. Any source-file modification flags a CRITICAL_ISSUES review finding.
- **No new NuGet dependencies.** All assertions use reflection from existing .NET BCL + the existing test project dependencies.
- **MSTest 4.x assertions:** `Assert.IsFalse`, `Assert.IsTrue`. NEVER `Assert.ThrowsException<>` (irrelevant here but enforced per repo convention).
- **LGPL-2.1 license header** on every new `.cs` file.
- **Tests must run as plain unit tests** (no DB connection, no `QueueContainer<T>.CreateProducer<T>` invocation). Type-system + reflection only.
- **Build cleanliness:** All 4 non-relational test projects build clean on net10.0 + net8.0 with `TreatWarningsAsErrors`. No new XML doc required (test methods aren't part of the public API surface).
- **No regressions** in existing non-relational test suites. The negative-path test additions must not break any pre-existing test.

## Exit Criteria for Phase 5

1. **4 negative-path unit tests pass:** one each in `Transport.Memory.Tests`, `Transport.Redis.Tests`, `Transport.LiteDb.Tests`, `Transport.SQLite.Tests`. Each test asserts the type-system invariant + reflection-based assembly assertion.
2. **SQLite test carries the extra `RelationalProducerQueue<T>` base-class assertion** per Decision 4.
3. **Build clean** on net10.0 + net8.0 across all 4 non-relational test projects.
4. **No regressions** in existing non-relational test suites.
5. **PROJECT.md §Success Criteria #2 satisfied** ("transport sub-types must not accidentally implement the new interface").

## Out of Scope (Phase 5)

- Any production-code change in non-relational transports (Memory/Redis/LiteDb/SQLite main projects).
- Runtime DI resolution tests (deferred to Phase 6 integration tests for relational; not applicable for non-relational).
- Adding new NuGet dependencies.
- Modifying `.shipyard/ISSUES.md` (no new issues expected from this phase — pure verification).
- Documentation updates (Phase 7 territory).

## Dependencies

- **Phase 3** (SqlServer) + **Phase 4** (PostgreSQL) — both shipped. Phase 5 confirms the absence of relational-producer types on the OTHER transports.
- Phase 2 (foundation: `IRelationalProducerQueue<T>` interface exists in `Transport.RelationalDatabase`). Already shipped.

Phase 5 does NOT depend on Phase 6.

## Notes for Architect

The architect should produce 2 plans:

**PLAN-1.1 (Memory + LiteDb):**
- Task 1: `Transport.Memory.Tests` — new test file (`Basic/MemoryProducerQueueDoesNotImplementRelationalTests.cs` or similar). 1 [TestMethod] with type-system check + reflection-based assembly assertion.
- Task 2: `Transport.LiteDb.Tests` — new test file (similar path). Same shape.

**PLAN-1.2 (Redis + SQLite):**
- Task 1: `Transport.Redis.Tests` — same shape as Memory/LiteDb.
- Task 2: `Transport.SQLite.Tests` — same shape PLUS the extra `RelationalProducerQueue<T>` base-class assertion (Decision 4).

Researcher should locate the concrete producer-queue types for each transport (likely `MemoryProducerQueue<T>`, `RedisProducerQueue<T>`, `LiteDbProducerQueue<T>`, `SqliteProducerQueue<T>` or similar — actual names per the transport projects). The test code uses these specific closed-generic types.
