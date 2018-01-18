using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class QueueConsumerConfigurationTests
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
        public void Set_Readonly_SetsMessageExpiration()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            configuration.MessageExpiration.Received(1).SetReadOnly();
        }
        [Fact]
        public void Set_Readonly_SetsHeartBeat()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            configuration.HeartBeat.Received(1).SetReadOnly();
        }
        [Fact]
        public void Set_Readonly_SetsWorker()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            configuration.Worker.Received(1).SetReadOnly();
        }
        [Fact]
        public void Set_Readonly_SetsTransportConfiguration()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.True(configuration.TransportConfiguration.IsReadOnly);
        }
        private QueueConsumerConfiguration GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueConsumerConfiguration>();
        }
    }
}
