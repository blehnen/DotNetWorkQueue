using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class ConsumerMethodQueueTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            using (var test = CreateQueue())
            {
                Assert.IsFalse(test.IsDisposed);
            }
        }

        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.IsTrue(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = CreateQueue())
            {
                test.Dispose();
            }
        }

        [TestMethod]
        public void Disposed_Instance_Get_Configuration_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var test = CreateQueue();
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
            var test = CreateQueue();
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
                delegate
                {
                    test.Start();
                });
        }

        [TestMethod]
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = CreateQueue())
            {
                test.Start();
                Assert.ThrowsExactly<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start();
                    });
            }
        }

        private ConsumerMethodQueue CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(CreateConsumerQueue());
            return fixture.Create<ConsumerMethodQueue>();
        }

        private IConsumerQueue CreateConsumerQueue()
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
