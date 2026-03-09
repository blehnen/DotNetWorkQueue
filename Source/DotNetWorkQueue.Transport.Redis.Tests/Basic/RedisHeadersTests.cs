using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisHeadersTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = Create();
            Assert.IsNotNull(test.CorrelationId);
            Assert.IsNotNull(test.IncreaseQueueDelay);
        }

        private RedisHeaders Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<RedisHeaders>();
        }
    }
}
