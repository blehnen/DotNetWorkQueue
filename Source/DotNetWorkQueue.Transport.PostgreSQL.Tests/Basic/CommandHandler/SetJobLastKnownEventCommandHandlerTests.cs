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
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NSubstitute;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.CommandHandler
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
            var fixture = CreateHandleFixture();

            fixture.Handler.Handle(CreateCommand());

            fixture.DbConnectionFactory.Received(1).Create();
            fixture.Connection.Received(1).Open();
            fixture.Command.Received(1).ExecuteNonQuery();
        }

        [TestMethod]
        public void Handle_SetsCommandText_FromCache()
        {
            var fixture = CreateHandleFixture();
            var expectedSql = fixture.CommandCache.GetCommand(CommandStringTypes.SetJobLastKnownEvent);

            fixture.Handler.Handle(CreateCommand());

            Assert.AreEqual(expectedSql, fixture.Command.CommandText);
        }

        [TestMethod]
        public void Handle_SetsParameters_NamesTypesAndValues()
        {
            var fixture = CreateHandleFixture();
            var jobName = "TestJob";
            var jobEventTime = new DateTimeOffset(2026, 4, 12, 10, 30, 0, TimeSpan.Zero);
            var jobScheduledTime = new DateTimeOffset(2026, 4, 12, 10, 0, 0, TimeSpan.Zero);
            var command = new SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>(
                jobName, jobEventTime, jobScheduledTime, null, null);

            fixture.Handler.Handle(command);

            Assert.HasCount(3, fixture.Parameters);
            // @JobName: AnsiString with the supplied name
            Assert.AreEqual("@JobName", fixture.Parameters[0].ParameterName);
            Assert.AreEqual(DbType.AnsiString, fixture.Parameters[0].DbType);
            Assert.AreEqual(jobName, fixture.Parameters[0].Value);
            // @JobEventTime: Int64 with UtcDateTime.Ticks
            Assert.AreEqual("@JobEventTime", fixture.Parameters[1].ParameterName);
            Assert.AreEqual(DbType.Int64, fixture.Parameters[1].DbType);
            Assert.AreEqual(jobEventTime.UtcDateTime.Ticks, fixture.Parameters[1].Value);
            // @JobScheduledTime: Int64 with UtcDateTime.Ticks
            Assert.AreEqual("@JobScheduledTime", fixture.Parameters[2].ParameterName);
            Assert.AreEqual(DbType.Int64, fixture.Parameters[2].DbType);
            Assert.AreEqual(jobScheduledTime.UtcDateTime.Ticks, fixture.Parameters[2].Value);
        }

        private static SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction> CreateCommand()
        {
            return new SetJobLastKnownEventCommand<NpgsqlConnection, NpgsqlTransaction>(
                "JobName",
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                null,
                null);
        }

        private sealed class HandleFixture
        {
            public SetJobLastKnownEventCommandHandler Handler { get; set; }
            public PostgreSqlCommandStringCache CommandCache { get; set; }
            public IDbConnectionFactory DbConnectionFactory { get; set; }
            public IDbConnection Connection { get; set; }
            public IDbCommand Command { get; set; }
            public System.Collections.Generic.List<IDbDataParameter> Parameters { get; set; }
        }

        private static HandleFixture CreateHandleFixture()
        {
            var commandCache = CreateCommandCache();
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var parametersList = new System.Collections.Generic.List<IDbDataParameter>();
            var parameters = Substitute.For<IDataParameterCollection>();
            parameters.Add(Arg.Do<object>(p => parametersList.Add((IDbDataParameter)p)));

            command.Parameters.Returns(parameters);
            command.CreateParameter().Returns(_ => Substitute.For<IDbDataParameter>());
            connection.CreateCommand().Returns(command);
            dbConnectionFactory.Create().Returns(connection);

            var handler = new SetJobLastKnownEventCommandHandler(commandCache, dbConnectionFactory);

            return new HandleFixture
            {
                Handler = handler,
                CommandCache = commandCache,
                DbConnectionFactory = dbConnectionFactory,
                Connection = connection,
                Command = command,
                Parameters = parametersList
            };
        }

        private static PostgreSqlCommandStringCache CreateCommandCache()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var connection = fixture.Create<IConnectionInformation>();
            connection.QueueName.Returns("TestQueue");
            fixture.Inject(connection);
            return fixture.Create<PostgreSqlCommandStringCache>();
        }
    }
}
