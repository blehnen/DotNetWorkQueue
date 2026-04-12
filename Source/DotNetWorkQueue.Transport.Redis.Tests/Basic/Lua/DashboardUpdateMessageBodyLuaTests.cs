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
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Lua
{
    [TestClass]
    public class DashboardUpdateMessageBodyLuaTests
    {
        private static TestableDashboardUpdateMessageBodyLua CreateSut()
        {
            var connection = Substitute.For<IRedisConnection>();
            var names = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());
            return new TestableDashboardUpdateMessageBodyLua(connection, names);
        }

        [TestMethod]
        public void Constructor_SetsScript()
        {
            var sut = CreateSut();

            Assert.IsNotNull(sut.Script);
            Assert.IsTrue(sut.Script.Contains("HEXISTS"));
            Assert.IsTrue(sut.Script.Contains("HSET"));
            Assert.IsTrue(sut.Script.Contains("@valueskey"));
            Assert.IsTrue(sut.Script.Contains("@headerskey"));
            Assert.IsTrue(sut.Script.Contains("@uuid"));
            Assert.IsTrue(sut.Script.Contains("@body"));
            Assert.IsTrue(sut.Script.Contains("@headers"));
        }

        [TestMethod]
        public void Execute_WhenScriptReturnsOne_ReturnsOne()
        {
            var sut = CreateSut();
            sut.NextResult = RedisResult.Create((RedisValue)1);

            var result = sut.Execute("abc-123", new byte[] { 1, 2, 3 }, new byte[] { 4, 5, 6 });

            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void Execute_WhenScriptReturnsZero_ReturnsZero()
        {
            var sut = CreateSut();
            sut.NextResult = RedisResult.Create((RedisValue)0);

            var result = sut.Execute("missing", new byte[] { 1 }, new byte[] { 2 });

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void Execute_WhenScriptReturnsNull_ReturnsZero()
        {
            var sut = CreateSut();
            sut.NextResult = RedisResult.Create(RedisValue.Null);

            var result = sut.Execute("abc-123", new byte[] { 1 }, new byte[] { 2 });

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void Execute_WhenConnectionDisposed_ReturnsZero()
        {
            // Simulates BaseLua.TryExecute returning RedisValue.Null when Connection.IsDisposed.
            var sut = CreateSut();
            sut.NextResult = RedisResult.Create(RedisValue.Null);

            var result = sut.Execute("abc-123", new byte[] { 1 }, new byte[] { 2 });

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void Execute_PassesParameters_ToTryExecute()
        {
            var sut = CreateSut();
            sut.NextResult = RedisResult.Create((RedisValue)1);

            sut.Execute("message-42", new byte[] { 9 }, new byte[] { 8 });

            Assert.IsNotNull(sut.LastParameters);
            // The parameters object is an anonymous type with the expected property names.
            var type = sut.LastParameters.GetType();
            Assert.IsNotNull(type.GetProperty("valueskey"));
            Assert.IsNotNull(type.GetProperty("headerskey"));
            Assert.IsNotNull(type.GetProperty("uuid"));
            Assert.IsNotNull(type.GetProperty("body"));
            Assert.IsNotNull(type.GetProperty("headers"));

            Assert.AreEqual("message-42", type.GetProperty("uuid").GetValue(sut.LastParameters));
        }

        /// <summary>
        /// Test subclass that overrides the virtual TryExecute to avoid touching a real Redis connection.
        /// Relies on Plan 1.1 which made BaseLua.TryExecute virtual.
        /// </summary>
        private class TestableDashboardUpdateMessageBodyLua : DashboardUpdateMessageBodyLua
        {
            public TestableDashboardUpdateMessageBodyLua(IRedisConnection connection, RedisNames redisNames)
                : base(connection, redisNames)
            {
            }

            public RedisResult NextResult { get; set; } = RedisResult.Create(RedisValue.Null);
            public object LastParameters { get; private set; }

            public override RedisResult TryExecute(object parameters)
            {
                LastParameters = parameters;
                return NextResult;
            }
        }
    }
}
