using System.Data;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class QueryMessageHistoryHandlerTests
    {
        [TestMethod]
        public void Get_When_Disabled_Returns_Empty_List()
        {
            var (handler, factory, _) = Create(enabled: false);
            var result = handler.Get(0, 10, null);
            Assert.AreEqual(0, result.Count);
            factory.DidNotReceive().Create();
        }

        [TestMethod]
        public void GetByQueueId_When_Disabled_Returns_Null()
        {
            var (handler, factory, _) = Create(enabled: false);
            var result = handler.GetByQueueId("q1");
            Assert.IsNull(result);
            factory.DidNotReceive().Create();
        }

        [TestMethod]
        public void GetCount_When_Disabled_Returns_Zero()
        {
            var (handler, factory, _) = Create(enabled: false);
            var result = handler.GetCount(null);
            Assert.AreEqual(0L, result);
            factory.DidNotReceive().Create();
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

        private static (QueryMessageHistoryHandler handler, IDbConnectionFactory factory, IHistoryConfiguration config)
            Create(bool enabled = false)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.HistoryName.Returns("TestHistory");
            var config = Substitute.For<IHistoryConfiguration>();
            config.Enabled.Returns(enabled);
            config.StoreBody.Returns(false);
            return (new QueryMessageHistoryHandler(factory, tableNameHelper, config), factory, config);
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
    }
}
