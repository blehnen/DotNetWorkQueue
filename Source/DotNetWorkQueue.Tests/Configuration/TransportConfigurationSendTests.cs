using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class TransportConfigurationSendTests
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
        }
        private TransportConfigurationSend GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<TransportConfigurationSend>();
        }
    }
}
