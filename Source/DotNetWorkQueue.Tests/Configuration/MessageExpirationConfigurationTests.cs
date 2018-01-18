using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;
using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class MessageExpirationConfigurationTests
    {
        [Theory, AutoData]
        public void SetAndGet_CheckExpiredMessagesTime(int value)
        {
            var configuration = GetConfiguration();
            configuration.MonitorTime = TimeSpan.FromSeconds(value);

            Assert.Equal(TimeSpan.FromSeconds(value), configuration.MonitorTime);
        }
        [Fact]
        public void SetAndGet_ClearExpiredMessagesEnabled()
        {
            var configuration = GetConfiguration();
            configuration.Enabled = true;

            Assert.True(configuration.Enabled);
        }
        [Fact]
        public void Get_ClearExpiredMessagesEnabled_DefaultsToFalse()
        {
            var configuration = GetConfiguration();
            Assert.False(configuration.Enabled);
        }
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
        [Theory, AutoData]
        public void Set_CheckExpiredMessagesTime_WhenReadOnly_Fails(int value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.MonitorTime = TimeSpan.FromHours(value);
              });
        }
        [Theory, AutoData]
        public void Set_ClearExpiredMessagesEnabled_WhenReadOnly_Fails(bool value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.Enabled = value;
              });
        }
        private MessageExpirationConfiguration GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var transport = fixture.Create<TransportConfigurationReceive>();
            transport.MessageExpirationSupported = true;
            fixture.Inject(transport);
            return fixture.Create<MessageExpirationConfiguration>();
        }
    }
}
