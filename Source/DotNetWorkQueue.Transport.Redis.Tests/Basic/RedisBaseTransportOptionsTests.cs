using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisBaseTransportOptionsTests
    {
        [Fact]
        public void EnablePriority_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.True(o.EnablePriority);
        }

        [Fact]
        public void EnableStatus_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.True(o.EnableStatus);
        }

        [Fact]
        public void EnableHeartBeat_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.True(o.EnableHeartBeat);
        }

        [Fact]
        public void EnableDelayedProcessing_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.True(o.EnableDelayedProcessing);
        }

        [Fact]
        public void EnableStatusTable_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.True(o.EnableStatusTable);
        }

        [Fact]
        public void EnableRoute_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.True(o.EnableRoute);
        }

        [Fact]
        public void EnableMessageExpiration_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.True(o.EnableMessageExpiration);
        }
    }
}
