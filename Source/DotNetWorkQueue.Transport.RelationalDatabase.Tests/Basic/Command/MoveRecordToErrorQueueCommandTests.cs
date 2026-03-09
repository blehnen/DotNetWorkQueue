using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    [TestClass]
    public class MoveRecordToErrorQueueCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            const int id = 19334;
            var error = new Exception();
            var context = Substitute.For<IMessageContext>();
            var test = new MoveRecordToErrorQueueCommand<long>(error, id, context);
            Assert.AreEqual(id, test.QueueId);
            Assert.AreEqual(error, test.Exception);
            Assert.AreEqual(context, test.MessageContext);
        }
    }
}
