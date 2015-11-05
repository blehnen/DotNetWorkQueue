// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System;
using DotNetWorkQueue.Configuration;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
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

            Assert.Equal(true, configuration.Enabled);
        }
        [Fact]
        public void Get_ClearExpiredMessagesEnabled_DefaultsToFalse()
        {
            var configuration = GetConfiguration();
            Assert.Equal(false, configuration.Enabled);
        }
        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = GetConfiguration();
            Assert.Equal(false, configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.Equal(true, configuration.IsReadOnly);
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
