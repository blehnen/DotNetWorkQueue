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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class SqliteRelationalProducerQueueTests
    {
        // ----- Test fixtures -----

        private sealed class TestMessage { public string Body { get; set; } }

        // Consistent DB name used across tests so the validator passes by default.
        private const string QueueDb = "myqueue";

        /// <summary>
        /// Constructs a fully-configured <see cref="SqliteRelationalProducerQueue{T}"/> with
        /// all 11 ctor params substituted via NSubstitute.  Any argument left null gets a
        /// sensible default, including a validator whose extractor and container both return
        /// <see cref="QueueDb"/> so validation passes unless explicitly overridden.
        /// </summary>
        private static SqliteRelationalProducerQueue<TestMessage> BuildSut(
            ICommandHandlerWithOutput<SendMessageCommand, long> sendHandler = null,
            ICommandHandlerWithOutputAsync<SendMessageCommand, long> sendHandlerAsync = null,
            ExternalTransactionValidator validator = null,
            ISentMessageFactory sentFactory = null,
            IMessageFactory messageFactory = null)
        {
            sendHandler ??= Substitute.For<ICommandHandlerWithOutput<SendMessageCommand, long>>();
            sendHandler.Handle(Arg.Any<SendMessageCommand>()).Returns(42L);

            sendHandlerAsync ??= Substitute.For<ICommandHandlerWithOutputAsync<SendMessageCommand, long>>();
            sendHandlerAsync.HandleAsync(Arg.Any<SendMessageCommand>()).Returns(Task.FromResult(42L));

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

            return new SqliteRelationalProducerQueue<TestMessage>(
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
                sendHandler, sendHandlerAsync, validator, sentFactory, messageFactory);
        }

        /// <summary>
        /// Returns a mocked <see cref="DbTransaction"/> that is NOT a
        /// <c>System.Data.SQLite.SQLiteTransaction</c>.  The underlying connection's
        /// <see cref="DbConnection.State"/> and <see cref="DbConnection.DataSource"/> are
        /// configured so the validator accepts it — only the cast guard (SQLiteTransaction
        /// check) rejects it.
        /// SQLiteConnection and SQLiteTransaction are sealed, so they cannot be substituted;
        /// this mock DbTransaction is what exercises the guard's rejection path.
        /// </summary>
        private static DbTransaction BuildNonSqliteTransaction(
            ConnectionState state = ConnectionState.Open,
            string dataSource = QueueDb)
        {
            var conn = Substitute.For<DbConnection>();
            conn.State.Returns(state);
            conn.DataSource.Returns(dataSource);
            conn.Database.Returns(dataSource);
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
        public void Send_NonSqliteTransaction_GuardFires_ThrowsInvalidOperationExceptionWithSqliteTransactionInMessage()
        {
            // Validator passes (extractor and container both return QueueDb).
            // The cast guard then rejects the non-SQLiteTransaction and includes
            // the fully-qualified type name in the exception message.
            var sut = BuildSut();
            var transaction = BuildNonSqliteTransaction();
            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => sut.Send(new TestMessage(), transaction));
            StringAssert.Contains(ex.Message, "System.Data.SQLite.SQLiteTransaction");
        }

        [TestMethod]
        public void Send_ValidatorRejectsDbNameMismatch_ThrowsBeforeCastGuard()
        {
            // Guard ordering test: the validator fires BEFORE GuardSQLiteTransaction.
            // Configure a DB-name mismatch so the validator throws first.
            // The resulting exception must mention the normalized names (queueA / queueB)
            // and must NOT mention SQLiteTransaction, proving the validator ran before the guard.
            var extractor = Substitute.For<IExternalDbNameExtractor>();
            extractor.Extract(Arg.Any<DbConnection>()).Returns("queueA");
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.Container.Returns("queueB");
            var validator = new ExternalTransactionValidator(extractor, connInfo);

            var sut = BuildSut(validator: validator);
            var transaction = BuildNonSqliteTransaction();
            var ex = Assert.ThrowsExactly<InvalidOperationException>(
                () => sut.Send(new TestMessage(), transaction));

            StringAssert.Contains(ex.Message, "queueA");
            StringAssert.Contains(ex.Message, "queueB");
            Assert.IsFalse(
                ex.Message.Contains("SQLiteTransaction"),
                "Validator must fire before the SQLiteTransaction cast guard.");
        }

        [TestMethod]
        public async Task SendAsync_NullTransaction_ThrowsArgumentNullException()
        {
            var sut = BuildSut();
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(
                async () => await sut.SendAsync(new TestMessage(), (DbTransaction)null));
        }

        [TestMethod]
        public async Task SendAsync_NonSqliteTransaction_GuardFires_ThrowsInvalidOperationException()
        {
            // Async path: validator passes, cast guard rejects the non-SQLiteTransaction.
            var sut = BuildSut();
            var transaction = BuildNonSqliteTransaction();
            var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                async () => await sut.SendAsync(new TestMessage(), transaction));
            StringAssert.Contains(ex.Message, "System.Data.SQLite.SQLiteTransaction");
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
        public void SendBatch_ValidatorFiresOncePerBatch_NotPerItem()
        {
            // The batch override calls _validator.Validate(transaction) ONCE before the loop
            // (CONTEXT-4 Decision 4).  Instrument the extractor to count calls — each
            // Validate() invocation calls Extract() exactly once on the happy-path through
            // check 4 (DB-name comparison).  3 items in the batch; extractor must be called 1×.
            var extractor = Substitute.For<IExternalDbNameExtractor>();
            extractor.Extract(Arg.Any<DbConnection>()).Returns(QueueDb);
            var connInfo = Substitute.For<IConnectionInformation>();
            connInfo.Container.Returns(QueueDb);
            var validator = new ExternalTransactionValidator(extractor, connInfo);

            var sut = BuildSut(validator: validator);
            var transaction = BuildNonSqliteTransaction();
            var msgs = new List<QueueMessage<TestMessage, IAdditionalMessageData>>
            {
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null),
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null),
                new QueueMessage<TestMessage, IAdditionalMessageData>(new TestMessage(), null)
            };

            // The validator passes but the cast guard throws; validator was already called.
            try { sut.Send(msgs, transaction); } catch (InvalidOperationException) { /* expected */ }
            extractor.Received(1).Extract(Arg.Any<DbConnection>());
        }

        [TestMethod]
        public void TypeHierarchy_ImplementsIRelationalProducerQueue()
        {
            // Type-system assertions: SqliteRelationalProducerQueue<T> must satisfy all
            // three capability surfaces required by the outbox DI registration.
            Assert.IsTrue(
                typeof(IRelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(SqliteRelationalProducerQueue<TestMessage>)),
                "SqliteRelationalProducerQueue<T> must implement IRelationalProducerQueue<T>.");

            Assert.IsTrue(
                typeof(RelationalProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(SqliteRelationalProducerQueue<TestMessage>)),
                "SqliteRelationalProducerQueue<T> must derive from RelationalProducerQueue<T>.");

            Assert.IsTrue(
                typeof(IProducerQueue<TestMessage>).IsAssignableFrom(
                    typeof(SqliteRelationalProducerQueue<TestMessage>)),
                "SqliteRelationalProducerQueue<T> must implement IProducerQueue<T> via base class.");
        }
    }
}
