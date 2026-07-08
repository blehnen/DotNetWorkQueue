using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class QueryMessageHistoryHandlerTests
    {
        // --- Connection-access tests ---
        // The handler no longer short-circuits on EnableHistory — reads always hit Redis.
        // ConnectionMultiplexer has no default constructor so NSubstitute cannot proxy it;
        // we confirm Connection is accessed, which means the read path executes.

        [TestMethod]
        public void Get_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var handler = new QueryMessageHistoryHandler(connection, redisNames);

            try { handler.Get(0, 10, null); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void Get_When_Enabled_With_Filter_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var handler = new QueryMessageHistoryHandler(connection, redisNames);

            try { handler.Get(0, 10, MessageHistoryStatus.Complete); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void GetByQueueId_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var handler = new QueryMessageHistoryHandler(connection, redisNames);

            try { handler.GetByQueueId("q1"); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void GetCount_When_Enabled_No_Filter_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var handler = new QueryMessageHistoryHandler(connection, redisNames);

            try { handler.GetCount(null); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void GetCount_When_Enabled_With_Filter_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var handler = new QueryMessageHistoryHandler(connection, redisNames);

            try { handler.GetCount(MessageHistoryStatus.Error); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void Construction_Does_Not_Throw()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var handler = new QueryMessageHistoryHandler(connection, redisNames);
            Assert.IsNotNull(handler);
        }

        // --- LoadRecord discriminator tests (DurationMs=0 preservation) ---

        /// <summary>Subclass that injects an IDatabase directly, bypassing ConnectionMultiplexer.</summary>
        private class TestableQueryHandler : QueryMessageHistoryHandler
        {
            private readonly IDatabase _db;
            public TestableQueryHandler(IRedisConnection connection, RedisNames redisNames, IDatabase db)
                : base(connection, redisNames) { _db = db; }
            protected override IDatabase GetDb() => _db;
        }

        private static QueryMessageHistoryHandler CreateEnabledWithDb(IDatabase db)
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            redisNames.Values.Returns("queue:test");

            return new TestableQueryHandler(connection, redisNames, db);
        }

        [TestMethod]
        public void LoadRecord_CompletedStatus_DurationZero_PreservesZero()
        {
            // Arrange: Complete row, DurationMs stored as 0 (sub-ms completion)
            var completedTicks = DateTime.UtcNow.Ticks;
            var startedTicks = completedTicks - 100;
            var entries = new HashEntry[]
            {
                new HashEntry("QueueID", "q1"),
                new HashEntry("CorrelationID", "corr1"),
                new HashEntry("Status", (int)MessageHistoryStatus.Complete),
                new HashEntry("EnqueuedUtc", startedTicks),
                new HashEntry("StartedUtc", startedTicks),
                new HashEntry("CompletedUtc", completedTicks),
                new HashEntry("DurationMs", 0L),
                new HashEntry("ExceptionText", ""),
                new HashEntry("RetryCount", 0),
                new HashEntry("Route", ""),
                new HashEntry("MessageType", "MyType"),
            };

            var db = Substitute.For<IDatabase>();
            db.HashGetAll(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(entries);

            var handler = CreateEnabledWithDb(db);

            // Act
            var record = handler.GetByQueueId("q1");

            // Assert: DurationMs must be 0, NOT null
            Assert.IsNotNull(record);
            Assert.AreEqual(0L, record.DurationMs,
                "DurationMs=0 on a completed row must be preserved as 0, not converted to null");
        }

        [TestMethod]
        public void LoadRecord_EnqueuedStatus_NoCompletedUtc_DurationIsNull()
        {
            // Arrange: Enqueued row — CompletedUtc=0 means the row never completed
            var enqueuedTicks = DateTime.UtcNow.Ticks;
            var entries = new HashEntry[]
            {
                new HashEntry("QueueID", "q2"),
                new HashEntry("CorrelationID", ""),
                new HashEntry("Status", (int)MessageHistoryStatus.Enqueued),
                new HashEntry("EnqueuedUtc", enqueuedTicks),
                new HashEntry("StartedUtc", 0L),
                new HashEntry("CompletedUtc", 0L),
                new HashEntry("DurationMs", 0L),
                new HashEntry("ExceptionText", ""),
                new HashEntry("RetryCount", 0),
                new HashEntry("Route", ""),
                new HashEntry("MessageType", ""),
            };

            var db = Substitute.For<IDatabase>();
            db.HashGetAll(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(entries);

            var handler = CreateEnabledWithDb(db);

            // Act
            var record = handler.GetByQueueId("q2");

            // Assert: DurationMs must be null — row never completed
            Assert.IsNotNull(record);
            Assert.IsNull(record.DurationMs,
                "DurationMs must be null when CompletedUtc=0 (row never completed)");
        }
    }
}
