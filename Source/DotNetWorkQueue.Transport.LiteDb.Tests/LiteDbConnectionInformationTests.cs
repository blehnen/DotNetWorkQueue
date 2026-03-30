using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.LiteDb.Tests
{
    [TestClass]
    public class LiteDbConnectionInformationTests
    {
        private const string GoodConnection =
            @"FileName=c:\temp\test.db;";

        [TestMethod]
        public void LiteDbConnectionInformation_Test()
        {
            var test = new LiteDbConnectionInformation(new QueueConnection("blah", GoodConnection));
            Assert.AreEqual("blah", test.QueueName);
            Assert.AreEqual(GoodConnection, test.ConnectionString);
        }

        [TestMethod]
        public void Clone_Test()
        {
            var test = new LiteDbConnectionInformation(new QueueConnection("blah", GoodConnection));
            var clone = test.Clone();

            Assert.AreEqual(test.ConnectionString, clone.ConnectionString);
            Assert.AreEqual(test.QueueName, clone.QueueName);
        }

        [TestMethod]
        public void QueueName_Valid_Alphanumeric()
        {
            var test = new LiteDbConnectionInformation(new QueueConnection("MyQueue123", GoodConnection));
            Assert.IsNotNull(test);
            Assert.AreEqual("MyQueue123", test.QueueName);
        }

        [TestMethod]
        public void QueueName_Valid_WithUnderscoreAndDot()
        {
            var test = new LiteDbConnectionInformation(new QueueConnection("my_queue.v2", GoodConnection));
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void QueueName_Invalid_SqlInjection()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new LiteDbConnectionInformation(new QueueConnection("queue; DROP TABLE users;--", GoodConnection));
                });
        }

        [TestMethod]
        public void QueueName_Invalid_Hyphen()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new LiteDbConnectionInformation(new QueueConnection("my-queue", GoodConnection));
                });
        }

        [TestMethod]
        public void QueueName_Invalid_Spaces()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new LiteDbConnectionInformation(new QueueConnection("my queue", GoodConnection));
                });
        }

        [TestMethod]
        public void QueueName_Empty_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new LiteDbConnectionInformation(new QueueConnection(string.Empty, GoodConnection));
                });
        }

        [TestMethod]
        public void QueueName_ExceedsMaxLength_256()
        {
            var longName = new string('a', 257);
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new LiteDbConnectionInformation(new QueueConnection(longName, GoodConnection));
                });
        }

        [TestMethod]
        public void QueueName_AtMaxLength_256()
        {
            var maxName = new string('a', 256);
            var test = new LiteDbConnectionInformation(new QueueConnection(maxName, GoodConnection));
            Assert.IsNotNull(test);
        }
    }
}