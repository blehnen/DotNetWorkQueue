using System.Collections.Generic;
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
        public void Test_Clone(string queue, string connection, Dictionary<string, string> settings)
        {
            var test = new BaseConnectionInformation(new QueueConnection(queue, connection, settings));
            var clone = (BaseConnectionInformation)test.Clone();

            Assert.Equal(test.ConnectionString, clone.ConnectionString);
            Assert.Equal(test.QueueName, clone.QueueName);

            foreach (var setting in settings)
            {
                Assert.Contains(setting.Key, clone.AdditionalConnectionSettings);
            }
        }

        [Theory, AutoData]
        public void Test_Equals(string queue, string connection, Dictionary<string, string> settings, Dictionary<string, string> settings2)
        {
            var test = new BaseConnectionInformation(new QueueConnection(queue, connection, settings));
            var clone = (BaseConnectionInformation)test.Clone();
            Assert.True(test.Equals(clone));
            Assert.False(test.Equals(null));

            var test2 = new BaseConnectionInformation(new QueueConnection(queue, connection));
            Assert.False(test2.Equals(test));

            var test3 = new BaseConnectionInformation(new QueueConnection(queue, connection, settings2));
            Assert.False(test2.Equals(test));
        }

        [Theory, AutoData]
        public void Test_Server(string queue, string connection, Dictionary<string, string> settings)
        {
            var test = new BaseConnectionInformation(new QueueConnection(queue, connection, settings));
            Assert.Equal("Base connection object cannot determine server", test.Server);
        }

        [Theory, AutoData]
        public void Test_Container(string queue, string connection, Dictionary<string, string> settings)
        {
            var test = new BaseConnectionInformation(new QueueConnection(queue, connection, settings));
            Assert.Equal("Base connection object cannot determine container", test.Container);
        }
    }
}
