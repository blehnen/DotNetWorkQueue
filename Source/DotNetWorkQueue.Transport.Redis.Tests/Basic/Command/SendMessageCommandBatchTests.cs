using System.Collections.Generic;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class SendMessageCommandBatchTests
    {
        [Fact]
        public void Create_Default()
        {
            var messages = new List<QueueMessage<IMessage, IAdditionalMessageData>>();
            var test = new SendMessageCommandBatch(messages);
            Assert.Equal(messages, test.Messages);
        }
    }
}
