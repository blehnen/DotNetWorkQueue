using System;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Tests
{
    [TestClass]
    public class SqlConnectionInformationTests
    {
        private const string GoodConnection =
            "Server=localhost;Application Name=Consumer;Database=db;User ID=sa;Password=password";

        private const string BadConnection =
           "Thisisabadconnectionstring";

        [TestMethod]
        public void GetSet_Connection()
        {
            var test = new SqlConnectionInformation(new QueueConnection(string.Empty, GoodConnection));
            Assert.IsNotNull(test);
        }
        [TestMethod]
        public void GetSet_Connection_Bad_Exception()
        {
            Assert.ThrowsExactly<ArgumentException>(
            delegate
            {
                // ReSharper disable once UnusedVariable
                var test = new SqlConnectionInformation(new QueueConnection(string.Empty, BadConnection));
            });
        }
        [TestMethod]
        public void Test_Clone()
        {
            var test = new SqlConnectionInformation(new QueueConnection("blah", GoodConnection));
            var clone = test.Clone();

            Assert.AreEqual(test.ConnectionString, clone.ConnectionString);
            Assert.AreEqual(test.QueueName, clone.QueueName);
        }
    }
}
