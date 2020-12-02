using System;
using DotNetWorkQueue.Configuration;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Tests
{
    public class SqlConnectionInformationTests
    {
        private const string GoodConnection =
            "Server=localhost;Application Name=Consumer;Database=db;User ID=sa;Password=password";

        private const string BadConnection =
           "Thisisabadconnectionstring";

        [Fact]
        public void GetSet_Connection()
        {
            var test = new SqlConnectionInformation(new QueueConnection(string.Empty, GoodConnection));
            Assert.NotNull(test);
        }
        [Fact]
        public void GetSet_Connection_Bad_Exception()
        {
            Assert.Throws<ArgumentException>(
            delegate
            {
                // ReSharper disable once UnusedVariable
                var test = new SqlConnectionInformation(new QueueConnection(string.Empty, BadConnection));
            });
        }
        [Fact]
        public void Test_Clone()
        {
            var test = new SqlConnectionInformation(new QueueConnection("blah", GoodConnection));
            var clone = test.Clone();

            Assert.Equal(test.ConnectionString, clone.ConnectionString);
            Assert.Equal(test.QueueName, clone.QueueName);
        }
    }
}
