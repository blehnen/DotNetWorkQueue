using System;
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

        [TestMethod]
        public void RecordComplete_WithoutStartedUtc_PassesDurationZero()
        {
            var (handler, factory, _) = Create(enabled: true);
            var connection = Substitute.For<IDbConnection>();

            var allParams = new System.Collections.Generic.List<IDbDataParameter>();
            // Capture the CommandText of every command created during RecordComplete.
            var capturedCommandTexts = new System.Collections.Generic.List<string>();

            IDbCommand MakeTrackingCommand(bool returnsDbNull = false)
            {
                var cmd = Substitute.For<IDbCommand>();
                var paramCollection = Substitute.For<IDataParameterCollection>();
                cmd.Parameters.Returns(paramCollection);
                cmd.CreateParameter().Returns(_ =>
                {
                    var p = Substitute.For<IDbDataParameter>();
                    allParams.Add(p);
                    return p;
                });
                // Intercept CommandText assignments so we can assert the SQL text later.
                cmd.When(x => { x.CommandText = Arg.Any<string>(); })
                   .Do(x => capturedCommandTexts.Add((string)x[0]));
                if (returnsDbNull)
                    cmd.ExecuteScalar().Returns(DBNull.Value);
                return cmd;
            }

            int commandCallCount = 0;
            connection.CreateCommand().Returns(_ =>
            {
                commandCallCount++;
                // cmd1: first UPDATE (status+completed), cmd2: GetStartedUtc SELECT, cmd3: duration UPDATE
                return MakeTrackingCommand(returnsDbNull: commandCallCount == 2);
            });
            factory.Create().Returns(connection);

            handler.RecordComplete("q1");

            // Assert the @DurationMs parameter was set to 0L (StartedUtc was null → duration = 0).
            IDbDataParameter durationParam = null;
            foreach (var p in allParams)
            {
                if ((string)p.ParameterName == "@DurationMs")
                {
                    durationParam = p;
                    break;
                }
            }
            Assert.IsNotNull(durationParam, "Expected a @DurationMs parameter to have been created");
            Assert.AreEqual(0L, durationParam.Value);

            // Assert the duration UPDATE SQL does NOT contain the StartedUtc IS NOT NULL guard.
            // That guard caused the UPDATE to be a no-op when StartedUtc was never persisted,
            // leaving DurationMs=NULL in the database even though C# computed 0L.
            bool foundGuard = false;
            foreach (var sql in capturedCommandTexts)
            {
                if (sql != null && sql.Contains("StartedUtc IS NOT NULL", System.StringComparison.OrdinalIgnoreCase))
                {
                    foundGuard = true;
                    break;
                }
            }
            Assert.IsFalse(foundGuard,
                "The duration UPDATE WHERE clause must not contain 'StartedUtc IS NOT NULL' — " +
                "that guard makes the UPDATE a no-op when StartedUtc was never persisted.");
        }

        [TestMethod]
        public void RecordError_WithoutStartedUtc_PassesDurationZero()
        {
            var (handler, factory, _) = Create(enabled: true);
            var connection = Substitute.For<IDbConnection>();

            var allParams = new System.Collections.Generic.List<IDbDataParameter>();

            IDbCommand MakeTrackingCommand(bool returnsDbNull = false)
            {
                var cmd = Substitute.For<IDbCommand>();
                var paramCollection = Substitute.For<IDataParameterCollection>();
                cmd.Parameters.Returns(paramCollection);
                cmd.CreateParameter().Returns(_ =>
                {
                    var p = Substitute.For<IDbDataParameter>();
                    allParams.Add(p);
                    return p;
                });
                if (returnsDbNull)
                    cmd.ExecuteScalar().Returns(DBNull.Value);
                return cmd;
            }

            int commandCallCount = 0;
            connection.CreateCommand().Returns(_ =>
            {
                commandCallCount++;
                // cmd1: GetStartedUtc SELECT, cmd2: UPDATE
                return MakeTrackingCommand(returnsDbNull: commandCallCount == 1);
            });
            factory.Create().Returns(connection);

            handler.RecordError("q1", "some error");

            // Find the parameter named @DurationMs
            IDbDataParameter durationParam = null;
            foreach (var p in allParams)
            {
                if ((string)p.ParameterName == "@DurationMs")
                {
                    durationParam = p;
                    break;
                }
            }
            Assert.IsNotNull(durationParam, "Expected a @DurationMs parameter to have been created");
            Assert.AreEqual(0L, durationParam.Value);
        }

        private static (WriteMessageHistoryHandler handler, IDbConnectionFactory factory, IBaseTransportOptions options)
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
            return (new WriteMessageHistoryHandler(factory, tableNameHelper, options), factory, options);
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
