using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WorkerHeartBeatNotificationNoOpTests
    {
        [TestMethod]
        public void Error_Is_Null()
        {
            var test = Create();
            Assert.IsNull(test.Error);
        }
        [TestMethod]
        public void ErrorCount_Zero()
        {
            var test = Create();
            Assert.AreEqual(0, test.ErrorCount);
        }
        [TestMethod]
        public void SetError_NoOp()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var test = Create();
            test.SetError(new AccessViolationException(value));
            Assert.IsNull(test.Error);
            Assert.AreEqual(0, test.ErrorCount);
        }

        private IWorkerHeartBeatNotification Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerHeartBeatNotificationNoOp>();
        }
    }
}
