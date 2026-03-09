using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class SqLiteMessageQueueTransportOptionsTests
    {
        [TestMethod]
        public void Readonly()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            test.SetReadOnly();
            Assert.IsTrue(test.IsReadOnly);
        }
        [TestMethod]
        public void Test_DefaultNotReadOnly()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            Assert.IsFalse(test.IsReadOnly);
        }

        [TestMethod]
        public void GetSet_Priority()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnablePriority;
            test.EnablePriority = !c;
            Assert.AreEqual(!c, test.EnablePriority);
        }

        [TestMethod]
        public void GetSet_EnableStatus()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnableStatus;
            test.EnableStatus = !c;
            Assert.AreEqual(!c, test.EnableStatus);
        }

        [TestMethod]
        public void GetSet_EnableHeartBeat()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnableHeartBeat;
            test.EnableHeartBeat = !c;
            Assert.AreEqual(!c, test.EnableHeartBeat);
        }

        [TestMethod]
        public void GetSet_EnableDelayedProcessing()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnableDelayedProcessing;
            test.EnableDelayedProcessing = !c;
            Assert.AreEqual(!c, test.EnableDelayedProcessing);
        }

        [TestMethod]
        public void GetSet_EnableStatusTable()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnableStatusTable;
            test.EnableStatusTable = !c;
            Assert.AreEqual(!c, test.EnableStatusTable);
        }


        [TestMethod]
        public void GetSet_EnableMessageExpiration()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            var c = test.EnableMessageExpiration;
            test.EnableMessageExpiration = !c;
            Assert.AreEqual(!c, test.EnableMessageExpiration);
        }

        [TestMethod]
        public void Validation()
        {
            var test = new SqLiteMessageQueueTransportOptions();
            test.ValidConfiguration();
        }
    }
}
