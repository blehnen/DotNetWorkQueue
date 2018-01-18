using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class RetryDelayFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = Create().Create();
            Assert.NotNull(test);
        }
        private IRetryDelayFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<RetryDelayFactory>();
        }
    }
}
