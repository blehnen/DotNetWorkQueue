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
            const int retryCount = 3;
            var type = "errorType";
            var test = new SetErrorCountCommand<long>(type, id, retryCount);
            Assert.AreEqual(id, test.QueueId);
            Assert.AreEqual(type, test.ExceptionType);
            Assert.AreEqual(retryCount, test.RetryCount);
        }

        [TestMethod]
        public void Create_RejectsACountBelowOne()
        {
            //the count is a total rather than an amount to add, so zero is not a thing that can be
            //recorded - a caller passing one has almost certainly passed an increment by mistake
            Assert.ThrowsExactly<System.ArgumentException>(
                () => new SetErrorCountCommand<long>("errorType", 1, 0));
        }
    }
}
