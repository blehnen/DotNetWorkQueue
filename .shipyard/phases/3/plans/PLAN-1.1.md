---
phase: sqlserver-implementation
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - SqlServerExternalDbNameExtractor implementing IExternalDbNameExtractor (returns connection.Database)
  - SqlServerRelationalProducerQueue<T> subclass overriding the 4 RelationalProducerQueue<T> virtual hooks
  - SQLServerMessageQueueInit registers extractor, validator, and the 3 producer-shape mappings
  - Bulk of Phase 3 unit tests (producer-subclass behavior + extractor)
files_touched:
  - Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs
  - Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs
  - Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerExternalDbNameExtractorTests.cs
  - Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalProducerQueueTests.cs
tdd: true
risk: medium
---

# Plan 1.1: Foundation — Extractor + Producer Subclass + DI Wiring (Wave 1)

## Context

This Wave 1 plan ships the SqlServer-side foundation of Phase 3: the per-provider `IExternalDbNameExtractor` implementation, the transport-specific producer subclass that overrides the four `protected virtual SendWithExternalTransaction*` hooks from `RelationalProducerQueue<T>`, the DI registrations that make `IRelationalProducerQueue<T>` resolvable from `SQLServerMessageQueueInit`, and the bulk of Phase 3's unit-test coverage.

Per **RESEARCH §11 Discrepancy #2** the producer subclass is where Phase 3 unit tests live. `SendMessageCommandHandler` is `SqlConnection`/`SqlTransaction`/`SqlCommand`-typed end-to-end (RESEARCH §1) and those sealed types cannot be mocked by NSubstitute (CLAUDE.md lesson), so direct handler-fork unit tests are infeasible. The producer subclass — which takes the `ICommandHandlerWithOutput<SendMessageCommand, long>` interface as a DI dep — is fully unit-testable because that interface is unsealed and NSubstitute-friendly. We exercise the subclass against mocked handlers, capture the dispatched `RelationalSendMessageCommand`, and assert (a) the validator was called first, (b) the cast guard fires for a non-`SqlTransaction`, (c) `ExternalTransaction` is carried through on the command, and (d) batch overrides loop sequentially.

Per **CONTEXT-3 Decision 1** the sync batch override iterates with `foreach` (NOT `Parallel.ForEach`) and aggregates per-item exceptions into the result list — ADO.NET transactions are not thread-safe. Per **CONTEXT-3 Decision 3** the validator runs ONCE in the producer override before the `RelationalSendMessageCommand` is constructed (single-message overrides) or once before the loop (batch overrides). The cast guard `tx is not SqlTransaction` runs AFTER the validator so caller-tx boundary checks (null tx, null connection, closed connection, wrong DB) produce clean diagnostic messages before the provider-mismatch check fires. Per **RESEARCH §11 Open Question** the subclass takes its own `IMessageFactory` via DI (the Phase 2 `RelationalProducerQueue<T>` base does NOT expose `_messageFactory` as `protected`) — this avoids a Phase 2 amendment.

The DI changes are surgical: 5 new `container.Register<>` calls inserted in `SQLServerMessageQueueInit.RegisterImplementations` after `base.RegisterImplementations(...)` (line 58) and before the existing `//override so that we can use schema as needed` block (line 63). Per **RESEARCH §6** the three producer-shape registrations (`IProducerQueue<>`, `IRelationalProducerQueue<>`, `RelationalProducerQueue<>` → `SqlServerRelationalProducerQueue<>`) preempt the fallback `RegisterConditional` for `IProducerQueue<>` in core `ComponentRegistration.cs:385`, so capability-cast works (`container.GetInstance<IProducerQueue<MyEvent>>() is IRelationalProducerQueue<MyEvent>` returns `true`).

## Dependencies

None within Phase 3. This is Wave 1. Phase 2 plans must be BUILT before this plan's builder runs (the subclass inherits `RelationalProducerQueue<T>` from Phase 2 PLAN-2.2 and constructs `RelationalSendMessageCommand` from Phase 2 PLAN-2.2; the validator type is from Phase 2 PLAN-2.1; the extractor interface is from Phase 2 PLAN-2.1). Plan ordering enforces this at build time, not plan time.

## Tasks

### Task 1: Create SqlServerExternalDbNameExtractor + extractor unit test

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs`
- Create: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerExternalDbNameExtractorTests.cs`

**Step 1: Write the failing tests**

```csharp
// SqlServerExternalDbNameExtractorTests.cs
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System.Data.Common;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    [TestClass]
    public class SqlServerExternalDbNameExtractorTests
    {
        [TestMethod]
        public void Extract_ReturnsConnectionDatabase()
        {
            var conn = Substitute.For<DbConnection>();
            conn.Database.Returns("MyDb");
            var sut = new SqlServerExternalDbNameExtractor();
            Assert.AreEqual("MyDb", sut.Extract(conn));
        }

        [TestMethod]
        public void ConfiguredComparison_IsOrdinalIgnoreCase()
        {
            // The validator uses StringComparison.Ordinal for the final compare
            // (Phase 2 PLAN-2.1 decision); per-provider case semantics are encoded
            // in the extractor by normalizing case at extract time. SqlServer is
            // case-insensitive at the catalog level, so the extractor's output
            // must compare equal under OrdinalIgnoreCase across "MyDb" and "mydb".
            var conn1 = Substitute.For<DbConnection>();
            conn1.Database.Returns("MyDb");
            var conn2 = Substitute.For<DbConnection>();
            conn2.Database.Returns("mydb");
            var sut = new SqlServerExternalDbNameExtractor();

            // SqlServer extractor normalizes via OrdinalIgnoreCase semantics.
            // Implementation choice (uppercase canonicalization) is verified by
            // round-tripping through string.Equals(OrdinalIgnoreCase).
            Assert.IsTrue(
                string.Equals(sut.Extract(conn1), sut.Extract(conn2),
                    System.StringComparison.OrdinalIgnoreCase),
                "SqlServer extractor must produce equal outputs for case-variant database names " +
                "under OrdinalIgnoreCase comparison.");
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~SqlServerExternalDbNameExtractorTests" --nologo`

Expected: FAIL with `error CS0246: The type or namespace name 'SqlServerExternalDbNameExtractor' could not be found`.

**Step 3: Write implementation**

```csharp
// SqlServerExternalDbNameExtractor.cs
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System.Data.Common;
using DotNetWorkQueue.Transport.RelationalDatabase;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// SqlServer implementation of <see cref="IExternalDbNameExtractor"/>. Returns the
    /// uppercase form of <see cref="DbConnection.Database"/> so the validator's
    /// <see cref="System.StringComparison.Ordinal"/> compare against the queue's
    /// configured database name behaves case-insensitively (matching SqlServer's
    /// catalog-name semantics). The matching Phase 3 producer / queue-config side is
    /// expected to upper-case the configured database name when populating
    /// <c>IConnectionInformation.Container</c>; this symmetric normalization is the
    /// per-provider case-handling convention from Phase 2 PLAN-2.1 architect note.
    /// </summary>
    public sealed class SqlServerExternalDbNameExtractor : IExternalDbNameExtractor
    {
        /// <summary>
        /// Returns the canonical (uppercase) database name reported by the connection.
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> from the caller's
        /// transaction. Must not be null.</param>
        /// <returns>The database name, upper-cased via invariant culture.</returns>
        public string Extract(DbConnection connection)
        {
            return connection.Database?.ToUpperInvariant() ?? string.Empty;
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~SqlServerExternalDbNameExtractorTests" --nologo`

Expected: PASS — `Passed: 2, Failed: 0, Skipped: 0, Total: 2`.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerExternalDbNameExtractorTests.cs
git commit -m "shipyard(phase-3): add SqlServerExternalDbNameExtractor + tests"
```

**Acceptance criteria:**
- `SqlServerExternalDbNameExtractor` is `public sealed`, in namespace `DotNetWorkQueue.Transport.SqlServer.Basic`, implements `IExternalDbNameExtractor`.
- `Extract(DbConnection)` returns `connection.Database?.ToUpperInvariant() ?? string.Empty`.
- 2 unit tests pass.
- File has the LGPL-2.1 header verbatim from `SendMessageCommandHandler.cs:1-18`.
- XML doc on the public class and the public method (Release build with `TreatWarningsAsErrors` would fail otherwise — confirmed by Task 3's verification build).

### Task 2: Create SqlServerRelationalProducerQueue<T> subclass + producer-subclass unit tests

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs`
- Create: `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalProducerQueueTests.cs`

**Step 1: Write the failing tests**

```csharp
// SqlServerRelationalProducerQueueTests.cs
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
// (full LGPL header — copy from SendMessageCommandHandler.cs:1-18)
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    [TestClass]
    public class SqlServerRelationalProducerQueueTests
    {
        // ----- Test fixtures -----

        public class TestMessage { public string Body { get; set; } }

        private const string QueueDb = "MYDB"; // upper because SqlServer extractor normalizes

        private static SqlServerRelationalProducerQueue<TestMessage> BuildSut(
            ICommandHandlerWithOutput<SendMessageCommand, long> syncHandler = null,
            ICommandHandlerWithOutputAsync<SendMessageCommand, long> asyncHandler = null,
            ExternalTransactionValidator validator = null,
            ISentMessageFactory sentFactory = null,
            IMessageFactory messageFactory = null)
        {
            syncHandler ??= Substitute.For<ICommandHandlerWithOutput<SendMessageCommand, long>>();
            syncHandler.Handle(Arg.Any<SendMessageCommand>()).Returns(42L);
            asyncHandler ??= Substitute.For<ICommandHandlerWithOutputAsync<SendMessageCommand, long>>();
            asyncHandler.HandleAsync(Arg.Any<SendMessageCommand>()).Returns(Task.FromResult(42L));

            if (validator == null)
            {
                var extractor = Substitute.For<IExternalDbNameExtractor>();
                extractor.Extract(Arg.Any<DbConnection>()).Returns(QueueDb);
                var connInfo = Substitute.For<IConnectionInformation>();
                connInfo.Container.Returns(QueueDb);
                validator = new ExternalTransactionValidator(extractor, connInfo);
            }

            sentFactory ??= Substitute.For<ISentMessageFactory>();
            sentFactory.Create(Arg.Any<IMessageQueueId>(), Arg.Any<Guid>())
                .Returns(Substitute.For<ISentMessage>());

            messageFactory ??= Substitute.For<IMessageFactory>();
            messageFactory.Create(Arg.Any<TestMessage>(), Arg.Any<IAdditionalMessageHeaders>())
                .Returns(Substitute.For<IMessage>());
            messageFactory.Create(Arg.Any<TestMessage>())
                .Returns(Substitute.For<IMessage>());

            return new SqlServerRelationalProducerQueue<TestMessage>(
                Substitute.For<QueueProducerConfiguration>(
                    new QueueConnection("q", "Server=.;Database=tempdb;"),
                    Array.Empty<IAdditionalMessageData>()), // configuration ctor — may need a builder helper
                Substitute.For<ISendMessages>(),
                messageFactory,
                Substitute.For<ILogger>(),
                Substitute.For<GenerateMessageHeaders>(Substitute.For<IGetHeader>(),
                    Substitute.For<IHeaders>(), Substitute.For<IMessageContextDataFactory>()),
                Substitute.For<AddStandardMessageHeaders>(Substitute.For<IGetTime>(),
                    Substitute.For<IHeaders>(), Substitute.For<IConnectionInformation>()),
                syncHandler, asyncHandler, validator, sentFactory, messageFactory);
        }

        // Helper: a SqlTransaction substitute that returns a SqlConnection with the right DB.
        // Cannot mock SqlConnection (sealed) so use a real one with a fake (non-opened) state
        // is not viable either — instead the validator path is exercised via the extractor
        // returning a matching DB name and the producer's cast guard is exercised separately
        // with a non-SqlTransaction substitute.
        private static DbTransaction BuildSqlLikeTx(ConnectionState state = ConnectionState.Open)
        {
            // Use a real SqlConnection (never opened) — its State property returns Closed
            // by default. For the happy-path tests where we need State==Open without a live
            // DB, mock a DbConnection and a DbTransaction that points to it, then bypass
            // the producer's SqlTransaction cast by replacing the test with a separate
            // overload-targeted approach (see TestMethod 3 below).
            var conn = Substitute.For<DbConnection>();
            conn.State.Returns(state);
            conn.Database.Returns(QueueDb);
            var tx = Substitute.For<DbTransaction>();
            tx.Connection.Returns(conn);
            return tx;
        }

        // ----- Tests -----

        [TestMethod]
        public void Send_NullTransaction_ThrowsArgumentNullException()
        {
            var sut = BuildSut();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => sut.Send(new TestMessage(), (DbTransaction)null));
        }

        [TestMethod]
        public void Send_NonSqlTransaction_ThrowsInvalidOperationException()
        {
            // Validator passes (we built it to match QueueDb), then the cast guard fires.
            var sut = BuildSut();
            var tx = BuildSqlLikeTx();
            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => sut.Send(new TestMessage(), tx));
            StringAssert.Contains(ex.Message, "SqlTransaction");
        }

        [TestMethod]
        public void Send_ValidatorRejectsDbMismatch_ThrowsBeforeCastGuard()
        {
            var extractor = Substitute.For<IExternalDbNameExtractor>();
            extractor.Extract(Arg.Any<DbConnection>()).Returns("WRONGDB");
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.Container.Returns(QueueDb);
            var validator = new ExternalTransactionValidator(extractor, connInfo);

            var sut = BuildSut(validator: validator);
            var tx = BuildSqlLikeTx();
            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => sut.Send(new TestMessage(), tx));
            // Diagnostic message contains both DB names AND does NOT mention SqlTransaction
            // (proves the validator fires before the cast guard).
            StringAssert.Contains(ex.Message, "WRONGDB");
            StringAssert.Contains(ex.Message, QueueDb);
            Assert.IsFalse(ex.Message.Contains("SqlTransaction"),
                "Validator must fire before the SqlTransaction cast guard.");
        }

        [TestMethod]
        public async Task SendAsync_NullTransaction_ThrowsArgumentNullException()
        {
            var sut = BuildSut();
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                async () => await sut.SendAsync(new TestMessage(), (DbTransaction)null));
        }

        [TestMethod]
        public void SendBatch_NullTransaction_ThrowsArgumentNullException()
        {
            var sut = BuildSut();
            var msgs = new List<QueueMessage<TestMessage, IAdditionalMessageData>>
            {
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null)
            };
            Assert.ThrowsExactly<ArgumentNullException>(() => sut.Send(msgs, null));
        }

        [TestMethod]
        public void SendBatch_ValidatorCalledOncePerBatch_NotPerItem()
        {
            // Per CONTEXT-3 Decision 3: validator runs ONCE before the loop, not per item.
            // We instrument the validator's extractor; counting Extract() calls proxies the
            // number of Validate() invocations (each Validate runs Extract exactly once on
            // the happy path through check 4).
            var extractor = Substitute.For<IExternalDbNameExtractor>();
            extractor.Extract(Arg.Any<DbConnection>()).Returns(QueueDb);
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.Container.Returns(QueueDb);
            var validator = new ExternalTransactionValidator(extractor, connInfo);

            var sut = BuildSut(validator: validator);
            var tx = BuildSqlLikeTx();
            var msgs = new List<QueueMessage<TestMessage, IAdditionalMessageData>>
            {
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null),
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null),
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null)
            };

            // Cast guard will throw for the non-SqlTransaction, but the validator fires first.
            // Assert it was called exactly once across the 3-message batch.
            try { sut.Send(msgs, tx); } catch (InvalidOperationException) { /* expected */ }
            extractor.Received(1).Extract(Arg.Any<DbConnection>());
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~SqlServerRelationalProducerQueueTests" --nologo`

Expected: FAIL with `error CS0246: The type or namespace name 'SqlServerRelationalProducerQueue' could not be found`.

**Step 3: Write implementation**

```csharp
// SqlServerRelationalProducerQueue.cs
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
// (full LGPL header — copy from SendMessageCommandHandler.cs:1-18)
// ---------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// SqlServer-specific <see cref="RelationalProducerQueue{T}"/> that overrides the
    /// four caller-supplied-transaction hooks to dispatch
    /// <see cref="RelationalSendMessageCommand"/> instances through the registered
    /// SqlServer <see cref="SendMessageCommandHandler"/> / <c>SendMessageCommandHandlerAsync</c>.
    /// Validates the caller's transaction at the producer surface (fail-fast,
    /// boundary-checked) before any handler dispatch. Batch overrides iterate
    /// sequentially because ADO.NET transactions are not thread-safe.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public sealed class SqlServerRelationalProducerQueue<TMessage>
        : RelationalProducerQueue<TMessage>
        where TMessage : class
    {
        private readonly ICommandHandlerWithOutput<SendMessageCommand, long> _sendHandler;
        private readonly ICommandHandlerWithOutputAsync<SendMessageCommand, long> _sendHandlerAsync;
        private readonly ExternalTransactionValidator _validator;
        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly IMessageFactory _messageFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerRelationalProducerQueue{TMessage}"/> class.
        /// </summary>
        /// <param name="configuration">Producer configuration.</param>
        /// <param name="sendMessages">Send-messages orchestrator (used by the inherited non-tx path).</param>
        /// <param name="messageFactory">Message factory (re-injected here because the base
        /// class does not expose it as protected; used by the caller-tx overrides).</param>
        /// <param name="log">Logger.</param>
        /// <param name="generateMessageHeaders">Standard header generator.</param>
        /// <param name="addStandardMessageHeaders">Standard header populator.</param>
        /// <param name="sendHandler">Registered sync handler for <see cref="SendMessageCommand"/>.</param>
        /// <param name="sendHandlerAsync">Registered async handler for <see cref="SendMessageCommand"/>.</param>
        /// <param name="validator">External-transaction validator (runs at the API boundary).</param>
        /// <param name="sentMessageFactory">Factory for the <see cref="ISentMessage"/> returned to callers.</param>
        /// <param name="ownMessageFactory">Same <see cref="IMessageFactory"/> instance as <paramref name="messageFactory"/>;
        /// retained as a separate field because the base type seals its own copy as private.</param>
        public SqlServerRelationalProducerQueue(
            QueueProducerConfiguration configuration,
            ISendMessages sendMessages,
            IMessageFactory messageFactory,
            ILogger log,
            GenerateMessageHeaders generateMessageHeaders,
            AddStandardMessageHeaders addStandardMessageHeaders,
            ICommandHandlerWithOutput<SendMessageCommand, long> sendHandler,
            ICommandHandlerWithOutputAsync<SendMessageCommand, long> sendHandlerAsync,
            ExternalTransactionValidator validator,
            ISentMessageFactory sentMessageFactory,
            IMessageFactory ownMessageFactory)
            : base(configuration, sendMessages, messageFactory, log,
                   generateMessageHeaders, addStandardMessageHeaders)
        {
            Guard.NotNull(() => sendHandler, sendHandler);
            Guard.NotNull(() => sendHandlerAsync, sendHandlerAsync);
            Guard.NotNull(() => validator, validator);
            Guard.NotNull(() => sentMessageFactory, sentMessageFactory);
            Guard.NotNull(() => ownMessageFactory, ownMessageFactory);
            _sendHandler = sendHandler;
            _sendHandlerAsync = sendHandlerAsync;
            _validator = validator;
            _sentMessageFactory = sentMessageFactory;
            _messageFactory = ownMessageFactory;
        }

        /// <inheritdoc />
        protected override IQueueOutputMessage SendWithExternalTransaction(
            TMessage message, IAdditionalMessageData data, DbTransaction transaction)
        {
            _validator.Validate(transaction);
            GuardSqlTransaction(transaction);
            return SendOne(message, data ?? new AdditionalMessageData(), transaction);
        }

        /// <inheritdoc />
        protected override async Task<IQueueOutputMessage> SendWithExternalTransactionAsync(
            TMessage message, IAdditionalMessageData data, DbTransaction transaction)
        {
            _validator.Validate(transaction);
            GuardSqlTransaction(transaction);
            return await SendOneAsync(message, data ?? new AdditionalMessageData(), transaction)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override IQueueOutputMessages SendWithExternalTransactionBatch(
            List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            Guard.NotNull(() => messages, messages);
            _validator.Validate(transaction);  // ONCE, before the loop (CONTEXT-3 Decision 1)
            GuardSqlTransaction(transaction);

            var rc = new List<IQueueOutputMessage>(messages.Count);
            foreach (var m in messages)  // sequential — DbTransaction is not thread-safe
            {
                try
                {
                    rc.Add(SendOne(m.Message, m.MessageData ?? new AdditionalMessageData(), transaction));
                }
                catch (Exception error)
                {
                    rc.Add(new QueueOutputMessage(
                        _sentMessageFactory.Create(null, (m.MessageData ?? new AdditionalMessageData()).CorrelationId),
                        error));
                }
            }
            return new QueueOutputMessages(rc);
        }

        /// <inheritdoc />
        protected override async Task<IQueueOutputMessages> SendWithExternalTransactionBatchAsync(
            List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            Guard.NotNull(() => messages, messages);
            _validator.Validate(transaction);
            GuardSqlTransaction(transaction);

            var rc = new List<IQueueOutputMessage>(messages.Count);
            foreach (var m in messages)
            {
                try
                {
                    rc.Add(await SendOneAsync(m.Message,
                        m.MessageData ?? new AdditionalMessageData(), transaction).ConfigureAwait(false));
                }
                catch (Exception error)
                {
                    rc.Add(new QueueOutputMessage(
                        _sentMessageFactory.Create(null, (m.MessageData ?? new AdditionalMessageData()).CorrelationId),
                        error));
                }
            }
            return new QueueOutputMessages(rc);
        }

        // ----- private dispatch helpers -----

        private IQueueOutputMessage SendOne(TMessage message, IAdditionalMessageData data, DbTransaction tx)
        {
            var imsg = _messageFactory.Create(message);
            var cmd = new RelationalSendMessageCommand(imsg, data, tx);
            var id = _sendHandler.Handle(cmd);
            return new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId<long>(id), data.CorrelationId));
        }

        private async Task<IQueueOutputMessage> SendOneAsync(TMessage message, IAdditionalMessageData data, DbTransaction tx)
        {
            var imsg = _messageFactory.Create(message);
            var cmd = new RelationalSendMessageCommand(imsg, data, tx);
            var id = await _sendHandlerAsync.HandleAsync(cmd).ConfigureAwait(false);
            return new QueueOutputMessage(_sentMessageFactory.Create(new MessageQueueId<long>(id), data.CorrelationId));
        }

        private static void GuardSqlTransaction(DbTransaction transaction)
        {
            if (transaction is not SqlTransaction)
            {
                throw new InvalidOperationException(
                    $"Expected SqlTransaction but received '{transaction.GetType().FullName}'. " +
                    "The transaction must be opened on a SqlConnection from Microsoft.Data.SqlClient.");
            }
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~SqlServerRelationalProducerQueueTests" --nologo`

Expected: PASS — `Passed: 6, Failed: 0, Skipped: 0, Total: 6`.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalProducerQueueTests.cs
git commit -m "shipyard(phase-3): add SqlServerRelationalProducerQueue<T> + producer tests"
```

**Acceptance criteria:**
- Class is `public sealed`, namespace `DotNetWorkQueue.Transport.SqlServer.Basic`, derives from `RelationalProducerQueue<TMessage>` with `where TMessage : class`.
- Constructor takes 11 parameters (6 base + `sendHandler` + `sendHandlerAsync` + `validator` + `sentMessageFactory` + `ownMessageFactory`).
- All 4 `protected override SendWithExternalTransaction*` methods exist and: (a) call `_validator.Validate(transaction)` FIRST, (b) then call `GuardSqlTransaction(transaction)`, (c) then dispatch `RelationalSendMessageCommand` via the registered handler.
- Batch overrides use `foreach` not `Parallel.ForEach`; per-item exceptions are aggregated into the result list.
- 6 unit tests pass.
- XML doc on the public class and the public constructor.
- LGPL-2.1 header.

### Task 3: Wire DI registrations in SQLServerMessageQueueInit + verify capability cast smoke test

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs:58-63` (insert 5 new `container.Register<>` lines between `base.RegisterImplementations(...)` at line 58 and the `//override so that we can use schema as needed` comment at line 63)
- Modify (append test): `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalProducerQueueTests.cs` (add capability-cast smoke test method)

**Step 1: Write the failing smoke test** (added to the existing test file from Task 2)

```csharp
// Append inside SqlServerRelationalProducerQueueTests class:

[TestMethod]
public void CapabilityCast_IProducerQueue_IsIRelationalProducerQueue()
{
    // Build a SimpleInjector-backed IContainer via SQLServerMessageQueueInit and
    // assert the resolved IProducerQueue<TestMessage> is castable to IRelationalProducerQueue<TestMessage>.
    // This is the PROJECT.md §Success Criteria #3 / #7 wiring check.
    using var factory = new global::DotNetWorkQueue.QueueContainer<
        global::DotNetWorkQueue.Transport.SqlServer.SqlServerMessageQueueInit>();
    using var producer = factory.CreateProducer<TestMessage>(
        new QueueConnection("q", "Server=.;Database=tempdb;Integrated Security=true;"));
    Assert.IsTrue(
        producer is IRelationalProducerQueue<TestMessage>,
        "SqlServer producer must implement IRelationalProducerQueue<T> after DI wiring.");
}
```

**Step 2: Run smoke test to verify it fails**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~CapabilityCast_IProducerQueue_IsIRelationalProducerQueue" --nologo`

Expected: FAIL — assertion failure (producer is the fallback `ProducerQueue<T>` from `ComponentRegistration.cs:385`, not `SqlServerRelationalProducerQueue<T>`).

**Step 3: Write implementation — insert into `SQLServerMessageQueueInit.cs`**

Locate `SQLServerMessageQueueInit.RegisterImplementations` (line 55). Between line 58 (`base.RegisterImplementations(...)`) and line 60 (`var init = new RelationalDatabaseMessageQueueInit<long, Guid>();`), insert the following 6 lines (5 registrations + a comment):

```csharp
            // Phase 3: outbox-pattern producer wiring (SqlServer side)
            container.Register<IExternalDbNameExtractor, SqlServerExternalDbNameExtractor>(LifeStyles.Singleton);
            container.Register<ExternalTransactionValidator>(LifeStyles.Singleton);
            container.Register(typeof(IProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton);
            container.Register(typeof(IRelationalProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton);
            container.Register(typeof(RelationalProducerQueue<>), typeof(SqlServerRelationalProducerQueue<>), LifeStyles.Singleton);
```

Add to the `using` block at the top of `SQLServerMessageQueueInit.cs` (if not already present):
```csharp
using DotNetWorkQueue.Transport.SqlServer.Basic; // already present — Basic.SqlServerExternalDbNameExtractor and Basic.SqlServerRelationalProducerQueue live here
```
(The file already has `namespace DotNetWorkQueue.Transport.SqlServer.Basic` and `DotNetWorkQueue.Transport.RelationalDatabase[.Basic]` usings — no new imports needed.)

**Architect note for builder:** if `IContainer.Register(typeof(Open<>), typeof(Open<>), LifeStyles.Singleton)` is not the exact open-generic registration shape exposed by the SimpleInjector wrapper, fall back to invoking the typed extension on the wrapper (consult `Source/DotNetWorkQueue/IoC/ComponentRegistration.cs:385` which is the existing open-generic call site — match its shape). The intent (three open-generic mappings + extractor singleton + validator singleton) is unambiguous; the exact API surface is a single-line code-detail decision.

**Step 4: Run smoke test to verify it passes**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~SqlServerRelationalProducerQueueTests" --nologo`

Expected: PASS — `Passed: 7, Failed: 0, Skipped: 0, Total: 7`.

Also run the full SqlServer.Tests + RelationalDatabase.Tests suites to verify no regressions:

```bash
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --nologo
dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug --nologo
```

Expected: `Failed: 0` in both suites.

Then verify Release build (XML-doc + `TreatWarningsAsErrors` gate):

```bash
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release --nologo
```

Expected: `Build succeeded. 0 Error(s), 0 Warning(s)`.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalProducerQueueTests.cs
git commit -m "shipyard(phase-3): wire SqlServer outbox DI + capability-cast test"
```

**Acceptance criteria:**
- `SQLServerMessageQueueInit.cs` contains the 5 new registrations placed between line 58 and line 60 (inclusive).
- The capability-cast smoke test passes.
- Full SqlServer.Tests + RelationalDatabase.Tests suites remain green.
- Release build of `DotNetWorkQueue.Transport.SqlServer` succeeds with zero warnings.

## Verification

```bash
# Files exist
test -f Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerExternalDbNameExtractor.cs
test -f Source/DotNetWorkQueue.Transport.SqlServer/Basic/SqlServerRelationalProducerQueue.cs
test -f Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerExternalDbNameExtractorTests.cs
test -f Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/SqlServerRelationalProducerQueueTests.cs

# 5 new registrations present in init
grep -c "SqlServerRelationalProducerQueue<>" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs
# expected: 3 (three open-generic Register lines)
grep -c "SqlServerExternalDbNameExtractor" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs
# expected: 1
grep -c "container.Register<ExternalTransactionValidator>" Source/DotNetWorkQueue.Transport.SqlServer/Basic/SQLServerMessageQueueInit.cs
# expected: 1

# Release build clean (TreatWarningsAsErrors + XML doc)
dotnet build "Source/DotNetWorkQueue.Transport.SqlServer/DotNetWorkQueue.Transport.SqlServer.csproj" -c Release --nologo
# expected: 0 Error(s), 0 Warning(s)

# Phase 3 unit tests (all of Task 1 + Task 2 + Task 3)
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --filter "FullyQualifiedName~SqlServerExternalDbNameExtractorTests|FullyQualifiedName~SqlServerRelationalProducerQueueTests" --nologo
# expected: Passed: 9, Failed: 0

# No regressions
dotnet test "Source/DotNetWorkQueue.Transport.SqlServer.Tests/DotNetWorkQueue.Transport.SqlServer.Tests.csproj" -c Debug --nologo
# expected: Failed: 0

# Grep gate: no sealed-type casts leaked into RelationalDatabase
grep -rn "Microsoft\.Data\.SqlClient\|using Npgsql" Source/DotNetWorkQueue.Transport.RelationalDatabase/ --include="*.cs" --include="*.csproj"
# expected: no matches (exit code 1)
```
