using System;
using DotNetWorkQueue.Configuration;
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
        /// Test wrapper that injects an IDatabase directly via the GetDb() seam,
        /// bypassing ConnectionMultiplexer which cannot be proxied by NSubstitute.
        /// </summary>
        private class TestableWriteMessageHistoryHandler : WriteMessageHistoryHandler
        {
            private readonly IDatabase _db;

            public TestableWriteMessageHistoryHandler(IRedisConnection connection, RedisNames redisNames, IBaseTransportOptions options, IDatabase db)
                : base(connection, redisNames, options)
            {
                _db = db;
            }

            protected override IDatabase GetDb() => _db;
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

        // --- Write-side regression: DurationMs=0 contract when StartedUtc is missing ---

        private static (WriteMessageHistoryHandler handler, IDatabase db) CreateEnabledWithDb()
        {
            var db = Substitute.For<IDatabase>();
            // HashGet(key, field, flags) — the 3-arg interface method that the 2-arg extension delegates to.
            // Returns 0 to simulate StartedUtc never being persisted (race-window scenario).
            db.HashGet(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>()).Returns((RedisValue)0L);

            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            redisNames.Values.Returns("queue:test");

            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);

            return (new TestableWriteMessageHistoryHandler(connection, redisNames, options, db), db);
        }

        [TestMethod]
        public void RecordComplete_WithoutStartedUtc_WritesDurationZero()
        {
            // Arrange: StartedUtc HashGet returns 0 (race-window: start not persisted)
            var (handler, db) = CreateEnabledWithDb();

            // Act
            handler.RecordComplete("q1");

            // Assert: HashSet(key, HashEntry[], flags) was called with DurationMs=0L
            db.Received().HashSet(
                Arg.Any<RedisKey>(),
                Arg.Is<HashEntry[]>(entries => ContainsEntry(entries, "DurationMs", 0L)),
                Arg.Any<CommandFlags>());
        }

        [TestMethod]
        public void RecordError_WithoutStartedUtc_WritesDurationZero()
        {
            // Arrange: StartedUtc HashGet returns 0 (race-window: start not persisted)
            var (handler, db) = CreateEnabledWithDb();

            // Act
            handler.RecordError("q1", "some error");

            // Assert: HashSet(key, HashEntry[], flags) was called with DurationMs=0L
            db.Received().HashSet(
                Arg.Any<RedisKey>(),
                Arg.Is<HashEntry[]>(entries => ContainsEntry(entries, "DurationMs", 0L)),
                Arg.Any<CommandFlags>());
        }

        [TestMethod]
        public void RecordProcessingStart_When_Status_Is_Error_Does_Not_Overwrite()
        {
            var (handler, db) = CreateEnabledWithDb();

            db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
                .Returns((RedisValue)(int)MessageHistoryStatus.Error);

            handler.RecordProcessingStart("q1");

            db.DidNotReceive().HashSet(
                Arg.Any<RedisKey>(),
                Arg.Is<HashEntry[]>(entries => ContainsEntry(entries, "Status", (int)MessageHistoryStatus.Processing)),
                Arg.Any<CommandFlags>());
        }

        [TestMethod]
        public void RecordProcessingStart_When_No_Record_Exists_Does_Not_Write()
        {
            var (handler, db) = CreateEnabledWithDb();

            // Override the default 0L return to RedisValue.Null for the Status field.
            // RedisValue.Null casts to (int)0, which is the same as MessageHistoryStatus.Enqueued —
            // the bug. The fix checks HasValue first so this must NOT write.
            db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
                .Returns(RedisValue.Null);

            handler.RecordProcessingStart("unknown-id");

            db.DidNotReceive().HashSet(
                Arg.Any<RedisKey>(),
                Arg.Any<HashEntry[]>(),
                Arg.Any<CommandFlags>());
        }

        [TestMethod]
        public void RecordProcessingStart_When_Status_Is_Enqueued_Sets_Processing()
        {
            var (handler, db) = CreateEnabledWithDb();

            db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
                .Returns((RedisValue)(int)MessageHistoryStatus.Enqueued);

            handler.RecordProcessingStart("q1");

            db.Received().HashSet(
                Arg.Any<RedisKey>(),
                Arg.Is<HashEntry[]>(entries => ContainsEntry(entries, "Status", (int)MessageHistoryStatus.Processing)),
                Arg.Any<CommandFlags>());
        }

        private static bool ContainsEntry(HashEntry[] entries, string name, long value)
        {
            if (entries == null) return false;
            foreach (var entry in entries)
            {
                if (entry.Name == name && (long)entry.Value == value)
                    return true;
            }
            return false;
        }
    }
}
