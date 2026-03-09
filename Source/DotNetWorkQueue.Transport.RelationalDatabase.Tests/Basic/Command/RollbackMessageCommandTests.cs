using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    [TestClass]
    public class RollbackMessageCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            DateTime? lastDateTime = DateTime.Now;
            TimeSpan? queue = TimeSpan.FromDays(1);
            const int id = 19334;
            var test = new RollbackMessageCommand<long>(lastDateTime, id, queue);
            Assert.AreEqual(id, test.QueueId);
            Assert.AreEqual(queue, test.IncreaseQueueDelay);
            Assert.AreEqual(lastDateTime, test.LastHeartBeat);
        }
        [TestMethod]
        public void Create_Default2()
        {
            const int id = 19334;
            var test = new RollbackMessageCommand<long>(null, id, null);
            Assert.AreEqual(id, test.QueueId);
            Assert.IsNull(test.IncreaseQueueDelay);
            Assert.IsNull(test.LastHeartBeat);
        }
    }
}
