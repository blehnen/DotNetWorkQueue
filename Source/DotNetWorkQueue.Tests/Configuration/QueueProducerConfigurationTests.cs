using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;


using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class QueueProducerConfigurationTests
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
        public void Set_Readonly_SetsTransportConfiguration()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.True(configuration.TransportConfiguration.IsReadOnly);
        }
        private QueueProducerConfiguration GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueProducerConfiguration>();
        }
    }
}
