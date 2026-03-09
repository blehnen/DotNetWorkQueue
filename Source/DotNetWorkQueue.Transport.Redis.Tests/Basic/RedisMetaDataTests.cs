using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisMetaDataTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new RedisMetaData(1000);
            Assert.AreEqual(1000, test.QueueDateTime);
            Assert.IsNotNull(test.ErrorTracking);
        }
    }
}
