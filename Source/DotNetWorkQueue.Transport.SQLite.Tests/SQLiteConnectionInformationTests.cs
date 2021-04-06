using DotNetWorkQueue.Configuration;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests
{
    public class SqLiteConnectionInformationTests
    {
        private const string GoodConnection =
             @"Data Source=c:\temp\test.db;Version=3;";

        private const string BadConnection =
           "Thisisabadconnectionstring";

        [Fact]
        public void GetSet_Connection()
        {
            var test = new SqliteConnectionInformation(new QueueConnection( string.Empty, GoodConnection), null);
            Assert.NotNull(test);
        }
        [Fact]
        public void Test_Clone()
        {
            var test = new SqliteConnectionInformation(new QueueConnection( "blah", GoodConnection), null);
            var clone = test.Clone();

            Assert.Equal(test.ConnectionString, clone.ConnectionString);
            Assert.Equal(test.QueueName, clone.QueueName);
        }
    }
}
