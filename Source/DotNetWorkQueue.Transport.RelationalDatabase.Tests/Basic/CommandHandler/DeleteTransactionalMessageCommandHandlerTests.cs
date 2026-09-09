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
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
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
    /// What the held-transaction delete actually deletes.
    ///
    /// This handler runs instead of the ordinary one when
    /// <c>EnableHoldTransactionUntilMessageCommitted</c> is on, and the two must clear the same tables:
    /// a message removed through one path and not the other leaves rows behind that nothing will ever
    /// collect. It skipped the metadata-errors table, so a message that had errored before being
    /// removed kept its error rows on SQL Server and PostgreSQL.
    /// </summary>
    [TestClass]
    public class DeleteTransactionalMessageCommandHandlerTests
    {
        private static readonly CommandStringTypes[] Expected =
        {
            CommandStringTypes.DeleteFromMetaData,
            CommandStringTypes.DeleteFromQueue,
            CommandStringTypes.DeleteFromErrorTracking,
            CommandStringTypes.DeleteFromMetaDataErrors
        };

        [TestMethod]
        public void Handle_ClearsEveryTableTheOrdinaryDeleteDoes()
        {
            var harness = new Harness(statusTable: false);

            harness.CreateSync().Handle(harness.Command);

            CollectionAssert.AreEquivalent(Expected, harness.Prepared,
                "The held-transaction delete cleared a different set of tables than the ordinary one.");
        }

        [TestMethod]
        public async Task HandleAsync_ClearsEveryTableTheOrdinaryDeleteDoes()
        {
            var harness = new Harness(statusTable: false);

            await harness.CreateAsync().HandleAsync(harness.Command).ConfigureAwait(false);

            CollectionAssert.AreEquivalent(Expected, harness.Prepared,
                "The asynchronous held-transaction delete diverged from its synchronous twin.");
        }

        [TestMethod]
        public async Task HandleAsync_WithTheStatusTableOn_AlsoClearsIt()
        {
            var harness = new Harness(statusTable: true);

            await harness.CreateAsync().HandleAsync(harness.Command).ConfigureAwait(false);

            Assert.Contains(CommandStringTypes.DeleteFromStatus, harness.Prepared);
        }

        [TestMethod]
        public void Handle_WithTheStatusTableOff_LeavesItAlone()
        {
            var harness = new Harness(statusTable: false);

            harness.CreateSync().Handle(harness.Command);

            Assert.DoesNotContain(CommandStringTypes.DeleteFromStatus, harness.Prepared);
        }

        private sealed class Harness
        {
            private readonly IPrepareCommandHandler<DeleteMessageCommand<long>> _prepare;
            private readonly ITransportOptionsFactory _optionsFactory;
            private readonly IConnectionHeader<DbConnection, DbTransaction, DbCommand> _headers;

            public List<CommandStringTypes> Prepared { get; } = new List<CommandStringTypes>();
            public DeleteTransactionalMessageCommand Command { get; }

            public Harness(bool statusTable)
            {
                _prepare = Substitute.For<IPrepareCommandHandler<DeleteMessageCommand<long>>>();
                _prepare.When(x => x.Handle(Arg.Any<DeleteMessageCommand<long>>(), Arg.Any<DbCommand>(),
                        Arg.Any<CommandStringTypes>()))
                    .Do(info => Prepared.Add(info.ArgAt<CommandStringTypes>(2)));

                var options = Substitute.For<ITransportOptions>();
                options.EnableStatusTable.Returns(statusTable);
                _optionsFactory = Substitute.For<ITransportOptionsFactory>();
                _optionsFactory.Create().Returns(options);

                var holder = Substitute.For<IConnectionHolder<DbConnection, DbTransaction, DbCommand>>();
                holder.CreateCommand().Returns(Substitute.For<DbCommand>());

                var connectionHeader = Substitute.For<IMessageContextData<IConnectionHolder<DbConnection, DbTransaction, DbCommand>>>();
                _headers = Substitute.For<IConnectionHeader<DbConnection, DbTransaction, DbCommand>>();
                _headers.Connection.Returns(connectionHeader);

                var context = Substitute.For<IMessageContext>();
                context.Get(connectionHeader).Returns(holder);

                Command = new DeleteTransactionalMessageCommand(1L, context);
            }

            public DeleteTransactionalMessageCommandHandler<DbConnection, DbTransaction, DbCommand> CreateSync() =>
                new DeleteTransactionalMessageCommandHandler<DbConnection, DbTransaction, DbCommand>(
                    _optionsFactory, _headers, _prepare);

            public DeleteTransactionalMessageCommandHandlerAsync<DbConnection, DbTransaction, DbCommand> CreateAsync() =>
                new DeleteTransactionalMessageCommandHandlerAsync<DbConnection, DbTransaction, DbCommand>(
                    _optionsFactory, _headers, _prepare);
        }
    }
}
