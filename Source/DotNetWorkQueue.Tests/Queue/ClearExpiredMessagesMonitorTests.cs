using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class ClearExpiredMessagesMonitorTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            var test = Create();
            Assert.IsFalse(test.IsDisposed);
        }

        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.IsTrue(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var test = Create();
            test.Dispose();
            test.Dispose();
        }

        [TestMethod]
        public void Start_Stop()
        {
            var test = Create();
            test.Start();
            test.Stop();
            test.Dispose();
        }

        private IClearExpiredMessagesMonitor Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<ClearExpiredMessagesMonitor>();
        }
    }
}
