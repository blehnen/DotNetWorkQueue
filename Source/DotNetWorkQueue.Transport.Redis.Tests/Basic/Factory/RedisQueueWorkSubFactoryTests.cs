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
    }
}
