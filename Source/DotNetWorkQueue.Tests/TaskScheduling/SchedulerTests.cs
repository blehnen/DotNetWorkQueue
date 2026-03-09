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
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class SchedulerTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create(1))
            {
                Assert.IsFalse(test.IsDisposed);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create(1))
            {
                test.Dispose();
                Assert.IsTrue(test.IsDisposed);
            }
        }

        [TestMethod]
        public void Disposed_Configuration_Exception()
        {
            using (var test = Create(1))
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.Configuration.SetReadOnly();
            });
            }
        }

        [TestMethod]
        public void Disposed_Start_Exception()
        {
            using (var test = Create(1))
            {
                test.Dispose();

                void Action(IReceivedMessage<FakeMessage> message, IWorkerNotification worker)
                {
                }

                Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.Start((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);
            });
            }
        }

        [TestMethod]
        public void Null_Start_Exception()
        {
            using (var test = Create(1))
            {
                Assert.ThrowsExactly<ArgumentNullException>(
            delegate
            {
                test.Start<FakeMessage>(null);
            });
            }
        }

        [TestMethod]
        public void Disposed_Get_TaskFactory_Exception()
        {
            using (var test = Create(1))
            {
                test.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.TaskFactory.Scheduler.Configuration.MaximumThreads = 10;
            });
            }
        }

        [TestMethod]
        public void Call_Start_Only_Once_Exception()
        {
            using (var test = Create(1))
            {
                void Action(IReceivedMessage<FakeMessage> message, IWorkerNotification worker)
                {
                }

                test.Start((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);
                Assert.ThrowsExactly<DotNetWorkQueueException>(
            delegate
            {
                test.Start((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);
            });
            }
        }

        [TestMethod]
        public void GetSet_WorkGroup_No_Name_IsNull()
        {
            var group = Substitute.For<IWorkGroup>();
            using (var test = Create(1, group))
            {
                Assert.IsNull(test.WorkGroup);
            }
        }

        [TestMethod]
        public void Set_WorkGroup()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var group = Substitute.For<IWorkGroup>();
            group.Name.Returns(value);
            using (var test = Create(1, group))
            {
                Assert.AreEqual(group, test.WorkGroup);
            }
        }

        private Scheduler Create(int workerCount, IWorkGroup workGroup = null)
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

            return fixture2.Create<Scheduler>();
        }
    }
}
