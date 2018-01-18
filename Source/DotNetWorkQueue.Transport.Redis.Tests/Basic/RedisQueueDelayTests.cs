using System;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisQueueDelayTests
    {
        [Fact]
        public void Create_Default()
        {
            var time = TimeSpan.FromSeconds(1);
            var test = new RedisQueueDelay(time);
            Assert.Equal(time, test.IncreaseDelay);
        }
    }
}
