using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisErrorTrackingTests
    {
        [Fact]
        public void Exception_Count_Zero_Default()
        {
            var test = new RedisErrorTracking();
            var value = test.GetExceptionCount("test");
            Assert.Equal(0, value);
        }
        [Fact]
        public void Exception_Count_Increment()
        {
            var test = new RedisErrorTracking();
            test.IncrementExceptionCount("test");
            var value = test.GetExceptionCount("test");
            Assert.Equal(1, value);
            value = test.GetExceptionCount("test_no_value");
            Assert.Equal(0, value);
            test.IncrementExceptionCount("test");
            value = test.GetExceptionCount("test");
            Assert.Equal(2, value);
        }
    }
}
