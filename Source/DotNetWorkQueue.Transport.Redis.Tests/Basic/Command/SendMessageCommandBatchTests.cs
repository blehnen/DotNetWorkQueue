using System.Collections.Generic;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    [TestClass]
    public class SendMessageCommandBatchTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var messages = new List<QueueMessage<IMessage, IAdditionalMessageData>>();
            var test = new SendMessageCommandBatch(messages);
            Assert.AreEqual(messages, test.Messages);
        }
    }
}
