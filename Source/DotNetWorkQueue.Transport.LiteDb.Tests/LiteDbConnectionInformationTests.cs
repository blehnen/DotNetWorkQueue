using Xunit;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.LiteDb.Tests
{
    public class LiteDbConnectionInformationTests
    {
        private const string GoodConnection =
            @"FileName=c:\temp\test.db;";

        [Fact()]
        public void LiteDbConnectionInformation_Test()
        {
            var test = new LiteDbConnectionInformation(new QueueConnection("blah", GoodConnection));
            Assert.Equal("blah", test.QueueName);
            Assert.Equal(GoodConnection, test.ConnectionString);
        }

        [Fact()]
        public void Clone_Test()
        {
            var test = new LiteDbConnectionInformation(new QueueConnection("blah", GoodConnection));
            var clone = test.Clone();

            Assert.Equal(test.ConnectionString, clone.ConnectionString);
            Assert.Equal(test.QueueName, clone.QueueName);
        }
    }
}