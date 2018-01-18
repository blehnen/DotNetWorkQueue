using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests
{
    /// <summary>
    /// 
    /// <remarks>Options are only set via the IoC container</remarks>
    /// </summary>
    public class ConfigurationExtensionsTests
    {
        [Fact]
        public void AdditionalMessageData()
        {      
            IAdditionalMessageData test = new AdditionalMessageData();
            test.SetDelay(TimeSpan.FromSeconds(1));
            Assert.Equal(TimeSpan.FromSeconds(1), test.GetDelay());

            test.SetExpiration(TimeSpan.FromSeconds(5));
            Assert.Equal(TimeSpan.FromSeconds(5), test.GetExpiration());

            test.SetExpiration(null);
            Assert.Null(test.GetExpiration());
        }

        [Fact]
        public void ConfigurationReceive()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IConfiguration config = fixture.Create<AdditionalConfiguration>();
            fixture.Inject(config);
            var configuration = fixture.Create<QueueConfigurationReceive>();
            var options = fixture.Create<RedisQueueTransportOptions>();
            configuration.AdditionalConfiguration.SetSetting("RedisQueueTransportOptions", options);

            Assert.NotNull(configuration.Options());
        }

        [Fact]
        public void ConfigurationSend()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IConfiguration config = fixture.Create<AdditionalConfiguration>();
            fixture.Inject(config);
            var configuration = fixture.Create<QueueConfigurationSend>();
            var options = fixture.Create<RedisQueueTransportOptions>();
            configuration.AdditionalConfiguration.SetSetting("RedisQueueTransportOptions", options);

            Assert.NotNull(configuration.Options());
        }
    }
}
