using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests
{
    /// <summary>
    /// 
    /// <remarks>Options are only set via the IoC container</remarks>
    /// </summary>
    [TestClass]
    public class ConfigurationExtensionsTests
    {
        [TestMethod]
        public void AdditionalMessageData()
        {
            IAdditionalMessageData test = new AdditionalMessageData();
            test.SetDelay(TimeSpan.FromSeconds(1));
            Assert.AreEqual(TimeSpan.FromSeconds(1), test.GetDelay());

            test.SetExpiration(TimeSpan.FromSeconds(5));
            Assert.AreEqual(TimeSpan.FromSeconds(5), test.GetExpiration());

            test.SetExpiration(null);
            Assert.IsNull(test.GetExpiration());
        }

        [TestMethod]
        public void ConfigurationReceive()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IConfiguration config = fixture.Create<AdditionalConfiguration>();
            fixture.Inject(config);
            var configuration = fixture.Create<QueueConfigurationReceive>();
            var options = fixture.Create<RedisQueueTransportOptions>();
            configuration.AdditionalConfiguration.SetSetting("RedisQueueTransportOptions", options);

            Assert.IsNotNull(configuration.Options());
        }

        [TestMethod]
        public void ConfigurationSend()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IConfiguration config = fixture.Create<AdditionalConfiguration>();
            fixture.Inject(config);
            var configuration = fixture.Create<QueueConfigurationSend>();
            var options = fixture.Create<RedisQueueTransportOptions>();
            configuration.AdditionalConfiguration.SetSetting("RedisQueueTransportOptions", options);

            Assert.IsNotNull(configuration.Options());
        }
    }
}
