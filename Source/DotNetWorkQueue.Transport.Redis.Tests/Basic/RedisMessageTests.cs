using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisMessageTests
    {
        [TestMethod]
        public void Create_Null_Message_OK()
        {
            var test = new RedisMessage(null, null, false);
            Assert.IsNull(test.Message);
        }
        [TestMethod]
        public void Create_Message()
        {
            var message = Substitute.For<IReceivedMessageInternal>();
            var test = new RedisMessage("1", message, false);
            Assert.AreEqual(message, test.Message);
            Assert.AreEqual("1", test.MessageId);
        }
        [TestMethod]
        public void Create_Null_Message_Expired_False()
        {
            var test = new RedisMessage("1", null, false);
            Assert.IsFalse(test.Expired);
        }
        [TestMethod]
        public void Create_Null_Message_Expired_True()
        {
            var test = new RedisMessage("1", null, true);
            Assert.IsTrue(test.Expired);
        }
    }
}
