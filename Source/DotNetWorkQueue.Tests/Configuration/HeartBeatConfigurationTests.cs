using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class HeartbeatConfigurationTests
    {
        [Fact]
        public void CreateDefaultConfiguration()
        {
            GetConfiguration();
        }
        [Theory, AutoData]
        public void SetAndGet_HeartBeatMonitorTime(int value)
        {
            var configuration = GetConfiguration();
            configuration.MonitorTime = TimeSpan.FromSeconds(value);

            Assert.Equal(TimeSpan.FromSeconds(value), configuration.MonitorTime);
        }
        [Theory, AutoData]
        public void SetAndGet_HeartBeatTime(int value)
        {
            var configuration = GetConfiguration();
            configuration.Time = TimeSpan.FromSeconds(value);

            Assert.Equal(TimeSpan.FromSeconds(value), configuration.Time);
        }
        [Theory, AutoData]
        public void SetAndGet_HeartBeatUpdateTime(string value)
        {
            var configuration = GetConfiguration();
            configuration.UpdateTime = value;

            Assert.Equal(value, configuration.UpdateTime);
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
        [Fact]
        public void Set_Readonly_SetsChildren()
        {
            var configuration = GetConfiguration(true);
            configuration.SetReadOnly();
            configuration.ThreadPoolConfiguration.Received(1).SetReadOnly();
        }
        [Theory, AutoData]
        public void Set_HeartBeatMonitorTime_WhenReadOnly_Fails(int value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.MonitorTime = TimeSpan.FromSeconds(value);
              });
        }

        [Theory, AutoData]
        public void Set_HeartBeatTime_WhenReadOnly_Fails(int value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.Time = TimeSpan.FromSeconds(value);
              });
        }
        [Theory, AutoData]
        public void Set_HeartBeatUpdateTime_WhenReadOnly_Fails(string value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.UpdateTime = value;
              });
        }

        private HeartBeatConfiguration GetConfiguration(bool heartBeatSupported = false)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<TransportConfigurationReceive>();
            configuration.HeartBeatSupported = heartBeatSupported;
            fixture.Inject(configuration);
            return fixture.Create<HeartBeatConfiguration>();
        }
    }
}
