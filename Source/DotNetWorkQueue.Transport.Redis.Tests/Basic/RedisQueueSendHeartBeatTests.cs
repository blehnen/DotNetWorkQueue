using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    /// <summary>
    /// A beat that did not renew the claim must report no heartbeat, not a heartbeat dated at the epoch -
    /// the worker judges its claim by whether a time came back.
    /// </summary>
    [TestClass]
    public class RedisQueueSendHeartBeatTests
    {
        [TestMethod]
        public void Send_WhenTheClaimWasRenewed_ReportsTheTime()
        {
            var h = new Harness(1_700_000_000_000);
            Assert.IsNotNull(h.Sender.Send(h.Context).LastHeartBeatTime);
        }

        [TestMethod]
        public void Send_WhenTheMessageHasBeenReclaimed_ReportsNoTime()
        {
            var h = new Harness(0);
            Assert.IsNull(h.Sender.Send(h.Context).LastHeartBeatTime);
        }

        [TestMethod]
        public async Task SendAsync_WhenTheClaimWasRenewed_ReportsTheTime()
        {
            var h = new Harness(1_700_000_000_000);
            Assert.IsNotNull((await h.Sender.SendAsync(h.Context)).LastHeartBeatTime);
        }

        [TestMethod]
        public async Task SendAsync_WhenTheMessageHasBeenReclaimed_ReportsNoTime()
        {
            var h = new Harness(0);
            Assert.IsNull((await h.Sender.SendAsync(h.Context)).LastHeartBeatTime);
        }

        [TestMethod]
        public async Task SendAsync_WithNoMessageId_ReportsNothing()
        {
            var h = new Harness(1_700_000_000_000, hasId: false);
            Assert.IsNull(await h.Sender.SendAsync(h.Context));
        }

        private sealed class Harness
        {
            public RedisQueueSendHeartBeat Sender { get; }
            public IMessageContext Context { get; }

            public Harness(long unixTime, bool hasId = true)
            {
                var sync = Substitute.For<ICommandHandlerWithOutput<SendHeartBeatCommand<string>, long>>();
                sync.Handle(Arg.Any<SendHeartBeatCommand<string>>()).Returns(unixTime);
                var async = Substitute.For<ICommandHandlerWithOutputAsync<SendHeartBeatCommand<string>, long>>();
                async.HandleAsync(Arg.Any<SendHeartBeatCommand<string>>()).Returns(Task.FromResult(unixTime));

                var unix = Substitute.For<IUnixTime>();
                unix.DateTimeFromUnixTimestampMilliseconds(Arg.Any<long>())
                    .Returns(new System.DateTime(2026, 1, 1, 0, 0, 0, System.DateTimeKind.Utc));
                var factory = Substitute.For<IUnixTimeFactory>();
                factory.Create().Returns(unix);

                Context = Substitute.For<IMessageContext>();
                if (hasId)
                {
                    var setting = Substitute.For<ISetting>();
                    setting.Value.Returns("a-message");
                    var id = Substitute.For<IMessageId>();
                    id.HasValue.Returns(true);
                    id.Id.Returns(setting);
                    Context.MessageId.Returns(id);
                }

                Sender = new RedisQueueSendHeartBeat(sync, async, factory);
            }
        }
    }
}
