using System;
using System.Data;
using System.Data.Common;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class WriteMessageHistoryHandlerTests
    {
        //deliberately nowhere near the machine clock, so a timestamp taken from DateTime.UtcNow
        //instead of the provider cannot coincidentally match
        private static readonly DateTime ProviderNow = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        /// <summary>A clock the test controls, standing in for the configured time provider.</summary>
        private static IGetTimeFactory Clock(DateTime now)
        {
            var time = Substitute.For<IGetTime>();
            time.GetCurrentUtcDate().Returns(now);
            var factory = Substitute.For<IGetTimeFactory>();
            factory.Create().Returns(time);
            return factory;
        }
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
            var connection = Substitute.For<DbConnection>();

            var allParams = new System.Collections.Generic.List<DbParameter>();
            // Capture the CommandText of every command created during RecordComplete.
            var capturedCommandTexts = new System.Collections.Generic.List<string>();

            DbCommand MakeTrackingCommand(bool returnsDbNull = false)
            {
                var cmd = Substitute.For<DbCommand>();
                var paramCollection = Substitute.For<DbParameterCollection>();
                cmd.Parameters.Returns(paramCollection);
                cmd.CreateParameter().Returns(_ =>
                {
                    var p = Substitute.For<DbParameter>();
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
            DbParameter durationParam = null;
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
            var connection = Substitute.For<DbConnection>();

            var allParams = new System.Collections.Generic.List<DbParameter>();

            DbCommand MakeTrackingCommand(bool returnsDbNull = false)
            {
                var cmd = Substitute.For<DbCommand>();
                var paramCollection = Substitute.For<DbParameterCollection>();
                cmd.Parameters.Returns(paramCollection);
                cmd.CreateParameter().Returns(_ =>
                {
                    var p = Substitute.For<DbParameter>();
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
            DbParameter durationParam = null;
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

        [TestMethod]
        public void RecordEnqueue_TakesTheTimestampFromTheConfiguredProvider()
        {
            //on SQL Server and PostgreSQL the provider is the database server's clock, which is what the
            //rest of the queue's timestamps are on. Taking this one from the application machine instead
            //makes history disagree with the data beside it, and durations can come out negative
            var (handler, factory, _) = Create(enabled: true);
            var parameters = new System.Collections.Generic.List<DbParameter>();
            var connection = Substitute.For<DbConnection>();
            var command = Substitute.For<DbCommand>();
            command.Parameters.Returns(Substitute.For<DbParameterCollection>());
            command.CreateParameter().Returns(_ =>
            {
                var parameter = Substitute.For<DbParameter>();
                parameters.Add(parameter);
                return parameter;
            });
            connection.CreateCommand().Returns(command);
            factory.Create().Returns(connection);

            handler.RecordEnqueue("q1", "c1", "route1", "MyType", new byte[] { 1 }, new byte[] { 2 });

            DbParameter enqueued = null;
            foreach (var parameter in parameters)
            {
                if ((string)parameter.ParameterName == "@EnqueuedUtc")
                {
                    enqueued = parameter;
                    break;
                }
            }
            Assert.IsNotNull(enqueued, "Expected an @EnqueuedUtc parameter to have been created");
            Assert.AreEqual(ProviderNow, enqueued.Value);
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
            return (new WriteMessageHistoryHandler(factory, tableNameHelper, options, Clock(ProviderNow)), factory, options);
        }

        private static (DbConnection connection, DbCommand command) SetupConnection(IDbConnectionFactory factory)
        {
            var connection = Substitute.For<DbConnection>();
            var command = Substitute.For<DbCommand>();
            var parameters = Substitute.For<DbParameterCollection>();
            var parameter = Substitute.For<DbParameter>();
            command.CreateParameter().Returns(parameter);
            command.Parameters.Returns(parameters);
            connection.CreateCommand().Returns(command);
            factory.Create().Returns(connection);
            return (connection, command);
        }
    }
}
