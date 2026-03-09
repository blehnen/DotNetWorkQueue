using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class QueueConfigurationSendTests
    {
        [TestMethod]
        public void DefaultCreation()
        {
            var test = Create();
            Assert.IsNotNull(test.AdditionalConfiguration);
            Assert.IsNotNull(test.HeaderNames);
            Assert.IsNotNull(test.TimeConfiguration);
            Assert.IsNotNull(test.TransportConfiguration);
        }

        private QueueConfigurationSend Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueConfigurationSend>();
        }
    }
}
