# Phase 1 Research: Polly Decorator Bypass Spike

**Investigation goal:** Catalog every decorator currently wrapping `SendMessageCommandHandler` (sync) and `SendMessageCommandHandlerAsync` (async) on both SqlServer and PostgreSQL transports. Identify viable SimpleInjector seams for resolving the bare (un-decorated) handler in the new caller-supplied-transaction code path.

**Researcher note.** The Shipyard researcher agent stalled during dispatch (known pattern on this repo per `feedback_agent_lockups.md`). Investigation was conducted directly by the orchestrator. Findings below are from direct file inspection of the init classes, decorator implementations, and policy registrations.

---

## Section 1: Decorator Inventory

### SqlServer — `ICommandHandlerWithOutput<SendMessageCommand, long>` (sync)

Registration in `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs`:

| Order | File:Line | Type | Scope | Behavior |
|---|---|---|---|---|
| Innermost | `Basic/CommandHandler/SendMessageCommandHandler.cs:39` | `SendMessageCommandHandler` (concrete) | n/a | The actual 3-INSERT send path (body, status, meta) |
| Decorator 1 | `Decorator/RetryCommandHandlerOutputDecorator.cs:28` | `RetryCommandHandlerOutputDecorator<TCommand, TOutput>` | Open generic — registered at line 154 of init via `RegisterDecorator(typeof(ICommandHandlerWithOutput<,>), typeof(RetryCommandHandlerOutputDecorator<,>))` | Wraps `Handle()` in a Polly `ResiliencePipeline` resolved from `IPolicies.Registry` keyed by `TransportPolicyDefinitions.RetryCommandHandler` |
| Decorator 2 (outermost) | `Trace/Decorator/SendMessageCommandHandlerDecorator.cs` | `SendMessageCommandHandlerDecorator` | Closed type for `SendMessageCommand` only — registered at line 182 | OpenTelemetry `ActivitySource.StartActivity` for the send |

**Final resolved chain:** `TraceDecorator → RetryDecorator → SendMessageCommandHandler`

### SqlServer — `ICommandHandlerWithOutputAsync<SendMessageCommand, long>` (async)

Same shape, async-flavored:

| Order | Type | Source |
|---|---|---|
| Innermost | `SendMessageCommandHandlerAsync` (concrete) | `Basic/CommandHandler/SendMessageCommandHandlerAsync.cs:39` |
| Decorator 1 | `RetryCommandHandlerOutputDecoratorAsync<TCommand, TOutput>` | `Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` — registered at init line 160 |
| Decorator 2 (outermost) | `SendMessageCommandHandlerAsyncDecorator` (trace) | `Trace/Decorator/SendMessageCommandHandlerAsyncDecorator.cs` — registered at init line 186 |

**Final resolved chain:** `TraceDecorator → RetryDecoratorAsync → SendMessageCommandHandlerAsync`

### PostgreSQL — sync + async

Inspection of `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` lines 179–214 shows **bit-for-bit identical decorator registrations** to SqlServer:

- Line 179: `RegisterDecorator(typeof(ICommandHandlerWithOutput<,>), typeof(RetryCommandHandlerOutputDecorator<,>))` — same shape as SqlServer line 154
- Line 185: `RegisterDecorator(typeof(ICommandHandlerWithOutputAsync<,>), typeof(RetryCommandHandlerOutputDecoratorAsync<,>))` — same shape as SqlServer line 160
- Line 208: `RegisterDecorator(typeof(ICommandHandlerWithOutput<SendMessageCommand, long>), typeof(...PostgreSQL.Trace.Decorator.SendMessageCommandHandlerDecorator))` — SqlServer line 182 mirrors
- Line 212: `RegisterDecorator(typeof(ICommandHandlerWithOutputAsync<SendMessageCommand, long>), typeof(...PostgreSQL.Trace.Decorator.SendMessageCommandHandlerAsyncDecorator))` — SqlServer line 186 mirrors

**Resolved chains identical to SqlServer.** The PostgreSQL retry decorators (`Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs` and `RetryCommandHandlerOutputDecoratorAsync.cs`) are namespace-isolated near-copies of the SqlServer versions, using `TransportPolicyDefinitions.RetryCommandHandler` keyed `"PostgreSQLRetryCommandHandler"` instead of `"SqlServerRetryCommandHandler"`.

### Per-transport divergence: NONE

The CONTEXT-1 user decision to "investigate both transports up-front" turned up **zero divergence** in the decorator chain. Both transports use the open-generic retry + closed trace pattern. Anything we do to one, we mirror to the other.

### Polly policy structure (reference)

The retry behavior driven by `RetryCommandHandlerOutputDecorator`:

- `Source/DotNetWorkQueue.Transport.SqlServer/Basic/RetrySqlPolicyCreation.cs:55` adds a Polly `ResiliencePipeline` to `policies.Registry` keyed by `TransportPolicyDefinitions.RetryCommandHandler`.
- Decorator's `Handle()` resolves the pipeline via `_policies.Registry.TryGetPipeline(...)` and wraps the inner `_decorated.Handle(command)` call in `pipeline.Execute(...)`.
- When the registry is disposed (shutdown race fixed in PR #121, commits `1d28d8c4` + `48582e6c`), the decorator catches `ObjectDisposedException` and falls through to the bare handler call. **This is a precedent for "decorator gracefully no-ops" pattern** — exactly the seam we want to extend.

---

## Section 2: SimpleInjector Seam Inventory

This codebase wraps SimpleInjector behind a custom `IContainer` abstraction. Relevant surface:

| Method | Purpose | Supports caller-tx bypass? |
|---|---|---|
| `Register<T, U>(LifeStyles)` | Standard service registration | No |
| `RegisterDecorator(typeof(Iface), typeof(Decorator), LifeStyles)` | Wraps an interface registration with a decorator | Indirectly (see below) |
| `RegisterDecorator(typeof(Iface), typeof(Decorator), predicate, LifeStyles)` | **Conditional decorator with `Predicate<DecoratorPredicateContext>`** | No — predicate fires at **container build time**, not resolution time. Cannot see runtime command state. |
| `Conditional<T>(...)` (SimpleInjector native) | Alternate registration depending on context | Not used in this codebase; not wrapped in `IContainer` |
| Direct `Container.GetInstance<T>()` | Fully decorated resolution | Always returns the decorated chain — no built-in bypass |

**Key finding:** SimpleInjector does **not** support resolution-time decorator skipping based on runtime command state. The decorator predicate runs at container compile time, not when `Handle(command)` is called.

**Existing "bare handler" precedent:** None found. Every `ICommandHandlerWithOutput<SendMessageCommand, long>` consumer in the codebase resolves through the full decorator chain.

---

## Section 3: Three Bypass Mechanism Candidates — Ranked

Reframing the three candidates from PROJECT.md against what SimpleInjector + this codebase actually support:

### Mechanism A: Runtime-check inside the existing retry decorator — **RECOMMENDED**

**Approach.** Modify `RetryCommandHandlerOutputDecorator<,>` (and `RetryCommandHandlerOutputDecoratorAsync<,>`) on both transports so that `Handle()` inspects the incoming command: if the command implements a marker interface `IRetrySkippable` and `SkipRetry == true`, invoke `_decorated.Handle(command)` directly without Polly. Add the marker to `SendMessageCommand` such that `SkipRetry => ExternalTransaction != null`.

Existing decorator already has a "no pipeline → call handler directly" branch (line 65 of `RetryCommandHandlerOutputDecorator.cs`). The new bypass is a sibling early-return at the top of `Handle()`:

```csharp
public TOutput Handle(TCommand command)
{
    Guard.NotNull(() => command, command);
    if (command is IRetrySkippable s && s.SkipRetry)
        return _decorated.Handle(command);
    // ... existing pipeline lookup + execute
}
```

**Feasibility:** Maximum. Zero changes to DI registration. Drops the marker check into a decorator pattern that already has multi-branch fallthrough (shutdown-race branch from PR #121).

**Complexity:** Phase 2 + 3 + 4 touch 4 files for this mechanism — the SqlServer + PostgreSQL sync + async retry decorators. Plus the marker interface (1 file in `Transport.RelationalDatabase`) and the `SendMessageCommand.SkipRetry` property override (1 file in `Transport.Shared` or wherever the command lives).

**Trace decorators preserved:** Yes — the marker check fires inside the retry decorator, which sits *inside* the trace decorator. Trace still wraps everything.

**Risk:** Low. The decorator's existing fallthrough behavior (shutdown-race + no-pipeline branches) demonstrates this exact pattern is safe.

**Test seam quality:** Excellent. Unit-testable with just a `SendMessageCommand` instance (no container needed). Construct decorator with a mocked inner handler that records call count, hand it a command with `ExternalTransaction != null`, assert single invocation.

### Mechanism B: SimpleInjector keyed registration — **NOT RECOMMENDED**

**Approach.** Register a second handler instance keyed as "no-retry": when the relational producer's caller-tx path resolves the handler, it asks for the keyed variant. SimpleInjector supports this via `Container.Collection.Append` or conditional registrations, neither of which is wrapped in this codebase's `IContainer` abstraction.

**Feasibility:** Medium. Requires extending `IContainer` to expose keyed registration. Extension affects every transport's init class.

**Complexity:** Phase 2 + 3 + 4 plus `Transport.RelationalDatabase` AND `DotNetWorkQueue.IoC` — at least 7 files touched, plus the abstraction extension.

**Trace decorators preserved:** Tricky. The keyed registration would bypass both retry AND trace unless trace is re-registered against the keyed variant separately — doubling the trace decorator registration surface.

**Risk:** Mid. Container-abstraction expansion is a cross-cutting change with potential ripple to other transports.

**Verdict:** Heavyweight relative to Mechanism A's 4-file diff. Skip.

### Mechanism C: Producer-side direct construction (bypass DI) — **NOT RECOMMENDED**

**Approach.** `RelationalProducerQueue<T>` takes a direct constructor reference to the bare `SendMessageCommandHandler` (resolved via a service locator escape hatch) and calls it directly for the caller-tx path.

**Feasibility:** Medium. SimpleInjector doesn't expose "give me X without its decorators" — we'd construct the handler manually, which means duplicating the handler's constructor parameters in the producer.

**Complexity:** Producer factory grows by N constructor parameters (the inner handler's deps). DI graph fragmented across two resolution paths.

**Trace decorators preserved:** No — direct construction skips both retry AND trace. We'd lose OpenTelemetry coverage on caller-tx sends.

**Risk:** Mid-high. Loses trace observability. Introduces a "manually wired handler" surface that drifts from the DI-managed one.

**Verdict:** Worst of the three. Skip.

### Ranked recommendation

1. **Mechanism A — `IRetrySkippable` marker + runtime branch in retry decorator.** Adopt.
2. Mechanism B (keyed registration) — only if A unexpectedly fails the spike PoC.
3. Mechanism C (direct construction) — only as a last resort.

---

## Section 4: Existing Precedent for Retry-Bypassed Paths

**Found:** Yes, two precedents in the same decorator file.

1. **Shutdown-race bypass (PR #121, commit `1d28d8c4`).** When `_policies.Registry.TryGetPipeline(...)` throws `ObjectDisposedException` (registry shut down before last handler call), the decorator catches and falls through to `_decorated.Handle(command)`. This is **exactly the pattern** Mechanism A extends — a runtime check that selectively bypasses Polly.

2. **No-pipeline bypass (line 63–65).** When the registry doesn't have a pipeline registered for `RetryCommandHandler` (early init, test scenarios), the decorator falls through to the bare handler call. Same pattern.

The retry decorator is **already a "retry-or-passthrough" decorator** — it has two passthrough conditions today. Adding a third (`IRetrySkippable.SkipRetry`) is consistent with its existing design.

**No "RetryConstants.Off" flag, no `Polly.NoOp` policy** — passthrough is implicit via the existing fallthrough branches, not via a configured opt-out.

---

## Section 5: Risks & Unknowns the Spike PoC Must Demonstrate

1. **Marker interface placement.** The `IRetrySkippable` marker interface should live somewhere both `Transport.Shared` (where `SendMessageCommand` is defined) and the retry decorators (in each transport's `Decorator/` folder) can reference. Likely `Transport.Shared` itself, since the decorators already reference `DotNetWorkQueue.Transport.Shared` (line 20 of `RetryCommandHandlerOutputDecorator.cs`). **Spike must verify this reference graph holds.**

2. **`SendMessageCommand.SkipRetry` semantics.** The default (when `ExternalTransaction == null`) must be `false` so the existing retry behavior is preserved for the queue-owned-tx path. Spike PoC must confirm property dispatch works as expected.

3. **`RetryCommandHandlerDecorator<>`** (the non-output variant for `ICommandHandler<>`) at SqlServer init line 157 + PostgreSQL init line 182 — does this wrap `SendMessageCommand`? Looking at the type signatures, `SendMessageCommand` is `ICommandHandlerWithOutput<SendMessageCommand, long>` (returns the inserted message ID), not plain `ICommandHandler<>`. So no — only the output variants of the retry decorator are relevant to the bypass. **Spike PoC should sanity-check this by enumerating the resolved decorator chain.**

4. **Verify trace decorator stays wrapped.** The trace decorator is registered AFTER the retry decorator, so it sits OUTSIDE the retry layer. When retry skips its policy and calls the bare handler, the trace decorator is still wrapping everything outside. **Spike PoC should register a fake `ActivityListener` and verify a span is created on a caller-tx send.**

5. **Async vs sync parity.** The bypass logic must be implemented identically in `RetryCommandHandlerOutputDecoratorAsync<,>` — same marker check, same branch. **Spike PoC must cover both** (the user decision in CONTEXT-1 #1 mandates this).

---

## Summary

- **Decorator chain is identical on both transports:** `Trace(Retry(Handler))` for sync and async — no per-transport divergence.
- **Recommended mechanism: `IRetrySkippable` marker** evaluated by a new branch inside the existing retry decorators. Touches 4 retry-decorator files + 1 marker interface + 1 property addition to `SendMessageCommand`. Preserves trace decoration. Mirrors two existing fallthrough patterns in the same decorator file.
- **Top spike-PoC risk:** the marker interface's reference graph (must live in `Transport.Shared` so both `Transport.RelationalDatabase` and per-transport `Decorator/` folders can see it). PoC must confirm the project-references allow this placement.

The architect should now produce a plan that codifies (a) the memo at `.shipyard/notes/phase-1-polly-bypass-spike.md` capturing these findings, (b) the throwaway PoC test demonstrating the marker mechanism works in a constructed retry decorator with a recording inner handler, and (c) verification commands to confirm the trace decorator stays wrapped.
