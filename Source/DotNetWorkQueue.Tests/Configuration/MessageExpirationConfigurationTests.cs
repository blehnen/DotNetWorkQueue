using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class MessageExpirationConfigurationTests
    {
        [TestMethod]
        public void SetAndGet_CheckExpiredMessagesTime()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.MonitorTime = TimeSpan.FromSeconds(value);

            Assert.AreEqual(TimeSpan.FromSeconds(value), configuration.MonitorTime);
        }
        [TestMethod]
        public void SetAndGet_ClearExpiredMessagesEnabled()
        {
            var configuration = GetConfiguration();
            configuration.Enabled = true;

            Assert.IsTrue(configuration.Enabled);
        }
        [TestMethod]
        public void Get_ClearExpiredMessagesEnabled_DefaultsToFalse()
        {
            var configuration = GetConfiguration();
            Assert.IsFalse(configuration.Enabled);
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
        public void Set_CheckExpiredMessagesTime_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.MonitorTime = TimeSpan.FromHours(value);
              });
        }
        [TestMethod]
        public void Set_ClearExpiredMessagesEnabled_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<bool>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
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
