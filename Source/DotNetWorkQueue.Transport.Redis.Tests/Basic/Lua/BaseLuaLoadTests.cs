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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Lua
{
    /// <summary>
    /// The script loaders, which are the NOSCRIPT recovery path.
    ///
    /// `TryExecuteAsync` used to recover by calling the synchronous loader, which loads onto every
    /// endpoint in turn - so a Redis restart, a failover or a SCRIPT FLUSH parked a thread-pool thread
    /// for the whole sweep, on the path that exists to avoid that.
    ///
    /// Recovery against a live server is not covered here: forcing a NOSCRIPT needs a SCRIPT FLUSH,
    /// which needs an admin connection the integration tests do not open. What is covered is that
    /// neither loader throws when there is no connection to load against, which is the state they can
    /// be called in while a connection is coming back after a failover.
    /// </summary>
    [TestClass]
    public class BaseLuaLoadTests
    {
        [TestMethod]
        public void LoadScript_WithNoConnection_DoesNothing()
        {
            var lua = Create();

            lua.LoadScript(); //must not throw: there is simply nothing to load onto
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = lua.LoadedLuaScript,
                "a script was somehow loaded without a connection");
        }

        [TestMethod]
        public async Task LoadScriptAsync_WithNoConnection_DoesNothing()
        {
            var lua = Create();

            await lua.LoadScriptAsync(); //must not throw
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = lua.LoadedLuaScript,
                "a script was somehow loaded without a connection");
        }

        [TestMethod]
        public async Task TryExecuteAsync_WhenTheConnectionIsDisposed_ReturnsNothing()
        {
            var connection = Substitute.For<IRedisConnection>();
            connection.IsDisposed.Returns(true);
            var lua = new TestableLua(connection, Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>()));

            var result = await lua.TryExecuteAsync(null);

            Assert.IsTrue(result.IsNull);
        }

        private static TestableLua Create()
        {
            //a substituted connection hands back a null multiplexer, which is what a loader sees while
            //the connection is still coming back
            var connection = Substitute.For<IRedisConnection>();
            connection.IsDisposed.Returns(false);
            return new TestableLua(connection, Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>()));
        }

        private sealed class TestableLua : BaseLua
        {
            public TestableLua(IRedisConnection connection, RedisNames redisNames)
                : base(connection, redisNames)
            {
                Script = "return 1";
            }
        }
    }
}
