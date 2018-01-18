using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class MessageProcessingFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create();
            factory.Create();
        }

        public IMessageProcessingFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageProcessingFactory>();
        }
    }
}
