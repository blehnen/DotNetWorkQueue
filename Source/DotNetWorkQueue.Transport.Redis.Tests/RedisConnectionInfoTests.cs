using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests
{
    public class RedisConnectionInfoTests
    {
        [Fact]
        public void CreateNullInputTest()
        {
            Assert.Throws<NullReferenceException>(
                delegate
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var test = new RedisConnectionInfo(null);
                });
        }

        [Fact]
        public void CreateTest()
        {
            var test = new RedisConnectionInfo(new QueueConnection("test", "test"));
            Assert.NotNull(test);
        }

        [Fact]
        public void CloneTest()
        {
            var test = new RedisConnectionInfo(new QueueConnection("test", "test"));
            var cloned = test.Clone();
            Assert.NotNull(cloned);
            Assert.Equal(test.Server, cloned.Server);
            Assert.Equal(test.AdditionalConnectionSettings, cloned.AdditionalConnectionSettings);
            Assert.Equal(test.ConnectionString, cloned.ConnectionString);
            Assert.Equal(test.Container, cloned.Container);
            Assert.Equal(test.QueueName, cloned.QueueName);
        }
    }
}
