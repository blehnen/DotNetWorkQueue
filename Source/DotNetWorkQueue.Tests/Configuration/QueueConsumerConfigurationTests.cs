using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using NSubstitute;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class QueueConsumerConfigurationTests
    {
        [TestMethod]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = GetConfiguration();
            Assert.IsFalse(configuration.IsReadOnly);
        }
        [TestMethod]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.IsTrue(configuration.IsReadOnly);
        }
        [TestMethod]
        public void Set_Readonly_SetsMessageExpiration()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            configuration.MessageExpiration.Received(1).SetReadOnly();
        }
        [TestMethod]
        public void Set_Readonly_SetsHeartBeat()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            configuration.HeartBeat.Received(1).SetReadOnly();
        }
        [TestMethod]
        public void Set_Readonly_SetsWorker()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            configuration.Worker.Received(1).SetReadOnly();
        }
        [TestMethod]
        public void Set_Readonly_SetsTransportConfiguration()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.IsTrue(configuration.TransportConfiguration.IsReadOnly);
        }
        private QueueConsumerConfiguration GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueConsumerConfiguration>();
        }
    }
}
