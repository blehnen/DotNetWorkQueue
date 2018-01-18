using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class WaitForEventOrCancelFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = Create();
            Assert.NotNull(test.Create());
        }

        private IWaitForEventOrCancelFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WaitForEventOrCancelFactory>();
        }
    }
}
