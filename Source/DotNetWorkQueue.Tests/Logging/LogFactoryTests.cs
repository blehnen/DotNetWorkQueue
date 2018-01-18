using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Logging;


using Xunit;

namespace DotNetWorkQueue.Tests.Logging
{
    public class LogFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = Create();
            Assert.NotNull(test.Create());
        }
        private ILogFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<LogFactory>();
        }
    }
}
