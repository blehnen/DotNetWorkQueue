using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    [TestClass]
    public class RedisBaseTransportOptionsTests
    {
        [TestMethod]
        public void EnablePriority_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.IsTrue(o.EnablePriority);
        }

        [TestMethod]
        public void EnableStatus_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.IsTrue(o.EnableStatus);
        }

        [TestMethod]
        public void EnableHeartBeat_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.IsTrue(o.EnableHeartBeat);
        }

        [TestMethod]
        public void EnableDelayedProcessing_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.IsTrue(o.EnableDelayedProcessing);
        }

        [TestMethod]
        public void EnableStatusTable_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.IsTrue(o.EnableStatusTable);
        }

        [TestMethod]
        public void EnableRoute_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.IsTrue(o.EnableRoute);
        }

        [TestMethod]
        public void EnableMessageExpiration_Test()
        {
            var o = new RedisBaseTransportOptions();
            Assert.IsTrue(o.EnableMessageExpiration);
        }
    }
}
