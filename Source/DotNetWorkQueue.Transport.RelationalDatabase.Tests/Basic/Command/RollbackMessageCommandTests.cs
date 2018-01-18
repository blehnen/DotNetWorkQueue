using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    public class RollbackMessageCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            DateTime? lastDateTime = DateTime.Now;
            TimeSpan? queue = TimeSpan.FromDays(1);
            const int id = 19334;
            var test = new RollbackMessageCommand(lastDateTime, id, queue);
            Assert.Equal(id, test.QueueId);
            Assert.Equal(queue, test.IncreaseQueueDelay);
            Assert.Equal(lastDateTime, test.LastHeartBeat);
        }
        [Fact]
        public void Create_Default2()
        {
            const int id = 19334;
            var test = new RollbackMessageCommand(null, id, null);
            Assert.Equal(id, test.QueueId);
            Assert.Null(test.IncreaseQueueDelay);
            Assert.Null(test.LastHeartBeat);
        }
    }
}
