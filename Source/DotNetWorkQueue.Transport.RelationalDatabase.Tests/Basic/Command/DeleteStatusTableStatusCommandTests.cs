using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    [TestClass]
    public class DeleteStatusTableStatusCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            const int id = 19334;
            var test = new DeleteStatusTableStatusCommand<long>(id);
            Assert.AreEqual(id, test.QueueId);
        }
    }
}
