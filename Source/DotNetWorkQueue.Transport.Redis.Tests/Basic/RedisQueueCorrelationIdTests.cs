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
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisQueueCorrelationIdTests
    {
        [Fact]
        public void Create_Default()
        {
            var id = Guid.NewGuid();
            var test = new RedisQueueCorrelationId(id);
            Assert.Equal(id, test.Id.Value);
            Assert.True(test.HasValue);
        }
        [Fact]
        public void Create_Default_ToString()
        {
            var id = Guid.NewGuid();
            var test = new RedisQueueCorrelationId(id);
            Assert.Equal(id.ToString(), test.ToString());
        }
        [Fact]
        public void Create_Default_Empty_Guid()
        {
            var id = Guid.Empty;
            var test = new RedisQueueCorrelationId(id);
            Assert.Equal(id, test.Id.Value);
            Assert.False(test.HasValue);
        }
        [Fact]
        public void Create_Default_Null_Serialized()
        {
            var test = new RedisQueueCorrelationId(null);
            Assert.Equal(Guid.Empty.ToString(), test.Id.Value.ToString());
            Assert.False(test.HasValue);
        }

        [Fact]
        public void Create_Default_Serialized()
        {
            var id = Guid.NewGuid();
            var input = new RedisQueueCorrelationIdSerialized(id);
            var test = new RedisQueueCorrelationId(input);
            Assert.Equal(id.ToString(), test.Id.Value.ToString());
            Assert.True(test.HasValue);
        }
    }
}
