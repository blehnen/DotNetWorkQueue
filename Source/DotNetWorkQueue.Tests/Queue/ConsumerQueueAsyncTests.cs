using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;



using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class ConsumerQueueAsyncTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            var test = CreateQueue(1);
            Assert.IsFalse(test.IsDisposed);
        }

        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue(1);
            test.Dispose();
            Assert.IsTrue(test.IsDisposed);
        }


        [TestMethod]
        public void Disposed_Instance_Get_Configuration_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var test = CreateQueue(1);
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.Configuration.Worker.WorkerCount = value;
            });
        }

        [TestMethod]
        public void Disposed_Instance_Start_Exception()
        {
            var test = CreateQueue(1);
            test.Dispose();
            Task Func(IReceivedMessage<FakeMessage> message, IWorkerNotification worker) => null;
            Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.Start((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>)Func);
            });
        }

        [TestMethod]
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = CreateQueue(1))
            {
                Task Func(IReceivedMessage<FakeMessage> message, IWorkerNotification worker) => null;
                test.Start((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>)Func);
                Assert.ThrowsExactly<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>)Func);
                    });
            }
        }

        [TestMethod]
        public void Calling_Worker_Count_Greater_Than_One_Exception()
        {
            using (var test = CreateQueue(2))
            {
                Task Func(IReceivedMessage<FakeMessage> message, IWorkerNotification worker) => null;
                test.Start((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>)Func);
                Assert.ThrowsExactly<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>)Func);
                    });
            }
        }

        [TestMethod]
        public void Calling_Start_Null_Action_Exception()
        {
            using (var test = CreateQueue(1))
            {
                Assert.ThrowsExactly<ArgumentNullException>(
                    delegate
                    {
                        test.Start<FakeMessage>(null);
                    });
            }
        }

        private ConsumerQueueAsync CreateQueue(int workerCount)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var cancelWork = fixture.Create<IQueueCancelWork>();

            var workerConfiguration = fixture.Create<IWorkerConfiguration>();
            workerConfiguration.WorkerCount.Returns(workerCount);

            cancelWork.CancellationTokenSource.Returns(new CancellationTokenSource());
            cancelWork.StopTokenSource.Returns(new CancellationTokenSource());

            fixture.Inject(workerConfiguration);
            fixture.Inject(cancelWork);

            var stopWorker = fixture.Create<StopWorker>();
            fixture.Inject(stopWorker);

            return fixture.Create<ConsumerQueueAsync>();
        }
    }
}
