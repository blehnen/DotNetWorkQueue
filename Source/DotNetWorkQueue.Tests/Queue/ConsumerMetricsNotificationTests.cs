using System;
using DotNetWorkQueue.Queue;
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
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void IncrementErrored_Calls_Delegate()
        {
            var count = 0;
            var sut = new ConsumerMetricsNotification(() => { }, () => count++, () => { }, () => { });
            sut.IncrementErrored();
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void IncrementRolledBack_Calls_Delegate()
        {
            var count = 0;
            var sut = new ConsumerMetricsNotification(() => { }, () => { }, () => count++, () => { });
            sut.IncrementRolledBack();
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void IncrementPoisonMessage_Calls_Delegate()
        {
            var count = 0;
            var sut = new ConsumerMetricsNotification(() => { }, () => { }, () => { }, () => count++);
            sut.IncrementPoisonMessage();
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void Constructor_Throws_On_Null_Delegates()
        {
            Action act1 = () => new ConsumerMetricsNotification(null, () => { }, () => { }, () => { });
            Action act2 = () => new ConsumerMetricsNotification(() => { }, null, () => { }, () => { });
            Action act3 = () => new ConsumerMetricsNotification(() => { }, () => { }, null, () => { });
            Action act4 = () => new ConsumerMetricsNotification(() => { }, () => { }, () => { }, null);

            Assert.Throws<ArgumentNullException>(act1);
            Assert.Throws<ArgumentNullException>(act2);
            Assert.Throws<ArgumentNullException>(act3);
            Assert.Throws<ArgumentNullException>(act4);
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
