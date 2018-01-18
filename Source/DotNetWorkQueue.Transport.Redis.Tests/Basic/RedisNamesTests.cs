using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisNamesTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new RedisNames(CreateConnection());
            Assert.Contains("testQueue", test.Values);
            Assert.Contains("testQueue", test.Delayed);
            Assert.Contains("testQueue", test.Error);
            Assert.Contains("testQueue", test.Expiration);
            Assert.Contains("testQueue", test.Id);
            Assert.Contains("testQueue", test.MetaData);
            Assert.Contains("testQueue", test.Notification);
            Assert.Contains("testQueue", test.Pending);
            Assert.Contains("testQueue", test.Working);
            Assert.Contains("testQueue", test.Headers);
        }

        public IConnectionInformation CreateConnection()
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("testQueue");
            return connection;
        }
    }
}
