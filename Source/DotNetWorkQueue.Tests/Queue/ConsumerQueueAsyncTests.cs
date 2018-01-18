using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;



using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class ConsumerQueueAsyncTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            var test = CreateQueue(1);
            Assert.False(test.IsDisposed);
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue(1);
            test.Dispose();
            Assert.True(test.IsDisposed);
        }


        [Theory, AutoData]
        public void Disposed_Instance_Get_Configuration_Exception(int value)
        {
            var test = CreateQueue(1);
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Configuration.Worker.WorkerCount = value;
            });
        }

        [Fact]
        public void Disposed_Instance_Start_Exception()
        {
            var test = CreateQueue(1);
            test.Dispose();
            Task Func(IReceivedMessage<FakeMessage> message, IWorkerNotification worker) => null;
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Start((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>) Func);
            });
        }

        [Fact]
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = CreateQueue(1))
            {
                Task Func(IReceivedMessage<FakeMessage> message, IWorkerNotification worker) => null;
                test.Start((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>) Func);
                Assert.Throws<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>) Func);
                    });
            }
        }

        [Fact]
        public void Calling_Worker_Count_Greater_Than_One_Exception()
        {
            using (var test = CreateQueue(2))
            {
                Task Func(IReceivedMessage<FakeMessage> message, IWorkerNotification worker) => null;
                test.Start((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>) Func);
                Assert.Throws<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>) Func);
                    });
            }
        }

        [Fact]
        public void Calling_Start_Null_Action_Exception()
        {
            using (var test = CreateQueue(1))
            {
                Assert.Throws<ArgumentNullException>(
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
