using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using NSubstitute;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class TransportConfigurationReceiveTests
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

            configuration.FatalExceptionDelayBehavior.Received(1).SetReadOnly();
            configuration.QueueDelayBehavior.Received(1).SetReadOnly();
            configuration.RetryDelayBehavior.Received(1).SetReadOnly();
        }

        private TransportConfigurationReceive GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<TransportConfigurationReceive>();
        }
    }
}
