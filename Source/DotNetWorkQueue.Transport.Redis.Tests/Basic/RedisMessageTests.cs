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
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisMessageTests
    {
        [Fact]
        public void Create_Null_Message_OK()
        {
            var test = new RedisMessage(null, null, false);
            Assert.Null(test.Message);
        }
        [Fact]
        public void Create_Message()
        {
            var message = NSubstitute.Substitute.For<IReceivedMessageInternal>();
            var test = new RedisMessage("1", message, false);
            Assert.Equal(message, test.Message);
            Assert.Equal("1", test.MessageId);
        }
        [Fact]
        public void Create_Null_Message_Expired_False()
        {
            var test = new RedisMessage("1", null, false);
            Assert.False(test.Expired);
        }
        [Fact]
        public void Create_Null_Message_Expired_True()
        {
            var test = new RedisMessage("1", null, true);
            Assert.True(test.Expired);
        }
    }
}
