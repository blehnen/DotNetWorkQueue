using System;
using DotNetWorkQueue.Queue;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class ConsumerMetricsNotificationTests
    {
        [TestMethod]
        public void IncrementProcessed_Calls_Delegate()
        {
            var count = 0;
            var sut = new ConsumerMetricsNotification(() => count++, () => { }, () => { }, () => { });
            sut.IncrementProcessed();
            sut.IncrementProcessed();
            count.Should().Be(2);
        }

        [TestMethod]
        public void IncrementErrored_Calls_Delegate()
        {
            var count = 0;
            var sut = new ConsumerMetricsNotification(() => { }, () => count++, () => { }, () => { });
            sut.IncrementErrored();
            count.Should().Be(1);
        }

        [TestMethod]
        public void IncrementRolledBack_Calls_Delegate()
        {
            var count = 0;
            var sut = new ConsumerMetricsNotification(() => { }, () => { }, () => count++, () => { });
            sut.IncrementRolledBack();
            count.Should().Be(1);
        }

        [TestMethod]
        public void IncrementPoisonMessage_Calls_Delegate()
        {
            var count = 0;
            var sut = new ConsumerMetricsNotification(() => { }, () => { }, () => { }, () => count++);
            sut.IncrementPoisonMessage();
            count.Should().Be(1);
        }

        [TestMethod]
        public void Constructor_Throws_On_Null_Delegates()
        {
            Action act1 = () => new ConsumerMetricsNotification(null, () => { }, () => { }, () => { });
            Action act2 = () => new ConsumerMetricsNotification(() => { }, null, () => { }, () => { });
            Action act3 = () => new ConsumerMetricsNotification(() => { }, () => { }, null, () => { });
            Action act4 = () => new ConsumerMetricsNotification(() => { }, () => { }, () => { }, null);

            act1.Should().Throw<ArgumentNullException>();
            act2.Should().Throw<ArgumentNullException>();
            act3.Should().Throw<ArgumentNullException>();
            act4.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void NoOp_Does_Not_Throw()
        {
            var sut = new ConsumerMetricsNotificationNoOp();
            sut.IncrementProcessed();
            sut.IncrementErrored();
            sut.IncrementRolledBack();
            sut.IncrementPoisonMessage();
        }
    }
}
