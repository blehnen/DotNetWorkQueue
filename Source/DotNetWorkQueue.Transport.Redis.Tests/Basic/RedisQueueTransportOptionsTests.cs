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
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisQueueTransportOptionsTests
    {
        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = new RedisQueueTransportOptions(new SntpTimeConfiguration(),
                new DelayedProcessingConfiguration());
            Assert.Equal(false, configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = new RedisQueueTransportOptions(new SntpTimeConfiguration(),
               new DelayedProcessingConfiguration());
            configuration.SetReadOnly();
            Assert.Equal(true, configuration.IsReadOnly);
        }
        [Fact]
        public void Create_Default()
        {
            var sntpTime = new SntpTimeConfiguration();
            var delay = new DelayedProcessingConfiguration();
            var test = new RedisQueueTransportOptions(sntpTime,
                delay);
            Assert.Equal(sntpTime, test.SntpTimeConfiguration);
            Assert.Equal(delay, test.DelayedProcessingConfiguration);

            test.ClearExpiredMessagesBatchLimit = 1000;
            Assert.Equal(1000, test.ClearExpiredMessagesBatchLimit);

            test.MessageIdLocation = MessageIdLocations.Custom;
            Assert.Equal(MessageIdLocations.Custom, test.MessageIdLocation);

            test.MoveDelayedMessagesBatchLimit = 1000;
            Assert.Equal(1000, test.MoveDelayedMessagesBatchLimit);

            test.TimeServer = TimeLocations.Custom;
            Assert.Equal(TimeLocations.Custom, test.TimeServer);
        }
    }
}
