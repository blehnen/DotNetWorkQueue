---
phase: foundation-layer
plan: 2.2
wave: 2
dependencies: [1.1]
must_haves:
  - RelationalSendMessageCommand derived class implementing IRetrySkippable
  - IRelationalProducerQueue<TMessage> interface with 6 tx-aware overloads
  - RelationalProducerQueue<TMessage> concrete with 4 virtual InvalidOperationException stub methods
files_touched:
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs
  - Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs
tdd: false
risk: low
---

# Plan 2.2: Producer Surface (Wave 2)

## Context

Wave 2 (parallel with PLAN-2.1) adds the producer-facing surface to `Transport.RelationalDatabase`. Three new files, no overlap with PLAN-2.1's three files. Both Wave 2 plans depend on PLAN-1.1 having landed (PLAN-1.1 ships `IRetrySkippable` + the `SendMessageCommand.ExternalTransaction` property — both consumed here).

This plan ships:
1. `RelationalSendMessageCommand : SendMessageCommand, IRetrySkippable` — per CONTEXT-2 Decision 2 Option B (recommended by RESEARCH.md §Decision 2 Answer, layering verified clean). Constructor forwards to base; `SkipRetry => ExternalTransaction != null` so the Wave 3 retry decorator branch fires exactly when an external transaction is supplied.
2. `IRelationalProducerQueue<TMessage> : IProducerQueue<TMessage>` — interface with the 6 tx-aware overloads from PROJECT.md §Functional New Public API.
3. `RelationalProducerQueue<TMessage> : ProducerQueue<TMessage>, IRelationalProducerQueue<TMessage>` — concrete that inherits the existing core `ProducerQueue<T>` (RESEARCH.md §Section 3 confirmed `ProducerQueue<T>` is public + non-sealed; constructor takes 6 params). Implements the 6 interface overloads by routing each to one of 4 `protected virtual` hooks (per CONTEXT-2 Decision 3), each of which throws `InvalidOperationException` by default with a "transport not configured" message. Phase 3 SqlServer subclasses this and overrides the 4 virtuals; Phase 4 PostgreSQL mirrors.

## Dependencies

- PLAN-1.1 (Wave 1) — uses `IRetrySkippable` (PLAN-1.1 Task 3) and `SendMessageCommand.ExternalTransaction` (PLAN-1.1 Task 2). Both must exist for this plan to compile.

## Tasks

### Task 1: Create RelationalSendMessageCommand derived class

**Files:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs` (NEW)

**Action:** create

**Description:**
Create the file under `Basic/Command/` (matches existing layout — `Transport.RelationalDatabase` already has `Basic/Command/` for relational-specific command types; see existing `Basic/Command/` folder structure). RESEARCH.md §Section 1 confirmed the base `SendMessageCommand` is public, non-sealed, and the constructor takes `(IMessage, IAdditionalMessageData)`.

```csharp
// (full LGPL header per repo convention)
using System.Data.Common;
using DotNetWorkQueue.Transport.Shared.Basic.Command;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command
{
    /// <summary>
    /// Relational-transport variant of <see cref="SendMessageCommand"/> that carries an
    /// optional caller-supplied <see cref="DbTransaction"/> and signals (via
    /// <see cref="IRetrySkippable"/>) that the retry decorator should bypass its Polly
    /// pipeline on this command. Constructed by <c>RelationalProducerQueue&lt;TMessage&gt;</c>
    /// when one of the tx-aware <c>Send</c>/<c>SendAsync</c> overloads is invoked.
    /// </summary>
    public class RelationalSendMessageCommand : SendMessageCommand, IRetrySkippable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RelationalSendMessageCommand"/>
        /// class.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="messageData">The additional message data.</param>
        /// <param name="externalTransaction">Caller-supplied transaction. May be null,
        /// in which case the command behaves identically to its base class (and
        /// <see cref="SkipRetry"/> evaluates to <c>false</c>).</param>
        public RelationalSendMessageCommand(IMessage messageToSend,
            IAdditionalMessageData messageData,
            DbTransaction externalTransaction)
            : base(messageToSend, messageData)
        {
            ExternalTransaction = externalTransaction;
        }

        // ExternalTransaction is exposed by the base class as an init-only property; we
        // set it via constructor below. Re-declaration is not needed — the base
        // property is inherited.

        /// <summary>
        /// New: signals the retry decorator to bypass its Polly pipeline whenever the
        /// caller supplied a transaction. The caller owns retry semantics on this path.
        /// </summary>
        public bool SkipRetry => ExternalTransaction != null;
    }
}
```

**Architect note:** The base `SendMessageCommand.ExternalTransaction` has an `init` setter (added by PLAN-1.1 Task 2). C# allows setting an `init` property from a derived constructor body OR via an initializer. We pass `externalTransaction` to the derived constructor and assign in the body, which works because the derived class is in the same assembly... wait — the base lives in `Transport.Shared`, not `Transport.RelationalDatabase`. `init` accessibility is at the type level (public init = settable by any caller during object-init expression OR by any subclass constructor). Both rules apply here, so assigning in the derived constructor body is legal across assemblies. **Acceptable.**

**Acceptance Criteria:**
- File exists at the path above with LGPL header.
- Class `public RelationalSendMessageCommand : SendMessageCommand, IRetrySkippable` in namespace `DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command`.
- Constructor signature: `(IMessage, IAdditionalMessageData, DbTransaction)` — forwards first two to base, sets `ExternalTransaction` from third.
- `SkipRetry => ExternalTransaction != null` expression-bodied property.
- `dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release --nologo` succeeds (Release = `TreatWarningsAsErrors` + XML doc).
- Smoke check via grep: `grep -n "SkipRetry => ExternalTransaction != null" Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs` returns 1 match.

### Task 2: Create IRelationalProducerQueue interface

**Files:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` (NEW)

**Action:** create

**Description:**
Create the file at the project root (matches existing layout for top-level interfaces — sibling to `IRetrySkippable.cs` and `IExternalDbNameExtractor.cs`).

Per PROJECT.md §Functional New Public API, six tx-aware overloads. RESEARCH.md §Section 11 confirmed `IAdditionalMessageData` lives in `DotNetWorkQueue` and `QueueMessage<,>` in `DotNetWorkQueue.Messages`; both already reachable via the existing project reference to core.

**Architect note (RESEARCH.md gap flag):** PROJECT.md §Functional spec lists the batch overloads as `IEnumerable<QueueMessage<TMessage, IAdditionalMessageData>>`. The base `IProducerQueue<TMessage>` (RESEARCH.md §Section 3 — read in this build session) uses `List<QueueMessage<TMessage, IAdditionalMessageData>>`. To stay consistent with the existing `IProducerQueue<T>` shape and avoid an `IEnumerable`→`List` boundary inside the relational producer, **this plan uses `List<QueueMessage<TMessage, IAdditionalMessageData>>`** for the batch overloads. Documented for verifier — if downstream phases need `IEnumerable`, lift it then with an explicit deviation note.

```csharp
// (full LGPL header)
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Capability-cast extension of <see cref="IProducerQueue{TMessage}"/> exposing
    /// caller-supplied-transaction <c>Send</c>/<c>SendAsync</c> overloads for the
    /// transactional outbox pattern. Implemented by SqlServer and PostgreSQL transport
    /// producers; Memory, Redis, LiteDb, and SQLite producers do NOT implement this
    /// interface (capability-cast deliberately fails).
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <remarks>
    /// On the tx-aware path the producer never commits, rolls back, or disposes the
    /// caller's transaction or its connection. The retry decorator is bypassed (the
    /// caller owns retry policy). See <c>docs/outbox-pattern.md</c> for the full
    /// lifecycle contract (Phase 7).
    /// </remarks>
    public interface IRelationalProducerQueue<TMessage> : IProducerQueue<TMessage>
        where TMessage : class
    {
        /// <summary>
        /// Sends a single message inside the caller-supplied transaction.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="transaction">Caller-supplied transaction. The producer enlists
        /// its INSERTs on <c>transaction.Connection</c> and never commits/rolls back/disposes
        /// the caller's resources.</param>
        IQueueOutputMessage Send(TMessage message, DbTransaction transaction);

        /// <summary>
        /// Sends a single message with additional metadata inside the caller-supplied
        /// transaction.
        /// </summary>
        IQueueOutputMessage Send(TMessage message, IAdditionalMessageData data, DbTransaction transaction);

        /// <summary>
        /// Async variant of <see cref="Send(TMessage, DbTransaction)"/>.
        /// </summary>
        Task<IQueueOutputMessage> SendAsync(TMessage message, DbTransaction transaction);

        /// <summary>
        /// Async variant of <see cref="Send(TMessage, IAdditionalMessageData, DbTransaction)"/>.
        /// </summary>
        Task<IQueueOutputMessage> SendAsync(TMessage message, IAdditionalMessageData data, DbTransaction transaction);

        /// <summary>
        /// Sends a batch of messages inside the caller-supplied transaction.
        /// </summary>
        /// <remarks>
        /// Batch type is <see cref="List{T}"/> to match the existing
        /// <see cref="IProducerQueue{TMessage}"/> shape; PROJECT.md spec used
        /// <c>IEnumerable</c>, deviation flagged for verifier in PLAN-2.2.
        /// </remarks>
        IQueueOutputMessages Send(List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction);

        /// <summary>
        /// Async batch send inside the caller-supplied transaction.
        /// </summary>
        Task<IQueueOutputMessages> SendAsync(List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction);
    }
}
```

**Acceptance Criteria:**
- File exists at the path above with LGPL header.
- Interface `public interface IRelationalProducerQueue<TMessage> : IProducerQueue<TMessage>` with `where TMessage : class`.
- Exactly 6 method overloads matching the signatures above.
- Each method has an XML `<summary>` doc.
- `dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release --nologo` succeeds.
- Method-count check: `grep -c "DbTransaction transaction" Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs` returns 6.

### Task 3: Create RelationalProducerQueue concrete class

**Files:**
- `Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs` (NEW)

**Action:** create

**Description:**
Create the file in `Basic/`. RESEARCH.md §Section 3 confirmed `ProducerQueue<T>` (in `DotNetWorkQueue.Queue`) is public + non-sealed, constructor takes 6 params: `(QueueProducerConfiguration, ISendMessages, IMessageFactory, ILogger, GenerateMessageHeaders, AddStandardMessageHeaders)`.

The concrete class inherits `ProducerQueue<T>` to satisfy `IProducerQueue<T>` registration in SimpleInjector. It implements `IRelationalProducerQueue<T>` by overloading the 6 tx-aware methods, each routing to one of 4 `protected virtual` hooks per CONTEXT-2 Decision 3:

- `SendWithExternalTransaction(TMessage, IAdditionalMessageData?, DbTransaction)`
- `SendWithExternalTransactionAsync(TMessage, IAdditionalMessageData?, DbTransaction)`
- `SendWithExternalTransactionBatch(List<QueueMessage<TMessage, IAdditionalMessageData>>, DbTransaction)`
- `SendWithExternalTransactionBatchAsync(List<QueueMessage<TMessage, IAdditionalMessageData>>, DbTransaction)`

Each virtual throws `InvalidOperationException` with the message "Caller-supplied-transaction send is not implemented for this transport. Override SendWithExternalTransaction[...] or resolve a SqlServer/PostgreSQL producer." per CONTEXT-2 Decision 3.

The two single-message public overloads (with and without `IAdditionalMessageData`) both route to `SendWithExternalTransaction`, normalizing a null `data` parameter to `null` (the override decides whether to substitute `new AdditionalMessageData()`); same for async. The two batch public overloads route to `SendWithExternalTransactionBatch[Async]` verbatim.

```csharp
// (full LGPL header)
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Relational-transport variant of <see cref="ProducerQueue{T}"/> exposing
    /// caller-supplied-transaction <c>Send</c>/<c>SendAsync</c> overloads. The four
    /// tx-aware virtual methods throw <see cref="InvalidOperationException"/> by default;
    /// SqlServer and PostgreSQL producers (Phases 3 and 4) override them to invoke the
    /// per-transport <c>SendMessageCommandHandler</c> directly with the caller's
    /// transaction in scope.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    public class RelationalProducerQueue<T> : ProducerQueue<T>, IRelationalProducerQueue<T>
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RelationalProducerQueue{T}"/>
        /// class. Constructor parameters mirror the base
        /// <see cref="ProducerQueue{T}"/>; SimpleInjector resolves them per the existing
        /// transport DI conventions.
        /// </summary>
        public RelationalProducerQueue(
            QueueProducerConfiguration configuration,
            ISendMessages sendMessages,
            IMessageFactory messageFactory,
            ILogger log,
            GenerateMessageHeaders generateMessageHeaders,
            AddStandardMessageHeaders addStandardMessageHeaders)
            : base(configuration, sendMessages, messageFactory, log, generateMessageHeaders, addStandardMessageHeaders)
        {
        }

        // --- IRelationalProducerQueue<T> public overloads ---

        /// <inheritdoc />
        public IQueueOutputMessage Send(T message, DbTransaction transaction)
            => SendWithExternalTransaction(message, null, transaction);

        /// <inheritdoc />
        public IQueueOutputMessage Send(T message, IAdditionalMessageData data, DbTransaction transaction)
            => SendWithExternalTransaction(message, data, transaction);

        /// <inheritdoc />
        public Task<IQueueOutputMessage> SendAsync(T message, DbTransaction transaction)
            => SendWithExternalTransactionAsync(message, null, transaction);

        /// <inheritdoc />
        public Task<IQueueOutputMessage> SendAsync(T message, IAdditionalMessageData data, DbTransaction transaction)
            => SendWithExternalTransactionAsync(message, data, transaction);

        /// <inheritdoc />
        public IQueueOutputMessages Send(List<QueueMessage<T, IAdditionalMessageData>> messages, DbTransaction transaction)
            => SendWithExternalTransactionBatch(messages, transaction);

        /// <inheritdoc />
        public Task<IQueueOutputMessages> SendAsync(List<QueueMessage<T, IAdditionalMessageData>> messages, DbTransaction transaction)
            => SendWithExternalTransactionBatchAsync(messages, transaction);

        // --- 4 protected virtual hooks (Phase 3/4 override these) ---

        /// <summary>
        /// Synchronous single-message caller-tx send. Phase 3 (SqlServer) and Phase 4
        /// (PostgreSQL) override this to enlist the queue INSERTs on the caller's
        /// transaction. Default implementation throws.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">Optional additional message data; <c>null</c> when invoked
        /// from the no-data overload.</param>
        /// <param name="transaction">Caller-supplied transaction.</param>
        /// <exception cref="InvalidOperationException">Thrown by the base implementation
        /// when no transport-specific override is registered.</exception>
        protected virtual IQueueOutputMessage SendWithExternalTransaction(T message,
            IAdditionalMessageData data, DbTransaction transaction)
        {
            throw new InvalidOperationException(NotConfiguredMessage());
        }

        /// <summary>Async equivalent of <see cref="SendWithExternalTransaction"/>.</summary>
        protected virtual Task<IQueueOutputMessage> SendWithExternalTransactionAsync(T message,
            IAdditionalMessageData data, DbTransaction transaction)
        {
            throw new InvalidOperationException(NotConfiguredMessage());
        }

        /// <summary>Synchronous batch caller-tx send.</summary>
        protected virtual IQueueOutputMessages SendWithExternalTransactionBatch(
            List<QueueMessage<T, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            throw new InvalidOperationException(NotConfiguredMessage());
        }

        /// <summary>Async batch caller-tx send.</summary>
        protected virtual Task<IQueueOutputMessages> SendWithExternalTransactionBatchAsync(
            List<QueueMessage<T, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            throw new InvalidOperationException(NotConfiguredMessage());
        }

        private static string NotConfiguredMessage()
            => "Caller-supplied-transaction send is not implemented for this transport. " +
               "Override SendWithExternalTransaction (and the batch + async variants) " +
               "in a transport-specific subclass, or resolve a SqlServer/PostgreSQL " +
               "producer that already does.";
    }
}
```

The class is `public` (so SqlServer/PostgreSQL can subclass it). The 4 hooks are `protected virtual`. The 6 interface methods are `public` (required by interface implementation; `IRelationalProducerQueue<T>` re-exposes these). No `Send` overloads from base `ProducerQueue<T>` are overridden — the existing non-tx path is unchanged.

**Acceptance Criteria:**
- File exists at the path above with LGPL header.
- Class `public class RelationalProducerQueue<T> : ProducerQueue<T>, IRelationalProducerQueue<T> where T : class`.
- Constructor has exactly 6 parameters matching base `ProducerQueue<T>`.
- Exactly 6 `public` interface implementation methods (no extra, no missing — verify by `grep -c "public.*Send.*DbTransaction transaction"` returns 6).
- Exactly 4 `protected virtual` `SendWithExternalTransaction*` methods, each throwing `InvalidOperationException` with the documented message.
- `dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release --nologo` succeeds with 0 warnings.
- Layering grep returns empty: `grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs` → no output.

## Verification

```bash
# Files exist
test -f Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/Command/RelationalSendMessageCommand.cs
test -f Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs
test -f Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs

# Release build (TreatWarningsAsErrors + XML doc gen)
dotnet build "Source/DotNetWorkQueue.Transport.RelationalDatabase/DotNetWorkQueue.Transport.RelationalDatabase.csproj" -c Release --nologo
# expected: 0 Error(s), 0 Warning(s)

# IRelationalProducerQueue surface check: 6 overloads with DbTransaction
grep -c "DbTransaction transaction" Source/DotNetWorkQueue.Transport.RelationalDatabase/IRelationalProducerQueue.cs
# expected: 6

# RelationalProducerQueue protected virtual hook count
grep -c "protected virtual" Source/DotNetWorkQueue.Transport.RelationalDatabase/Basic/RelationalProducerQueue.cs
# expected: 4

# Layering invariant (CONTEXT-2 Hard Rules)
grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/ --include="*.cs" --include="*.csproj"
# expected: no matches (exit code 1)

# Existing test suite (no regressions)
dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug --nologo
# expected: Failed: 0
```
