using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Queue;



using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerHeartBeatNotificationNoOpTests
    {
        [Fact]
        public void Error_Is_Null()
        {
            var test = Create();
            Assert.Null(test.Error);
        }
        [Fact]
        public void ErrorCount_Zero()
        {
            var test = Create();
            Assert.Equal(0, test.ErrorCount);
        }
        [Theory, AutoData]
        public void SetError_NoOp(string value)
        {
            var test = Create();
            test.SetError(new AccessViolationException(value));
            Assert.Null(test.Error);
            Assert.Equal(0, test.ErrorCount);
        }

        private IWorkerHeartBeatNotification Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerHeartBeatNotificationNoOp>();
        }
    }
}
