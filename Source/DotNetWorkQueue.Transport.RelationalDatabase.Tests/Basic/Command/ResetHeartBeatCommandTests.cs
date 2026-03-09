using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    [TestClass]
    public class ResetHeartBeatCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            const int id = 293;
            var date = DateTime.Now;
            var message = new MessageToReset<long>(id, date, null);
            var test = new ResetHeartBeatCommand<long>(message);
            Assert.AreEqual(message, test.MessageReset);
        }
    }
}
