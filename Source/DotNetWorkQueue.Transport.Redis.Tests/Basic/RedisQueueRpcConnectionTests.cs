using System;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisQueueRpcConnectionTests
    {
        [Fact]
        public void Create_Null_Or_Empty_Fails()
        {
            Assert.Throws<ArgumentException>(
           delegate
           {
               var test = new RedisQueueRpcConnection(string.Empty, string.Empty);
               Assert.Null(test);
           });

            Assert.Throws<ArgumentNullException>(
           delegate
           {
               var test = new RedisQueueRpcConnection(null, null);
               Assert.Null(test);
           });

        }
        [Theory, AutoData]
        public void Create_Default(string connection, string queue)
        {
            var test = new RedisQueueRpcConnection(connection, queue);
            test.GetConnection(ConnectionTypes.NotSpecified);
            test.GetConnection(ConnectionTypes.Receive);
            test.GetConnection(ConnectionTypes.Send);
        }
    }
}
