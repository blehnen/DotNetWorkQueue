using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class HeartBeatWorkerNoOpTests
    {
        [TestMethod]
        public void Create_Default()
        {
            using (var test = Create())
            {
                test.Start();
                test.Stop();
            }
        }
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.IsFalse(test.IsDisposed);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.IsTrue(test.IsDisposed);
            }
        }

        private IHeartBeatWorker Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<HeartBeatWorkerNoOp>();
        }
    }
}
