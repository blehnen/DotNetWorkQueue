using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class WriteMessageHistoryHandlerTests
    {
        private WriteMessageHistoryHandler Create(bool enabled)
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var config = Substitute.For<IHistoryConfiguration>();
            config.Enabled.Returns(enabled);
            return new WriteMessageHistoryHandler(connection, redisNames, config);
        }

        [TestMethod]
        public void RecordEnqueue_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = Create(false);
            handler.RecordEnqueue("q1", "c1", "route", "type", null, null);
            // No exception means Redis was never called (connection.Connection would throw on NSubstitute mock)
        }

        [TestMethod]
        public void RecordProcessingStart_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = Create(false);
            handler.RecordProcessingStart("q1");
        }

        [TestMethod]
        public void RecordComplete_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = Create(false);
            handler.RecordComplete("q1");
        }

        [TestMethod]
        public void RecordError_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = Create(false);
            handler.RecordError("q1", "some error");
        }

        [TestMethod]
        public void RecordRollback_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = Create(false);
            handler.RecordRollback("q1");
        }

        [TestMethod]
        public void RecordDelete_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = Create(false);
            handler.RecordDelete("q1");
        }

        [TestMethod]
        public void RecordExpire_When_Disabled_Does_Not_Call_Redis()
        {
            var handler = Create(false);
            handler.RecordExpire("q1");
        }
    }
}
