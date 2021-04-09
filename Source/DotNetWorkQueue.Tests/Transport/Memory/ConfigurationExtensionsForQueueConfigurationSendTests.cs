using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Memory;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport.Memory
{
    public class ConfigurationExtensionsForQueueConfigurationSendTests
    {
        [Fact()]
        public void Options_Test()
        {
            var config = Create();
            //options will be null
            Assert.Throws<DotNetWorkQueueException>(() => config.Options());

            config.AdditionalConfiguration.SetSetting("MemoryTransportOptions", new TransportOptions());
            var data = config.Options();
            Assert.NotNull(data);
        }

        private QueueConfigurationSend Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject<IConfiguration>(new AdditionalConfiguration());
            return fixture.Create<QueueConfigurationSend>();
        }
    }
}