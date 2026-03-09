using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    [TestClass]
    public class SetErrorCountCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            const int id = 19334;
            var type = "errorType";
            var test = new SetErrorCountCommand<long>(type, id);
            Assert.AreEqual(id, test.QueueId);
            Assert.AreEqual(type, test.ExceptionType);
        }
    }
}
