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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Moving a message to the error queue while a receive transaction is being held.
    ///
    /// The insert used to run on a connection of its own, with no transaction, so it committed on the
    /// spot. The source delete and the commit then happened on the *held* transaction - and if either
    /// failed, the error copy stayed behind while the message rolled back into the queue. The same
    /// message existed in both places and could be processed again.
    ///
    /// It shares the held transaction now, so the move has one outcome. That only became possible once
    /// PostgreSQL stopped deleting the metadata row during the receive: the copy reads that row, and
    /// inside the transaction that had deleted it there was nothing left to copy.
    /// </summary>
    [TestClass]
    public class MoveRecordToErrorQueueCommandHandlerTests
    {
        [TestMethod]
        public void HandleWithAHeldTransaction_RunsTheInsertOnTheHeldTransaction()
        {
            var h = new Harness();

            h.Handler.Handle(new MoveRecordToErrorQueueCommand<long>(new System.Exception(), 42, h.Context));

            Assert.AreSame(h.HeldTransaction, h.HeldCommand.Transaction,
                "the insert did not run inside the held transaction, so the move can leave an error copy behind");
        }

        [TestMethod]
        public void HandleWithAHeldTransaction_DoesNotOpenASecondConnection()
        {
            var h = new Harness();

            h.Handler.Handle(new MoveRecordToErrorQueueCommand<long>(new System.Exception(), 42, h.Context));

            h.ConnectionFactory.DidNotReceive().Create();
        }

        [TestMethod]
        public void HandleWithAHeldTransaction_CommitsTheHeldTransactionOnce()
        {
            var h = new Harness();

            h.Handler.Handle(new MoveRecordToErrorQueueCommand<long>(new System.Exception(), 42, h.Context));

            h.HeldTransaction.Received(1).Commit();
        }

        [TestMethod]
        public void HandleWithAHeldTransaction_WhenTheMessageIsNotThere_DoesNotCommit()
        {
            var h = new Harness(rowsCopied: 0);

            h.Handler.Handle(new MoveRecordToErrorQueueCommand<long>(new System.Exception(), 42, h.Context));

            h.HeldTransaction.DidNotReceive().Commit();
        }

        private sealed class Harness
        {
            public MoveRecordToErrorQueueCommandHandler<DbConnection, DbTransaction, DbCommand> Handler { get; }
            public IDbConnectionFactory ConnectionFactory { get; }
            public DbTransaction HeldTransaction { get; }
            public DbCommand HeldCommand { get; }
            public IMessageContext Context { get; }

            public Harness(int rowsCopied = 1)
            {
                var heldConnection = Substitute.For<DbConnection>();
                HeldTransaction = Substitute.For<DbTransaction>();
                HeldCommand = Substitute.For<DbCommand>();
                heldConnection.CreateCommand().Returns(HeldCommand);
                HeldCommand.ExecuteNonQuery().Returns(rowsCopied);

                var holder = Substitute.For<IConnectionHolder<DbConnection, DbTransaction, DbCommand>>();
                holder.Connection.Returns(heldConnection);
                holder.Transaction.Returns(HeldTransaction);

                var header = Substitute.For<IMessageContextData<IConnectionHolder<DbConnection, DbTransaction, DbCommand>>>();
                var headers = Substitute.For<IConnectionHeader<DbConnection, DbTransaction, DbCommand>>();
                headers.Connection.Returns(header);

                Context = Substitute.For<IMessageContext>();
                Context.Get(header).Returns(holder);

                var options = Substitute.For<ITransportOptions>();
                options.EnableHoldTransactionUntilMessageCommitted.Returns(true);
                options.EnableStatusTable.Returns(false);
                var optionsFactory = Substitute.For<ITransportOptionsFactory>();
                optionsFactory.Create().Returns(options);

                ConnectionFactory = Substitute.For<IDbConnectionFactory>();

                Handler = new MoveRecordToErrorQueueCommandHandler<DbConnection, DbTransaction, DbCommand>(
                    optionsFactory,
                    Substitute.For<ICommandHandler<DeleteMetaDataCommand>>(),
                    Substitute.For<ICommandHandler<SetStatusTableStatusTransactionCommand>>(),
                    ConnectionFactory,
                    Substitute.For<ITransactionFactory>(),
                    Substitute.For<IPrepareCommandHandler<MoveRecordToErrorQueueCommand<long>>>(),
                    headers,
                    Substitute.For<ICommandHandler<SetStatusTableStatusCommand<long>>>());
            }
        }
    }
}
