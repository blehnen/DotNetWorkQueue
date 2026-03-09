using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests
{
    [TestClass]
    public class SqLiteConnectionInformationTests
    {
        private const string GoodConnection =
             @"Data Source=c:\temp\test.db;Version=3;";

        private const string BadConnection =
           "Thisisabadconnectionstring";

        [TestMethod]
        public void GetSet_Connection()
        {
            var test = new SqliteConnectionInformation(new QueueConnection(string.Empty, GoodConnection), null);
            Assert.IsNotNull(test);
        }
        [TestMethod]
        public void Test_Clone()
        {
            var test = new SqliteConnectionInformation(new QueueConnection("blah", GoodConnection), null);
            var clone = test.Clone();

            Assert.AreEqual(test.ConnectionString, clone.ConnectionString);
            Assert.AreEqual(test.QueueName, clone.QueueName);
        }
    }
}
