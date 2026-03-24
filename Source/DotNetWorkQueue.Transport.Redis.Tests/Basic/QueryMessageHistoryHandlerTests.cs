using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class QueryMessageHistoryHandlerTests
    {
        private QueryMessageHistoryHandler Create(bool enabled)
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var config = Substitute.For<IHistoryConfiguration>();
            config.Enabled.Returns(enabled);
            return new QueryMessageHistoryHandler(connection, redisNames, config);
        }

        [TestMethod]
        public void Get_When_Disabled_Returns_Empty_List()
        {
            var handler = Create(false);
            var result = handler.Get(0, 10, null);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetByQueueId_When_Disabled_Returns_Null()
        {
            var handler = Create(false);
            var result = handler.GetByQueueId("q1");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetCount_When_Disabled_Returns_Zero()
        {
            var handler = Create(false);
            var result = handler.GetCount(null);
            Assert.AreEqual(0, result);
        }
    }
}
