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
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisSimpleBatchSizeTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new RedisSimpleBatchSize();
            Assert.Equal(1, test.BatchSize(1));
            Assert.Equal(25, test.BatchSize(25));
            Assert.Equal(50, test.BatchSize(50));
            Assert.Equal(40, test.BatchSize(80));
            Assert.Equal(50, test.BatchSize(100));
            Assert.Equal(250, test.BatchSize(500));
            Assert.Equal(256, test.BatchSize(512));
            Assert.Equal(256, test.BatchSize(10000));
            Assert.Equal(256, test.BatchSize(25000));
            Assert.Equal(256, test.BatchSize(int.MaxValue));
        }
    }
}
