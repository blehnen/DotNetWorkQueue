using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class TransportConfigurationReceiveTests
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
