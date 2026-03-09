using DotNetWorkQueue.Transport.Redis.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisNamesTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new RedisNames(CreateConnection());
            StringAssert.Contains(test.Values, "testQueue");
            StringAssert.Contains(test.Delayed, "testQueue");
            StringAssert.Contains(test.Error, "testQueue");
            StringAssert.Contains(test.Expiration, "testQueue");
            StringAssert.Contains(test.Id, "testQueue");
            StringAssert.Contains(test.MetaData, "testQueue");
            StringAssert.Contains(test.Notification, "testQueue");
            StringAssert.Contains(test.Pending, "testQueue");
            StringAssert.Contains(test.Working, "testQueue");
            StringAssert.Contains(test.Headers, "testQueue");
        }

        public IConnectionInformation CreateConnection()
        {
            var connection = Substitute.For<IConnectionInformation>();
            connection.QueueName.Returns("testQueue");
            return connection;
        }
    }
}
