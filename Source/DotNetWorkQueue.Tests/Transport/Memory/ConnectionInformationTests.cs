using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Memory;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport.Memory
{
    public class ConnectionInformationTests
    {
        [Fact()]
        public void ConnectionInformation_Test()
        {
            var queue = new QueueConnection("test", string.Empty);
            var connection = new ConnectionInformation(queue);
            Assert.Equal(connection.Container, queue.Queue);
            Assert.Empty(connection.Server);
            Assert.Equal(connection.QueueName, queue.Queue);
            Assert.Equal(connection.ConnectionString, queue.Connection);
        }

        [Fact()]
        public void Clone_Test()
        {
            var queue = new QueueConnection("test", string.Empty);
            var connection = new ConnectionInformation(queue);
            var clone = connection.Clone();
            Assert.Equal(connection.Container, clone.Container);
            Assert.Equal(connection.QueueName, clone.QueueName);
            Assert.Equal(connection.ConnectionString, clone.ConnectionString);
            Assert.Equal(connection.Server, clone.Server);
        }
    }
}