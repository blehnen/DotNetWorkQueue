using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    [TestClass]
    public class SendHeartBeatCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            const int id = 19334;
            var test = new SendHeartBeatCommand<long>(id);
            Assert.AreEqual(id, test.QueueId);
        }
    }
}
