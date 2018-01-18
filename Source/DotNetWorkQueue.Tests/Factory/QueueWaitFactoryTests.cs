using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Factory;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class QueueWaitFactoryTests
    {
        [Fact]
        public void Create_Disabled()
        {
            var factory = Create(false);
            var test1 = factory.CreateQueueDelay();
            var test2 = factory.CreateFatalErrorDelay();

            Assert.IsAssignableFrom<INoOperation>(test1);
            Assert.IsAssignableFrom<INoOperation>(test2);
        }

        private IQueueWaitFactory Create(bool enable)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(GetConfiguration(enable));
            return fixture.Create<QueueWaitFactory>();
        }
        private QueueConsumerConfiguration GetConfiguration(bool enabled)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<TransportConfigurationReceive>();
            fixture.Inject(configuration);

            var config = fixture.Create<QueueConsumerConfiguration>();
            if (!enabled) return config;
            config.TransportConfiguration.QueueDelayBehavior.Add(TimeSpan.Zero);
            config.TransportConfiguration.FatalExceptionDelayBehavior.Add(TimeSpan.Zero);
            return config;
        }
    }
}
