using System;
using System.Collections.Generic;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardErrorRetriesQueryHandlerTests
    {
        [TestMethod]
        public void Handle_Returns_Retries_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(1);

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, reader).Returns(10L);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, reader).Returns(42L);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, reader).Returns("TimeoutException");
            readColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, reader).Returns(3);

            var result = handler.Handle(new GetDashboardErrorRetriesQuery("42"));

            Assert.ContainsSingle(result);
            Assert.AreEqual(10L, result[0].ErrorTrackingId);
            Assert.AreEqual("42", result[0].QueueId);
            Assert.AreEqual("TimeoutException", result[0].ExceptionType);
            Assert.AreEqual(3, result[0].RetryCount);
        }

        [TestMethod]
        public void Handle_Returns_Empty_List_When_No_Rows()
        {
            var (handler, _, _) = CreateHandler(0);

            var result = handler.Handle(new GetDashboardErrorRetriesQuery("42"));

            Assert.IsEmpty(result);
        }

        [TestMethod]
        public void Handle_Returns_Multiple_Retries_From_Reader()
        {
            var (handler, readColumn, reader) = CreateHandler(3);

            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, reader).Returns(10L, 20L, 30L);
            readColumn.ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, reader).Returns(100L, 200L, 300L);
            readColumn.ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, reader).Returns("ExA", "ExB", "ExC");
            readColumn.ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, reader).Returns(1, 2, 3);

            var result = handler.Handle(new GetDashboardErrorRetriesQuery("100"));

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(10L, result[0].ErrorTrackingId);
            Assert.AreEqual("100", result[0].QueueId);
            Assert.AreEqual("ExA", result[0].ExceptionType);
            Assert.AreEqual(1, result[0].RetryCount);
            Assert.AreEqual(30L, result[2].ErrorTrackingId);
            Assert.AreEqual("300", result[2].QueueId);
            Assert.AreEqual("ExC", result[2].ExceptionType);
            Assert.AreEqual(3, result[2].RetryCount);
            readColumn.Received(3).ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 0, reader);
            readColumn.Received(3).ReadAsInt64(CommandStringTypes.GetDashboardErrorRetries, 1, reader);
            readColumn.Received(3).ReadAsString(CommandStringTypes.GetDashboardErrorRetries, 2, reader);
            readColumn.Received(3).ReadAsInt32(CommandStringTypes.GetDashboardErrorRetries, 3, reader);
        }

        [TestMethod]
        public void Handle_Invokes_PrepareQuery_With_Correct_CommandString()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(false);

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardErrorRetriesQueryHandler(factory, prepareQuery, readColumn);
            var query = new GetDashboardErrorRetriesQuery("42");

            handler.Handle(query);

            prepareQuery.Received(1).Handle(query, command, CommandStringTypes.GetDashboardErrorRetries);
            connection.Received(1).Open();
        }

        [TestMethod]
        public void Constructor_Throws_When_DbConnectionFactory_Is_Null()
        {
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardErrorRetriesQueryHandler(null, prepareQuery, readColumn));
        }

        [TestMethod]
        public void Constructor_Throws_When_PrepareQuery_Is_Null()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var readColumn = Substitute.For<IReadColumn>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardErrorRetriesQueryHandler(factory, null, readColumn));
        }

        [TestMethod]
        public void Constructor_Throws_When_ReadColumn_Is_Null()
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                new GetDashboardErrorRetriesQueryHandler(factory, prepareQuery, null));
        }

        private static (IQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>> handler, IReadColumn readColumn, IDataReader reader) CreateHandler(int rowCount)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var prepareQuery = Substitute.For<IPrepareQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var readColumn = Substitute.For<IReadColumn>();

            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var reader = Substitute.For<IDataReader>();

            if (rowCount <= 0)
            {
                reader.Read().Returns(false);
            }
            else
            {
                var rest = new bool[rowCount];
                for (var i = 0; i < rowCount - 1; i++) rest[i] = true;
                rest[rowCount - 1] = false;
                reader.Read().Returns(true, rest);
            }

            factory.Create().Returns(connection);
            connection.CreateCommand().Returns(command);
            command.ExecuteReader().Returns(reader);

            var handler = new GetDashboardErrorRetriesQueryHandler(factory, prepareQuery, readColumn);
            return (handler, readColumn, reader);
        }
    }
}
