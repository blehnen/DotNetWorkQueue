using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.CommandHandler;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.CommandHandler
{
    /// <summary>
    /// The heartbeat has to say whether it actually renewed the claim.
    ///
    /// It used to call SortedSetAdd with When.Exists and throw the result away - which it had to, since
    /// that overload reports whether a member was *added*, and with When.Exists it never is. A message
    /// the monitor had already reclaimed still came back with a fresh timestamp, so a worker could never
    /// learn it had lost the message.
    /// </summary>
    [TestClass]
    public class SendHeartBeatCommandHandlerTests
    {
        [TestMethod]
        public void Handle_WhenTheMessageIsStillInTheWorkingSet_ReturnsTheTime()
        {
            var harness = new Harness(stillWorking: true);
            var result = harness.Handler.Handle(new SendHeartBeatCommand<string>("a-message"));
            Assert.IsGreaterThan(0, result);
        }

        [TestMethod]
        public void Handle_WhenTheMessageHasBeenReclaimed_ReportsNoHeartbeat()
        {
            var harness = new Harness(stillWorking: false);
            Assert.AreEqual(0, harness.Handler.Handle(new SendHeartBeatCommand<string>("a-message")));
        }

        [TestMethod]
        public async Task HandleAsync_WhenTheMessageIsStillInTheWorkingSet_ReturnsTheTime()
        {
            var harness = new Harness(stillWorking: true);
            var result = await harness.Handler.HandleAsync(new SendHeartBeatCommand<string>("a-message"));
            Assert.IsGreaterThan(0, result);
        }

        [TestMethod]
        public async Task HandleAsync_WhenTheMessageHasBeenReclaimed_ReportsNoHeartbeat()
        {
            var harness = new Harness(stillWorking: false);
            Assert.AreEqual(0, await harness.Handler.HandleAsync(new SendHeartBeatCommand<string>("a-message")));
        }

        [TestMethod]
        public void Handle_WithNoMessageId_DoesNotTouchRedis()
        {
            var harness = new Harness(stillWorking: true);
            Assert.AreEqual(0, harness.Handler.Handle(new SendHeartBeatCommand<string>(string.Empty)));
            Assert.AreEqual(0, harness.Lua.Calls, "a message with no id still reached redis");
        }

        private sealed class Harness
        {
            public TestableHandler Handler { get; }
            public IDatabase Db { get; }
            public FakeHeartBeatLua Lua { get; }

            public Harness(bool stillWorking)
            {
                Db = Substitute.For<IDatabase>();

                var unixTime = Substitute.For<IUnixTime>();
                unixTime.GetCurrentUnixTimestampMilliseconds().Returns(1_700_000_000_000);
                var unixTimeFactory = Substitute.For<IUnixTimeFactory>();
                unixTimeFactory.Create().Returns(unixTime);

                var connection = Substitute.For<IRedisConnection>();
                connection.IsDisposed.Returns(false);
                var names = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());

                //the claim check lives in the script now, so that is what decides whether a beat lands
                Lua = new FakeHeartBeatLua(connection, names, stillWorking ? 1_700_000_000_000 : 0);
                Handler = new TestableHandler(unixTimeFactory, connection, Db, Lua);
            }
        }

        /// <summary>
        /// Stands in for the real script: the compare-and-set runs inside redis, so a unit test can only
        /// say what answer came back.
        /// </summary>
        private sealed class FakeHeartBeatLua : HeartBeatLua
        {
            private readonly long _result;
            public int Calls { get; private set; }

            public FakeHeartBeatLua(IRedisConnection connection, RedisNames redisNames, long result)
                : base(connection, redisNames)
            {
                _result = result;
            }

            public override long Execute(string messageId, long timestamp, long? previousTimestamp)
            {
                Calls++;
                return _result;
            }

            public override Task<long> ExecuteAsync(string messageId, long timestamp, long? previousTimestamp)
            {
                Calls++;
                return Task.FromResult(_result);
            }
        }

        private sealed class TestableHandler : SendHeartBeatCommandHandler
        {
            private readonly IDatabase _db;

            public TestableHandler(IUnixTimeFactory unixTimeFactory, IRedisConnection connection,
                IDatabase db, HeartBeatLua heartBeatLua)
                : base(unixTimeFactory, connection, heartBeatLua)
            {
                _db = db;
            }

            protected override IDatabase GetDb() => _db;
        }
    }
}
