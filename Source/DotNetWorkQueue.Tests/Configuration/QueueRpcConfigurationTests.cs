using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class QueueRpcConfigurationTests
    {
        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = GetConfiguration();
            Assert.False(configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.True(configuration.IsReadOnly);
        }

        [Fact]
        public void Set_Readonly_SetsTransportConfigurationReceive()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.True(configuration.TransportConfigurationReceive.IsReadOnly);
        }

        [Fact]
        public void Set_Readonly_SetsMessageExpiration()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            configuration.MessageExpiration.Received(1).SetReadOnly();
        }

        [Fact]
        public void Set_Readonly_SetsTransportConfigurationSend()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.True(configuration.TransportConfigurationSend.IsReadOnly);
        }

        private QueueRpcConfiguration GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueRpcConfiguration>();
        }
    }
}
