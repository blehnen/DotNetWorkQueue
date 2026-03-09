using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WorkerHeartBeatNotificationTests
    {
        [TestMethod]
        public void ErrorCount_Zero()
        {
            var test = Create();
            Assert.AreEqual(0, test.ErrorCount);
        }

        [TestMethod]
        public void Set_Error_SetsException()
        {
            var test = Create();
            var error = new Exception();
            test.SetError(error);
            Assert.IsNotNull(test.Error);
            Assert.AreEqual(1, test.ErrorCount);
            Assert.AreEqual(error, test.Error);
        }

        [TestMethod]
        public void Status_SetGet()
        {
            var test = Create();
            var status = Substitute.For<IHeartBeatStatus>();
            test.Status = status;
            Assert.AreEqual(status, test.Status);
        }

        private IWorkerHeartBeatNotification Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerHeartBeatNotification>();
        }
    }
}
