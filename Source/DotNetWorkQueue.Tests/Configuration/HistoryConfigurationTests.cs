using System;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class HistoryConfigurationTests
    {
        [TestMethod]
        public void Defaults_Test()
        {
            var configuration = new HistoryConfiguration();
            Assert.IsFalse(configuration.Enabled);
            Assert.AreEqual(30, configuration.RetentionDays);
            Assert.AreEqual(4000, configuration.MaxExceptionLength);
            Assert.IsFalse(configuration.StoreBody);
            Assert.IsTrue(configuration.TrackEnqueue);
            Assert.IsTrue(configuration.TrackProcessing);
            Assert.IsTrue(configuration.TrackComplete);
            Assert.IsTrue(configuration.TrackError);
            Assert.IsTrue(configuration.TrackDelete);
            Assert.IsTrue(configuration.TrackExpire);
            Assert.AreEqual(TimeSpan.FromDays(1), configuration.MonitorTime);
        }

        [TestMethod]
        public void SetAndGet_Enabled()
        {
            var configuration = new HistoryConfiguration { Enabled = true };
            Assert.IsTrue(configuration.Enabled);
        }

        [TestMethod]
        public void SetAndGet_RetentionDays()
        {
            var configuration = new HistoryConfiguration { RetentionDays = 7 };
            Assert.AreEqual(7, configuration.RetentionDays);
        }

        [TestMethod]
        public void SetAndGet_MaxExceptionLength()
        {
            var configuration = new HistoryConfiguration { MaxExceptionLength = 2000 };
            Assert.AreEqual(2000, configuration.MaxExceptionLength);
        }

        [TestMethod]
        public void SetAndGet_StoreBody()
        {
            var configuration = new HistoryConfiguration { StoreBody = true };
            Assert.IsTrue(configuration.StoreBody);
        }

        [TestMethod]
        public void SetAndGet_TrackEnqueue()
        {
            var configuration = new HistoryConfiguration { TrackEnqueue = false };
            Assert.IsFalse(configuration.TrackEnqueue);
        }

        [TestMethod]
        public void SetAndGet_TrackProcessing()
        {
            var configuration = new HistoryConfiguration { TrackProcessing = false };
            Assert.IsFalse(configuration.TrackProcessing);
        }

        [TestMethod]
        public void SetAndGet_TrackComplete()
        {
            var configuration = new HistoryConfiguration { TrackComplete = false };
            Assert.IsFalse(configuration.TrackComplete);
        }

        [TestMethod]
        public void SetAndGet_TrackError()
        {
            var configuration = new HistoryConfiguration { TrackError = false };
            Assert.IsFalse(configuration.TrackError);
        }

        [TestMethod]
        public void SetAndGet_TrackDelete()
        {
            var configuration = new HistoryConfiguration { TrackDelete = false };
            Assert.IsFalse(configuration.TrackDelete);
        }

        [TestMethod]
        public void SetAndGet_TrackExpire()
        {
            var configuration = new HistoryConfiguration { TrackExpire = false };
            Assert.IsFalse(configuration.TrackExpire);
        }

        [TestMethod]
        public void SetAndGet_MonitorTime()
        {
            var configuration = new HistoryConfiguration { MonitorTime = TimeSpan.FromHours(6) };
            Assert.AreEqual(TimeSpan.FromHours(6), configuration.MonitorTime);
        }

        [TestMethod]
        public void ReadOnly_Test()
        {
            var configuration = new HistoryConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.Enabled = true; });
            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.RetentionDays = 7; });
            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.MaxExceptionLength = 100; });
            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.StoreBody = true; });
            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.TrackEnqueue = false; });
            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.TrackProcessing = false; });
            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.TrackComplete = false; });
            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.TrackError = false; });
            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.TrackDelete = false; });
            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.TrackExpire = false; });
            Assert.ThrowsExactly<InvalidOperationException>(delegate { configuration.MonitorTime = TimeSpan.FromHours(1); });
        }
    }
}
