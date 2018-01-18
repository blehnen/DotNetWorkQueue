using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.Redis.Basic.Factory;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Factory
{
    public class RedisQueueWorkSubFactoryTests
    {
        [Fact]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<RedisQueueWorkSubFactory>();
            test.Create();
        }
        [Fact]
        public void Create_With_ID()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<RedisQueueWorkSubFactory>();
            test.Create(Substitute.For<IMessageId>());
        }
    }
}
