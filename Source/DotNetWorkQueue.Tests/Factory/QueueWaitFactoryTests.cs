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
using DotNetWorkQueue.Factory;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class QueueWaitFactoryTests
    {
        [Fact]
        public void Create_Enabled()
        {
            var factory = Create(true);
            var test1 = factory.CreateQueueDelay();
            var test2 = factory.CreateFatalErrorDelay();

            Assert.IsNotType<INoOperation>(test1);
            Assert.IsNotType<INoOperation>(test2);
        }

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
