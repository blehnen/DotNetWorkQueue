using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.Shared.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class MessageQueueIdTests
    {
        [TestMethod]
        public void Create_Default()
        {
            long id = 1;
            var test = new MessageQueueId<long>(id);
            Assert.AreEqual(id, test.Id.Value);
            Assert.IsTrue(test.HasValue);
        }
        [TestMethod]
        public void Create_Default_ToString()
        {
            long id = 1;
            var test = new MessageQueueId<long>(id);
            Assert.AreEqual("1", test.ToString());
        }
        [TestMethod]
        public void Create_Default_0()
        {
            long id = 0;
            var test = new MessageQueueId<long>(id);
            Assert.AreEqual(id, test.Id.Value);
            Assert.IsFalse(test.HasValue);
        }
    }
}
