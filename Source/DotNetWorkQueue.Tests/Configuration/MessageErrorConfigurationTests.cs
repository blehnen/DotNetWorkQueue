using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class MessageErrorConfigurationTests
    {
        [TestMethod]
        public void MessageAge_Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageAge = fixture.Create<TimeSpan>();
            var test = new MessageErrorConfiguration
            {
                MessageAge = messageAge
            };
            Assert.AreEqual(messageAge, test.MessageAge);
        }
        [TestMethod]
        public void MonitorTime_Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var monitorTime = fixture.Create<TimeSpan>();
            var test = new MessageErrorConfiguration
            {
                MonitorTime = monitorTime
            };
            Assert.AreEqual(monitorTime, test.MonitorTime);
        }
        [TestMethod]
        public void Enabled_Test()
        {
            var test = new MessageErrorConfiguration();
            Assert.IsTrue(test.Enabled);
            test.Enabled = false;
            Assert.IsFalse(test.Enabled);
        }

        [TestMethod]
        public void Defaults_Test()
        {
            var test = new MessageErrorConfiguration();
            Assert.AreEqual(TimeSpan.FromDays(30), test.MessageAge);
            Assert.AreEqual(TimeSpan.FromDays(1), test.MonitorTime);
        }

        [TestMethod]
        public void ReadOnly_Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var monitorTime = fixture.Create<TimeSpan>();
            var test = new MessageErrorConfiguration
            {
                MonitorTime = monitorTime
            };
            Assert.IsFalse(test.IsReadOnly);
            test.SetReadOnly();
            Assert.IsTrue(test.IsReadOnly);

            Assert.ThrowsExactly<InvalidOperationException>(
                delegate
                {
                    test.MonitorTime = TimeSpan.FromSeconds(1);
                });
        }
    }
}
