using System;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests
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
        public void QueueName_Valid_WithUnderscore()
        {
            var test = new SqlConnectionInformation(new QueueConnection("my_queue_v2", GoodConnection));
            Assert.IsNotNull(test);
        }

        /// <summary>
        /// A dot was accepted here and could never be created: the name is written into the DDL
        /// unquoted, so PostgreSQL reads it as a schema separator and CREATE TABLE fails with a syntax
        /// error - which was then reported as the queue already existing, with Success (GitHub #375).
        /// </summary>
        [TestMethod]
        public void QueueName_Invalid_WithDot()
        {
            var error = Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqlConnectionInformation(new QueueConnection("my_queue.v2", GoodConnection));
                });

            Assert.Contains("underscores", error.Message, "the message does not say what is allowed");
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
        /// The limit is 51 rather than PostgreSQL's own 63, because the queue name is a prefix for
        /// identifiers longer than itself. These two tests used to assert 63, which accepted names that
        /// could never create a queue: at 52 and above, PK_{name}MetaDataErrors truncates onto
        /// PK_{name}MetaData and creation fails as duplicate_object - reported as "already exists" with
        /// Success == true, having created nothing (GitHub #339).
        /// </summary>
        [TestMethod]
        public void QueueName_ExceedsMaxLength_52()
        {
            var longName = new string('a', 52);
            var error = Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new SqlConnectionInformation(new QueueConnection(longName, GoodConnection));
                });

            //the message has to say why, or 51 reads as an arbitrary number
            Assert.Contains("51", error.Message, "the limit is not stated");
            Assert.Contains("MetaDataErrors", error.Message, "the reason for the limit is not stated");
        }

        [TestMethod]
        public void QueueName_AtMaxLength_51()
        {
            var maxName = new string('a', 51);
            var test = new SqlConnectionInformation(new QueueConnection(maxName, GoodConnection));
            Assert.IsNotNull(test);
        }
    }
}
