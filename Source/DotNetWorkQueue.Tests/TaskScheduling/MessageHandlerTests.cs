using System;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class MessageHandlerTests
    {
        [TestMethod]
        public async Task Handle_Null_Params_Fails()
        {
            var test = Create();
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => test.HandleAsync<FakeMessage>(Substitute.For<IWorkGroup>(),
                null, null, null, null));
        }

        [TestMethod]
        public void Handle_Message()
        {
            void Action(IReceivedMessage<FakeMessage> message, IWorkerNotification notification)
            {
            }

            var factory = Substitute.For<ITaskFactory>();
            factory.Scheduler.Returns(Substitute.For<ATaskScheduler>());
            factory.TryStartNew(null, null, null, out _).ReturnsForAnyArgs(TryStartNewResult.Added);

            var test = Create();
            test.HandleAsync(null, Substitute.For<IReceivedMessage<FakeMessage>>(),
                Substitute.For<IWorkerNotification>(),
                Action,
                factory);
        }

        private SchedulerMessageHandler Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<SchedulerMessageHandler>();
        }
    }
}
