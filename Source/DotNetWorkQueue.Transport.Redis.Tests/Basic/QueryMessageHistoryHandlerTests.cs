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
        private QueryMessageHistoryHandler CreateDisabled()
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(false);
            return new QueryMessageHistoryHandler(connection, redisNames, options);
        }

        // --- Disabled path tests ---

        [TestMethod]
        public void Get_When_Disabled_Returns_Empty_List()
        {
            var handler = CreateDisabled();
            var result = handler.Get(0, 10, null);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void Get_When_Disabled_With_Filter_Returns_Empty_List()
        {
            var handler = CreateDisabled();
            var result = handler.Get(0, 10, MessageHistoryStatus.Complete);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetByQueueId_When_Disabled_Returns_Null()
        {
            var handler = CreateDisabled();
            var result = handler.GetByQueueId("q1");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetCount_When_Disabled_Returns_Zero()
        {
            var handler = CreateDisabled();
            var result = handler.GetCount(null);
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetCount_When_Disabled_With_Filter_Returns_Zero()
        {
            var handler = CreateDisabled();
            var result = handler.GetCount(MessageHistoryStatus.Error);
            Assert.AreEqual(0, result);
        }

        // --- Enabled path tests ---
        // ConnectionMultiplexer has no default constructor so NSubstitute cannot proxy it.
        // We verify that the enabled path does NOT short-circuit by confirming Connection is accessed.

        [TestMethod]
        public void Get_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new QueryMessageHistoryHandler(connection, redisNames, options);

            try { handler.Get(0, 10, null); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void Get_When_Enabled_With_Filter_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new QueryMessageHistoryHandler(connection, redisNames, options);

            try { handler.Get(0, 10, MessageHistoryStatus.Complete); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void GetByQueueId_When_Enabled_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new QueryMessageHistoryHandler(connection, redisNames, options);

            try { handler.GetByQueueId("q1"); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void GetCount_When_Enabled_No_Filter_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new QueryMessageHistoryHandler(connection, redisNames, options);

            try { handler.GetCount(null); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void GetCount_When_Enabled_With_Filter_Accesses_Connection()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            options.EnableHistory.Returns(true);
            var handler = new QueryMessageHistoryHandler(connection, redisNames, options);

            try { handler.GetCount(MessageHistoryStatus.Error); } catch (NullReferenceException) { }
            _ = connection.Received(1).Connection;
        }

        [TestMethod]
        public void Construction_Does_Not_Throw()
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            var options = Substitute.For<IBaseTransportOptions>();
            var handler = new QueryMessageHistoryHandler(connection, redisNames, options);
            Assert.IsNotNull(handler);
        }

        [TestMethod]
        public void Get_When_Disabled_PageIndex_NonZero_Returns_Empty()
        {
            var handler = CreateDisabled();
            var result = handler.Get(5, 10, null);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void Get_When_Disabled_With_All_StatusFilters_Returns_Empty()
        {
            var handler = CreateDisabled();
            foreach (MessageHistoryStatus status in Enum.GetValues(typeof(MessageHistoryStatus)))
            {
                var result = handler.Get(0, 10, status);
                Assert.AreEqual(0, result.Count);
            }
        }
    }
}
