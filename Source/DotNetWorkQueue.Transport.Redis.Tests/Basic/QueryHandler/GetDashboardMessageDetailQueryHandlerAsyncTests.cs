// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.QueryHandler;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardMessageDetailQueryHandlerAsyncTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var redisHeaders = Substitute.For<RedisHeaders>(Substitute.For<IMessageContextDataFactory>(), Substitute.For<IHeaders>());
            var serializer = Substitute.For<IInternalSerializer>();
            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            Assert.IsNotNull(new GetDashboardMessageDetailQueryHandlerAsync(connection, redisNames, redisHeaders, serializer, compositeSerialization));
        }

        [TestMethod]
        public void Create_NullConnection_Throws()
        {
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var redisHeaders = Substitute.For<RedisHeaders>(Substitute.For<IMessageContextDataFactory>(), Substitute.For<IHeaders>());
            var serializer = Substitute.For<IInternalSerializer>();
            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetDashboardMessageDetailQueryHandlerAsync(null, redisNames, redisHeaders, serializer, compositeSerialization));
        }

        [TestMethod]
        public void Create_NullRedisNames_Throws()
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisHeaders = Substitute.For<RedisHeaders>(Substitute.For<IMessageContextDataFactory>(), Substitute.For<IHeaders>());
            var serializer = Substitute.For<IInternalSerializer>();
            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetDashboardMessageDetailQueryHandlerAsync(connection, null, redisHeaders, serializer, compositeSerialization));
        }

        [TestMethod]
        public void Create_NullRedisHeaders_Throws()
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var serializer = Substitute.For<IInternalSerializer>();
            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetDashboardMessageDetailQueryHandlerAsync(connection, redisNames, null, serializer, compositeSerialization));
        }

        [TestMethod]
        public void Create_NullSerializer_Throws()
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var redisHeaders = Substitute.For<RedisHeaders>(Substitute.For<IMessageContextDataFactory>(), Substitute.For<IHeaders>());
            var compositeSerialization = Substitute.For<ICompositeSerialization>();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetDashboardMessageDetailQueryHandlerAsync(connection, redisNames, redisHeaders, null, compositeSerialization));
        }

        [TestMethod]
        public void Create_NullCompositeSerialization_Throws()
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            var redisHeaders = Substitute.For<RedisHeaders>(Substitute.For<IMessageContextDataFactory>(), Substitute.For<IHeaders>());
            var serializer = Substitute.For<IInternalSerializer>();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetDashboardMessageDetailQueryHandlerAsync(connection, redisNames, redisHeaders, serializer, null));
        }
    }
}
