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
using System.Data;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.CommandHandler
{
    [TestClass]
    public class SetJobLastKnownEventCommandHandlerTests
    {
        [TestMethod]
        public void Constructor_NullCommandCache_Throws()
        {
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new SetJobLastKnownEventCommandHandler(null, dbConnectionFactory));
        }

        [TestMethod]
        public void Constructor_NullDbConnectionFactory_Throws()
        {
            var commandCache = CreateCommandCache();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new SetJobLastKnownEventCommandHandler(commandCache, null));
        }

        [TestMethod]
        public void Constructor_ValidArgs_Succeeds()
        {
            var commandCache = CreateCommandCache();
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var handler = new SetJobLastKnownEventCommandHandler(commandCache, dbConnectionFactory);
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Handle_HappyPath_OpensConnectionAndExecutes()
        {
            var commandCache = CreateCommandCache();
            var (factory, connection, dbCommand, _) = CreateMockedFactory();
            var handler = new SetJobLastKnownEventCommandHandler(commandCache, factory);

            var command = CreateCommand("TestJob");

            handler.Handle(command);

            connection.Received(1).Open();
            dbCommand.Received(1).ExecuteNonQuery();
            connection.Received(1).Dispose();
            dbCommand.Received(1).Dispose();
        }

        [TestMethod]
        public void Handle_SetsCommandText_FromCache()
        {
            var commandCache = CreateCommandCache();
            var (factory, _, dbCommand, _) = CreateMockedFactory();
            var handler = new SetJobLastKnownEventCommandHandler(commandCache, factory);

            var command = CreateCommand("TestJob");

            handler.Handle(command);

            var expected = commandCache.GetCommand(CommandStringTypes.SetJobLastKnownEvent);
            dbCommand.Received(1).CommandText = expected;
        }

        [TestMethod]
        public void Handle_SetsParameters_AddsThreeToCollection()
        {
            var commandCache = CreateCommandCache();
            var (factory, _, dbCommand, parameters) = CreateMockedFactory();
            var handler = new SetJobLastKnownEventCommandHandler(commandCache, factory);

            var command = CreateCommand("TestJob");

            handler.Handle(command);

            parameters.Received(3).Add(Arg.Any<IDbDataParameter>());
            dbCommand.Received(3).CreateParameter();
        }

        [TestMethod]
        public void Handle_SetsParameters_NamesTypesAndValues()
        {
            var commandCache = CreateCommandCache();

            var connection = Substitute.For<IDbConnection>();
            var dbCommand = Substitute.For<IDbCommand>();
            var parameters = Substitute.For<IDataParameterCollection>();
            dbCommand.Parameters.Returns(parameters);

            var paramJobName = Substitute.For<IDbDataParameter>();
            var paramEventTime = Substitute.For<IDbDataParameter>();
            var paramScheduledTime = Substitute.For<IDbDataParameter>();
            dbCommand.CreateParameter().Returns(paramJobName, paramEventTime, paramScheduledTime);

            connection.CreateCommand().Returns(dbCommand);
            var factory = Substitute.For<IDbConnectionFactory>();
            factory.Create().Returns(connection);

            var handler = new SetJobLastKnownEventCommandHandler(commandCache, factory);

            var eventTime = new DateTimeOffset(2026, 3, 25, 10, 0, 0, TimeSpan.Zero);
            var scheduledTime = new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero);
            var command = new SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>(
                "MyJob", eventTime, scheduledTime, null, null);

            handler.Handle(command);

            paramJobName.Received(1).ParameterName = "@JobName";
            paramJobName.Received(1).DbType = DbType.AnsiString;
            paramJobName.Received(1).Value = "MyJob";

            paramEventTime.Received(1).ParameterName = "@JobEventTime";
            paramEventTime.Received(1).DbType = DbType.DateTimeOffset;
            paramEventTime.Received(1).Value = eventTime;

            paramScheduledTime.Received(1).ParameterName = "@JobScheduledTime";
            paramScheduledTime.Received(1).DbType = DbType.DateTimeOffset;
            paramScheduledTime.Received(1).Value = scheduledTime;
        }

        private static (IDbConnectionFactory factory, IDbConnection connection, IDbCommand command, IDataParameterCollection parameters) CreateMockedFactory()
        {
            var connection = Substitute.For<IDbConnection>();
            var dbCommand = Substitute.For<IDbCommand>();
            var parameters = Substitute.For<IDataParameterCollection>();
            dbCommand.Parameters.Returns(parameters);
            dbCommand.CreateParameter().Returns(
                _ => Substitute.For<IDbDataParameter>(),
                _ => Substitute.For<IDbDataParameter>(),
                _ => Substitute.For<IDbDataParameter>());
            connection.CreateCommand().Returns(dbCommand);

            var factory = Substitute.For<IDbConnectionFactory>();
            factory.Create().Returns(connection);

            return (factory, connection, dbCommand, parameters);
        }

        private static SetJobLastKnownEventCommand<SqlConnection, SqlTransaction> CreateCommand(string jobName)
        {
            return new SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>(
                jobName,
                new DateTimeOffset(2026, 3, 25, 10, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero),
                null,
                null);
        }

        private static SqlServerCommandStringCache CreateCommandCache()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var connection = fixture.Create<IConnectionInformation>();
            connection.QueueName.Returns("TestQueue");
            fixture.Inject(connection);
            return fixture.Create<SqlServerCommandStringCache>();
        }
    }
}
