using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class TableNameHelperTests
    {
        [TestMethod]
        public void ValidNames()
        {
            var test = Create(true);
            Assert.AreEqual("testQueue", test.QueueName);
            Assert.StartsWith("testQueue", test.MetaDataName);
            Assert.StartsWith("testQueue", test.StatusName);
            Assert.StartsWith("testQueue", test.ConfigurationName);
            Assert.StartsWith("testQueue", test.ErrorTrackingName);
            Assert.StartsWith("testQueue", test.MetaDataErrorsName);
        }

        [TestMethod]
        public void InValidNames()
        {
            var test = Create(false);
            Assert.AreEqual("", test.QueueName);
            Assert.AreEqual("Error-Name-Not-Set", test.MetaDataName);
            Assert.AreEqual("Error-Name-Not-Set", test.StatusName);
            Assert.AreEqual("Error-Name-Not-Set", test.ConfigurationName);
            Assert.AreEqual("Error-Name-Not-Set", test.ErrorTrackingName);
            Assert.AreEqual("Error-Name-Not-Set", test.MetaDataErrorsName);
        }

        private TableNameHelper Create(bool validConnection)
        {
            var connectionInformation = Substitute.For<IConnectionInformation>();
            if (validConnection)
                connectionInformation.QueueName.Returns("testQueue");
            return new TableNameHelper(connectionInformation);
        }
    }
}
