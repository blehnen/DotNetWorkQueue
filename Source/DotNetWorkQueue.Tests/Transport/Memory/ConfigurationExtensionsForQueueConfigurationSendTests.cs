using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory
{
    [TestClass]
    public class ConfigurationExtensionsForQueueConfigurationSendTests
    {
        [TestMethod]
        public void Options_Test()
        {
            var config = Create();
            //options will be null
            Assert.ThrowsExactly<DotNetWorkQueueException>(() => config.Options());

            config.AdditionalConfiguration.SetSetting("MemoryTransportOptions", new TransportOptions());
            var data = config.Options();
            Assert.IsNotNull(data);
        }

        private QueueConfigurationSend Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject<IConfiguration>(new AdditionalConfiguration());
            return fixture.Create<QueueConfigurationSend>();
        }
    }
}