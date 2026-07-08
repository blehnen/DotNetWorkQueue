using System;
using System.Collections.Generic;
using System.Threading;
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

        [TestMethod]
        public void Handle_When_Worker_Stopping_Throws_OperationCanceled()
        {
            void Action(IReceivedMessage<FakeMessage> message, IWorkerNotification notification)
            {
            }

            //worker is stopping and the transport supports rollback -> the item must not be
            //queued. The handler must surface OperationCanceledException (the clean rollback
            //path) rather than returning a null Task, which would NRE when the consumer awaits it.
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var cancel = Substitute.For<ICancelWork>();
            cancel.Tokens.Returns(new List<CancellationToken> { cts.Token });

            var notifications = Substitute.For<IWorkerNotification>();
            notifications.TransportSupportsRollback.Returns(true);
            notifications.WorkerStopping.Returns(cancel);

            var test = Create();

            Assert.ThrowsExactly<OperationCanceledException>(() =>
                test.HandleAsync(Substitute.For<IWorkGroup>(), Substitute.For<IReceivedMessage<FakeMessage>>(),
                    notifications, Action, Substitute.For<ITaskFactory>()));
        }

        private SchedulerMessageHandler Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<SchedulerMessageHandler>();
        }
    }
}
