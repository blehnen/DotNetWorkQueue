using System;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Transport.Memory
{
    [TestClass]
    public class ConfigurationExtensionsForIAdditionalMessageDataTests
    {
        [TestMethod]
        public void SetDelay_Test()
        {
            var time = DateTime.UtcNow;
            var data = new AdditionalMessageData();
            data.SetDelay(time.TimeOfDay);

            Assert.AreEqual(time.TimeOfDay, data.GetDelay());
        }

        [TestMethod]
        public void GetDelay_Test()
        {
            var data = new AdditionalMessageData();
            Assert.IsNull(data.GetDelay());

            var time = DateTime.UtcNow;
            data.SetDelay(time.TimeOfDay);
            Assert.AreEqual(time.TimeOfDay, data.GetDelay());

            time = DateTime.UtcNow.AddHours(1);
            data.SetDelay(time.TimeOfDay);
            Assert.AreEqual(time.TimeOfDay, data.GetDelay());
        }
    }
}