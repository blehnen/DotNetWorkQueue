using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Factory;
using DotNetWorkQueue.Transport.Redis.Basic.MessageID;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Factory
{
    public class GetMessageIdFactoryTests
    {
        [Fact]
        public void Create()
        {
            var redisId = new GetRedisIncrId();
            var uuId = new GetUuidMessageId();

            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var container = Substitute.For<IContainer>();
            container.GetInstance<GetRedisIncrId>().Returns(redisId);
            container.GetInstance<GetUuidMessageId>().Returns(uuId);
            fixture.Inject(container);

            var factory = fixture.Create<IContainerFactory>();
            factory.Create().ReturnsForAnyArgs(container);

            var options = Helpers.CreateOptions();
            options.MessageIdLocation = MessageIdLocations.RedisIncr;
            var test = new GetMessageIdFactory(factory, options);
            var result = test.Create();
            Assert.IsAssignableFrom<GetRedisIncrId>(result);

            options.MessageIdLocation = MessageIdLocations.Uuid;
            result = test.Create();
            Assert.IsAssignableFrom<GetUuidMessageId>(result);

            options.MessageIdLocation = MessageIdLocations.Custom;
            test.Create();

            options.MessageIdLocation = (MessageIdLocations)99;
            Assert.Throws<DotNetWorkQueueException>(
           delegate
           {
               test.Create();
           });
        }
    }
}
