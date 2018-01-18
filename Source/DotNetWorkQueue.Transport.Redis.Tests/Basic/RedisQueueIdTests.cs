using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisQueueIdTests
    {
        [Fact]
        public void Create_Default()
        {
            const long id = 1;
            var test = new RedisQueueId(id.ToString());
            Assert.Equal(id.ToString(), test.Id.Value);
            Assert.True(test.HasValue);
        }
        [Fact]
        public void Create_Default_ToString()
        {
            const long id = 1;
            var test = new RedisQueueId(id.ToString());
            Assert.Equal("1", test.ToString());
        }
        [Fact]
        public void Create_Default_Empty()
        {
            var id = string.Empty;
            var test = new RedisQueueId(id);
            Assert.Equal(id, test.Id.Value);
            Assert.False(test.HasValue);
        }
    }
}
