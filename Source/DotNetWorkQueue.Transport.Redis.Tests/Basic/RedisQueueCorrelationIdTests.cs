using System;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisQueueCorrelationIdTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var id = Guid.NewGuid();
            var test = new RedisQueueCorrelationId(id);
            Assert.AreEqual(id, test.Id.Value);
            Assert.IsTrue(test.HasValue);
        }
        [TestMethod]
        public void Create_Default_ToString()
        {
            var id = Guid.NewGuid();
            var test = new RedisQueueCorrelationId(id);
            Assert.AreEqual(id.ToString(), test.ToString());
        }
        [TestMethod]
        public void Create_Default_Empty_Guid()
        {
            var id = Guid.Empty;
            var test = new RedisQueueCorrelationId(id);
            Assert.AreEqual(id, test.Id.Value);
            Assert.IsFalse(test.HasValue);
        }
        [TestMethod]
        public void Create_Default_Null_Serialized()
        {
            var test = new RedisQueueCorrelationId(null);
            Assert.AreEqual(Guid.Empty.ToString(), test.Id.Value.ToString());
            Assert.IsFalse(test.HasValue);
        }

        [TestMethod]
        public void Create_Default_Serialized()
        {
            var id = Guid.NewGuid();
            var input = new RedisQueueCorrelationIdSerialized(id);
            var test = new RedisQueueCorrelationId(input);
            Assert.AreEqual(id.ToString(), test.Id.Value.ToString());
            Assert.IsTrue(test.HasValue);
        }
    }
}
