using System;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Queue
{
    /// <summary>
    /// The schedule check a consumer makes before it starts.
    ///
    /// A claim that expires before a late beat can land is reset by the monitor and the message is
    /// handed to another worker, so it runs twice. The worker is only asked to stop - handling code may
    /// ignore the token - so refusing the configuration is the only lever the queue has.
    /// </summary>
    [TestClass]
    public class ConsumerQueueConfigurationGuardTests
    {
        [TestMethod]
        public void ThreeAttemptsOrMore_IsAccepted()
        {
            ConsumerQueueConfigurationGuard.GuardHeartBeatSchedule(
                Configuration(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(3)));
        }

        [TestMethod]
        public void TheShippedTransportDefaults_AreAccepted()
        {
            //10 minute claim, beat every 2 minutes - what the relational, Redis and LiteDb inits set
            ConsumerQueueConfigurationGuard.GuardHeartBeatSchedule(
                Configuration(TimeSpan.FromSeconds(600), TimeSpan.FromMinutes(2)));
        }

        [TestMethod]
        public void FewerThanThreeAttempts_IsRejected()
        {
            var error = Assert.ThrowsExactly<DotNetWorkQueueException>(() =>
                ConsumerQueueConfigurationGuard.GuardHeartBeatSchedule(
                    Configuration(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(3))));

            //the message has to say what is wrong and both ways out, or it just blocks a start-up
            Assert.Contains("2 attempts", error.Message);
            Assert.Contains("00:00:09", error.Message);
            Assert.Contains("00:00:02", error.Message);

            //Three times is the floor, not advice, and the reason is that a claim has to outlast the
            //slowest heartbeat write - a beat measured at 15 seconds against a 10 second claim is what
            //reset a message mid-processing in GitHub #328. Someone tidying this message would take
            //the only statement of that with it, so it is asserted rather than left to survive on its
            //own. Matched on the phrases that carry the meaning rather than whole sentences, so
            //rewording around them still passes - but split any further and they stop asserting it:
            //"not" and "a recommendation" can both appear in a message that says neither.
            Assert.Contains("not a recommendation", error.Message);
            Assert.Contains("longest a single heartbeat", error.Message);
        }

        [TestMethod]
        public void AnUpdateTimeLongerThanTheClaim_IsRejected()
        {
            Assert.ThrowsExactly<DotNetWorkQueueException>(() =>
                ConsumerQueueConfigurationGuard.GuardHeartBeatSchedule(
                    Configuration(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30))));
        }

        [TestMethod]
        public void AZeroUpdateTime_IsRejected()
        {
            Assert.ThrowsExactly<DotNetWorkQueueException>(() =>
                ConsumerQueueConfigurationGuard.GuardHeartBeatSchedule(
                    Configuration(TimeSpan.FromSeconds(30), TimeSpan.Zero)));
        }

        [TestMethod]
        public void AZeroClaim_IsRejected()
        {
            Assert.ThrowsExactly<DotNetWorkQueueException>(() =>
                ConsumerQueueConfigurationGuard.GuardHeartBeatSchedule(
                    Configuration(TimeSpan.Zero, TimeSpan.FromSeconds(3))));
        }

        [TestMethod]
        public void HeartBeatsTurnedOff_AreNotChecked()
        {
            //the Memory transport does not support heartbeats; nothing to validate
            ConsumerQueueConfigurationGuard.GuardHeartBeatSchedule(
                Configuration(TimeSpan.Zero, TimeSpan.Zero, enabled: false));
        }

        [TestMethod]
        public void NoConfiguration_IsNotChecked()
        {
            ConsumerQueueConfigurationGuard.GuardHeartBeatSchedule(null);
        }

        [TestMethod]
        public void AnIntervalPeriodicTimerCannotAccept_IsRejected()
        {
            //caught at start-up rather than when the first message builds its heartbeat worker
            Assert.ThrowsExactly<DotNetWorkQueueException>(() =>
                ConsumerQueueConfigurationGuard.GuardHeartBeatSchedule(
                    Configuration(TimeSpan.FromDays(365), TimeSpan.FromTicks(1))));

            Assert.ThrowsExactly<DotNetWorkQueueException>(() =>
                ConsumerQueueConfigurationGuard.GuardHeartBeatSchedule(
                    Configuration(TimeSpan.FromDays(365), TimeSpan.FromDays(90))));
        }

        private static IHeartBeatConfiguration Configuration(TimeSpan time, TimeSpan updateTime, bool enabled = true)
        {
            var heartBeat = Substitute.For<IHeartBeatConfiguration>();
            heartBeat.Enabled.Returns(enabled);
            heartBeat.Time.Returns(time);
            heartBeat.UpdateTime.Returns(updateTime);
            return heartBeat;
        }
    }
}
