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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Tests.TestHelpers;
using DotNetWorkQueue.Transport.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.CommandHandler
{
    [TestClass]
    public class CreateJobTablesCommandHandlerTests
    {
        [TestMethod]
        public void Handle_OpensConnection_AndReturnsSuccess()
        {
            var (handler, fixture, prepare, command) = CreateHandler();

            var result = handler.Handle(command);

            Assert.IsNotNull(result);
            Assert.AreEqual(QueueCreationStatus.Success, result.Status);
            fixture.ConnectionFactory.Received(1).Create();
            fixture.Connection.Received(1).Open();
            _ = prepare; // suppress unused warning
        }

        [TestMethod]
        public void Handle_CallsPrepareCommandHandler_WithCreateJobTablesCommandType()
        {
            var (handler, fixture, prepare, command) = CreateHandler();

            handler.Handle(command);

            prepare.Received(1).Handle(
                command,
                fixture.Command,
                CommandStringTypes.CreateJobTables);
        }

        [TestMethod]
        public void Handle_ExecutesNonQuery_OnCommand()
        {
            var (handler, fixture, _, command) = CreateHandler();

            handler.Handle(command);

            fixture.Command.Received(1).ExecuteNonQuery();
        }

        [TestMethod]
        public void Handle_CommitsTransaction()
        {
            var (handler, fixture, _, command) = CreateHandler();

            handler.Handle(command);

            fixture.TransactionFactory.Received(1).Create(fixture.Connection);
            fixture.TransactionWrapper.Received(1).BeginTransaction();
            fixture.Transaction.Received(1).Commit();
            fixture.Command.Received().Transaction = fixture.Transaction;
        }

        [TestMethod]
        public void Constructor_NullDbConnectionFactory_Throws()
        {
            var prepare = Substitute.For<IPrepareCommandHandler<CreateJobTablesCommand<ITable>>>();
            var transactionFactory = Substitute.For<ITransactionFactory>();

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
                _ = new CreateJobTablesCommandHandler(null, prepare, transactionFactory));
            Assert.AreEqual("dbConnectionFactory", ex.ParamName);
        }

        [TestMethod]
        public void Constructor_NullPrepareCommandHandler_Throws()
        {
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var transactionFactory = Substitute.For<ITransactionFactory>();

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
                _ = new CreateJobTablesCommandHandler(dbConnectionFactory, null, transactionFactory));
            Assert.AreEqual("prepareCommandHandler", ex.ParamName);
        }

        [TestMethod]
        public void Constructor_NullTransactionFactory_Throws()
        {
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var prepare = Substitute.For<IPrepareCommandHandler<CreateJobTablesCommand<ITable>>>();

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
                _ = new CreateJobTablesCommandHandler(dbConnectionFactory, prepare, null));
            Assert.AreEqual("transactionFactory", ex.ParamName);
        }

        private static (CreateJobTablesCommandHandler handler,
                        AdoNetMockFixture fixture,
                        IPrepareCommandHandler<CreateJobTablesCommand<ITable>> prepare,
                        CreateJobTablesCommand<ITable> command) CreateHandler()
        {
            var fixture = AdoNetMockFixture.Create(withTransaction: true);
            var prepareCommandHandler = Substitute.For<IPrepareCommandHandler<CreateJobTablesCommand<ITable>>>();

            var handler = new CreateJobTablesCommandHandler(
                fixture.ConnectionFactory,
                prepareCommandHandler,
                fixture.TransactionFactory);

            var table = Substitute.For<ITable>();
            var command = new CreateJobTablesCommand<ITable>(new List<ITable> { table });

            return (handler, fixture, prepareCommandHandler, command);
        }
    }
}
