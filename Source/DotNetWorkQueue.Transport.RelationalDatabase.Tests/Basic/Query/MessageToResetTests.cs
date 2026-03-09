using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class MessageToResetTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var date = DateTime.UtcNow;
            var test = new MessageToReset<long>(100, date, null);
            Assert.AreEqual(100, test.QueueId);
            Assert.AreEqual(date, test.HeartBeat);
        }
    }
}
