using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisSimpleBatchSizeTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new RedisSimpleBatchSize();
            Assert.Equal(1, test.BatchSize(1));
            Assert.Equal(25, test.BatchSize(25));
            Assert.Equal(50, test.BatchSize(50));
            Assert.Equal(40, test.BatchSize(80));
            Assert.Equal(50, test.BatchSize(100));
            Assert.Equal(250, test.BatchSize(500));
            Assert.Equal(256, test.BatchSize(512));
            Assert.Equal(256, test.BatchSize(10000));
            Assert.Equal(256, test.BatchSize(25000));
            Assert.Equal(256, test.BatchSize(int.MaxValue));
        }
    }
}
