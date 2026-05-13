# Phase 1 Spike: Polly Decorator Bypass for Outbox Caller-Tx Path

**Outcome:** Mechanism confirmed. `IRetrySkippable` marker interface evaluated at the top of `RetryCommandHandlerOutputDecorator.Handle()` (and async equivalent) on both relational transports. Risk #1 downgraded to LOW. See PoC test at `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs` (deleted in Phase 2 Task 1).

## Decorator Inventory

The decorator chain wrapping `SendMessageCommandHandler` and `SendMessageCommandHandlerAsync` is the same on both SqlServer and PostgreSQL transports. Resolved chain: `TraceDecorator -> RetryDecorator -> SendMessageCommandHandler` (sync + async).

### SqlServer

Init class: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs`.

| Layer | Type | File | Init line |
|---|---|---|---|
| Innermost (sync) | `SendMessageCommandHandler` | `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandler.cs:39` | (concrete) |
| Retry (sync) | `RetryCommandHandlerOutputDecorator<TCommand, TOutput>` | `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs:28` | 154 |
| Trace (sync, outermost) | `SendMessageCommandHandlerDecorator` | `Source/DotNetWorkQueue.Transport.SqlServer/Trace/Decorator/SendMessageCommandHandlerDecorator.cs` | 182 |
| Innermost (async) | `SendMessageCommandHandlerAsync` | `Source/DotNetWorkQueue.Transport.SqlServer/Basic/CommandHandler/SendMessageCommandHandlerAsync.cs:39` | (concrete) |
| Retry (async) | `RetryCommandHandlerOutputDecoratorAsync<TCommand, TOutput>` | `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` | 160 |
| Trace (async, outermost) | `SendMessageCommandHandlerAsyncDecorator` | `Source/DotNetWorkQueue.Transport.SqlServer/Trace/Decorator/SendMessageCommandHandlerAsyncDecorator.cs` | 186 |

### PostgreSQL

Init class: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs`.

| Layer | Type | Init line |
|---|---|---|
| Retry (sync, open generic) | `RetryCommandHandlerOutputDecorator<,>` | 179 |
| Retry (async, open generic) | `RetryCommandHandlerOutputDecoratorAsync<,>` | 185 |
| Trace (sync, closed on `SendMessageCommand`) | `Trace.Decorator.SendMessageCommandHandlerDecorator` | 208 |
| Trace (async, closed on `SendMessageCommand`) | `Trace.Decorator.SendMessageCommandHandlerAsyncDecorator` | 212 |

The PostgreSQL retry decorators (`Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs` and `RetryCommandHandlerOutputDecoratorAsync.cs`) are namespace-isolated near-copies of the SqlServer versions. They resolve their pipeline via the same `TransportPolicyDefinitions.RetryCommandHandler` / `RetryCommandHandlerAsync` keys; the registered policy is keyed under a transport-specific prefix (`"PostgreSQLRetryCommandHandler"` vs `"SqlServerRetryCommandHandler"`) by the policy-creation classes.

## Per-Transport Divergence

**None found.** Both transports use the open-generic retry + closed trace pattern at parallel positions in their init class. The order of registration is identical: retry first (open generic, applies to all command output handlers), trace second (closed on `SendMessageCommand`). The bypass mechanism is mirrored across both transports without per-transport conditional logic. This closes the open question from CONTEXT-1 Decision 1.

## Chosen Mechanism: `IRetrySkippable` Marker Interface

A marker interface in `DotNetWorkQueue.Transport.Shared` that `SendMessageCommand` implements:

```csharp
namespace DotNetWorkQueue.Transport.Shared
{
    /// <summary>
    /// Marker interface for command objects that opt out of the relational retry decorator
    /// on a per-call basis. The retry decorator inspects this property at Handle() time and
    /// invokes the inner handler directly when SkipRetry is true.
    /// </summary>
    public interface IRetrySkippable
    {
        bool SkipRetry { get; }
    }
}
```

`SendMessageCommand` (in `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs`) implements the interface with `SkipRetry => ExternalTransaction != null`. The `ExternalTransaction` property is the new optional `DbTransaction` added by Phase 2.

The retry decorator on both transports gets a sibling early-return branch at the top of `Handle()`, placed before the pipeline-lookup `try`:

```csharp
public TOutput Handle(TCommand command)
{
    Guard.NotNull(() => command, command);

    if (command is IRetrySkippable skippable && skippable.SkipRetry)
        return _decorated.Handle(command);

    ResiliencePipeline pipeline = null;
    try
    {
        _policies.Registry.TryGetPipeline(TransportPolicyDefinitions.RetryCommandHandler, out pipeline);
    }
    catch (ObjectDisposedException)
    {
        // Shutdown race: registry disposed before the last handler call.
        // Fall through to direct handler — same semantics as the "no policy" branch.
    }

    if (pipeline != null)
        return pipeline.Execute(_ => _decorated.Handle(command));
    return _decorated.Handle(command);
}
```

The async variant in `RetryCommandHandlerOutputDecoratorAsync.HandleAsync` gets the same branch returning `await _decorated.HandleAsync(command).ConfigureAwait(false)`.

### Design justification

The existing decorator already has two passthrough branches that bypass Polly and call `_decorated.Handle(command)` directly:

1. **No-pipeline branch** (`RetryCommandHandlerOutputDecorator.cs:63-65`) — when `TryGetPipeline` returns false (pipeline never registered, e.g., test scenarios), the decorator falls through to the bare handler.
2. **Shutdown-race branch** (`RetryCommandHandlerOutputDecorator.cs:57-61`, added by PR #121 commits `1d28d8c4` + `48582e6c`) — when the registry has been disposed mid-shutdown, the `ObjectDisposedException` is swallowed and the decorator falls through to the bare handler.

Adding a third passthrough condition keyed on a per-command marker interface is consistent with the decorator's existing "retry-or-passthrough" design. The trace decorator sits **outside** the retry decorator, so the marker check fires after trace span creation — observability is preserved on the caller-tx path.

## Files to Touch in Phase 2+

Six files modified to ship the production change:

1. `Source/DotNetWorkQueue.Transport.Shared/IRetrySkippable.cs` — **new file**, the marker interface.
2. `Source/DotNetWorkQueue.Transport.Shared/Basic/Command/SendMessageCommand.cs` — implement `IRetrySkippable`; add the new optional `DbTransaction ExternalTransaction { get; }` property; expose `SkipRetry => ExternalTransaction != null`.
3. `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecorator.cs` — add the early-return branch.
4. `Source/DotNetWorkQueue.Transport.SqlServer/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` — add the async early-return branch.
5. `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecorator.cs` — mirror SqlServer change.
6. `Source/DotNetWorkQueue.Transport.PostgreSQL/Decorator/RetryCommandHandlerOutputDecoratorAsync.cs` — mirror SqlServer async change.

No DI registration changes are required — the decorator is already in the chain; only its `Handle()` body grows by one branch.

The `IRetrySkippable` placement in `Transport.Shared` (not `Transport.RelationalDatabase`) is dictated by the existing reference graph: `RetryCommandHandlerOutputDecorator.cs` already has `using DotNetWorkQueue.Transport.Shared;` (line 20), and `SendMessageCommand` lives in `Transport.Shared.Basic.Command`. Placing the marker in `Transport.Shared` lets both the command and both transports' decorators reference it without new project references.

## Risk #1 Disposition

Risk #1 from `.shipyard/PROJECT.md` (Polly decorator bypass cleanness, mid) is **downgraded to low — closed by Phase 1 spike**. The mechanism is proven feasible with a single ~3-line decorator branch reusing the same fallthrough pattern as the existing no-pipeline and shutdown-race branches, with no DI registration changes and no per-transport divergence to manage. The PoC test demonstrates the branch behaves correctly in compiled code; Phase 2 ships the production version.

## PoC Reference

Throwaway proof-of-concept test: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Decorator/_SpikePollyBypassPoC.cs`. The PoC defines its own `_SpikeIRetrySkippable` marker, a `_SpikeSendCommand` subclass of `SendMessageCommand`, and a `_SpikePatchedRetryDecorator<TCommand, TOutput>` that mirrors `RetryCommandHandlerOutputDecorator` with the proposed marker branch added. Two test methods cover the positive case (marker present + `SkipRetry = true` → single inner-handler call, no pipeline lookup) and the negative case (`SkipRetry = false` → pipeline lookup happens). Phase 2's first task deletes this file; the production change ships against the real `RetryCommandHandlerOutputDecorator` files.
