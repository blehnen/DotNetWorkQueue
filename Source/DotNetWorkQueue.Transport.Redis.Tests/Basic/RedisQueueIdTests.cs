using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisQueueIdTests
    {
        [TestMethod]
        public void Create_Default()
        {
            const long id = 1;
            var test = new RedisQueueId(id.ToString());
            Assert.AreEqual(id.ToString(), test.Id.Value);
            Assert.IsTrue(test.HasValue);
        }
        [TestMethod]
        public void Create_Default_ToString()
        {
            const long id = 1;
            var test = new RedisQueueId(id.ToString());
            Assert.AreEqual("1", test.ToString());
        }
        [TestMethod]
        public void Create_Default_Empty()
        {
            var id = string.Empty;
            var test = new RedisQueueId(id);
            Assert.AreEqual(id, test.Id.Value);
            Assert.IsFalse(test.HasValue);
        }
    }
}
