# Phase 2 Documentation Review

## Status: SUFFICIENT

Phase 2 is foundation plumbing per `.shipyard/PROJECT.md` and `.shipyard/ROADMAP.md`. The user-facing `docs/outbox-pattern.md` is explicitly scoped to **Phase 7**. Phase 2's docs requirement (Hard Rules in `CONTEXT-2.md`: "Every new public type has an XML doc comment") is already gated by the Release build's `TreatWarningsAsErrors` + XML doc generation. All 5 SUMMARY files confirm clean Release builds, so coverage is enforced mechanically.

## XML Doc Coverage (Phase 2 new types)

Spot-checked all 7 new public types/members against the live source. Every public surface has a `<summary>` plus parameter/return/exception docs where applicable.

- **`IRetrySkippable`** (`Source/DotNetWorkQueue.Transport.RelationalDatabase/IRetrySkippable.cs`): complete. Type-level `<summary>` + `<remarks>` (introduces the outbox bypass rationale and points at `RelationalSendMessageCommand`); `SkipRetry` member `<summary>`.
- **`IExternalDbNameExtractor`** (`Source/DotNetWorkQueue.Transport.RelationalDatabase/IExternalDbNameExtractor.cs`): complete. Type-level `<summary>` documents the per-provider case-comparison split (SqlServer = OrdinalIgnoreCase, PostgreSQL = Ordinal) and forward-points to Phase 3/4 implementations; `Extract` member has `<summary>`, `<param>`, `<returns>`.
- **`ExternalTransactionValidator`** (`Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/ExternalTransactionValidator.cs`): complete. Type-level `<summary>` enumerates the four ordered validation checks; constructor and `Validate` have full `<param>` and `<exception>` docs.
- **`RelationalSendMessageCommand`** (`Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs`): complete. Type-level `<summary>` cross-references `IRetrySkippable` and explains construction site; constructor params and `SkipRetry` documented.
- **`IRelationalProducerQueue<TMessage>`** (`Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs`): complete. Type-level `<summary>` + `<remarks>` (capability-cast semantics + retry-decorator bypass + forward-pointer to `docs/outbox-pattern.md`); all 6 overloads documented; PROJECT.md `IEnumerable` vs `List<>` deviation flagged inline on the batch overload.
- **`RelationalProducerQueue<T>`** (`Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs`): complete. Type-level `<summary>` explains the four `protected virtual` throw-by-default hooks; all 4 hooks have `<summary>`/`<param>`/`<returns>`/`<exception>`; the public `IRelationalProducerQueue<T>` overloads use `<inheritdoc />`.
- **`SendMessageCommand.ExternalTransaction`** (`Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs`): complete. `<summary>` describes the bypass contract; `<remarks>` documents why the base class deliberately does NOT implement `IRetrySkippable` (layering rule — keeps `Transport.Shared` free of `Transport.RelationalDatabase` refs).
- **4 decorator bypass branches** (SqlServer sync/async + PostgreSQL sync/async): no new public API surface — single `if (command is IRetrySkippable skippable && skippable.SkipRetry)` early-return inserted into existing `Handle`/`HandleAsync` of pre-existing public decorators. No XML doc obligation.

Build evidence (from SUMMARYs):
- SUMMARY-1.1: `Transport.Shared` + `Transport.RelationalDatabase` Release builds — 0 errors (TreatWarningsAsErrors + XML doc on).
- SUMMARY-2.1: `Transport.RelationalDatabase` Release (net10.0 + net8.0) — 0 errors.
- SUMMARY-2.2: `Transport.RelationalDatabase` Release — 0 errors. Note: SUMMARY-2.2 explicitly records that XML docs were filled in beyond plan stubs precisely because CS1591 would otherwise fail Release.
- SUMMARY-3.1 / SUMMARY-3.2: SqlServer + PostgreSQL Release builds — 0 errors each.

## Architecture / Codebase Docs

`.shipyard/codebase/ARCHITECTURE.md` exists (read at review time). Phase 2 introduces an outbox-pattern producer capability layered above the existing producer/consumer pattern. The diagrams and major sections (Layer Diagram, Producer Side data flow, Transport Init Hierarchy, Decorator Pattern) are all still accurate — the bypass branch is an unobtrusive early-return inside the existing retry decorator, and `IRelationalProducerQueue<T>` is a capability-cast extension of `IProducerQueue<T>`, not a replacement.

Updates **recommended but not required at Phase 2**:

1. **Add a brief "Outbox Pattern (Phase 2 foundation)" subsection** under `## Key Architectural Patterns` (after §5 Producer/Consumer Pattern, before `## Message Lifecycle / Data Flow`) explaining:
   - `IRelationalProducerQueue<TMessage>` is an opt-in capability-cast on SqlServer/PostgreSQL producers — Memory/Redis/SQLite/LiteDb deliberately do NOT implement it.
   - `IRetrySkippable` is a per-command marker (not transport-wide) that lets `RelationalSendMessageCommand` bypass the Polly retry pipeline on tx-aware sends.
   - The marker interface lives in `Transport.RelationalDatabase` (not `Transport.Shared`) because moving it down would force a cyclic project reference. The base `SendMessageCommand.ExternalTransaction` property lives in `Transport.Shared`, but `Transport.Shared` does NOT implement `IRetrySkippable` for the same reason.
2. **Annotate `## Message Lifecycle / Data Flow` → "Producer Side (Send)"** with the bypass branch on the SqlServer/PostgreSQL RetryDecorator path: `if (command is IRetrySkippable { SkipRetry: true }) → skip Polly pipeline, invoke inner _decorated directly`.

**Recommendation:** Hold these edits for Phase 7's `docs/outbox-pattern.md` work, when the full user-facing semantics land and the architecture text can be updated in one coherent pass. Phase 2's bypass branches are not externally observable except through Phase 3+ wiring.

Other `.shipyard/codebase/` docs reviewed — none need Phase-2 updates:
- `CONVENTIONS.md`, `STACK.md`, `STRUCTURE.md`, `TESTING.md`, `CONCERNS.md`, `INTEGRATIONS.md` — Phase 2 adds files inside the existing transport-init hierarchy and respects all current conventions; no new directories, no new top-level patterns.

## CLAUDE.md Lessons Learned candidates

Two new lessons worth capturing **after Phase 3 lands** (Phase 3 will validate / contradict each; commit now would be premature):

1. **Marker-interface placement in layered transports.** A capability-affecting marker (`IRetrySkippable`) must live in the lowest layer that all consumers of it can reference without cycles. `Transport.Shared` would have been the "lowest" layer textually, but it cannot reference `Transport.RelationalDatabase` types, and the derived `RelationalSendMessageCommand` lives in `Transport.RelationalDatabase`. So the marker lives in `Transport.RelationalDatabase` and the **base** `SendMessageCommand` (in `Transport.Shared`) carries the `DbTransaction` property but NOT the marker — the derived class adds the marker. The retry decorators in each transport (SqlServer/PostgreSQL) reference the marker via `using DotNetWorkQueue.Transport.RelationalDatabase;` since those projects already depend on it. This pattern generalizes: when a feature spans Transport.Shared → Transport.RelationalDatabase → per-transport, carry the data in Shared and the capability marker in RelationalDatabase.

2. **`IRetrySkippable` is a per-call decorator bypass — not a transport-wide opt-out.** The decorator inspects each command instance at `Handle()` time. This is materially different from "registering a different decorator chain for this command type" — there is one decorator chain, but commands self-identify as bypass candidates. The pattern is reusable for any future cross-cutting decorator (logging, tracing, metrics) that needs per-call opt-out semantics for callers that own the cross-cutting concern themselves.

The Phase-1 "property-getter assertion pattern for unmockable sealed Polly types" lesson is already in CLAUDE.md and was applied during Phase 2 plan critique — no new entry needed.

**Recommendation:** Defer both lessons to the end of Phase 3. Phase 3 either (a) cleanly applies the marker pattern by registering SqlServer's `RelationalProducerQueue<T>` subclass — confirming lesson 1's layering claim — or (b) reveals a layering surprise that reshapes the lesson's wording.

## User-facing docs status

- `docs/outbox-pattern.md` is **scoped to Phase 7** per `.shipyard/ROADMAP.md`. Phase 2 does NOT create or stub this file. The `<remarks>` block on `IRelationalProducerQueue<TMessage>` already forward-references it (`See <c>docs/outbox-pattern.md</c> for the full lifecycle contract (Phase 7).`), which is the correct posture.
- `README.md`: N/A for Phase 2. The README pointer to `docs/outbox-pattern.md` lands with Phase 7.
- `docs/jenkins-setup.md`: no Phase-2 impact (no CI changes in this phase).

## Recommendations

1. **No documentation work required for Phase 2 to complete.** All Hard Rules are satisfied. Mechanical XML-doc enforcement via Release build is the right gate and is working as designed.
2. Defer `.shipyard/codebase/ARCHITECTURE.md` Outbox subsection to Phase 7, when it can be written against the full user-facing surface.
3. Defer the two CLAUDE.md "Lessons Learned" entries to end of Phase 3, when the layering claim is validated by the SqlServer wiring.
4. Phase 3 documenter should re-check this report's "deferred" items against Phase 3's outcomes before deciding whether to escalate them earlier than Phase 7.
