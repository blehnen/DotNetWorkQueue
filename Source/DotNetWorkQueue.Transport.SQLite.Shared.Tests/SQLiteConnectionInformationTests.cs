using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Tests
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
            var test = new SqliteConnectionInformation(string.Empty, GoodConnection, null);
            Assert.NotNull(test);
        }
        [Fact]
        public void Test_Clone()
        {
            var test = new SqliteConnectionInformation("blah", GoodConnection, null);
            var clone = test.Clone();

            Assert.Equal(test.ConnectionString, clone.ConnectionString);
            Assert.Equal(test.QueueName, clone.QueueName);
        }
    }
}
