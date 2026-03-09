using System;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisQueueDelayTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var time = TimeSpan.FromSeconds(1);
            var test = new RedisQueueDelay(time);
            Assert.AreEqual(time, test.IncreaseDelay);
        }
    }
}
