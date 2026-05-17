# Phase 1 Context: Polly Decorator Bypass Spike

## Phase Scope (from ROADMAP.md)

Discovery spike, no production code shipped to master. Stand up a throwaway test harness that resolves `SendMessageCommandHandler` (sync + async) for SqlServer and PostgreSQL **without** the `BeginTransactionRetryDecorator` and any related Polly retry wrappers in the chain. Confirm whether a bare-handler resolution is reachable via existing SimpleInjector seams, a keyed registration, or whether the producer must construct the handler directly.

**Phase risk classification:** mid → resolves Risk #1 from PROJECT.md Risk Inventory. The single most likely source of late-phase rework, deliberately scheduled first.

**Phase size:** S (1–3 hours wall time).

## User Decisions

### Decision 1: Investigate both transports up-front

The spike enumerates the decorator chain on **both** SqlServer and PostgreSQL during this phase, not SqlServer-only with an assumption that PostgreSQL mirrors. Each transport-specific init class can layer additional decorators on top of the shared `Transport.RelationalDatabase` registrations, so per-transport divergence must be discovered now rather than during Phase 4 (PostgreSQL implementation) where it would cost a build cycle and possibly a plan revision.

Concretely:
- The decorator-chain enumeration lists every wrapper around `SendMessageCommandHandler` and `SendMessageCommandHandlerAsync` for **both** transports.
- The keep/skip decision per decorator is made per-transport (the same decorator may be present in one and absent in the other).
- The chosen bypass mechanism (keyed registration / direct / child scope) is validated against **both** transports' DI graphs.

### Decision 2: Memo + throwaway proof-of-concept on the feature branch

The phase produces two artifacts:

1. **Durable:** A memo at `.shipyard/notes/phase-1-polly-bypass-spike.md` capturing the decorator inventory, keep/skip decisions, and the chosen bypass mechanism. This is committed and persists past Phase 2.

2. **Throwaway:** A proof-of-concept test that resolves the bare `SendMessageCommandHandler` via SimpleInjector using the chosen mechanism, against both transports' DI containers. The PoC lives on the feature branch only. It must be deleted at the start of Phase 2 (the foundation phase's first task explicitly removes it). Its purpose is to prove the chosen mechanism actually works in this codebase's SimpleInjector wiring — a memo alone would leave Phase 3's builder discovering the wiring doesn't work as planned.

Rejected: memo-only. Risk of Phase 3 finding out the mechanism doesn't work, then stalling.

### Decision 3: Memo path

Spike memo lives at `.shipyard/notes/phase-1-polly-bypass-spike.md`. Reasoning: this is throwaway-spike output, not a long-lived architecture decision record. The `.shipyard/notes/` location signals "context for the in-flight milestone," matches the roadmap's reference, and is co-located with other Shipyard state. If a future maintainer asks "why does the relational producer reach the bare handler through mechanism X," the memo is one path-component away from the milestone's PROJECT.md and ROADMAP.md.

Rejected: `docs/decisions/polly-bypass-mechanism.md` (ADR-style). The spike is intentionally short-lived analysis; the durable contract is captured in PROJECT.md (the lifecycle and retry contracts) and the wiring is captured in code + XML docs (Phase 7's `docs/outbox-pattern.md`). An ADR is too heavy for a 1–3 hour spike output.

## Exit Criteria for Phase 1

1. `.shipyard/notes/phase-1-polly-bypass-spike.md` exists and contains:
   - Per-transport decorator inventory for `SendMessageCommandHandler` and `SendMessageCommandHandlerAsync`.
   - Keep/skip decision per decorator entry.
   - The chosen bypass mechanism (one of: keyed registration via SimpleInjector, direct handler injection bypassing DI decoration, container child scope, or producer-side construction with explicit dependencies).
   - Justification linking the choice to what SimpleInjector actually supports in this codebase's wiring.

2. A throwaway proof-of-concept exists on the feature branch (location to be decided in the plan — likely under an `_spike/` or `Source/` test project temporary location). It must:
   - Build and run locally.
   - Resolve the bare handler via the chosen mechanism for **both** SqlServer and PostgreSQL DI containers.
   - Be tagged with a comment or filename pattern that makes its throwaway nature unambiguous (e.g., `_SpikePollyBypassPoC.cs` or a folder named `_spike/`).
   - Have its deletion encoded as a Phase 2 task.

3. PROJECT.md Risk Inventory Risk #1 is downgraded or closed.

## Out of Scope (Phase 1)

- Any production-code changes to `Transport.RelationalDatabase`, `Transport.SqlServer`, or `Transport.PostgreSQL`.
- The `IRelationalProducerQueue<T>` interface (Phase 2).
- The `SendMessageCommand.ExternalTransaction` property (Phase 2).
- Any of Phase 3–6 work.
- A wiki / `docs/outbox-pattern.md` page (Phase 7).

## Dependencies

None. Phase 1 is the first phase.
