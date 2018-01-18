using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.TaskScheduling;
using DotNetWorkQueue.Tests.IoC;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class SchedulerMethodTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create(1))
            {
                Assert.False(test.IsDisposed);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create(1))
            {
                test.Dispose();
                Assert.True(test.IsDisposed);
            }
        }

        [Fact]
        public void Disposed_Configuration_Exception()
        {
            using (var test = Create(1))
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Configuration.SetReadOnly();
            });
            }
        }

        [Fact]
        public void Disposed_Start_Exception()
        {
            using (var test = Create(1))
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Start();
            });
            }
        }

        [Fact]
        public void Call_Start_Only_Once_Exception()
        {
            using (var test = Create(1))
            {
                test.Start();
                Assert.Throws<DotNetWorkQueueException>(
            delegate
            {
                test.Start();
            });
            }
        }

        private SchedulerMethod Create(int workerCount, IWorkGroup workGroup = null)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var cancelWork = fixture.Create<IQueueCancelWork>();
            var workerConfiguration = fixture.Create<IWorkerConfiguration>();
            workerConfiguration.WorkerCount.Returns(workerCount);

            fixture.Inject(cancelWork);
            fixture.Inject(workerConfiguration);


            cancelWork.CancellationTokenSource.Returns(new CancellationTokenSource());
            cancelWork.StopTokenSource.Returns(new CancellationTokenSource());

            var stopWorker = fixture.Create<StopWorker>();
            fixture.Inject(stopWorker);

            IConsumerQueueAsync queue = fixture.Create<ConsumerQueueAsync>();
            var fixture2 = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture2.Inject(queue);

            var factoryFactory = fixture2.Create<ITaskFactoryFactory>();
            fixture2.Inject(factoryFactory);

            IConsumerQueueScheduler scheduler = fixture2.Create<Scheduler>();
            fixture2.Inject(scheduler);

            ATaskScheduler schedule = new CreateContainerTest.TaskSchedulerNoOp();
            schedule.Start();
            fixture2.Inject(schedule);
            var taskFactory = fixture2.Create<ITaskFactory>();
            taskFactory.Scheduler.Returns(schedule);
            fixture2.Inject(taskFactory);

            factoryFactory.Create().Returns(taskFactory);

            if (workGroup != null)
                fixture2.Inject(workGroup);

            var handler = fixture2.Create<SchedulerMessageHandler>();
            fixture2.Inject(handler);

            return fixture2.Create<SchedulerMethod>();
        }
    }
}
