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

using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Xunit;
namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    public class MessageQueueIdTests
    {
        [Fact]
        public void Create_Default()
        {
            long id = 1;
            var test = new MessageQueueId(id);
            Assert.Equal(id, test.Id.Value);
            Assert.True(test.HasValue);
        }
        [Fact]
        public void Create_Default_ToString()
        {
            long id = 1;
            var test = new MessageQueueId(id);
            Assert.Equal("1", test.ToString());
        }
        [Fact]
        public void Create_Default_0()
        {
            long id = 0;
            var test = new MessageQueueId(id);
            Assert.Equal(id, test.Id.Value);
            Assert.False(test.HasValue);
        }
    }
}
