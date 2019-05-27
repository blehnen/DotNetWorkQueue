using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class MessageContextFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create();
            var test = factory.Create();
            Assert.NotNull(test);
        }
        private IMessageContextFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageContextFactory>();
        }
    }
}
