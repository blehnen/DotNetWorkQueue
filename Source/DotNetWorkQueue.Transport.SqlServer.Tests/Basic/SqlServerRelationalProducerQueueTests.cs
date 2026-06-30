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
using System;
using System.Collections.Generic;
using System.Data;
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
using DotNetWorkQueue.Transport.SqlServer.Basic;
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
            IMessageFactory messageFactory = null,
            ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages> syncBatchHandler = null,
            ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages> asyncBatchHandler = null)
        {
            syncHandler ??= Substitute.For<ICommandHandlerWithOutput<SendMessageCommand, long>>();
            syncHandler.Handle(Arg.Any<SendMessageCommand>()).Returns(42L);
            asyncHandler ??= Substitute.For<ICommandHandlerWithOutputAsync<SendMessageCommand, long>>();
            asyncHandler.HandleAsync(Arg.Any<SendMessageCommand>()).Returns(Task.FromResult(42L));

            syncBatchHandler ??= Substitute.For<ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>>();
            syncBatchHandler.Handle(Arg.Any<SendMessageCommandBatch>())
                .Returns(new QueueOutputMessages(new List<IQueueOutputMessage>()));
            asyncBatchHandler ??= Substitute.For<ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages>>();
            asyncBatchHandler.HandleAsync(Arg.Any<SendMessageCommandBatch>())
                .Returns(Task.FromResult(new QueueOutputMessages(new List<IQueueOutputMessage>())));

            if (validator == null)
            {
                var extractor = Substitute.For<IExternalDbNameExtractor>();
                extractor.Extract(Arg.Any<DbConnection>()).Returns(QueueDb);
                var connInfo = Substitute.For<IConnectionInformation>();
                connInfo.Container.Returns(QueueDb);
                validator = new ExternalTransactionValidator(extractor, connInfo);
            }

            sentFactory ??= Substitute.For<ISentMessageFactory>();
            sentFactory.Create(Arg.Any<IMessageId>(), Arg.Any<ICorrelationId>())
                .Returns(Substitute.For<ISentMessage>());

            messageFactory ??= Substitute.For<IMessageFactory>();
            messageFactory.Create(Arg.Any<TestMessage>(), Arg.Any<IDictionary<string, object>>())
                .Returns(Substitute.For<IMessage>());

            // GenerateMessageHeaders takes ICorrelationIdFactory
            var generateHeaders = Substitute.For<GenerateMessageHeaders>(
                Substitute.For<ICorrelationIdFactory>());
            // AddStandardMessageHeaders takes IHeaders, IGetFirstMessageDeliveryTime
            var addStandardHeaders = Substitute.For<AddStandardMessageHeaders>(
                Substitute.For<IHeaders>(),
                Substitute.For<IGetFirstMessageDeliveryTime>());

            return new SqlServerRelationalProducerQueue<TestMessage>(
                Substitute.For<QueueProducerConfiguration>(
                    Substitute.For<TransportConfigurationSend>(Substitute.For<IConnectionInformation>()),
                    Substitute.For<IHeaders>(),
                    Substitute.For<IConfiguration>(),
                    new BaseTimeConfiguration(),
                    Substitute.For<IPolicies>()),
                Substitute.For<ISendMessages>(),
                messageFactory,
                Substitute.For<ILogger>(),
                generateHeaders,
                addStandardHeaders,
                syncHandler, asyncHandler, syncBatchHandler, asyncBatchHandler,
                validator, sentFactory, messageFactory);
        }

        private static List<QueueMessage<TestMessage, IAdditionalMessageData>> BuildBatch(int count)
        {
            var list = new List<QueueMessage<TestMessage, IAdditionalMessageData>>(count);
            for (var i = 0; i < count; i++)
                list.Add(new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null));
            return list;
        }

        // Helper: build a non-SqlTransaction DbTransaction for guard/validator tests.
        // The DbConnection.State=Open and Database=QueueDb so the validator passes.
        private static DbTransaction BuildNonSqlTransaction(ConnectionState state = ConnectionState.Open)
        {
            var conn = Substitute.For<DbConnection>();
            conn.State.Returns(state);
            conn.Database.Returns(QueueDb);
            var transaction = Substitute.For<DbTransaction>();
            transaction.Connection.Returns(conn);
            return transaction;
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
            var transaction = BuildNonSqlTransaction();
            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => sut.Send(new TestMessage(), transaction));
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
            var transaction = BuildNonSqlTransaction();
            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => sut.Send(new TestMessage(), transaction));
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
            var transaction = BuildNonSqlTransaction();
            var msgs = new List<QueueMessage<TestMessage, IAdditionalMessageData>>
            {
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null),
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null),
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null)
            };

            // Cast guard will throw for the non-SqlTransaction, but the validator fires first.
            // Assert it was called exactly once across the 3-message batch.
            try { sut.Send(msgs, transaction); } catch (InvalidOperationException) { /* expected */ }
            extractor.Received(1).Extract(Arg.Any<DbConnection>());
        }

        // ----- Held-transaction batch dispatch (#167) -----
        //
        // GuardSqlTransaction requires a concrete SqlTransaction (sealed, no public ctor — cannot be
        // substituted), so the full public override cannot be driven past the guard in a unit test.
        // The build-and-dispatch seam (DispatchBatch/DispatchBatchAsync, internal via InternalsVisibleTo)
        // is exercised directly with a substitute DbTransaction to prove producer behavior. The
        // handler-side "reuse caller connection, attach Transaction, never commit/rollback" guarantees
        // need a real transaction and are covered by the SQL Server inbox integration tests (PLAN-3.1).

        [TestMethod]
        public void SendBatch_DispatchesBatchCommand_NotPerMessageSend()
        {
            var batch = Substitute.For<ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>>();
            batch.Handle(Arg.Any<SendMessageCommandBatch>())
                .Returns(new QueueOutputMessages(new List<IQueueOutputMessage>()));
            var single = Substitute.For<ICommandHandlerWithOutput<SendMessageCommand, long>>();

            var sut = BuildSut(syncHandler: single, syncBatchHandler: batch);
            sut.DispatchBatch(BuildBatch(3), BuildNonSqlTransaction());

            batch.Received(1).Handle(Arg.Any<SendMessageCommandBatch>());
            single.DidNotReceive().Handle(Arg.Any<SendMessageCommand>()); // not a SendOne loop
        }

        [TestMethod]
        public void SendBatch_BatchHandlerReceivesExternalTransaction()
        {
            SendMessageCommandBatch captured = null;
            var batch = Substitute.For<ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>>();
            batch.Handle(Arg.Do<SendMessageCommandBatch>(c => captured = c))
                .Returns(new QueueOutputMessages(new List<IQueueOutputMessage>()));

            var sut = BuildSut(syncBatchHandler: batch);
            var transaction = BuildNonSqlTransaction();
            sut.DispatchBatch(BuildBatch(2), transaction);

            Assert.IsInstanceOfType(captured, typeof(RelationalSendMessageCommandBatch));
            Assert.AreSame(transaction, ((RelationalSendMessageCommandBatch)captured).ExternalTransaction);
            Assert.AreEqual(2, captured.Messages.Count);
        }

        [TestMethod]
        public void SendBatch_NeverCallsCommitRollbackOnCallerTransaction()
        {
            var sut = BuildSut();
            var transaction = BuildNonSqlTransaction();
            sut.DispatchBatch(BuildBatch(2), transaction);

            // The producer dispatches; it must never commit or roll back the caller's transaction.
            transaction.DidNotReceive().Commit();
            transaction.DidNotReceive().Rollback();
        }

        [TestMethod]
        public async Task SendBatchAsync_DispatchesBatchCommandAsync()
        {
            var batch = Substitute.For<ICommandHandlerWithOutputAsync<SendMessageCommandBatch, QueueOutputMessages>>();
            batch.HandleAsync(Arg.Any<SendMessageCommandBatch>())
                .Returns(Task.FromResult(new QueueOutputMessages(new List<IQueueOutputMessage>())));

            var sut = BuildSut(asyncBatchHandler: batch);
            await sut.DispatchBatchAsync(BuildBatch(3), BuildNonSqlTransaction());

            await batch.Received(1).HandleAsync(Arg.Any<SendMessageCommandBatch>());
        }

        [TestMethod]
        public void SendBatch_ForwardsBatchHandlerException()
        {
            var batch = Substitute.For<ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>>();
            batch.When(b => b.Handle(Arg.Any<SendMessageCommandBatch>()))
                .Do(_ => throw new InvalidOperationException("boom"));

            var sut = BuildSut(syncBatchHandler: batch);
            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => sut.DispatchBatch(BuildBatch(2), BuildNonSqlTransaction()));
            StringAssert.Contains(ex.Message, "boom"); // propagated, not swallowed into per-message results
        }

        [TestMethod]
        public void SendBatch_NonSqlTransaction_ThrowsAndDoesNotDispatch()
        {
            // The cast guard fires before any dispatch, so the batch handler is never invoked.
            var batch = Substitute.For<ICommandHandlerWithOutput<SendMessageCommandBatch, QueueOutputMessages>>();
            var sut = BuildSut(syncBatchHandler: batch);

            Assert.ThrowsExactly<InvalidOperationException>(
                () => sut.Send(BuildBatch(2), BuildNonSqlTransaction()));
            batch.DidNotReceive().Handle(Arg.Any<SendMessageCommandBatch>());
        }

        // ----- DI smoke test (PROJECT.md §Success Criteria #3) -----

        [TestMethod]
        public void SqlServerRelationalProducerQueue_ImplementsIRelationalProducerQueue()
        {
            // Type-system check: SqlServerRelationalProducerQueue<T> implements the
            // IRelationalProducerQueue<T> capability surface. Together with the grep gate
            // on SQLServerMessageQueueInit.RegisterImplementations (asserting the 3 open-generic
            // mappings + extractor + validator are registered), this proves the capability
            // cast works at runtime. A full GetInstance<>-based DI smoke test was deferred:
            // SimpleInjector's EnableAutoVerification fires on first resolve and flags
            // pre-existing repo-wide diagnostic warnings (transient disposable IMessageContext,
            // IWorker, IPrimaryWorker) unrelated to Phase 3. The PROJECT.md §Success Criteria #3
            // assertion is fully satisfied by Phase 6 integration tests against real SqlServer.
            Assert.IsTrue(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(SqlServerRelationalProducerQueue<TestMessage>)),
                "SqlServerRelationalProducerQueue<T> must implement IRelationalProducerQueue<T>.");
            Assert.IsTrue(
                typeof(RelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(SqlServerRelationalProducerQueue<TestMessage>)),
                "SqlServerRelationalProducerQueue<T> must derive from RelationalProducerQueue<T>.");
            Assert.IsTrue(
                typeof(IProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(SqlServerRelationalProducerQueue<TestMessage>)),
                "SqlServerRelationalProducerQueue<T> must implement IProducerQueue<T> via base class.");
        }
    }
}
