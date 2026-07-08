using System;
using System.Data;
using System.Data.Common;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class PurgeMessageHistoryHandlerTests
    {
        [TestMethod]
        public void Purge_When_History_Table_Missing_Returns_Zero()
        {
            // After the EnableHistory read-path guard was removed, Purge relies on a
            // DbException catch to handle the case where the history table was never
            // created. Simulate that with a thrown DbException from ExecuteNonQuery.
            var (handler, factory, _) = Create(enabled: true);
            var (_, command) = SetupConnection(factory);
            command.When(c => c.ExecuteNonQuery()).Do(_ => throw new FakeDbException());

            var result = handler.Purge(DateTime.UtcNow.AddDays(-30));

            Assert.AreEqual(0L, result);
        }

        [TestMethod]
        public void Purge_When_Enabled_Opens_Connection_And_Executes()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);
            command.ExecuteNonQuery().Returns(3);

            var result = handler.Purge(DateTime.UtcNow.AddDays(-30));

            Assert.AreEqual(3L, result);
            connection.Received(1).Open();
            command.Received(1).ExecuteNonQuery();
        }

        [TestMethod]
        public void Purge_When_Enabled_No_Records_Returns_Zero()
        {
            var (handler, factory, _) = Create(enabled: true);
            var (connection, command) = SetupConnection(factory);
            command.ExecuteNonQuery().Returns(0);

            var result = handler.Purge(DateTime.UtcNow.AddDays(-30));

            Assert.AreEqual(0L, result);
            connection.Received(1).Open();
        }

        private static (PurgeMessageHistoryHandler handler, IDbConnectionFactory factory, IBaseTransportOptions options)
            Create(bool enabled = false)
        {
            var factory = Substitute.For<IDbConnectionFactory>();
            var tableNameHelper = Substitute.For<ITableNameHelper>();
            tableNameHelper.HistoryName.Returns("TestHistory");
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(enabled);
            return (new PurgeMessageHistoryHandler(factory, tableNameHelper), factory, options);
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
