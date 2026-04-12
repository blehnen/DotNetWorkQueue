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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
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
            var fixture = CreateFixture();

            var result = fixture.Handler.Handle(fixture.Command);

            Assert.IsNotNull(result);
            Assert.AreEqual(QueueCreationStatus.Success, result.Status);
            fixture.DbConnectionFactory.Received(1).Create();
            fixture.Connection.Received(1).Open();
        }

        [TestMethod]
        public void Handle_CallsPrepareCommandHandler_WithCreateJobTablesCommandType()
        {
            var fixture = CreateFixture();

            fixture.Handler.Handle(fixture.Command);

            fixture.PrepareCommandHandler.Received(1).Handle(
                fixture.Command,
                fixture.DbCommand,
                CommandStringTypes.CreateJobTables);
        }

        [TestMethod]
        public void Handle_ExecutesNonQuery_OnCommand()
        {
            var fixture = CreateFixture();

            fixture.Handler.Handle(fixture.Command);

            fixture.DbCommand.Received(1).ExecuteNonQuery();
        }

        [TestMethod]
        public void Handle_CommitsTransaction()
        {
            var fixture = CreateFixture();

            fixture.Handler.Handle(fixture.Command);

            fixture.TransactionFactory.Received(1).Create(fixture.Connection);
            fixture.TransactionWrapper.Received(1).BeginTransaction();
            fixture.Transaction.Received(1).Commit();
            fixture.DbCommand.Received().Transaction = fixture.Transaction;
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

        private TestFixture CreateFixture()
        {
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var prepareCommandHandler = Substitute.For<IPrepareCommandHandler<CreateJobTablesCommand<ITable>>>();
            var transactionFactory = Substitute.For<ITransactionFactory>();

            var connection = Substitute.For<IDbConnection>();
            var transactionWrapper = Substitute.For<ITransactionWrapper>();
            var transaction = Substitute.For<IDbTransaction>();
            var dbCommand = Substitute.For<IDbCommand>();

            dbConnectionFactory.Create().Returns(connection);
            transactionFactory.Create(connection).Returns(transactionWrapper);
            transactionWrapper.BeginTransaction().Returns(transaction);
            connection.CreateCommand().Returns(dbCommand);

            var handler = new CreateJobTablesCommandHandler(
                dbConnectionFactory,
                prepareCommandHandler,
                transactionFactory);

            var table = Substitute.For<ITable>();
            var command = new CreateJobTablesCommand<ITable>(new List<ITable> { table });

            return new TestFixture
            {
                Handler = handler,
                DbConnectionFactory = dbConnectionFactory,
                PrepareCommandHandler = prepareCommandHandler,
                TransactionFactory = transactionFactory,
                Connection = connection,
                TransactionWrapper = transactionWrapper,
                Transaction = transaction,
                DbCommand = dbCommand,
                Command = command
            };
        }

        private class TestFixture
        {
            public CreateJobTablesCommandHandler Handler { get; set; }
            public IDbConnectionFactory DbConnectionFactory { get; set; }
            public IPrepareCommandHandler<CreateJobTablesCommand<ITable>> PrepareCommandHandler { get; set; }
            public ITransactionFactory TransactionFactory { get; set; }
            public IDbConnection Connection { get; set; }
            public ITransactionWrapper TransactionWrapper { get; set; }
            public IDbTransaction Transaction { get; set; }
            public IDbCommand DbCommand { get; set; }
            public CreateJobTablesCommand<ITable> Command { get; set; }
        }
    }
}
