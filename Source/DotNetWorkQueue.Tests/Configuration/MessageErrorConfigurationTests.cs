using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;
using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class MessageErrorConfigurationTests
    {
        [Theory, AutoData]
        public void MessageAge_Test(TimeSpan messageAge)
        {
            var test = new MessageErrorConfiguration
            {
                MessageAge = messageAge
            };
            Assert.Equal(messageAge, test.MessageAge);
        }
        [Theory, AutoData]
        public void MonitorTime_Test(TimeSpan monitorTime)
        {
            var test = new MessageErrorConfiguration
            {
                MonitorTime = monitorTime
            };
            Assert.Equal(monitorTime, test.MonitorTime);
        }
        [Fact]
        public void Enabled_Test()
        {
            var test = new MessageErrorConfiguration();
            Assert.True(test.Enabled);
            test.Enabled = false;
            Assert.False(test.Enabled);
        }

        [Fact]
        public void Defaults_Test()
        {
            var test = new MessageErrorConfiguration();
            Assert.Equal(TimeSpan.FromDays(30), test.MessageAge);
            Assert.Equal(TimeSpan.FromDays(1), test.MonitorTime);
        }

        [Theory, AutoData]
        public void ReadOnly_Test(TimeSpan monitorTime)
        {
            var test = new MessageErrorConfiguration
            {
                MonitorTime = monitorTime
            };
            Assert.False(test.IsReadOnly);
            test.SetReadOnly();
            Assert.True(test.IsReadOnly);

            Assert.Throws<InvalidOperationException>(
                delegate
                {
                    test.MonitorTime = TimeSpan.FromSeconds(1);
                });
        }
    }
}
