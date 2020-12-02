using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;
using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class BaseConnectionInformationTests
    {
        [Theory, AutoData]
        public void GetSet_Connection(string expected)
        {
            var test = new BaseConnectionInformation(new QueueConnection(string.Empty, expected));
            Assert.Equal(expected, test.ConnectionString);
        }
        [Theory, AutoData]
        public void GetSet_Queue(string expected)
        {
            var test = new BaseConnectionInformation(new QueueConnection(expected, string.Empty));
            Assert.Equal(expected, test.QueueName);
        }
        [Theory, AutoData]
        public void Test_Clone(string queue, string connection)
        {
            var test = new BaseConnectionInformation(new QueueConnection(queue, connection));
            var clone = (BaseConnectionInformation)test.Clone();

            Assert.Equal(test.ConnectionString, clone.ConnectionString);
            Assert.Equal(test.QueueName, clone.QueueName);
        }
    }
}
