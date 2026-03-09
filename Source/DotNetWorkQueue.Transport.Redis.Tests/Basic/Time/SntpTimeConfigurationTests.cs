using System;
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Time
{
    [TestClass]
    public class SntpTimeConfigurationTests
    {
        [TestMethod]
        public void Create()
        {
            var test = new SntpTimeConfiguration();
            Assert.AreEqual(TimeSpan.FromSeconds(900), test.RefreshTime);
            Assert.AreEqual(123, test.Port);
            Assert.AreEqual("pool.ntp.org", test.Server);

            test.RefreshTime = TimeSpan.FromSeconds(100);
            Assert.AreEqual(TimeSpan.FromSeconds(100), test.RefreshTime);

            test.Port = 567;
            Assert.AreEqual(567, test.Port);

            test.Server = "test";
            Assert.AreEqual("test", test.Server);
        }
    }
}
