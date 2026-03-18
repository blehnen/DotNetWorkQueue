using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class ClearHistoryMonitorNoOpTests
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

        [TestMethod]
        public void Start_Stop_DoNotThrow()
        {
            var test = Create();
            test.Start();
            test.Stop();
            test.Dispose();
        }

        [TestMethod]
        public void LastRunUtc_Is_Null()
        {
            var test = Create();
            Assert.IsNull(test.LastRunUtc);
        }

        private ClearHistoryMonitorNoOp Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<ClearHistoryMonitorNoOp>();
        }
    }
}
