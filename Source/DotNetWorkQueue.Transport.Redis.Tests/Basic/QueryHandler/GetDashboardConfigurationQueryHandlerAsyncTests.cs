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
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.QueryHandler;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardConfigurationQueryHandlerAsyncTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var connection = Substitute.For<IRedisConnection>();
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            Assert.IsNotNull(new GetDashboardConfigurationQueryHandlerAsync(connection, redisNames));
        }

        [TestMethod]
        public void Create_NullConnection_Throws()
        {
            var redisNames = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetDashboardConfigurationQueryHandlerAsync(null, redisNames));
        }

        [TestMethod]
        public void Create_NullRedisNames_Throws()
        {
            var connection = Substitute.For<IRedisConnection>();
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new GetDashboardConfigurationQueryHandlerAsync(connection, null));
        }

        private class TestableHandler : GetDashboardConfigurationQueryHandlerAsync
        {
            private readonly IDatabase _db;
            public TestableHandler(IRedisConnection connection, RedisNames redisNames, IDatabase db)
                : base(connection, redisNames) { _db = db; }
            protected override IDatabase GetDb() => _db;
        }

        private static TestableHandler CreateHandler(IDatabase db)
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            redisNames.Configuration.Returns("queue:test:Configuration");
            return new TestableHandler(connection, redisNames, db);
        }

        [TestMethod]
        public async Task HandleAsync_ReturnsBytes_WhenKeyHasValue()
        {
            const string json = "{\"EnableHistory\":true}";
            var db = Substitute.For<IDatabase>();
            db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
              .Returns(Task.FromResult<RedisValue>(json));

            var handler = CreateHandler(db);
            var result = await handler.HandleAsync(new GetDashboardConfigurationQuery());

            Assert.IsNotNull(result);
            Assert.AreEqual(json, Encoding.UTF8.GetString(result));
        }

        [TestMethod]
        public async Task HandleAsync_ReturnsNull_WhenKeyHasNoValue()
        {
            var db = Substitute.For<IDatabase>();
            db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
              .Returns(Task.FromResult<RedisValue>(RedisValue.Null));

            var handler = CreateHandler(db);
            var result = await handler.HandleAsync(new GetDashboardConfigurationQuery());

            Assert.IsNull(result);
        }
    }
}
