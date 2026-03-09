using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisErrorTrackingTests
    {
        [TestMethod]
        public void Exception_Count_Zero_Default()
        {
            var test = new RedisErrorTracking();
            var value = test.GetExceptionCount("test");
            Assert.AreEqual(0, value);
        }
        [TestMethod]
        public void Exception_Count_Increment()
        {
            var test = new RedisErrorTracking();
            test.IncrementExceptionCount("test");
            var value = test.GetExceptionCount("test");
            Assert.AreEqual(1, value);
            value = test.GetExceptionCount("test_no_value");
            Assert.AreEqual(0, value);
            test.IncrementExceptionCount("test");
            value = test.GetExceptionCount("test");
            Assert.AreEqual(2, value);
        }
    }
}
