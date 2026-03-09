using System;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Time
{
    [TestClass]
    public class BaseTimeConfigurationTests
    {
        [TestMethod]
        public void Create()
        {
            var test = new BaseTimeConfiguration();
            Assert.AreEqual(TimeSpan.FromSeconds(900), test.RefreshTime);
            test.RefreshTime = TimeSpan.FromSeconds(100);
            Assert.AreEqual(TimeSpan.FromSeconds(100), test.RefreshTime);
        }
    }
}
