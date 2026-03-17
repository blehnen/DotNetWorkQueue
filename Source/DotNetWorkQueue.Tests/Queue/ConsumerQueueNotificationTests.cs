using DotNetWorkQueue.Notifications;
using DotNetWorkQueue.Queue;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class ConsumerQueueNotificationTests
    {
        [TestMethod]
        public void InvokeMessageComplete_Calls_IncrementProcessed()
        {
            var metrics = Substitute.For<IConsumerMetricsNotification>();
            var sut = new ConsumerQueueNotification(metrics);

            sut.InvokeMessageComplete(new MessageCompleteNotification(null, null, null, null));

            metrics.Received(1).IncrementProcessed();
        }

        [TestMethod]
        public void InvokeRollback_Calls_IncrementRolledBack()
        {
            var metrics = Substitute.For<IConsumerMetricsNotification>();
            var sut = new ConsumerQueueNotification(metrics);

            sut.InvokeRollback(new RollBackNotification(null, null, null, null));

            metrics.Received(1).IncrementRolledBack();
        }

        [TestMethod]
        public void InvokeMessageComplete_Also_Calls_User_Callback()
        {
            var metrics = Substitute.For<IConsumerMetricsNotification>();
            var sut = new ConsumerQueueNotification(metrics);
            var called = false;
            var notifications = new ConsumerQueueNotifications(
                null, null, null, null, null,
                _ => called = true);
            sut.Sub(notifications);

            sut.InvokeMessageComplete(new MessageCompleteNotification(null, null, null, null));

            called.Should().BeTrue();
            metrics.Received(1).IncrementProcessed();
        }

        [TestMethod]
        public void InvokeRollback_Also_Calls_User_Callback()
        {
            var metrics = Substitute.For<IConsumerMetricsNotification>();
            var sut = new ConsumerQueueNotification(metrics);
            var called = false;
            var notifications = new ConsumerQueueNotifications(
                null, null, null, null,
                _ => called = true, null);
            sut.Sub(notifications);

            sut.InvokeRollback(new RollBackNotification(null, null, null, null));

            called.Should().BeTrue();
            metrics.Received(1).IncrementRolledBack();
        }
    }

    [TestClass]
    public class ConsumerQueueErrorNotificationTests
    {
        [TestMethod]
        public void InvokeError_Calls_IncrementErrored()
        {
            var metrics = Substitute.For<IConsumerMetricsNotification>();
            var sut = new ConsumerQueueErrorNotification(metrics);

            sut.InvokeError(new ErrorNotification(null, null, null, null));

            metrics.Received(1).IncrementErrored();
        }

        [TestMethod]
        public void InvokePoisonMessageError_Calls_IncrementPoisonMessage()
        {
            var metrics = Substitute.For<IConsumerMetricsNotification>();
            var sut = new ConsumerQueueErrorNotification(metrics);

            sut.InvokePoisonMessageError(new PoisonMessageNotification(new DotNetWorkQueue.Exceptions.PoisonMessageException()));

            metrics.Received(1).IncrementPoisonMessage();
        }

        [TestMethod]
        public void InvokeError_Also_Calls_User_Callback()
        {
            var metrics = Substitute.For<IConsumerMetricsNotification>();
            var sut = new ConsumerQueueErrorNotification(metrics);
            var called = false;
            var notifications = new ConsumerQueueNotifications(
                _ => called = true, null, null, null, null, null);
            sut.Sub(notifications);

            sut.InvokeError(new ErrorNotification(null, null, null, null));

            called.Should().BeTrue();
            metrics.Received(1).IncrementErrored();
        }

        [TestMethod]
        public void InvokePoisonMessageError_Also_Calls_User_Callback()
        {
            var metrics = Substitute.For<IConsumerMetricsNotification>();
            var sut = new ConsumerQueueErrorNotification(metrics);
            var called = false;
            var notifications = new ConsumerQueueNotifications(
                null, null, null, _ => called = true, null, null);
            sut.Sub(notifications);

            sut.InvokePoisonMessageError(new PoisonMessageNotification(new DotNetWorkQueue.Exceptions.PoisonMessageException()));

            called.Should().BeTrue();
            metrics.Received(1).IncrementPoisonMessage();
        }

        [TestMethod]
        public void InvokeReceiveError_Does_Not_Call_IncrementErrored()
        {
            var metrics = Substitute.For<IConsumerMetricsNotification>();
            var sut = new ConsumerQueueErrorNotification(metrics);

            sut.InvokeError(new ErrorReceiveNotification(null));

            metrics.DidNotReceive().IncrementErrored();
        }
    }
}
