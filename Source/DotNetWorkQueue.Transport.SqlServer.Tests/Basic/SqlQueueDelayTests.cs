using System;
using DotNetWorkQueue.Queue;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic
{
    public class SqlQueueDelayTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new QueueDelay(TimeSpan.FromHours(1));
            Assert.Equal(TimeSpan.FromHours(1), test.IncreaseDelay);
        }
    }
}
