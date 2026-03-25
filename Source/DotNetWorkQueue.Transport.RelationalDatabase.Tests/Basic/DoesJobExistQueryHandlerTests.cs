using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class DoesJobExistQueryHandlerTests
    {
        [TestMethod]
        public void Handle_WithConnection_ReturnsNotQueued_WhenReaderHasNoRows()
        {
            var fixture = CreateFixture();

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            connection.CreateCommand().Returns(command);
            reader.Read().Returns(false);
            command.ExecuteReader().Returns(reader);

            // Table does not exist
            fixture.TableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(false);

            var query = new DoesJobExistQuery<IDbConnection, IDbTransaction>(
                "testJob", DateTimeOffset.UtcNow, connection, transaction);

            var result = fixture.Handler.Handle(query);

            Assert.AreEqual(QueueStatuses.NotQueued, result);
        }

        [TestMethod]
        public void Handle_WithConnection_ReturnsStatus_WhenReaderHasRow()
        {
            var fixture = CreateFixture();

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            connection.CreateCommand().Returns(command);
            reader.Read().Returns(true, false);
            command.ExecuteReader().Returns(reader);
            fixture.ReadColumn.ReadAsInt32(CommandStringTypes.DoesJobExist, 0, reader)
                .Returns((int)QueueStatuses.Waiting);

            var query = new DoesJobExistQuery<IDbConnection, IDbTransaction>(
                "testJob", DateTimeOffset.UtcNow, connection, transaction);

            var result = fixture.Handler.Handle(query);

            Assert.AreEqual(QueueStatuses.Waiting, result);
        }

        [TestMethod]
        public void Handle_WithConnection_ReturnsProcessed_WhenScheduleTimeMatches()
        {
            var fixture = CreateFixture();
            var scheduledTime = DateTimeOffset.UtcNow;

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var command = Substitute.For<IDbCommand>();

            // First reader: no rows (status query)
            var reader1 = Substitute.For<IDataReader>();
            reader1.Read().Returns(false);

            // Second reader: has row with matching schedule time
            var reader2 = Substitute.For<IDataReader>();
            reader2.Read().Returns(true, false);

            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader1, reader2);

            // Table exists
            fixture.TableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(true);
            fixture.ReadColumn.ReadAsDateTimeOffset(CommandStringTypes.GetJobLastScheduleTime, 0, reader2)
                .Returns(scheduledTime);

            var query = new DoesJobExistQuery<IDbConnection, IDbTransaction>(
                "testJob", scheduledTime, connection, transaction);

            var result = fixture.Handler.Handle(query);

            Assert.AreEqual(QueueStatuses.Processed, result);
        }

        [TestMethod]
        public void Handle_WithConnection_ReturnsNotQueued_WhenScheduleTimeDoesNotMatch()
        {
            var fixture = CreateFixture();
            var scheduledTime = DateTimeOffset.UtcNow;

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var command = Substitute.For<IDbCommand>();

            // First reader: no rows
            var reader1 = Substitute.For<IDataReader>();
            reader1.Read().Returns(false);

            // Second reader: has row with different schedule time
            var reader2 = Substitute.For<IDataReader>();
            reader2.Read().Returns(true, false);

            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader1, reader2);

            fixture.TableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(true);
            fixture.ReadColumn.ReadAsDateTimeOffset(CommandStringTypes.GetJobLastScheduleTime, 0, reader2)
                .Returns(scheduledTime.AddHours(1));

            var query = new DoesJobExistQuery<IDbConnection, IDbTransaction>(
                "testJob", scheduledTime, connection, transaction);

            var result = fixture.Handler.Handle(query);

            Assert.AreEqual(QueueStatuses.NotQueued, result);
        }

        [TestMethod]
        public void Handle_WithConnection_ReturnsNotQueued_WhenTableDoesNotExist()
        {
            var fixture = CreateFixture();

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            connection.CreateCommand().Returns(command);
            reader.Read().Returns(false);
            command.ExecuteReader().Returns(reader);

            // Table does not exist
            fixture.TableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(false);

            var query = new DoesJobExistQuery<IDbConnection, IDbTransaction>(
                "testJob", DateTimeOffset.UtcNow, connection, transaction);

            var result = fixture.Handler.Handle(query);

            Assert.AreEqual(QueueStatuses.NotQueued, result);
        }

        [TestMethod]
        public void Handle_WithConnection_ReturnsNotQueued_WhenJobTableExistsButNoScheduleRow()
        {
            var fixture = CreateFixture();

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var command = Substitute.For<IDbCommand>();

            // First reader: no rows
            var reader1 = Substitute.For<IDataReader>();
            reader1.Read().Returns(false);

            // Second reader: no rows in job table either
            var reader2 = Substitute.For<IDataReader>();
            reader2.Read().Returns(false);

            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader1, reader2);

            fixture.TableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(true);

            var query = new DoesJobExistQuery<IDbConnection, IDbTransaction>(
                "testJob", DateTimeOffset.UtcNow, connection, transaction);

            var result = fixture.Handler.Handle(query);

            Assert.AreEqual(QueueStatuses.NotQueued, result);
        }

        [TestMethod]
        public void Handle_WithoutConnection_CreatesConnectionAndTransaction()
        {
            var fixture = CreateFixture();

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var transactionWrapper = Substitute.For<ITransactionWrapper>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            fixture.DbConnectionFactory.Create().Returns(connection);
            fixture.TransactionFactory.Create(connection).Returns(transactionWrapper);
            transactionWrapper.BeginTransaction().Returns(transaction);
            connection.CreateCommand().Returns(command);
            reader.Read().Returns(false);
            command.ExecuteReader().Returns(reader);

            // Table doesn't exist
            fixture.TableExists.Handle(Arg.Any<GetTableExistsQuery>()).Returns(false);

            var query = new DoesJobExistQuery<IDbConnection, IDbTransaction>(
                "testJob", DateTimeOffset.UtcNow);

            var result = fixture.Handler.Handle(query);

            Assert.AreEqual(QueueStatuses.NotQueued, result);
            connection.Received(1).Open();
            fixture.DbConnectionFactory.Received(1).Create();
        }

        [TestMethod]
        public void Handle_NonQueuedStatus_SkipsJobTableCheck()
        {
            var fixture = CreateFixture();

            var connection = Substitute.For<IDbConnection>();
            var transaction = Substitute.For<IDbTransaction>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            connection.CreateCommand().Returns(command);
            reader.Read().Returns(true, false);
            command.ExecuteReader().Returns(reader);
            // Return Processing status (not NotQueued)
            fixture.ReadColumn.ReadAsInt32(CommandStringTypes.DoesJobExist, 0, reader)
                .Returns((int)QueueStatuses.Processing);

            var query = new DoesJobExistQuery<IDbConnection, IDbTransaction>(
                "testJob", DateTimeOffset.UtcNow, connection, transaction);

            var result = fixture.Handler.Handle(query);

            Assert.AreEqual(QueueStatuses.Processing, result);
            // Table exists check should not have been called
            fixture.TableExists.DidNotReceive().Handle(Arg.Any<GetTableExistsQuery>());
        }

        private TestFixture CreateFixture()
        {
            var commandCache = new DoesJobExistCommandStringCache();
            var connectionInfo = Substitute.For<IConnectionInformation>();
            connectionInfo.ConnectionString.Returns("Server=test");
            var tableExists = Substitute.For<IQueryHandler<GetTableExistsQuery, bool>>();
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.JobTableName.Returns("JobTable");
            var dbConnectionFactory = Substitute.For<IDbConnectionFactory>();
            var transactionFactory = Substitute.For<ITransactionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses>>();
            var readColumn = Substitute.For<IReadColumn>();

            var handler = new DoesJobExistQueryHandler<IDbConnection, IDbTransaction>(
                commandCache,
                connectionInfo,
                tableExists,
                tableNameHelper,
                dbConnectionFactory,
                transactionFactory,
                prepareQuery,
                readColumn);

            return new TestFixture
            {
                Handler = handler,
                CommandCache = commandCache,
                ConnectionInfo = connectionInfo,
                TableExists = tableExists,
                TableNameHelper = tableNameHelper,
                DbConnectionFactory = dbConnectionFactory,
                TransactionFactory = transactionFactory,
                PrepareQuery = prepareQuery,
                ReadColumn = readColumn
            };
        }

        private class DoesJobExistCommandStringCache : CommandStringCache
        {
            public DoesJobExistCommandStringCache() : base(Substitute.For<ITableNameHelper>())
            {
            }

            protected override void BuildCommands()
            {
                CommandCache[CommandStringTypes.DoesJobExist] = "SELECT status FROM meta WHERE JobName = @JobName";
                CommandCache[CommandStringTypes.GetJobLastScheduleTime] = "SELECT ScheduledTime FROM jobs WHERE JobName = @JobName";
            }
        }

        private class TestFixture
        {
            public DoesJobExistQueryHandler<IDbConnection, IDbTransaction> Handler { get; set; }
            public CommandStringCache CommandCache { get; set; }
            public IConnectionInformation ConnectionInfo { get; set; }
            public IQueryHandler<GetTableExistsQuery, bool> TableExists { get; set; }
            public ITableNameHelper TableNameHelper { get; set; }
            public IDbConnectionFactory DbConnectionFactory { get; set; }
            public ITransactionFactory TransactionFactory { get; set; }
            public IPrepareQueryHandler<DoesJobExistQuery<IDbConnection, IDbTransaction>, QueueStatuses> PrepareQuery { get; set; }
            public IReadColumn ReadColumn { get; set; }
        }
    }
}
