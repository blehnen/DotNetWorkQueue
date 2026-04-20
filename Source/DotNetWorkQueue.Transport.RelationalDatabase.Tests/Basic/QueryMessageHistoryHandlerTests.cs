using System.Data;
using System.Data.Common;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class QueryMessageHistoryHandlerTests
    {
        // After the EnableHistory read-path guard was removed, these methods rely on a
        // DbException catch to handle the case where the history table was never created.
        // We simulate that by throwing DbException from ExecuteReader / ExecuteScalar.

        [TestMethod]
        public void Get_When_History_Table_Missing_Returns_Empty_List()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (_, command) = SetupConnection(factory);
            command.When(c => c.ExecuteReader()).Do(_ => throw new FakeDbException());

            var result = handler.Get(0, 10, null);

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetByQueueId_When_History_Table_Missing_Returns_Null()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (_, command) = SetupConnection(factory);
            command.When(c => c.ExecuteReader()).Do(_ => throw new FakeDbException());

            var result = handler.GetByQueueId("q1");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetCount_When_History_Table_Missing_Returns_Zero()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (_, command) = SetupConnection(factory);
            command.When(c => c.ExecuteScalar()).Do(_ => throw new FakeDbException());

            var result = handler.GetCount(null);

            Assert.AreEqual(0L, result);
        }

        [TestMethod]
        public void GetCount_When_Enabled_Opens_Connection()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);
            command.ExecuteScalar().Returns(5L);

            var result = handler.GetCount(null);

            Assert.AreEqual(5L, result);
            connection.Received(1).Open();
            command.Received(1).ExecuteScalar();
        }

        [TestMethod]
        public void GetCount_With_StatusFilter_When_Enabled_Opens_Connection()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);
            command.ExecuteScalar().Returns(2L);

            var result = handler.GetCount(MessageHistoryStatus.Error);

            Assert.AreEqual(2L, result);
            connection.Received(1).Open();
        }

        [TestMethod]
        public void Get_When_Enabled_Opens_Connection()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(false);
            command.ExecuteReader().Returns(reader);

            var result = handler.Get(0, 10, null);

            Assert.AreEqual(0, result.Count);
            connection.Received(1).Open();
            command.Received(1).ExecuteReader();
        }

        [TestMethod]
        public void GetByQueueId_When_Enabled_Opens_Connection()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(false);
            command.ExecuteReader().Returns(reader);

            var result = handler.GetByQueueId("q1");

            Assert.IsNull(result);
            connection.Received(1).Open();
            command.Received(1).ExecuteReader();
        }

        private static (QueryMessageHistoryHandler handler, IDbConnectionFactory factory, IBaseTransportOptions options)
            Create(bool enabled = false)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.HistoryName.Returns("TestHistory");
            var historyOptions = Substitute.For<IHistoryTransportOptions>();
            historyOptions.StoreBody.Returns(false);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(enabled);
            options.HistoryOptions.Returns(historyOptions);
            var pagination = new LimitOffsetPaginationSyntax();
            return (new QueryMessageHistoryHandler(factory, tableNameHelper, options, pagination), factory, options);
        }

        private static (IDbConnection connection, IDbCommand command) SetupConnection(IDbConnectionFactory factory)
        {
            var connection = Substitute.For<IDbConnection>();
            var command = Substitute.For<IDbCommand>();
            var parameters = Substitute.For<IDataParameterCollection>();
            var parameter = Substitute.For<IDbDataParameter>();
            command.CreateParameter().Returns(parameter);
            command.Parameters.Returns(parameters);
            connection.CreateCommand().Returns(command);
            factory.Create().Returns(connection);
            return (connection, command);
        }

        private sealed class FakeDbException : DbException
        {
            public FakeDbException() : base("simulated: history table does not exist") { }
        }
    }
}
