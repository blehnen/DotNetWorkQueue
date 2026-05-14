---
phase: postgresql-implementation
plan: 1.1
wave: 1
dependencies: []
must_haves:
  - PostgreSqlExternalDbNameExtractor implementing IExternalDbNameExtractor (pass-through connection.Database, no normalization — Decision 2)
  - PostgreSqlRelationalProducerQueue<T> subclass overriding the 4 RelationalProducerQueue<T> virtual hooks
  - PostgreSQLMessageQueueInit registers extractor, validator, and the 3 open-generic producer mappings via RegisterConditional (Rule A)
  - Bulk of Phase 4 unit tests (producer-subclass behavior + extractor + Risk #3 case-sensitive closure test)
files_touched:
  - Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlExternalDbNameExtractorTests.cs
  - Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalProducerQueueTests.cs
tdd: true
risk: medium
---

# Plan 1.1: Foundation — Extractor + Producer Subclass + DI Wiring (Wave 1)

## Context

This Wave 1 plan ships the PostgreSQL-side foundation of Phase 4 — structural mirror of Phase 3 PLAN-1.1 with four PG-specific deviations encoded in the task bodies:

1. **Pass-through extractor (CONTEXT-4 Decision 2 + RESEARCH §8).** `PostgreSqlExternalDbNameExtractor.Extract(connection)` returns `connection.Database` verbatim — NO `.ToUpperInvariant()` (unlike SqlServer). PG identifier case semantics are preserved by Npgsql, so an `Ordinal` compare in `ExternalTransactionValidator` against `IConnectionInformation.Container` (which is sourced from `NpgsqlConnectionStringBuilder.Database` per RESEARCH §9) is byte-for-byte correct.

2. **`RegisterConditional` not `Register` (CONTEXT-4 Rule A + RESEARCH §3).** The 3 open-generic producer mappings (`IProducerQueue<>`, `IRelationalProducerQueue<>`, `RelationalProducerQueue<>` → `PostgreSqlRelationalProducerQueue<>`) MUST use `container.RegisterConditional(...)`. Plain `Register` triggers SimpleInjector `EnableAutoVerification` on first resolve, surfaces pre-existing repo-wide diagnostic warnings, and breaks the 6 `QueueCreatorTests` (confirmed at lines 14–127 of `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/QueueCreatorTests.cs`, asserting `Assert.ThrowsExactly<NpgsqlException>`).

3. **Init insertion anchor (RESEARCH §2 + §11).** PG has no `//override so that we can use schema as needed` comment block (unlike SqlServer). Insert the 5 new registrations directly between `init.RegisterStandardImplementations(...)` at `PostgreSQLMessageQueueInit.cs` line 62 and the `//**all` comment at line 64.

4. **11-param constructor (CONTEXT-4 Rule C).** `PostgreSqlRelationalProducerQueue<T>` mirrors SqlServer's 11-param ctor exactly: 6 base + 5 new (`sendHandler`, `sendHandlerAsync`, `validator`, `sentMessageFactory`, `ownMessageFactory`). The `ownMessageFactory` parameter is the second `IMessageFactory` injection — necessary because `RelationalProducerQueue<T>` (Phase 2) does not expose `_messageFactory` as `protected`. SimpleInjector passes the same singleton to both slots.

Per **CONTEXT-4 Decision 1** batch overrides use `foreach` (NOT `Parallel.ForEach`) — ADO.NET transactions are not thread-safe. Per **CONTEXT-4 Decision 4** validator runs ONCE in the producer override before the `RelationalSendMessageCommand` is constructed.

Per **RESEARCH §1 + §5** direct handler-fork execution tests are infeasible at the unit-test level because `NpgsqlConnection`/`NpgsqlTransaction` are sealed — same constraint as SqlServer's `SqlConnection`/`SqlTransaction`. Phase 4 Wave 1 tests exercise the producer subclass against mocked handlers; the handler fork itself is covered by Wave 2 structural smoke tests + Phase 6 integration tests.

**Capability-cast test follows Phase 3 SUMMARY-1.1 substitution:** type-system `IsAssignableFrom` assertions, not a runtime `CreateProducer<T>` resolution (the SimpleInjector `EnableAutoVerification` blocker applies identically to PG).

## Dependencies

None within Phase 4. This is Wave 1. Phase 2 plans must be BUILT before this plan's builder runs (the subclass inherits `RelationalProducerQueue<T>` from Phase 2; the validator type is from Phase 2 PLAN-2.1; the extractor interface is from Phase 2 PLAN-2.1).

## Tasks

### Task 1: Create PostgreSqlExternalDbNameExtractor + extractor unit tests (incl. Risk #3 closure)

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs`
- Create: `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlExternalDbNameExtractorTests.cs`

**Step 1: Write the failing tests**

```csharp
// PostgreSqlExternalDbNameExtractorTests.cs
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
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    [TestClass]
    public class PostgreSqlExternalDbNameExtractorTests
    {
        [TestMethod]
        public void Extract_ReturnsConnectionDatabase_Verbatim()
        {
            // CONTEXT-4 Decision 2: pass-through with NO normalization.
            var conn = Substitute.For<DbConnection>();
            conn.Database.Returns("MyDb");
            var sut = new PostgreSqlExternalDbNameExtractor();
            Assert.AreEqual("MyDb", sut.Extract(conn));
        }

        [TestMethod]
        public void Extract_PreservesCase_NoUpperCasing()
        {
            // CONTEXT-4 Decision 2 / Risk #3 closure: PG identifier case is preserved by Npgsql.
            // Verify the extractor returns the raw connection.Database string without folding
            // case. Contrast with SqlServerExternalDbNameExtractor which upper-cases.
            var conn1 = Substitute.For<DbConnection>();
            conn1.Database.Returns("MyDb");
            var conn2 = Substitute.For<DbConnection>();
            conn2.Database.Returns("mydb");
            var sut = new PostgreSqlExternalDbNameExtractor();

            // Two different inputs MUST produce two different outputs (case-sensitive).
            Assert.AreNotEqual(sut.Extract(conn1), sut.Extract(conn2),
                "PG extractor must NOT normalize case — pass-through is required for " +
                "PostgreSQL's case-sensitive quoted-identifier semantics.");
            Assert.AreEqual("MyDb", sut.Extract(conn1));
            Assert.AreEqual("mydb", sut.Extract(conn2));
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~PostgreSqlExternalDbNameExtractorTests" --nologo`

Expected: FAIL with `error CS0246: The type or namespace name 'PostgreSqlExternalDbNameExtractor' could not be found`.

**Step 3: Write implementation**

```csharp
// PostgreSqlExternalDbNameExtractor.cs
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

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// PostgreSQL implementation of <see cref="IExternalDbNameExtractor"/>. Returns
    /// <see cref="DbConnection.Database"/> verbatim with NO case normalization,
    /// matching PostgreSQL's case-sensitive identifier semantics. Quoted-identifier
    /// database names (e.g. <c>"MyDb"</c>) preserve case both server-side and in
    /// Npgsql's reported <c>Database</c> property; the matching
    /// <c>IConnectionInformation.Container</c> value is sourced from
    /// <c>NpgsqlConnectionStringBuilder.Database</c>, so both sides of the
    /// validator's <see cref="System.StringComparison.Ordinal"/> compare are
    /// byte-for-byte consistent.
    /// </summary>
    public sealed class PostgreSqlExternalDbNameExtractor : IExternalDbNameExtractor
    {
        /// <summary>
        /// Returns the database name reported by the connection, verbatim
        /// (no normalization).
        /// </summary>
        /// <param name="connection">An open <see cref="DbConnection"/> from the caller's
        /// transaction. Must not be null.</param>
        /// <returns>The database name from <see cref="DbConnection.Database"/>, or
        /// an empty string if the connection reports null.</returns>
        public string Extract(DbConnection connection)
        {
            return connection.Database ?? string.Empty;
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~PostgreSqlExternalDbNameExtractorTests" --nologo`

Expected: PASS — `Passed: 2, Failed: 0, Skipped: 0, Total: 2`.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlExternalDbNameExtractorTests.cs
git commit -m "shipyard(phase-4): add PostgreSqlExternalDbNameExtractor + tests"
```

**Acceptance criteria:**
- `PostgreSqlExternalDbNameExtractor` is `public sealed`, in namespace `DotNetWorkQueue.Transport.PostgreSQL.Basic`, implements `IExternalDbNameExtractor`.
- `Extract(DbConnection)` returns `connection.Database ?? string.Empty` — NO `.ToUpperInvariant()` or `.ToLowerInvariant()` (Decision 2 / Risk #3).
- 2 unit tests pass. Test 2 (`Extract_PreservesCase_NoUpperCasing`) is the explicit Risk #3 closure proof — it asserts `"MyDb"` and `"mydb"` produce DIFFERENT outputs.
- File has the LGPL-2.1 header verbatim from `SendMessageCommandHandler.cs:1-18`.
- XML doc on the public class and the public method.

### Task 2: Create PostgreSqlRelationalProducerQueue<T> subclass + producer-subclass unit tests

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs`
- Create: `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalProducerQueueTests.cs`

**Step 1: Write the failing tests**

```csharp
// PostgreSqlRelationalProducerQueueTests.cs
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
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    [TestClass]
    public class PostgreSqlRelationalProducerQueueTests
    {
        public class TestMessage { public string Body { get; set; } }

        // PG is case-sensitive — extractor passes through verbatim.
        // Use the exact same string on both sides for happy-path tests.
        private const string QueueDb = "mydb";

        private static PostgreSqlRelationalProducerQueue<TestMessage> BuildSut(
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
            sentFactory.Create(Arg.Any<IMessageId>(), Arg.Any<Guid>())
                .Returns(Substitute.For<ISentMessage>());

            messageFactory ??= Substitute.For<IMessageFactory>();
            messageFactory.Create(Arg.Any<TestMessage>(), Arg.Any<IAdditionalMessageHeaders>())
                .Returns(Substitute.For<IMessage>());
            messageFactory.Create(Arg.Any<TestMessage>())
                .Returns(Substitute.For<IMessage>());

            // QueueProducerConfiguration construction mirrors the resolution Phase 3 builder
            // settled on (see SUMMARY-1.1 "Decisions Made: BuildSut helper"). Phase 4 builder
            // should reuse the exact same fixture wiring shape; adjust here if API drifts.
            var configuration = new QueueProducerConfiguration(
                new TransportConfigurationSend(Substitute.For<IConnectionInformation>()),
                Substitute.For<IHeaders>(),
                Substitute.For<IConfiguration>(),
                Substitute.For<BaseTimeConfiguration>(),
                Substitute.For<IPolicies>());

            return new PostgreSqlRelationalProducerQueue<TestMessage>(
                configuration,
                Substitute.For<ISendMessages>(),
                messageFactory,
                Substitute.For<ILogger>(),
                Substitute.For<GenerateMessageHeaders>(Substitute.For<IGetHeader>(),
                    Substitute.For<IHeaders>(), Substitute.For<IMessageContextDataFactory>()),
                Substitute.For<AddStandardMessageHeaders>(Substitute.For<IGetTime>(),
                    Substitute.For<IHeaders>(), Substitute.For<IConnectionInformation>()),
                syncHandler, asyncHandler, validator, sentFactory, messageFactory);
        }

        private static DbTransaction BuildPgLikeTx(ConnectionState state = ConnectionState.Open)
        {
            // NpgsqlConnection is sealed — we cannot substitute it. The validator path
            // is exercised with a mocked DbConnection (validator works against the base type),
            // and the cast guard is exercised separately by passing this non-NpgsqlTransaction.
            var conn = Substitute.For<DbConnection>();
            conn.State.Returns(state);
            conn.Database.Returns(QueueDb);
            var tx = Substitute.For<DbTransaction>();
            tx.Connection.Returns(conn);
            return tx;
        }

        [TestMethod]
        public void Send_NullTransaction_ThrowsArgumentNullException()
        {
            var sut = BuildSut();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => sut.Send(new TestMessage(), (DbTransaction)null));
        }

        [TestMethod]
        public void Send_NonNpgsqlTransaction_ThrowsInvalidOperationException()
        {
            // Validator passes (configured to match QueueDb), then the cast guard fires.
            var sut = BuildSut();
            var tx = BuildPgLikeTx();
            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => sut.Send(new TestMessage(), tx));
            StringAssert.Contains(ex.Message, "NpgsqlTransaction");
        }

        [TestMethod]
        public void Send_ValidatorRejectsCaseMismatch_ThrowsBeforeCastGuard()
        {
            // CONTEXT-4 Decision 2 / Risk #3 closure: case-sensitive validator behavior.
            // Configure container=QueueDb (lowercase "mydb"), extractor returns "MyDb"
            // (mixed case). The validator's StringComparison.Ordinal compare MUST treat
            // these as unequal and throw InvalidOperationException before the cast guard.
            var extractor = Substitute.For<IExternalDbNameExtractor>();
            extractor.Extract(Arg.Any<DbConnection>()).Returns("MyDb");
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.Container.Returns("mydb");
            var validator = new ExternalTransactionValidator(extractor, connInfo);

            var sut = BuildSut(validator: validator);
            var tx = BuildPgLikeTx();
            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => sut.Send(new TestMessage(), tx));
            // Both names appear in the diagnostic. The validator fires BEFORE the cast guard,
            // so the message must NOT mention NpgsqlTransaction.
            StringAssert.Contains(ex.Message, "MyDb");
            StringAssert.Contains(ex.Message, "mydb");
            Assert.IsFalse(ex.Message.Contains("NpgsqlTransaction"),
                "Validator must fire before the NpgsqlTransaction cast guard.");
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
            // CONTEXT-4 Decision 4: validator runs ONCE before the loop, not per item.
            var extractor = Substitute.For<IExternalDbNameExtractor>();
            extractor.Extract(Arg.Any<DbConnection>()).Returns(QueueDb);
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.Container.Returns(QueueDb);
            var validator = new ExternalTransactionValidator(extractor, connInfo);

            var sut = BuildSut(validator: validator);
            var tx = BuildPgLikeTx();
            var msgs = new List<QueueMessage<TestMessage, IAdditionalMessageData>>
            {
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null),
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null),
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null)
            };

            // Cast guard throws for the non-NpgsqlTransaction; validator already fired once.
            try { sut.Send(msgs, tx); } catch (InvalidOperationException) { /* expected */ }
            extractor.Received(1).Extract(Arg.Any<DbConnection>());
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~PostgreSqlRelationalProducerQueueTests" --nologo`

Expected: FAIL with `error CS0246: The type or namespace name 'PostgreSqlRelationalProducerQueue' could not be found`.

**Step 3: Write implementation**

```csharp
// PostgreSqlRelationalProducerQueue.cs
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
// (full LGPL header — copy from SendMessageCommandHandler.cs:1-18)
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.Common;
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
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// PostgreSQL-specific <see cref="RelationalProducerQueue{T}"/> that overrides the
    /// four caller-supplied-transaction hooks to dispatch
    /// <see cref="RelationalSendMessageCommand"/> instances through the registered
    /// PostgreSQL <c>SendMessageCommandHandler</c> / <c>SendMessageCommandHandlerAsync</c>.
    /// Validates the caller's transaction at the producer surface (fail-fast,
    /// boundary-checked) before any handler dispatch. Batch overrides iterate
    /// sequentially because ADO.NET transactions are not thread-safe.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    public sealed class PostgreSqlRelationalProducerQueue<TMessage>
        : RelationalProducerQueue<TMessage>
        where TMessage : class
    {
        private readonly ICommandHandlerWithOutput<SendMessageCommand, long> _sendHandler;
        private readonly ICommandHandlerWithOutputAsync<SendMessageCommand, long> _sendHandlerAsync;
        private readonly ExternalTransactionValidator _validator;
        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly IMessageFactory _messageFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlRelationalProducerQueue{TMessage}"/> class.
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
        public PostgreSqlRelationalProducerQueue(
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
            GuardNpgsqlTransaction(transaction);
            return SendOne(message, data ?? new AdditionalMessageData(), transaction);
        }

        /// <inheritdoc />
        protected override async Task<IQueueOutputMessage> SendWithExternalTransactionAsync(
            TMessage message, IAdditionalMessageData data, DbTransaction transaction)
        {
            _validator.Validate(transaction);
            GuardNpgsqlTransaction(transaction);
            return await SendOneAsync(message, data ?? new AdditionalMessageData(), transaction)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override IQueueOutputMessages SendWithExternalTransactionBatch(
            List<QueueMessage<TMessage, IAdditionalMessageData>> messages, DbTransaction transaction)
        {
            Guard.NotNull(() => messages, messages);
            _validator.Validate(transaction);   // ONCE, before the loop (CONTEXT-4 Decision 4)
            GuardNpgsqlTransaction(transaction);

            var rc = new List<IQueueOutputMessage>(messages.Count);
            foreach (var m in messages)         // sequential — DbTransaction is not thread-safe
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
            GuardNpgsqlTransaction(transaction);

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

        private static void GuardNpgsqlTransaction(DbTransaction transaction)
        {
            if (transaction is not NpgsqlTransaction)
            {
                throw new InvalidOperationException(
                    $"Expected NpgsqlTransaction but received '{transaction.GetType().FullName}'. " +
                    "The transaction must be opened on an NpgsqlConnection from the Npgsql provider.");
            }
        }
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~PostgreSqlRelationalProducerQueueTests" --nologo`

Expected: PASS — `Passed: 6, Failed: 0, Skipped: 0, Total: 6`.

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalProducerQueueTests.cs
git commit -m "shipyard(phase-4): add PostgreSqlRelationalProducerQueue<T> + producer tests"
```

**Acceptance criteria:**
- Class is `public sealed`, namespace `DotNetWorkQueue.Transport.PostgreSQL.Basic`, derives from `RelationalProducerQueue<TMessage>` with `where TMessage : class`.
- Constructor takes **exactly 11 parameters** (CONTEXT-4 Rule C): 6 base + `sendHandler` + `sendHandlerAsync` + `validator` + `sentMessageFactory` + `ownMessageFactory`.
- All 4 `protected override SendWithExternalTransaction*` methods exist and: (a) call `_validator.Validate(transaction)` FIRST, (b) then call `GuardNpgsqlTransaction(transaction)`, (c) then dispatch `RelationalSendMessageCommand` via the registered handler.
- Batch overrides use `foreach` not `Parallel.ForEach`; per-item exceptions are aggregated into the result list (Decision 1).
- `GuardNpgsqlTransaction` throws `InvalidOperationException` with a message that contains both `"NpgsqlTransaction"` and `"Npgsql"` substrings.
- 6 unit tests pass — including Test 3 (`Send_ValidatorRejectsCaseMismatch_ThrowsBeforeCastGuard`) which is the **Risk #3 closure proof** at the producer-subclass level.
- XML doc on the public class and the public constructor; LGPL-2.1 header.

### Task 3: Wire DI registrations in PostgreSQLMessageQueueInit + type-system capability-cast smoke test

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs` — insert 5 new registrations between line 62 (`init.RegisterStandardImplementations(...)`) and line 64 (`//**all`).
- Modify (append test): `Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalProducerQueueTests.cs` — add capability-cast type-system smoke test.

**Step 1: Write the failing smoke test** (appended to the existing test file from Task 2)

Per Phase 3 SUMMARY-1.1 "Decisions Made: Capability-cast smoke test substitution," the runtime `CreateProducer<T>` resolution path is blocked by SimpleInjector `EnableAutoVerification` surfacing pre-existing repo-wide diagnostic warnings. Substitute three type-system `IsAssignableFrom` assertions — same capability surface, no DI-resolution side effects. The full runtime resolution check lives in Phase 6 integration tests.

```csharp
// Append inside PostgreSqlRelationalProducerQueueTests class:

[TestMethod]
public void CapabilityCast_TypeImplementsRelationalProducerQueueShapes()
{
    // PROJECT.md §Success Criteria #3 / #7 wiring check at the type-system level.
    // Phase 6 integration tests cover runtime resolution against a real PG instance.
    // Three IsAssignableFrom assertions cover the full capability surface:
    //   - IRelationalProducerQueue<T>  (the new outbox-facing interface)
    //   - RelationalProducerQueue<T>   (the Phase 2 base class)
    //   - IProducerQueue<T>            (the legacy IProducerQueue surface, preserved)
    var openType = typeof(PostgreSqlRelationalProducerQueue<>);
    var closedType = openType.MakeGenericType(typeof(TestMessage));

    Assert.IsTrue(
        typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(closedType),
        "PostgreSqlRelationalProducerQueue<T> must implement IRelationalProducerQueue<T>.");
    Assert.IsTrue(
        typeof(RelationalProducerQueue<TestMessage>).IsAssignableFrom(closedType),
        "PostgreSqlRelationalProducerQueue<T> must derive from RelationalProducerQueue<T>.");
    Assert.IsTrue(
        typeof(IProducerQueue<TestMessage>).IsAssignableFrom(closedType),
        "PostgreSqlRelationalProducerQueue<T> must implement IProducerQueue<T> (transitively via RelationalProducerQueue<T>).");
}
```

**Step 2: Run smoke test to verify it fails on the cast / DI side**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~CapabilityCast_TypeImplementsRelationalProducerQueueShapes" --nologo`

Expected: PASS once Task 2's `PostgreSqlRelationalProducerQueue<T>` exists with the correct base class — this is the static type-system check. The DI wiring failure mode is covered by the grep-gate in **Verification** below.

**Step 3: Write the DI implementation — insert 7 lines into `PostgreSQLMessageQueueInit.cs`**

Locate `PostgreSqlMessageQueueInit.RegisterImplementations` (line 55). Between line 62 (`init.RegisterStandardImplementations(...)`) and line 64 (`//**all`), insert the following block. The existing `using` statements at lines 32–34 already include `DotNetWorkQueue.Transport.RelationalDatabase` and `DotNetWorkQueue.Transport.RelationalDatabase.Basic` — no new imports needed.

```csharp
            // Phase 4: outbox-pattern producer wiring (PostgreSQL side)
            container.Register<IExternalDbNameExtractor, PostgreSqlExternalDbNameExtractor>(LifeStyles.Singleton);
            container.Register<ExternalTransactionValidator>(LifeStyles.Singleton);
            // RegisterConditional preempts the open-generic IProducerQueue<> fallback in
            // ComponentRegistration.RegisterFallbacks (also conditional) and preserves
            // SimpleInjector's lazy-verification semantics for these open generics — plain
            // Register triggers eager verification that surfaces pre-existing repo-wide
            // diagnostic warnings on transient IDisposable types.
            container.RegisterConditional(typeof(IProducerQueue<>), typeof(PostgreSqlRelationalProducerQueue<>), LifeStyles.Singleton);
            container.RegisterConditional(typeof(IRelationalProducerQueue<>), typeof(PostgreSqlRelationalProducerQueue<>), LifeStyles.Singleton);
            container.RegisterConditional(typeof(RelationalProducerQueue<>), typeof(PostgreSqlRelationalProducerQueue<>), LifeStyles.Singleton);
```

**Architect note for builder (CONTEXT-4 Rule A):** Plain `container.Register(typeof(IProducerQueue<>), ...)` MUST NOT be used here. Use `container.RegisterConditional(typeof(...), typeof(...), LifeStyles.Singleton)` — the same overload Phase 3 settled on. The `ContainerWrapper.RegisterConditional(Type, Type, LifeStyles)` overload (lines 179–183 of `Source/DotNetWorkQueue/IoC/ContainerWrapper.cs`) is the correct API surface. The internal predicate is `c => !c.Handled`.

**Step 4: Run smoke test to verify it passes**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~PostgreSqlRelationalProducerQueueTests" --nologo`

Expected: PASS — `Passed: 7, Failed: 0, Skipped: 0, Total: 7`.

Then run the full PG.Tests + RelationalDatabase.Tests suites to verify no regressions (especially the 6 `QueueCreatorTests` that assert `NpgsqlException`):

```bash
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --nologo
dotnet test "Source/DotNetWorkQueue.Transport.RelationalDatabase.Tests/DotNetWorkQueue.Transport.RelationalDatabase.Tests.csproj" -c Debug --nologo
```

Expected: `Failed: 0` in both suites.

Then verify Release build (XML-doc + `TreatWarningsAsErrors` gate):

```bash
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release --nologo
```

Expected: `Build succeeded. 0 Error(s), 0 Warning(s)` (modulo pre-existing NU1902 advisory warnings carried forward from Phase 2).

**Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalProducerQueueTests.cs
git commit -m "shipyard(phase-4): wire PostgreSQL outbox DI + capability-cast test"
```

**Acceptance criteria:**
- `PostgreSQLMessageQueueInit.cs` contains the 5 new registrations placed between line 62 and line 64. Order: extractor → validator → 3 `RegisterConditional` mappings.
- All 3 open-generic producer registrations use `RegisterConditional` (CONTEXT-4 Rule A — grep gate enforces 3 matches; 0 matches for plain `Register(typeof(...ProducerQueue<>)...)`).
- The capability-cast smoke test passes (type-system level).
- Full `PostgreSQL.Tests` + `RelationalDatabase.Tests` suites remain green — specifically the 6 `QueueCreatorTests.Create_*` tests that assert `Assert.ThrowsExactly<Npgsql.NpgsqlException>` must still pass (Rule A validation).
- Release build of `DotNetWorkQueue.Transport.PostgreSQL` succeeds with zero new warnings.

## Verification

```bash
# Files exist
test -f Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs
test -f Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs
test -f Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlExternalDbNameExtractorTests.cs
test -f Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/Basic/PostgreSqlRelationalProducerQueueTests.cs

# Decision 2: extractor is pass-through — NO upper/lower casing
grep -c "ToUpperInvariant\|ToLowerInvariant" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlExternalDbNameExtractor.cs
# expected: 0

# Rule A: 3 RegisterConditional + 0 plain Register(typeof(I*ProducerQueue<>)...)
grep -c "container.RegisterConditional(typeof(.*ProducerQueue<>)" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs
# expected: 3
grep -cE "container\.Register\(typeof\([A-Za-z]*ProducerQueue<>\)" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs
# expected: 0

# Extractor + validator singletons present
grep -c "PostgreSqlExternalDbNameExtractor" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs
# expected: 1
grep -c "container.Register<ExternalTransactionValidator>" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSQLMessageQueueInit.cs
# expected: 1

# Rule C: 11-param constructor on the producer subclass
grep -c "public PostgreSqlRelationalProducerQueue(" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs
# expected: 1
# Count commas in the constructor signature (will be 10 → 11 params).
awk '/public PostgreSqlRelationalProducerQueue\(/,/\)$/' Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs | tr -cd ',' | wc -c
# expected: 10

# GuardNpgsqlTransaction present
grep -c "GuardNpgsqlTransaction" Source/DotNetWorkQueue.Transport.PostgreSQL/Basic/PostgreSqlRelationalProducerQueue.cs
# expected: >=5 (4 override call-sites + 1 definition)

# Release build clean (TreatWarningsAsErrors + XML doc)
dotnet build "Source/DotNetWorkQueue.Transport.PostgreSQL/DotNetWorkQueue.Transport.PostgreSQL.csproj" -c Release --nologo
# expected: 0 Error(s), 0 Warning(s) (modulo pre-existing NU1902 advisory)

# Phase 4 unit tests (Task 1 + Task 2 + Task 3 capability-cast test)
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --filter "FullyQualifiedName~PostgreSqlExternalDbNameExtractorTests|FullyQualifiedName~PostgreSqlRelationalProducerQueueTests" --nologo
# expected: Passed: 9, Failed: 0

# No regressions — specifically the 6 QueueCreatorTests asserting NpgsqlException
dotnet test "Source/DotNetWorkQueue.Transport.PostgreSQL.Tests/DotNetWorkQueue.Transport.PostgreSQL.Tests.csproj" -c Debug --nologo
# expected: Failed: 0

# Layering invariant grep on RelationalDatabase
grep -rn "using Npgsql\|using Microsoft\.Data\.SqlClient" Source/DotNetWorkQueue.Transport.RelationalDatabase/ --include="*.cs" --include="*.csproj"
# expected: no matches (exit code 1)
```
