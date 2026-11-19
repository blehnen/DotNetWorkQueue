using System;
using System.Collections.Concurrent;
using System.Linq;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Queue
{
    /// <summary>
    /// The claim and lapse decisions, on times the test chooses (GitHub #419).
    /// </summary>
    /// <remarks>
    /// <see cref="HeartBeatWorkerTests"/> covers the same rules through the timer and real time; these
    /// put the clock exactly on each boundary.
    /// </remarks>
    [TestClass]
    public class HeartBeatClaimTests
    {
        private static readonly TimeSpan Expiry = TimeSpan.FromSeconds(60);
        private static readonly DateTime PickedUp = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public void A_Claim_Lapses_Exactly_At_The_Expiry()
        {
            var harness = new Harness();
            harness.Claim.Begin();

            harness.Now = PickedUp + Expiry - TimeSpan.FromTicks(1);
            Assert.IsFalse(harness.Claim.HasLapsed());

            harness.Now = PickedUp + Expiry;
            Assert.IsTrue(harness.Claim.HasLapsed());
        }

        [TestMethod]
        public void With_No_Expiry_A_Claim_Never_Lapses()
        {
            var harness = new Harness(expiry: TimeSpan.Zero);
            harness.Claim.Begin();

            harness.Now = PickedUp + TimeSpan.FromDays(1);

            Assert.IsFalse(harness.Claim.HasLapsed());
            Assert.IsFalse(harness.Claim.TryGiveUpAnUnprovableClaim());
        }

        [TestMethod]
        public void A_Landed_Beat_Restarts_The_Window_From_When_It_Was_Sent()
        {
            var harness = new Harness();
            harness.Claim.Begin();
            var sentAt = PickedUp + TimeSpan.FromSeconds(30);
            var status = harness.Landed(sentAt + TimeSpan.FromSeconds(5));

            //the reply arrives 20 seconds after the send; the window still starts at the send
            harness.Now = sentAt + TimeSpan.FromSeconds(20);
            harness.Claim.BeatLanded(status, sentAt);

            Assert.AreSame(status, harness.Context.WorkerNotification.HeartBeat.Status);
            harness.Now = sentAt + Expiry - TimeSpan.FromTicks(1);
            Assert.IsFalse(harness.Claim.HasLapsed());
            harness.Now = sentAt + Expiry;
            Assert.IsTrue(harness.Claim.HasLapsed());
        }

        [TestMethod]
        public void A_Claim_With_Nothing_To_Prove_It_Is_Given_Up_Only_Once_It_Has_Lapsed()
        {
            var harness = new Harness();
            harness.Claim.Begin();

            harness.Now = PickedUp + Expiry - TimeSpan.FromTicks(1);
            Assert.IsFalse(harness.Claim.TryGiveUpAnUnprovableClaim(), "a fresh claim cannot have been reset");
            Assert.IsFalse(harness.Claim.IsLost);

            harness.Now = PickedUp + Expiry;
            Assert.IsTrue(harness.Claim.TryGiveUpAnUnprovableClaim());
            Assert.IsTrue(harness.Claim.IsLost);
            Assert.AreEqual(1, harness.Count(LogLevel.Error));
        }

        [TestMethod]
        public void A_Late_Claim_The_DeQueue_Stamped_Is_Not_Given_Up()
        {
            //the stamp gives the next beat a value to name, so it is sent rather than abandoned (GitHub #336)
            var harness = new Harness(claimedAt: PickedUp);
            harness.Claim.Begin();

            harness.Now = PickedUp + Expiry + TimeSpan.FromSeconds(1);

            Assert.IsFalse(harness.Claim.TryGiveUpAnUnprovableClaim());
            Assert.IsFalse(harness.Claim.IsLost);
        }

        [TestMethod]
        public void A_Late_Claim_With_A_Landed_Beat_Is_Not_Given_Up()
        {
            var harness = new Harness();
            harness.Claim.Begin();
            harness.Claim.BeatLanded(harness.Landed(PickedUp), PickedUp);

            harness.Now = PickedUp + Expiry + TimeSpan.FromSeconds(1);

            Assert.IsFalse(harness.Claim.TryGiveUpAnUnprovableClaim());
        }

        [TestMethod]
        public void A_Beat_That_Matched_No_Record_While_Fresh_Keeps_The_Claim()
        {
            //the message finished while the beat was in flight
            var harness = new Harness();
            harness.Claim.Begin();
            harness.Now = PickedUp + Expiry - TimeSpan.FromTicks(1);

            Assert.IsFalse(harness.Claim.BeatMatchedNoRecord(harness.NoRecord()));
            Assert.IsFalse(harness.Claim.IsLost);
            Assert.AreEqual(0, harness.Count(LogLevel.Error));
            Assert.AreEqual(1, harness.Count(LogLevel.Debug));
        }

        [TestMethod]
        public void A_Beat_That_Matched_No_Record_After_The_Expiry_Loses_The_Claim()
        {
            var harness = new Harness();
            harness.Claim.Begin();
            harness.Now = PickedUp + Expiry;

            Assert.IsTrue(harness.Claim.BeatMatchedNoRecord(harness.NoRecord()));
            Assert.IsTrue(harness.Claim.IsLost);
            Assert.AreEqual(1, harness.Count(LogLevel.Error));
        }

        [TestMethod]
        public void A_Lost_Claim_Stays_Lost_After_A_Beat_Lands()
        {
            var harness = new Harness();
            harness.Claim.Begin();
            harness.Now = PickedUp + Expiry;
            Assert.IsTrue(harness.Claim.BeatMatchedNoRecord(harness.NoRecord()));

            harness.Claim.BeatLanded(harness.Landed(harness.Now), harness.Now);

            Assert.IsTrue(harness.Claim.IsLost);
        }

        [TestMethod]
        public void The_Late_Warning_Is_Logged_Once_Per_Lapse()
        {
            var harness = new Harness();
            harness.Claim.Begin();

            harness.Now = PickedUp + Expiry - TimeSpan.FromTicks(1);
            harness.Claim.WarnOnceIfLate();
            Assert.AreEqual(0, harness.Count(LogLevel.Warning), "not late yet");

            harness.Now = PickedUp + Expiry;
            harness.Claim.WarnOnceIfLate();
            harness.Claim.WarnOnceIfLate();
            Assert.AreEqual(1, harness.Count(LogLevel.Warning));

            //a beat lands, and the next lapse is reported again
            harness.Claim.BeatLanded(harness.Landed(harness.Now), harness.Now);
            harness.Now += Expiry;
            harness.Claim.WarnOnceIfLate();
            Assert.AreEqual(2, harness.Count(LogLevel.Warning));
            Assert.IsFalse(harness.Claim.IsLost, "being late is not losing the claim");
        }

        [TestMethod]
        public void Begin_Publishes_The_Heartbeat_The_DeQueue_Stamped()
        {
            var harness = new Harness(claimedAt: PickedUp.AddSeconds(-1));

            harness.Claim.Begin();

            var status = harness.Context.WorkerNotification.HeartBeat.Status;
            Assert.IsNotNull(status);
            Assert.AreEqual(PickedUp.AddSeconds(-1), status.LastHeartBeatTime);
            Assert.AreSame(harness.Context.MessageId, status.MessageId);
        }

        [TestMethod]
        public void Begin_Publishes_Nothing_Without_A_Stamp_Or_A_Message_Id()
        {
            var noStamp = new Harness();
            noStamp.Claim.Begin();
            Assert.IsNull(noStamp.Context.WorkerNotification.HeartBeat.Status);

            var noId = new Harness(claimedAt: PickedUp);
            noId.Context.MessageId.HasValue.Returns(false);
            noId.Claim.Begin();
            Assert.IsNull(noId.Context.WorkerNotification.HeartBeat.Status);
        }

        private sealed class Harness
        {
            private readonly CapturingLogger _log = new CapturingLogger();

            public Harness(TimeSpan? expiry = null, DateTime? claimedAt = null)
            {
                Now = PickedUp;
                var clock = Substitute.For<IGetTime>();
                clock.GetCurrentUtcDate().Returns(_ => Now);

                Context = Substitute.For<IMessageContext>();
                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(true);
                Context.MessageId.Returns(messageId);
                var heartBeat = Substitute.For<IWorkerHeartBeatNotification>();
                heartBeat.Status = null;
                Context.WorkerNotification.HeartBeat.Returns(heartBeat);

                var messageClaim = Substitute.For<IMessageClaim>();
                var claimedAtData = new MessageContextData<ValueTypeWrapper<DateTime>>("claimedAt", null);
                messageClaim.ClaimedAt.Returns(claimedAtData);
                Context.Get(claimedAtData).Returns(claimedAt.HasValue
                    ? new ValueTypeWrapper<DateTime>(claimedAt.Value)
                    : null);

                Claim = new HeartBeatClaim(expiry ?? Expiry, clock, Context, messageClaim, _log);
            }

            public DateTime Now { get; set; }
            public IMessageContext Context { get; }
            public HeartBeatClaim Claim { get; }

            public IHeartBeatStatus Landed(DateTime storedTime) =>
                new HeartBeatStatus(Context.MessageId, storedTime);

            public IHeartBeatStatus NoRecord() => new HeartBeatStatus(Context.MessageId, null);

            public int Count(LogLevel level) => _log.Entries.Count(e => e == level);
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly ConcurrentQueue<LogLevel> _entries = new();

            public LogLevel[] Entries => _entries.ToArray();

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                //format the message so a template that throws on these arguments fails the test
                formatter(state, exception);
                _entries.Enqueue(logLevel);
            }
        }
    }
}
