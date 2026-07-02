using System;
using System.Data;
using System.Globalization;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic.CommandHandler
{
    [TestClass]
    public class SetJobLastKnownEventCommandHandlerTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var commandCache = CreateCommandCache();
            var handler = new SetJobLastKnownEventCommandHandler(commandCache);
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Create_Null_CommandCache_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new SetJobLastKnownEventCommandHandler(null));
        }

        [TestMethod]
        public void Handle_Sets_Parameters_And_Executes()
        {
            var commandCache = CreateCommandCache();
            var handler = new SetJobLastKnownEventCommandHandler(commandCache);

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var dbCommand = Substitute.For<IDbCommand>();
            var parameters = Substitute.For<IDataParameterCollection>();
            dbCommand.Parameters.Returns(parameters);
            dbCommand.CreateParameter().Returns(
                _ => Substitute.For<IDbDataParameter>(),
                _ => Substitute.For<IDbDataParameter>(),
                _ => Substitute.For<IDbDataParameter>());
            connection.CreateCommand().Returns(dbCommand);

            var eventTime = new DateTimeOffset(2026, 3, 25, 10, 0, 0, TimeSpan.Zero);
            var scheduledTime = new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero);

            var command = new SetJobLastKnownEventCommand<IDbConnection, IDbTransaction>(
                "TestJob", eventTime, scheduledTime, connection, transaction);

            handler.Handle(command);

            dbCommand.Received(1).ExecuteNonQuery();
            dbCommand.Received(1).Transaction = transaction;
            parameters.Received(3).Add(Arg.Any<IDbDataParameter>());
        }

        [TestMethod]
        public void Handle_Sets_Correct_Parameter_Names()
        {
            var commandCache = CreateCommandCache();
            var handler = new SetJobLastKnownEventCommandHandler(commandCache);

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var dbCommand = Substitute.For<IDbCommand>();
            var parameters = Substitute.For<IDataParameterCollection>();
            dbCommand.Parameters.Returns(parameters);

            var param1 = Substitute.For<IDbDataParameter>();
            var param2 = Substitute.For<IDbDataParameter>();
            var param3 = Substitute.For<IDbDataParameter>();
            var callCount = 0;
            dbCommand.CreateParameter().Returns(_ =>
            {
                callCount++;
                switch (callCount)
                {
                    case 1: return param1;
                    case 2: return param2;
                    default: return param3;
                }
            });
            connection.CreateCommand().Returns(dbCommand);

            var eventTime = new DateTimeOffset(2026, 3, 25, 10, 0, 0, TimeSpan.Zero);
            var scheduledTime = new DateTimeOffset(2026, 3, 25, 9, 0, 0, TimeSpan.Zero);

            var command = new SetJobLastKnownEventCommand<IDbConnection, IDbTransaction>(
                "MyJob", eventTime, scheduledTime, connection, transaction);

            handler.Handle(command);

            param1.Received(1).ParameterName = "@JobName";
            param1.Received(1).DbType = DbType.AnsiString;
            param1.Received(1).Value = "MyJob";

            param2.Received(1).ParameterName = "@JobEventTime";
            param2.Received(1).DbType = DbType.AnsiString;
            param2.Received(1).Value = eventTime.ToString("o", CultureInfo.InvariantCulture);

            param3.Received(1).ParameterName = "@JobScheduledTime";
            param3.Received(1).DbType = DbType.AnsiString;
            param3.Received(1).Value = scheduledTime.ToString("o", CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void Handle_Uses_Command_From_Cache()
        {
            var commandCache = CreateCommandCache();
            var handler = new SetJobLastKnownEventCommandHandler(commandCache);

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var dbCommand = Substitute.For<IDbCommand>();
            var parameters = Substitute.For<IDataParameterCollection>();
            dbCommand.Parameters.Returns(parameters);
            dbCommand.CreateParameter().Returns(_ => Substitute.For<IDbDataParameter>());
            connection.CreateCommand().Returns(dbCommand);

            var command = new SetJobLastKnownEventCommand<IDbConnection, IDbTransaction>(
                "TestJob",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                connection,
                transaction);

            handler.Handle(command);

            // Verify the command text was set from the cache
            var expectedCommand = commandCache.GetCommand(CommandStringTypes.SetJobLastKnownEvent);
            dbCommand.Received(1).CommandText = expectedCommand;
        }

        [TestMethod]
        public void Handle_Disposes_DbCommand()
        {
            var commandCache = CreateCommandCache();
            var handler = new SetJobLastKnownEventCommandHandler(commandCache);

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var dbCommand = Substitute.For<IDbCommand>();
            var parameters = Substitute.For<IDataParameterCollection>();
            dbCommand.Parameters.Returns(parameters);
            dbCommand.CreateParameter().Returns(_ => Substitute.For<IDbDataParameter>());
            connection.CreateCommand().Returns(dbCommand);

            var command = new SetJobLastKnownEventCommand<IDbConnection, IDbTransaction>(
                "TestJob", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, connection, transaction);

            handler.Handle(command);

            dbCommand.Received(1).Dispose();
        }

        private static IDbCommandStringCache CreateCommandCache()
        {
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.QueueName.Returns("testQueue");
            tableNameHelper.MetaDataName.Returns("testQueueMetaData");
            tableNameHelper.StatusName.Returns("testQueueStatus");
            tableNameHelper.ConfigurationName.Returns("testQueueConfig");
            tableNameHelper.ErrorTrackingName.Returns("testQueueErrorTracking");
            tableNameHelper.MetaDataErrorsName.Returns("testQueueMetaDataErrors");
            tableNameHelper.JobTableName.Returns("DNWQJobs");
            tableNameHelper.HistoryName.Returns("testQueueHistory");

            return new IDbCommandStringCache(tableNameHelper);
        }
    }
}
