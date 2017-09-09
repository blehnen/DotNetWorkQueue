// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
            Assert.Equal(false, configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.Equal(true, configuration.IsReadOnly);
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
