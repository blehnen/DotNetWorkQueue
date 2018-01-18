using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;


using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class QueueConfigurationRpcTests
    {
        [Fact]
        public void DefaultCreation()
        {
            var test = Create();
            Assert.NotNull(test.HeaderNames);
            Assert.NotNull(test.TransportConfigurationReceive);
            Assert.NotNull(test.TransportConfigurationSend);
        }
        private QueueConfigurationRpc Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueConfigurationRpc>();
        }
    }
}
