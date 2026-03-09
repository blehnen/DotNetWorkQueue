using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory
{
    [TestClass]
    public class ConnectionInformationTests
    {
        [TestMethod]
        public void ConnectionInformation_Test()
        {
            var queue = new QueueConnection("test", string.Empty);
            var connection = new ConnectionInformation(queue);
            Assert.AreEqual(connection.Container, queue.Queue);
            Assert.IsEmpty(connection.Server);
            Assert.AreEqual(connection.QueueName, queue.Queue);
            Assert.AreEqual(connection.ConnectionString, queue.Connection);
        }

        [TestMethod]
        public void Clone_Test()
        {
            var queue = new QueueConnection("test", string.Empty);
            var connection = new ConnectionInformation(queue);
            var clone = connection.Clone();
            Assert.AreEqual(connection.Container, clone.Container);
            Assert.AreEqual(connection.QueueName, clone.QueueName);
            Assert.AreEqual(connection.ConnectionString, clone.ConnectionString);
            Assert.AreEqual(connection.Server, clone.Server);
        }
    }
}