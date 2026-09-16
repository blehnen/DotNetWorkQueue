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

        [TestMethod]
        public void QueueName_Valid_Alphanumeric()
        {
            var test = new SqlConnectionInformation(new QueueConnection("MyQueue123", GoodConnection));
            Assert.IsNotNull(test);
            Assert.AreEqual("MyQueue123", test.QueueName);
        }

        [TestMethod]
        public void QueueName_Valid_WithUnderscoreAndDot()
        {
            var test = new SqlConnectionInformation(new QueueConnection("my_queue.v2", GoodConnection));
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void QueueName_Invalid_SqlInjection()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqlConnectionInformation(new QueueConnection("queue; DROP TABLE users;--", GoodConnection));
                });
        }

        [TestMethod]
        public void QueueName_Invalid_SpecialChars()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqlConnectionInformation(new QueueConnection("queue@name!", GoodConnection));
                });
        }

        [TestMethod]
        public void QueueName_Invalid_Spaces()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqlConnectionInformation(new QueueConnection("my queue", GoodConnection));
                });
        }

        [TestMethod]
        public void QueueName_Invalid_Hyphen()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqlConnectionInformation(new QueueConnection("my-queue", GoodConnection));
                });
        }

        [TestMethod]
        public void QueueName_Empty_Allowed()
        {
            var test = new SqlConnectionInformation(new QueueConnection(string.Empty, GoodConnection));
            Assert.IsNotNull(test);
        }

        /// <summary>
        /// The limit is 101 rather than SQL Server's own 128, because the queue name is a prefix for
        /// identifiers longer than itself. These two used to assert 128, which accepted names that
        /// cannot create a queue: at 102 and above, IX_{name}History_Status_Completed exceeds the
        /// identifier limit and creation fails (GitHub #344).
        /// </summary>
        [TestMethod]
        public void QueueName_ExceedsMaxLength_102()
        {
            var longName = new string('a', 102);
            var error = Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqlConnectionInformation(new QueueConnection(longName, GoodConnection));
                });

            //the message has to say why, or 101 reads as an arbitrary number
            Assert.Contains("101", error.Message, "the limit is not stated");
            Assert.Contains("History_Status_Completed", error.Message, "the reason for the limit is not stated");
        }

        [TestMethod]
        public void QueueName_AtMaxLength_101()
        {
            var maxName = new string('a', 101);
            var test = new SqlConnectionInformation(new QueueConnection(maxName, GoodConnection));
            Assert.IsNotNull(test);
        }
    }
}
