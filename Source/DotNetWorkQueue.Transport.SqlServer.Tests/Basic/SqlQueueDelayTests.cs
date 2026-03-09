using System;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    [TestClass]
    public class SqlQueueDelayTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new QueueDelay(TimeSpan.FromHours(1));
            Assert.AreEqual(TimeSpan.FromHours(1), test.IncreaseDelay);
        }
    }
}
