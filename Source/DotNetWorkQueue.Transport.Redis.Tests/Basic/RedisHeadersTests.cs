using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisHeadersTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = Create();
            Assert.NotNull(test.CorrelationId);
            Assert.NotNull(test.IncreaseQueueDelay);
        }

        private RedisHeaders Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<RedisHeaders>();
        }
    }
}
