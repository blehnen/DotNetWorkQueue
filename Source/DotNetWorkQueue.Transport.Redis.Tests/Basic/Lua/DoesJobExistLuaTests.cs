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
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Lua
{
    [TestClass]
    public class DoesJobExistLuaTests
    {
        [TestMethod]
        public void Execute_ConnectionDisposed_ReturnsNotQueued()
        {
            var connection = CreateConnection(isDisposed: true);
            var redisNames = new RedisNames(CreateConnectionInformation());
            var sut = new TestableDoesJobExistLua(connection, redisNames);

            var result = sut.Execute("job1", DateTimeOffset.UtcNow);

            Assert.AreEqual(QueueStatuses.NotQueued, result);
            Assert.IsFalse(sut.TryExecuteCalled, "TryExecute should not be invoked when connection is disposed");
        }

        [TestMethod]
        public void Execute_NullResult_ReturnsNotQueued()
        {
            var connection = CreateConnection(isDisposed: false);
            var redisNames = new RedisNames(CreateConnectionInformation());
            var sut = new TestableDoesJobExistLua(connection, redisNames)
            {
                NextResult = RedisResult.Create(RedisValue.Null)
            };

            var result = sut.Execute("job1", DateTimeOffset.UtcNow);

            Assert.AreEqual(QueueStatuses.NotQueued, result);
            Assert.IsTrue(sut.TryExecuteCalled);
        }

        [TestMethod]
        public void Execute_ProcessedResult_ReturnsProcessed()
        {
            var connection = CreateConnection(isDisposed: false);
            var redisNames = new RedisNames(CreateConnectionInformation());
            var sut = new TestableDoesJobExistLua(connection, redisNames)
            {
                NextResult = RedisResult.Create((RedisValue)(int)QueueStatuses.Processed)
            };

            var result = sut.Execute("job1", DateTimeOffset.UtcNow);

            Assert.AreEqual(QueueStatuses.Processed, result);
        }

        [TestMethod]
        public void Execute_WaitingResult_ReturnsWaiting()
        {
            var connection = CreateConnection(isDisposed: false);
            var redisNames = new RedisNames(CreateConnectionInformation());
            var sut = new TestableDoesJobExistLua(connection, redisNames)
            {
                NextResult = RedisResult.Create((RedisValue)(int)QueueStatuses.Waiting)
            };

            var result = sut.Execute("job1", DateTimeOffset.UtcNow);

            Assert.AreEqual(QueueStatuses.Waiting, result);
        }

        [TestMethod]
        public void Execute_ProcessingResult_ReturnsProcessing()
        {
            var connection = CreateConnection(isDisposed: false);
            var redisNames = new RedisNames(CreateConnectionInformation());
            var sut = new TestableDoesJobExistLua(connection, redisNames)
            {
                NextResult = RedisResult.Create((RedisValue)(int)QueueStatuses.Processing)
            };

            var result = sut.Execute("job1", DateTimeOffset.UtcNow);

            Assert.AreEqual(QueueStatuses.Processing, result);
        }

        [TestMethod]
        public void Execute_ErrorResult_ReturnsError()
        {
            var connection = CreateConnection(isDisposed: false);
            var redisNames = new RedisNames(CreateConnectionInformation());
            var sut = new TestableDoesJobExistLua(connection, redisNames)
            {
                NextResult = RedisResult.Create((RedisValue)(int)QueueStatuses.Error)
            };

            var result = sut.Execute("job1", DateTimeOffset.UtcNow);

            Assert.AreEqual(QueueStatuses.Error, result);
        }

        [TestMethod]
        public void Execute_PassesParametersToTryExecute()
        {
            var connection = CreateConnection(isDisposed: false);
            var redisNames = new RedisNames(CreateConnectionInformation());
            var sut = new TestableDoesJobExistLua(connection, redisNames)
            {
                NextResult = RedisResult.Create(RedisValue.Null)
            };

            sut.Execute("myJob", DateTimeOffset.UtcNow);

            Assert.IsNotNull(sut.LastParameters, "Parameters should have been passed to TryExecute");
        }

        private static IRedisConnection CreateConnection(bool isDisposed)
        {
            var connection = Substitute.For<IRedisConnection>();
            connection.IsDisposed.Returns(isDisposed);
            return connection;
        }

        private static IConnectionInformation CreateConnectionInformation()
        {
            var info = Substitute.For<IConnectionInformation>();
            info.QueueName.Returns("testQueue");
            return info;
        }

        private class TestableDoesJobExistLua : DoesJobExistLua
        {
            public TestableDoesJobExistLua(IRedisConnection connection, RedisNames redisNames)
                : base(connection, redisNames)
            {
            }

            public RedisResult NextResult { get; set; } = RedisResult.Create(RedisValue.Null);

            public bool TryExecuteCalled { get; private set; }

            public object LastParameters { get; private set; }

            public override RedisResult TryExecute(object parameters)
            {
                TryExecuteCalled = true;
                LastParameters = parameters;
                return NextResult;
            }
        }
    }
}
