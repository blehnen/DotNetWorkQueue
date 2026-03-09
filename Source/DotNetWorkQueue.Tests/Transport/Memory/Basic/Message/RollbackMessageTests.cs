using DotNetWorkQueue.Transport.Memory.Basic.Message;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic.Message
{
    [TestClass]
    public class RollbackMessageTests
    {
        [TestMethod]
        public void Rollback_Test()
        {
            var message = new RollbackMessage();
            //no-op
            message.Rollback(null);
        }
    }
}