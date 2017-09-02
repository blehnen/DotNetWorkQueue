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
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class RollbackMessageCommandTests
    {
        [Theory, AutoData]
        public void Create_Null_Constructor_Time_Ok(string number)
        {
            var test = new RollbackMessageCommand(new RedisQueueId(number), null);
            Assert.NotNull(test);
        }
        [Theory, AutoData]
        public void Create_Default(string number)
        {
            var id = new RedisQueueId(number);
            var test = new RollbackMessageCommand(id, null);
            Assert.Equal(id, test.Id);
            Assert.Null(test.IncreaseQueueDelay);

            TimeSpan? time = TimeSpan.MinValue;
            test = new RollbackMessageCommand(id, time);
            Assert.Equal(id, test.Id);
            Assert.Equal(time, test.IncreaseQueueDelay);
        }
    }
}
