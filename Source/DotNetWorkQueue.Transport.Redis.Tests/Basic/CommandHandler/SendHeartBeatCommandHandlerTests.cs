using System;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.CommandHandler;
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
            harness.Db.DidNotReceiveWithAnyArgs()
                .SortedSetUpdate(default, default, default, default(SortedSetWhen), default);
        }

        private sealed class Harness
        {
            public TestableHandler Handler { get; }
            public IDatabase Db { get; }

            public Harness(bool stillWorking)
            {
                Db = Substitute.For<IDatabase>();
                Db.SortedSetUpdate(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<double>(),
                    Arg.Any<SortedSetWhen>(), Arg.Any<CommandFlags>()).Returns(stillWorking);
                Db.SortedSetUpdateAsync(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<double>(),
                    Arg.Any<SortedSetWhen>(), Arg.Any<CommandFlags>()).Returns(Task.FromResult(stillWorking));

                var unixTime = Substitute.For<IUnixTime>();
                unixTime.GetCurrentUnixTimestampMilliseconds().Returns(1_700_000_000_000);
                var unixTimeFactory = Substitute.For<IUnixTimeFactory>();
                unixTimeFactory.Create().Returns(unixTime);

                var connection = Substitute.For<IRedisConnection>();
                connection.IsDisposed.Returns(false);
                var names = Substitute.For<RedisNames>(Substitute.For<IConnectionInformation>());

                Handler = new TestableHandler(unixTimeFactory, connection, names, Db);
            }
        }

        private sealed class TestableHandler : SendHeartBeatCommandHandler
        {
            private readonly IDatabase _db;

            public TestableHandler(IUnixTimeFactory unixTimeFactory, IRedisConnection connection,
                RedisNames redisNames, IDatabase db)
                : base(unixTimeFactory, connection, redisNames)
            {
                _db = db;
            }

            protected override IDatabase GetDb() => _db;
        }
    }
}
