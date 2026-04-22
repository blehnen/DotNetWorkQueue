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
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Factory
{
    [TestClass]
    public class RedisTransportOptionsFactoryTests
    {
        private class TestableFactory : RedisTransportOptionsFactory
        {
            private readonly IDatabase _db;
            public TestableFactory(IRedisConnection connection, RedisNames redisNames, IDatabase db)
                : base(connection, redisNames) { _db = db; }
            protected override IDatabase GetDb() => _db;
        }

        private static TestableFactory CreateFactory(IDatabase db)
        {
            var connection = Substitute.For<IRedisConnection>();
            var connInfo = Substitute.For<IConnectionInformation>();
            var redisNames = Substitute.For<RedisNames>(connInfo);
            redisNames.Configuration.Returns("queue:test:Configuration");
            return new TestableFactory(connection, redisNames, db);
        }

        [TestMethod]
        public void Create_WhenKeyMissing_DoesNotCacheTheDefaultFallback()
        {
            var db = Substitute.For<IDatabase>();
            db.StringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
              .Returns(RedisValue.Null);
            var factory = CreateFactory(db);

            var first = factory.Create();
            var second = factory.Create();

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            // Critical: the key was read on BOTH Create() calls (no sticky-default caching)
            db.Received(2).StringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        }

        [TestMethod]
        public void Create_WhenKeyHasJson_CachesAndReturnsSameInstance()
        {
            var db = Substitute.For<IDatabase>();
            var stored = new RedisBaseTransportOptions { EnableHistory = true };
            db.StringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
              .Returns((RedisValue)JsonConvert.SerializeObject(stored));
            var factory = CreateFactory(db);

            var first = factory.Create();
            var second = factory.Create();

            Assert.AreSame(first, second);
            Assert.IsTrue(first.EnableHistory);
            db.Received(1).StringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>());
        }

        [TestMethod]
        public void Create_AfterDefaultFallback_ThenKeyPopulated_ReturnsLoadedOptions()
        {
            var db = Substitute.For<IDatabase>();
            RedisValue value = RedisValue.Null;
            db.StringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
              .Returns(_ => value);
            var factory = CreateFactory(db);

            var firstDefaults = factory.Create();
            Assert.IsFalse(firstDefaults.EnableHistory);

            value = JsonConvert.SerializeObject(new RedisBaseTransportOptions { EnableHistory = true });
            var second = factory.Create();

            Assert.IsTrue(second.EnableHistory,
                "After options are persisted, the factory must observe the new value.");
        }
    }
}
