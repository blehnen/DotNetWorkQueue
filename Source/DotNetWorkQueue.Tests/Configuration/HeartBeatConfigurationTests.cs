using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class HeartbeatConfigurationTests
    {
        [TestMethod]
        public void CreateDefaultConfiguration()
        {
            GetConfiguration();
        }
        [TestMethod]
        public void SetAndGet_HeartBeatMonitorTime()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.MonitorTime = TimeSpan.FromSeconds(value);

            Assert.AreEqual(TimeSpan.FromSeconds(value), configuration.MonitorTime);
        }
        [TestMethod]
        public void SetAndGet_HeartBeatTime()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.Time = TimeSpan.FromSeconds(value);

            Assert.AreEqual(TimeSpan.FromSeconds(value), configuration.Time);
        }
        [TestMethod]
        public void SetAndGet_HeartBeatUpdateTime()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var configuration = GetConfiguration();
            configuration.UpdateTime = value;

            Assert.AreEqual(value, configuration.UpdateTime);
        }
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
            Assert.IsTrue(configuration.IsReadOnly);
        }
        [TestMethod]
        public void Set_Readonly_SetsChildren()
        {
            var configuration = GetConfiguration(true);
            configuration.SetReadOnly();
            configuration.ThreadPoolConfiguration.Received(1).SetReadOnly();
        }
        [TestMethod]
        public void Set_HeartBeatMonitorTime_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.MonitorTime = TimeSpan.FromSeconds(value);
              });
        }

        [TestMethod]
        public void Set_HeartBeatTime_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.Time = TimeSpan.FromSeconds(value);
              });
        }
        [TestMethod]
        public void Set_HeartBeatUpdateTime_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
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
