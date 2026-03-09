using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisSimpleBatchSizeTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new RedisSimpleBatchSize();
            Assert.AreEqual(1, test.BatchSize(1));
            Assert.AreEqual(25, test.BatchSize(25));
            Assert.AreEqual(50, test.BatchSize(50));
            Assert.AreEqual(40, test.BatchSize(80));
            Assert.AreEqual(50, test.BatchSize(100));
            Assert.AreEqual(250, test.BatchSize(500));
            Assert.AreEqual(256, test.BatchSize(512));
            Assert.AreEqual(256, test.BatchSize(10000));
            Assert.AreEqual(256, test.BatchSize(25000));
            Assert.AreEqual(256, test.BatchSize(int.MaxValue));
        }
    }
}
