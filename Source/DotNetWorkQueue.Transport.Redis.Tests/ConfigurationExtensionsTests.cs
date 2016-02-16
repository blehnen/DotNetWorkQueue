// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis.Basic;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Transport.Redis.Tests
{
    /// <summary>
    /// 
    /// <remarks>Options are only set via the IoC container</remarks>
    /// </summary>
    public class ConfigurationExtensionsTests
    {
        [Fact]
        public void AdditionalMessageData()
        {      
            IAdditionalMessageData test = new AdditionalMessageData();
            test.SetDelay(TimeSpan.FromSeconds(1));
            Assert.Equal(TimeSpan.FromSeconds(1), test.GetDelay());

            test.SetExpiration(TimeSpan.FromSeconds(5));
            Assert.Equal(TimeSpan.FromSeconds(5), test.GetExpiration());

            test.SetExpiration(null);
            Assert.Null(test.GetExpiration());
        }

        [Fact]
        public void ConfigurationReceive()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IConfiguration config = fixture.Create<AdditionalConfiguration>();
            fixture.Inject(config);
            var configuration = fixture.Create<QueueConfigurationReceive>();
            var options = fixture.Create<RedisQueueTransportOptions>();
            configuration.AdditionalConfiguration.SetSetting("RedisQueueTransportOptions", options);

            Assert.NotNull(configuration.Options());
        }

        [Fact]
        public void ConfigurationSend()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IConfiguration config = fixture.Create<AdditionalConfiguration>();
            fixture.Inject(config);
            var configuration = fixture.Create<QueueConfigurationSend>();
            var options = fixture.Create<RedisQueueTransportOptions>();
            configuration.AdditionalConfiguration.SetSetting("RedisQueueTransportOptions", options);

            Assert.NotNull(configuration.Options());
        }
    }
}
