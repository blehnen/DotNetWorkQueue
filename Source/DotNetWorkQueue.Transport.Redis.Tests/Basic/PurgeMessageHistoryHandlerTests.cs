using System;
using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class PurgeMessageHistoryHandlerTests
    {
        private PurgeMessageHistoryHandler Create(bool enabled)
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var config = Substitute.For<IHistoryConfiguration>();
            config.Enabled.Returns(enabled);
            return new PurgeMessageHistoryHandler(connection, redisNames, config);
        }

        [TestMethod]
        public void Purge_When_Disabled_Returns_Zero()
        {
            var handler = Create(false);
            var result = handler.Purge(DateTime.UtcNow);
            Assert.AreEqual(0, result);
        }
    }
}
