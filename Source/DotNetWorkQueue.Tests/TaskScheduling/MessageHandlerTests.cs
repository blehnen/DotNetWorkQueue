using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class MessageHandlerTests
    {
        [Fact]
        public async void Handle_Null_Params_Fails()
        {
            var test = Create();
            await Assert.ThrowsAsync<ArgumentNullException>(() => test.HandleAsync<FakeMessage>(Substitute.For<IWorkGroup>(),
                null, null, null, null));
        }

        [Fact]
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
