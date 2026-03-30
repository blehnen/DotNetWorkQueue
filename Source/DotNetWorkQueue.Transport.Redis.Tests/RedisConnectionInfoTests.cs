using System;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests
{
    [TestClass]
    public class RedisConnectionInfoTests
    {
        [TestMethod]
        public void CreateNullInputTest()
        {
            Assert.ThrowsExactly<NullReferenceException>(
                delegate
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var test = new RedisConnectionInfo(null);
                });
        }

        [TestMethod]
        public void CreateTest()
        {
            var test = new RedisConnectionInfo(new QueueConnection("test", "test"));
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void CloneTest()
        {
            var test = new RedisConnectionInfo(new QueueConnection("test", "test"));
            var cloned = test.Clone();
            Assert.IsNotNull(cloned);
            Assert.AreEqual(test.Server, cloned.Server);
            CollectionAssert.AreEquivalent((System.Collections.ICollection)test.AdditionalConnectionSettings, (System.Collections.ICollection)cloned.AdditionalConnectionSettings);
            Assert.AreEqual(test.ConnectionString, cloned.ConnectionString);
            Assert.AreEqual(test.Container, cloned.Container);
            Assert.AreEqual(test.QueueName, cloned.QueueName);
        }

        [TestMethod]
        public void QueueName_Valid_Alphanumeric()
        {
            var test = new RedisConnectionInfo(new QueueConnection("MyQueue123", "localhost"));
            Assert.IsNotNull(test);
            Assert.AreEqual("MyQueue123", test.QueueName);
        }

        [TestMethod]
        public void QueueName_Valid_WithHyphen()
        {
            var test = new RedisConnectionInfo(new QueueConnection("my-queue", "localhost"));
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void QueueName_Valid_WithUnderscoreAndDot()
        {
            var test = new RedisConnectionInfo(new QueueConnection("my_queue.v2", "localhost"));
            Assert.IsNotNull(test);
        }

        [TestMethod]
        public void QueueName_Invalid_SqlInjection()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new RedisConnectionInfo(new QueueConnection("queue; DROP TABLE users;--", "localhost"));
                });
        }

        [TestMethod]
        public void QueueName_Invalid_SpecialChars()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new RedisConnectionInfo(new QueueConnection("queue@name!", "localhost"));
                });
        }

        [TestMethod]
        public void QueueName_Invalid_Spaces()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new RedisConnectionInfo(new QueueConnection("my queue", "localhost"));
                });
        }

        [TestMethod]
        public void QueueName_Invalid_CurlyBrace()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new RedisConnectionInfo(new QueueConnection("queue{name}", "localhost"));
                });
        }

        [TestMethod]
        public void QueueName_Empty_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new RedisConnectionInfo(new QueueConnection(string.Empty, "localhost"));
                });
        }

        [TestMethod]
        public void QueueName_ExceedsMaxLength_512()
        {
            var longName = new string('a', 513);
            Assert.ThrowsExactly<ArgumentException>(
                delegate
                {
                    var test = new RedisConnectionInfo(new QueueConnection(longName, "localhost"));
                });
        }

        [TestMethod]
        public void QueueName_AtMaxLength_512()
        {
            var maxName = new string('a', 512);
            var test = new RedisConnectionInfo(new QueueConnection(maxName, "localhost"));
            Assert.IsNotNull(test);
        }
    }
}
