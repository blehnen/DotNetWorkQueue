using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    }
}
