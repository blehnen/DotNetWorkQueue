using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
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
    public class ConsumerQueueTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = CreateQueue())
            {
                Assert.False(test.IsDisposed);
            }
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.True(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = CreateQueue())
            {
                test.Dispose();
            }
        }

        [Theory, AutoData]
        public void Disposed_Instance_Get_Configuration_Exception(int value)
        {
            var test = CreateQueue();
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
            var test = CreateQueue();
            test.Dispose();

            void Action(IReceivedMessage<FakeMessage> message, IWorkerNotification worker)
            {
            }

            Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.Start((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);
                });
        }

        [Fact]
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = CreateQueue())
            {
                void Action(IReceivedMessage<FakeMessage> message, IWorkerNotification worker)
                {
                }

                test.Start((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);
                Assert.Throws<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);
                    });
            }
        }

        [Fact]
        public void Calling_Start_Null_Action_Exception()
        {
            using (var test = CreateQueue())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.Start<FakeMessage>(null);
                    });
            }
        }

        private ConsumerQueue CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var cancelWork = fixture.Create<IQueueCancelWork>();
            fixture.Inject(cancelWork);
            cancelWork.CancellationTokenSource.Returns(new CancellationTokenSource());
            cancelWork.StopTokenSource.Returns(new CancellationTokenSource());

            var stopWorker = fixture.Create<StopWorker>();
            fixture.Inject(stopWorker);
            return fixture.Create<ConsumerQueue>();
        }
    }
}
