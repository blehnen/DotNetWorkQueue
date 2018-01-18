using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisMetaDataTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new RedisMetaData(1000);
            Assert.Equal(1000, test.QueueDateTime);
            Assert.NotNull(test.ErrorTracking);
        }
    }
}
