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

using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisNamesTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new RedisNames(CreateConnection());
            Assert.Contains("testQueue", test.Values);
            Assert.Contains("testQueue", test.Delayed);
            Assert.Contains("testQueue", test.Error);
            Assert.Contains("testQueue", test.Expiration);
            Assert.Contains("testQueue", test.Id);
            Assert.Contains("testQueue", test.MetaData);
            Assert.Contains("testQueue", test.Notification);
            Assert.Contains("testQueue", test.Pending);
            Assert.Contains("testQueue", test.Working);
            Assert.Contains("testQueue", test.Headers);
        }

        public IConnectionInformation CreateConnection()
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("testQueue");
            return connection;
        }
    }
}
