using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;


using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class QueueConfigurationSendTests
    {
        [Fact]
        public void DefaultCreation()
        {
            var test = Create();
            Assert.NotNull(test.AdditionalConfiguration);
            Assert.NotNull(test.HeaderNames);
            Assert.NotNull(test.TimeConfiguration);
            Assert.NotNull(test.TransportConfiguration);
        }

        private QueueConfigurationSend Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueConfigurationSend>();
        }
    }
}
