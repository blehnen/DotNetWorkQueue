using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class WriteMessageHistoryHandlerTests
    {
        [TestMethod]
        public void RecordEnqueue_When_Disabled_Does_Not_Open_Connection()
        {
            var (handler, factory, _) = Create(enabled: false);
            handler.RecordEnqueue("q1", "c1", null, null, null, null);
            factory.DidNotReceive().Create();
        }

        [TestMethod]
        public void RecordEnqueue_When_Enabled_Opens_Connection_And_Executes()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);

            handler.RecordEnqueue("q1", "c1", "route1", "MyType", new byte[] { 1 }, new byte[] { 2 });

            connection.Received(1).Open();
            command.Received(1).ExecuteNonQuery();
        }

        [TestMethod]
        public void RecordProcessingStart_When_Disabled_Does_Not_Open_Connection()
        {
            var (handler, factory, _) = Create(enabled: false);
            handler.RecordProcessingStart("q1");
            factory.DidNotReceive().Create();
        }

        [TestMethod]
        public void RecordProcessingStart_When_Enabled_Opens_Connection_And_Executes()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);

            handler.RecordProcessingStart("q1");

            connection.Received(1).Open();
            command.Received().ExecuteNonQuery();
        }

        [TestMethod]
        public void RecordComplete_When_Disabled_Does_Not_Open_Connection()
        {
            var (handler, factory, _) = Create(enabled: false);
            handler.RecordComplete("q1");
            factory.DidNotReceive().Create();
        }

        [TestMethod]
        public void RecordComplete_When_Enabled_Opens_Connection_And_Executes()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);

            // GetStartedUtc also calls ExecuteScalar via CreateCommand
            command.ExecuteScalar().Returns(System.DBNull.Value);

            handler.RecordComplete("q1");

            connection.Received(1).Open();
            command.Received().ExecuteNonQuery();
        }

        [TestMethod]
        public void RecordError_When_Disabled_Does_Not_Open_Connection()
        {
            var (handler, factory, _) = Create(enabled: false);
            handler.RecordError("q1", "Some error");
            factory.DidNotReceive().Create();
        }

        [TestMethod]
        public void RecordError_When_Enabled_Opens_Connection_And_Executes()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);

            // GetStartedUtc calls ExecuteScalar
            command.ExecuteScalar().Returns(System.DBNull.Value);

            handler.RecordError("q1", "Some error");

            connection.Received(1).Open();
            command.Received().ExecuteNonQuery();
        }

        [TestMethod]
        public void RecordRollback_When_Disabled_Does_Not_Open_Connection()
        {
            var (handler, factory, _) = Create(enabled: false);
            handler.RecordRollback("q1");
            factory.DidNotReceive().Create();
        }

        [TestMethod]
        public void RecordRollback_When_Enabled_Opens_Connection_And_Executes()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);

            handler.RecordRollback("q1");

            connection.Received(1).Open();
            command.Received(1).ExecuteNonQuery();
        }

        [TestMethod]
        public void RecordDelete_When_Disabled_Does_Not_Open_Connection()
        {
            var (handler, factory, _) = Create(enabled: false);
            handler.RecordDelete("q1");
            factory.DidNotReceive().Create();
        }

        [TestMethod]
        public void RecordDelete_When_Enabled_Opens_Connection_And_Executes()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);

            handler.RecordDelete("q1");

            connection.Received(1).Open();
            command.Received(1).ExecuteNonQuery();
        }

        [TestMethod]
        public void RecordExpire_When_Disabled_Does_Not_Open_Connection()
        {
            var (handler, factory, _) = Create(enabled: false);
            handler.RecordExpire("q1");
            factory.DidNotReceive().Create();
        }

        [TestMethod]
        public void RecordExpire_When_Enabled_Opens_Connection_And_Executes()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);

            handler.RecordExpire("q1");

            connection.Received(1).Open();
            command.Received(1).ExecuteNonQuery();
        }

        private static (WriteMessageHistoryHandler handler, IDbConnectionFactory factory, IHistoryConfiguration config)
            Create(bool enabled = false)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.HistoryName.Returns("TestHistory");
            var config = Substitute.For<IHistoryConfiguration>();
            config.Enabled.Returns(enabled);
            config.StoreBody.Returns(false);
            return (new WriteMessageHistoryHandler(factory, tableNameHelper, config), factory, config);
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
