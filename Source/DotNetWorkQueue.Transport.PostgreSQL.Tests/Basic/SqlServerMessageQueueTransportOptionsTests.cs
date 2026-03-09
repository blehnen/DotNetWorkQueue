using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    [TestClass]
    public class PostgreSqlMessageQueueTransportOptionsTests
    {
        [TestMethod]
        public void Readonly()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            test.SetReadOnly();
            Assert.IsTrue(test.IsReadOnly);
        }
        [TestMethod]
        public void Test_DefaultNotReadOnly()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            Assert.IsFalse(test.IsReadOnly);
        }

        [TestMethod]
        public void GetSet_Priority()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnablePriority;
            test.EnablePriority = !c;
            Assert.AreEqual(!c, test.EnablePriority);
        }

        [TestMethod]
        public void GetSet_EnableHoldTransactionUntilMessageCommitted()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableHoldTransactionUntilMessageCommitted;
            test.EnableHoldTransactionUntilMessageCommitted = !c;
            Assert.AreEqual(!c, test.EnableHoldTransactionUntilMessageCommitted);
        }

        [TestMethod]
        public void GetSet_EnableStatus()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableStatus;
            test.EnableStatus = !c;
            Assert.AreEqual(!c, test.EnableStatus);
        }

        [TestMethod]
        public void GetSet_EnableHeartBeat()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableHeartBeat;
            test.EnableHeartBeat = !c;
            Assert.AreEqual(!c, test.EnableHeartBeat);
        }

        [TestMethod]
        public void GetSet_EnableDelayedProcessing()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableDelayedProcessing;
            test.EnableDelayedProcessing = !c;
            Assert.AreEqual(!c, test.EnableDelayedProcessing);
        }

        [TestMethod]
        public void GetSet_EnableStatusTable()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableStatusTable;
            test.EnableStatusTable = !c;
            Assert.AreEqual(!c, test.EnableStatusTable);
        }

        [TestMethod]
        public void GetSet_EnableMessageExpiration()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            var c = test.EnableMessageExpiration;
            test.EnableMessageExpiration = !c;
            Assert.AreEqual(!c, test.EnableMessageExpiration);
        }

        [TestMethod]
        public void Validation()
        {
            var test = new PostgreSqlMessageQueueTransportOptions();
            test.ValidConfiguration();
        }
    }
}
