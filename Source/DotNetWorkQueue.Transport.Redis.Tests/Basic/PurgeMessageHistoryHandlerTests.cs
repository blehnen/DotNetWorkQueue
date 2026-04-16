using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class PurgeMessageHistoryHandlerTests
    {
        /// <summary>
        /// Test wrapper that injects an IDatabase directly via the GetDb() seam,
        /// bypassing ConnectionMultiplexer which cannot be proxied by NSubstitute.
        /// </summary>
        private class TestablePurgeMessageHistoryHandler : PurgeMessageHistoryHandler
        {
            private readonly IDatabase _db;

            public TestablePurgeMessageHistoryHandler(IRedisConnection connection, RedisNames redisNames, IBaseTransportOptions options, IDatabase db)
                : base(connection, redisNames, options)
            {
                _db = db;
            }

            protected override IDatabase GetDb() => _db;
        }

        private static (PurgeMessageHistoryHandler handler, IDatabase db) CreateEnabledWithDb()
        {
            var db = Substitute.For<IDatabase>();
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            redisNames.Values.Returns("queue:test");

            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);

            return (new TestablePurgeMessageHistoryHandler(connection, redisNames, options, db), db);
        }

        [TestMethod]
        public void Purge_Returns_Zero_When_History_Disabled()
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(false);
            var handler = new PurgeMessageHistoryHandler(connection, redisNames, options);

            var result = handler.Purge(DateTime.UtcNow);

            Assert.AreEqual(0L, result);
        }

        [TestMethod]
        public void Purge_Skips_Processing_Records()
        {
            // Arrange
            var (handler, db) = CreateEnabledWithDb();
            var cutoff = DateTime.UtcNow;
            var queueId = "msg-processing";
            var completedTicks = (cutoff - TimeSpan.FromHours(2)).Ticks;

            // SortedSetRangeByScore returns one member (8-param interface signature)
            db.SortedSetRangeByScore(
                    Arg.Any<RedisKey>(),
                    Arg.Any<double>(),
                    Arg.Any<double>(),
                    Arg.Any<Exclude>(),
                    Arg.Any<Order>(),
                    Arg.Any<long>(),
                    Arg.Any<long>(),
                    Arg.Any<CommandFlags>())
                .Returns(new RedisValue[] { queueId });

            // Status = Processing
            db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
                .Returns((RedisValue)(int)MessageHistoryStatus.Processing);
            db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("CompletedUtc"), Arg.Any<CommandFlags>())
                .Returns((RedisValue)completedTicks);

            // Act
            var result = handler.Purge(cutoff);

            // Assert: Processing records must NOT be deleted
            db.DidNotReceive().KeyDelete(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
            Assert.AreEqual(0L, result);
        }

        [TestMethod]
        public void Purge_Removes_Old_Complete_Records()
        {
            // Arrange
            var (handler, db) = CreateEnabledWithDb();
            var cutoff = DateTime.UtcNow;
            var queueId = "msg-complete";
            var completedTicks = (cutoff - TimeSpan.FromHours(1)).Ticks; // 1 hour old — older than cutoff

            db.SortedSetRangeByScore(
                    Arg.Any<RedisKey>(),
                    Arg.Any<double>(),
                    Arg.Any<double>(),
                    Arg.Any<Exclude>(),
                    Arg.Any<Order>(),
                    Arg.Any<long>(),
                    Arg.Any<long>(),
                    Arg.Any<CommandFlags>())
                .Returns(new RedisValue[] { queueId });

            // Status = Complete, CompletedUtc = 1 hour ago (older than cutoff)
            db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
                .Returns((RedisValue)(int)MessageHistoryStatus.Complete);
            db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("CompletedUtc"), Arg.Any<CommandFlags>())
                .Returns((RedisValue)completedTicks);

            // Act
            var result = handler.Purge(cutoff);

            // Assert: hash key must be deleted
            db.Received(1).KeyDelete(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
            Assert.AreEqual(1L, result);
        }

        [TestMethod]
        public void Purge_Handles_Missing_Hash_Gracefully()
        {
            // Arrange: orphaned index entry — hash was already deleted (Status HasValue = false)
            var (handler, db) = CreateEnabledWithDb();
            var cutoff = DateTime.UtcNow;
            var queueId = "msg-orphan";

            db.SortedSetRangeByScore(
                    Arg.Any<RedisKey>(),
                    Arg.Any<double>(),
                    Arg.Any<double>(),
                    Arg.Any<Exclude>(),
                    Arg.Any<Order>(),
                    Arg.Any<long>(),
                    Arg.Any<long>(),
                    Arg.Any<CommandFlags>())
                .Returns(new RedisValue[] { queueId });

            // Status hash field missing => RedisValue.Null (HasValue = false)
            db.HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("Status"), Arg.Any<CommandFlags>())
                .Returns(RedisValue.Null);

            // Act
            var result = handler.Purge(cutoff);

            // Assert: orphan index entry cleaned up via SortedSetRemove, no KeyDelete
            db.Received(1).SortedSetRemove(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>());
            db.DidNotReceive().KeyDelete(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
            db.DidNotReceive().HashGet(Arg.Any<RedisKey>(), Arg.Is<RedisValue>("CompletedUtc"), Arg.Any<CommandFlags>());
            Assert.AreEqual(1L, result);
        }
    }
}
