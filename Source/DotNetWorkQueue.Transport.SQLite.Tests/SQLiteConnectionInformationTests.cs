using System;
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

        [TestMethod]
        public void QueueName_Valid_Alphanumeric()
        {
            var test = new SqliteConnectionInformation(new QueueConnection("MyQueue123", GoodConnection), null);
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void QueueName_Valid_WithUnderscoreAndDot()
        {
            var test = new SqliteConnectionInformation(new QueueConnection("my_queue.v2", GoodConnection), null);
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void QueueName_Invalid_SqlInjection()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqliteConnectionInformation(new QueueConnection("queue; DROP TABLE users;--", GoodConnection), null);
                });
        }

        [TestMethod]
        public void QueueName_Invalid_SpecialChars()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqliteConnectionInformation(new QueueConnection("queue@name!", GoodConnection), null);
                });
        }

        [TestMethod]
        public void QueueName_Invalid_Spaces()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqliteConnectionInformation(new QueueConnection("my queue", GoodConnection), null);
                });
        }

        [TestMethod]
        public void QueueName_Invalid_Hyphen()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqliteConnectionInformation(new QueueConnection("my-queue", GoodConnection), null);
                });
        }

        [TestMethod]
        public void QueueName_Empty_Allowed()
        {
            var test = new SqliteConnectionInformation(new QueueConnection(string.Empty, GoodConnection), null);
            Assert.IsNotNull(test);
        }
    }
}
