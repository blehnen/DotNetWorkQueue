using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerHeartBeatNotificationTests
    {
        [Fact]
        public void ErrorCount_Zero()
        {
            var test = Create();
            Assert.Equal(0, test.ErrorCount);
        }

        [Fact]
        public void Set_Error_SetsException()
        {
            var test = Create();
            var error = new Exception();
            test.SetError(error);
            Assert.NotNull(test.Error);
            Assert.Equal(1, test.ErrorCount);
            Assert.Equal(error, test.Error);
        }

        [Fact]
        public void Status_SetGet()
        {
            var test = Create();
            var status = Substitute.For<IHeartBeatStatus>();
            test.Status = status;
            Assert.Equal(status, test.Status);
        }

        private IWorkerHeartBeatNotification Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerHeartBeatNotification>();
        }
    }
}
