using System;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisQueueCorrelationIdTests
    {
        [Fact]
        public void Create_Default()
        {
            var id = Guid.NewGuid();
            var test = new RedisQueueCorrelationId(id);
            Assert.Equal(id, test.Id.Value);
            Assert.True(test.HasValue);
        }
        [Fact]
        public void Create_Default_ToString()
        {
            var id = Guid.NewGuid();
            var test = new RedisQueueCorrelationId(id);
            Assert.Equal(id.ToString(), test.ToString());
        }
        [Fact]
        public void Create_Default_Empty_Guid()
        {
            var id = Guid.Empty;
            var test = new RedisQueueCorrelationId(id);
            Assert.Equal(id, test.Id.Value);
            Assert.False(test.HasValue);
        }
        [Fact]
        public void Create_Default_Null_Serialized()
        {
            var test = new RedisQueueCorrelationId(null);
            Assert.Equal(Guid.Empty.ToString(), test.Id.Value.ToString());
            Assert.False(test.HasValue);
        }

        [Fact]
        public void Create_Default_Serialized()
        {
            var id = Guid.NewGuid();
            var input = new RedisQueueCorrelationIdSerialized(id);
            var test = new RedisQueueCorrelationId(input);
            Assert.Equal(id.ToString(), test.Id.Value.ToString());
            Assert.True(test.HasValue);
        }
    }
}
