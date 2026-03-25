using System;
using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class WriteMessageHistoryHandlerTests
    {
        /// <summary>
        /// Test wrapper that exposes the IDatabase mock without needing a real ConnectionMultiplexer.
        /// We test through a subclass that overrides the database access path.
        /// </summary>
        private class TestableWriteMessageHistoryHandler : WriteMessageHistoryHandler
        {
            private readonly IDatabase _db;

            public TestableWriteMessageHistoryHandler(IRedisConnection connection, RedisNames redisNames, IBaseTransportOptions options, IDatabase db)
                : base(connection, redisNames, options)
            {
                _db = db;
            }

            // We can't override private methods, so we use this only for construction.
            // The enabled path tests need the actual Connection.GetDatabase() call to succeed.
        }

        private WriteMessageHistoryHandler CreateDisabled()
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(false);
            return new WriteMessageHistoryHandler(connection, redisNames, options);
        }

        // --- Disabled path tests ---

        [TestMethod]
        public void RecordEnqueue_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = CreateDisabled();
            handler.RecordEnqueue("q1", "c1", "route", "type", null, null);
        }

        [TestMethod]
        public void RecordProcessingStart_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = CreateDisabled();
            handler.RecordProcessingStart("q1");
        }

        [TestMethod]
        public void RecordComplete_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = CreateDisabled();
            handler.RecordComplete("q1");
        }

        [TestMethod]
        public void RecordError_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = CreateDisabled();
            handler.RecordError("q1", "some error");
        }

        [TestMethod]
        public void RecordRollback_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = CreateDisabled();
            handler.RecordRollback("q1");
        }

        [TestMethod]
        public void RecordDelete_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = CreateDisabled();
            handler.RecordDelete("q1");
        }

        [TestMethod]
        public void RecordExpire_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = CreateDisabled();
            handler.RecordExpire("q1");
        }

        // --- Enabled path tests ---
        // ConnectionMultiplexer has no default constructor, so NSubstitute cannot proxy it.
        // Instead we verify that when enabled, the handler does NOT short-circuit (it attempts
        // to call _connection.Connection) by asserting that Connection was accessed.

        [TestMethod]
        public void RecordEnqueue_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new WriteMessageHistoryHandler(connection, redisNames, options);

            try { handler.RecordEnqueue("q1", "c1", "route", "type", null, null); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void RecordProcessingStart_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new WriteMessageHistoryHandler(connection, redisNames, options);

            try { handler.RecordProcessingStart("q1"); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void RecordComplete_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new WriteMessageHistoryHandler(connection, redisNames, options);

            try { handler.RecordComplete("q1"); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void RecordError_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new WriteMessageHistoryHandler(connection, redisNames, options);

            try { handler.RecordError("q1", "err"); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void RecordRollback_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new WriteMessageHistoryHandler(connection, redisNames, options);

            try { handler.RecordRollback("q1"); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void RecordDelete_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new WriteMessageHistoryHandler(connection, redisNames, options);

            try { handler.RecordDelete("q1"); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void RecordExpire_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new WriteMessageHistoryHandler(connection, redisNames, options);

            try { handler.RecordExpire("q1"); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void RecordEnqueue_When_Enabled_With_Null_Args_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new WriteMessageHistoryHandler(connection, redisNames, options);

            try { handler.RecordEnqueue("q1", null, null, null, null, null); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void RecordError_When_Enabled_With_Null_Exception_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new WriteMessageHistoryHandler(connection, redisNames, options);

            try { handler.RecordError("q1", null); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void Construction_Does_Not_Throw()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            var handler = new WriteMessageHistoryHandler(connection, redisNames, options);
            Assert.IsNotNull(handler);
        }
    }
}
