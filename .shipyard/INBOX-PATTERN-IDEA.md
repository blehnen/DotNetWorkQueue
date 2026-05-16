# Inbox Pattern — initial idea (2026-05-16)

**GitHub issue:** [#142](https://github.com/blehnen/DotNetWorkQueue/issues/142)

**Status:** Brainstorming. Not a plan or commitment.

## User's framing (verbatim intent)

> It looks like we can leverage the `EnableHoldTransactionUntilMessageCommitted`
> feature for SqlServer / PostgreSQL. We would need to make the connection /
> transaction available to the processor — probably not easy, but it looks
> like it might be possible. Unlike the outbox, we own the connection /
> transaction for the inbox.

## Why this is interesting

The inbox pattern is the dual of the outbox: the **consumer** wants to write
business data and acknowledge the queue message inside a single transaction so
either both commit or both roll back. Today, the user's message handler runs
during message dispatch but the library's read/dequeue transaction is invisible
to it — any business write the handler does opens its own connection / transaction
and is therefore not atomic with the dequeue.

The outbox milestone solved this on the **producer** side by letting the caller
supply a `DbTransaction`. The inbox is the mirror image: the library already owns
a connection + transaction (when `EnableHoldTransactionUntilMessageCommitted` is
true), and what's missing is a seam to **expose** that transaction to the user
handler so its business writes can join it.

## Key contrast with outbox

| Direction | Who owns the connection/transaction | What needs exposing |
|---|---|---|
| Outbox (producer) | Caller | Caller passes `DbTransaction` in; library uses it. |
| Inbox (consumer) | Library | Library passes its `DbTransaction` (or connection) out to the handler. |

Because the library owns the resource on the inbox path, the seam direction
flips — and the lifecycle ownership is the library's, not the caller's. That
means the producer-side outbox contract ("we never commit / rollback / dispose")
does NOT apply here: the library DOES commit / rollback (driven by the
existing rollback-on-handler-throws semantics + `RemoveMessage` commit).

## Likely entry points (from a quick grep — not validated)

- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/ConnectionHolder.cs` and the
  matching SqlServer / SQLite equivalents — these are where the per-message
  connection + transaction live across `Receive` → handler dispatch → `RemoveMessage`.
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/ITransportOptions.cs`
  carries the `EnableHoldTransactionUntilMessageCommitted` option flag.
- `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/RemoveMessage.cs` is the
  commit-side of the held transaction.
- The user's message handler runs in the `Worker` / `MessageProcessing` path;
  the handoff from `ConnectionHolder` into the handler context is the seam to
  find.

## Open questions to answer in brainstorming

1. **Surface shape.** Does the handler receive a `DbTransaction`? A
   `(DbConnection, DbTransaction)` pair? A `IServiceProvider`-style accessor
   that the user resolves only if they need it? Something else entirely
   (e.g., `IReceivedMessage<T>` gains a `DbTransaction` member, populated
   only on the relational-transport `EnableHoldTransactionUntilMessageCommitted` path)?

2. **Capability cast on the receive side.** Outbox uses
   `IRelationalProducerQueue<T>` capability cast for opt-in. Inbox would
   presumably mirror that with `IRelationalConsumer<T>` or a property on
   `IReceivedMessage<T>` that's non-null only when the transport supports it.
   The receive surface is more complex than the producer surface — needs
   careful design.

3. **HoldTransaction precondition.** The feature only works when
   `EnableHoldTransactionUntilMessageCommitted = true`. What happens if the
   user-handler tries to use the inbox seam but the option is off? Throw at
   queue start? Throw at first attempted access? Make the property null?

4. **Cross-DB safety.** Like outbox, the user could conceivably try to use the
   exposed transaction against a different database via a different connection
   string. The outbox solution was `ExternalTransactionValidator`. Inbox doesn't
   have that problem natively (the user uses our connection, not theirs) — but
   if we expose only the transaction, the user could `transaction.Connection`
   and use it for arbitrary writes. That's the intended use; just worth noting
   it isn't fenced like outbox is.

5. **SQLite + LiteDb.** SQLite supports `EnableHoldTransactionUntilMessageCommitted`
   too. LiteDb doesn't have ADO.NET transactions in the same shape. Scope
   decision: SqlServer + PG (matches outbox), or extend to SQLite too?

   **User note (2026-05-16):** "If we extend inbox to SQLite, we should also
   extend outbox — I think that's ok, it's just a lot more 'stuff'." So the
   SQLite scope decision is paired across both patterns, not independent. The
   outbox milestone called SQLite-outbox "out of scope; design extends cleanly
   if requested later" (PROJECT.md §Non-Goals). If the brainstorm lands on
   "yes SQLite for inbox", the same milestone should sweep up SQLite-outbox
   as a co-feature so the relational-transport surface stays symmetric.

6. **Sync vs Async handler paths.** Two sets of receive paths exist (see the
   sync + async smoke tests in the outbox work). The inbox seam needs to work
   on both.

## Things NOT to chase yet

- Do NOT start designing the user-facing API surface before brainstorming. The
  outbox milestone benefited from a deliberate `/shipyard:brainstorm` pass
  (CONTEXT files captured user decisions before plans were drafted) — the inbox
  should get the same treatment.
- Do NOT assume the existing `ConnectionHolder` is the right seam without
  validating; the user said "probably not easy" and meant it — the receive-path
  threading might not allow clean exposure.

## Next step when picking this up

Run `/shipyard:brainstorm` with this note as input. Decisions to drive out
during brainstorming: surface shape (Q1), opt-in pattern (Q2), precondition
behavior (Q3), and transport scope (Q5). Skip planning until those four are
settled.

## Pointer to PR #141

In-flight cleanup PR (cleanup/post-outbox-issues) closes the 8 follow-up
issues from the outbox milestone. Worth letting that ship + landing on master
before this milestone kicks off, since several of those issues (#039, #042)
clarified the public surface of the outbox machinery the inbox will mirror.
