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
using DotNetWorkQueue.Transport.Redis.Basic;
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisQueueRpcConnectionTests
    {
        [Fact]
        public void Create_Null_Or_Empty_Fails()
        {
            Assert.Throws<ArgumentException>(
           delegate
           {
               var test = new RedisQueueRpcConnection(string.Empty, string.Empty);
               Assert.Null(test);
           });

            Assert.Throws<ArgumentNullException>(
           delegate
           {
               var test = new RedisQueueRpcConnection(null, null);
               Assert.Null(test);
           });

        }
        [Theory, AutoData]
        public void Create_Default(string connection, string queue)
        {
            var test = new RedisQueueRpcConnection(connection, queue);
            test.GetConnection(ConnectionTypes.NotSpecified);
            test.GetConnection(ConnectionTypes.Receive);
            test.GetConnection(ConnectionTypes.Send);
        }
    }
}
